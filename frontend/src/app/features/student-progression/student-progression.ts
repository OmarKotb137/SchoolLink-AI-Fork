import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { AcademicYear, AcademicYearService } from '../../core/services/academic-year.service';
import { ClassEntity, ClassService } from '../../core/services/class.service';
import { GradeLevel, GradeLevelService } from '../../core/services/grade-level.service';
import {
  AcademicStatus,
  ProgressionTermScope,
  StudentProgressionCandidate,
  StudentProgressionRequest,
  StudentProgressionResult,
  StudentProgressionService
} from '../../core/services/student-progression.service';
import { Sidebar } from '../../layouts/sidebar/sidebar';
type ProgressionAction = 1 | 2 | 3;
type AccountFilter = 'all' | 'with-account' | 'without-account';
type StatusFilter = 'all' | 'passed' | 'failed' | 'unpublished' | 'no-grades';
type SortMode = 'name' | 'high' | 'low';

@Component({
  selector: 'app-student-progression',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './student-progression.html',
  styleUrl: './student-progression.css'
})
export class StudentProgression implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  private academicYearService = inject(AcademicYearService);
  private gradeLevelService = inject(GradeLevelService);
  private classService = inject(ClassService);
  private studentProgressionService = inject(StudentProgressionService);

  academicYears = signal<AcademicYear[]>([]);
  gradeLevels = signal<GradeLevel[]>([]);
  classes = signal<ClassEntity[]>([]);
  candidates = signal<StudentProgressionCandidate[]>([]);
  selectedEnrollmentIds = signal<number[]>([]);

  selectedSourceAcademicYearId = signal<number | null>(null);
  selectedSourceGradeLevelId = signal<number | null>(null);
  selectedTargetAcademicYearId = signal<number | null>(null);
  selectedTargetClassId = signal<number | null>(null);

  action = signal<ProgressionAction>(1);
  effectiveDate = signal(this.getTodayDate());
  note = signal('');
  threshold = signal(50);
  searchQuery = signal('');
  accountFilter = signal<AccountFilter>('all');
  statusFilter = signal<StatusFilter>('all');
  sortMode = signal<SortMode>('name');
  termScope = signal<ProgressionTermScope>(3); // Both semesters = end-of-year basis

  /** Enrollment IDs whose subject-breakdown row is expanded in the table. */
  expandedSubjectIds = signal<Set<number>>(new Set());

  currentPage = signal(1);
  itemsPerPage = signal(10);

  /** Snapshot of academic-status counts (Passed/Failed/Unpublished/NoGrades) for the loaded list. */
  statusCounts = computed(() => {
    const counts = { passed: 0, failed: 0, unpublished: 0, noGrades: 0 };
    for (const c of this.candidates()) {
      if (c.academicStatus === 2) counts.passed++;
      else if (c.academicStatus === 3) counts.failed++;
      else if (c.academicStatus === 1) counts.unpublished++;
      else counts.noGrades++;
    }
    return counts;
  });

  isBootstrapping = signal(false);
  isLoadingCandidates = signal(false);
  isExecuting = signal(false);
  errorMessage = signal('');
  successMessage = signal('');
  lastResult = signal<StudentProgressionResult | null>(null);

  selectedSourceAcademicYear = computed(() =>
    this.academicYears().find(year => year.id === this.selectedSourceAcademicYearId()) ?? null
  );

  selectedSourceGradeLevel = computed(() =>
    this.gradeLevels().find(grade => grade.id === this.selectedSourceGradeLevelId()) ?? null
  );

  nextGradeLevel = computed(() => {
    const sourceGrade = this.selectedSourceGradeLevel();
    if (!sourceGrade) return null;

    return this.gradeLevels().find(grade => grade.levelOrder === sourceGrade.levelOrder + 1) ?? null;
  });

  futureAcademicYears = computed(() => {
    const sourceYear = this.selectedSourceAcademicYear();
    if (!sourceYear) return [];

    const sourceStart = new Date(sourceYear.startDate).getTime();
    return this.academicYears()
      .filter(year => year.id !== sourceYear.id && new Date(year.startDate).getTime() > sourceStart)
      .sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime());
  });

  targetGradeLevelId = computed(() => {
    if (this.action() === 1) return this.nextGradeLevel()?.id ?? null;
    if (this.action() === 2) return this.selectedSourceGradeLevelId();
    return null;
  });

  availableTargetClasses = computed(() => {
    const targetYearId = this.selectedTargetAcademicYearId();
    const targetGradeId = this.targetGradeLevelId();

    if (!targetYearId || !targetGradeId) return [];

    return this.classes()
      .filter(cls => cls.academicYearId === targetYearId && cls.gradeLevelId === targetGradeId)
      .sort((a, b) => a.name.localeCompare(b.name, 'ar'));
  });

  filteredCandidates = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const accountFilter = this.accountFilter();
    const statusFilter = this.statusFilter();
    const sortMode = this.sortMode();

    const filtered = this.candidates().filter(candidate => {
      const matchesQuery =
        !query ||
        candidate.studentName.toLowerCase().includes(query) ||
        candidate.currentClassName.toLowerCase().includes(query);

      const matchesAccount =
        accountFilter === 'all' ||
        (accountFilter === 'with-account' && candidate.hasStudentAccount) ||
        (accountFilter === 'without-account' && !candidate.hasStudentAccount);

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'passed' && candidate.academicStatus === 2) ||
        (statusFilter === 'failed' && candidate.academicStatus === 3) ||
        (statusFilter === 'unpublished' && candidate.academicStatus === 1) ||
        (statusFilter === 'no-grades' && candidate.academicStatus === 0);

      return matchesQuery && matchesAccount && matchesStatus;
    });

    return filtered.sort((a, b) => {
      if (sortMode === 'name') {
        return a.studentName.localeCompare(b.studentName, 'ar');
      }

      const aTotal = a.finalTotal ?? Number.NEGATIVE_INFINITY;
      const bTotal = b.finalTotal ?? Number.NEGATIVE_INFINITY;
      return sortMode === 'high' ? bTotal - aTotal : aTotal - bTotal;
    });
  });

  paginatedCandidates = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredCandidates().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredCandidates().length / this.itemsPerPage()));
  });

  rangeStart = computed(() => {
    if (this.filteredCandidates().length === 0) return 0;
    return (this.currentPage() - 1) * this.itemsPerPage() + 1;
  });

  rangeEnd = computed(() => {
    return Math.min(this.currentPage() * this.itemsPerPage(), this.filteredCandidates().length);
  });

  pages = computed<(number | string)[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const result: (number | string)[] = [];
    result.push(1);
    if (current > 3) result.push('...');
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      result.push(i);
    }
    if (current < total - 2) result.push('...');
    if (total > 1) result.push(total);
    return result;
  });

  trackByPageIndex = (_: number, item: number | string) =>
    typeof item === 'string' ? `dot-${_}` : `page-${item}`;

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  goToPage(page: number) {
    this.currentPage.set(page);
  }

  selectedCandidates = computed(() => {
    const selectedIds = new Set(this.selectedEnrollmentIds());
    return this.candidates().filter(candidate => selectedIds.has(candidate.enrollmentId));
  });

  selectedVisibleCount = computed(() => {
    const selectedIds = new Set(this.selectedEnrollmentIds());
    return this.filteredCandidates().filter(candidate => selectedIds.has(candidate.enrollmentId)).length;
  });

  allVisibleSelected = computed(() => {
    const visible = this.filteredCandidates();
    if (!visible.length) return false;

    const selectedIds = new Set(this.selectedEnrollmentIds());
    return visible.every(candidate => selectedIds.has(candidate.enrollmentId));
  });

  selectedCount = computed(() => this.selectedEnrollmentIds().length);

  selectedStudentsWithAccountsCount = computed(() =>
    this.selectedCandidates().filter(candidate => candidate.hasStudentAccount).length
  );

  hasTargetConfigurationIssue = computed(() => {
    if (this.action() === 3) return '';
    if (!this.selectedSourceGradeLevel() || !this.selectedSourceAcademicYear()) return '';

    if (this.action() === 1 && !this.nextGradeLevel()) {
      return 'هذا الصف هو الأخير حالياً، لذلك لا يمكن تنفيذ الترقية ويجب استخدام التخرج.';
    }

    if (this.futureAcademicYears().length === 0) {
      return 'لا توجد سنة دراسية لاحقة متاحة حالياً لتنفيذ الترقية أو الإبقاء.';
    }

    if (this.selectedTargetAcademicYearId() && this.availableTargetClasses().length === 0) {
      return 'لا توجد فصول متاحة في الوجهة المختارة. أنشئ الفصول أولاً ثم أعد المحاولة.';
    }

    return '';
  });

  canExecute = computed(() => {
    if (this.selectedCount() === 0 || !this.effectiveDate()) return false;

    if (this.action() === 3) {
      return !this.nextGradeLevel();
    }

    if (this.hasTargetConfigurationIssue()) return false;

    return !!this.selectedTargetAcademicYearId() && !!this.selectedTargetClassId();
  });

  ngOnInit(): void {
    this.loadPageData();
  }

  loadPageData() {
    this.isBootstrapping.set(true);
    this.clearMessages();

    forkJoin({
      years: this.academicYearService.getAll(),
      grades: this.gradeLevelService.getAll(),
      classes: this.classService.getAll()
    })
      .pipe(finalize(() => this.isBootstrapping.set(false)))
      .subscribe({
        next: ({ years, grades, classes }) => {
          const unwrappedYears = this.unwrapData<AcademicYear[]>(years) ?? [];
          const unwrappedGrades = (this.unwrapData<GradeLevel[]>(grades) ?? [])
            .slice()
            .sort((a, b) => a.levelOrder - b.levelOrder);
          const unwrappedClasses = this.unwrapData<ClassEntity[]>(classes) ?? [];

          this.academicYears.set(unwrappedYears);
          this.gradeLevels.set(unwrappedGrades);
          this.classes.set(unwrappedClasses);

          const currentYear = unwrappedYears.find(year => year.isCurrent) ?? unwrappedYears[0] ?? null;
          if (currentYear) {
            this.selectedSourceAcademicYearId.set(currentYear.id);
          }
        },
        error: err => this.showError(this.extractErrorMessage(err, 'تعذر تحميل بيانات الصفحة.'))
      });
  }

  loadCandidates() {
    const sourceYearId = this.selectedSourceAcademicYearId();
    const sourceGradeId = this.selectedSourceGradeLevelId();

    if (!sourceYearId || !sourceGradeId) {
      this.showError('اختر السنة الدراسية المصدر والصف الدراسي المصدر أولاً.');
      return;
    }

    this.isLoadingCandidates.set(true);
    this.clearMessages();
    this.lastResult.set(null);
    this.selectedEnrollmentIds.set([]);

    this.studentProgressionService
        .getCandidates(sourceGradeId, sourceYearId, this.termScope(), this.threshold())
        .pipe(finalize(() => this.isLoadingCandidates.set(false)))
        .subscribe({
          next: res => {
            const candidates = this.unwrapData<StudentProgressionCandidate[]>(res) ?? [];
            this.candidates.set(candidates);
            this.selectedTargetClassId.set(null);
            this.currentPage.set(1);

            if (!candidates.length) {
              this.successMessage.set('لا يوجد طلاب نشطون في هذا الصف خلال السنة الدراسية المحددة.');
            }
          },
          error: err => {
            this.candidates.set([]);
            this.showError(this.extractErrorMessage(err, 'تعذر تحميل الطلاب المرشحين.'));
          }
        });
  }

  onSourceSelectionChanged() {
    this.candidates.set([]);
    this.selectedEnrollmentIds.set([]);
    this.selectedTargetAcademicYearId.set(null);
    this.selectedTargetClassId.set(null);
    this.lastResult.set(null);
    this.clearMessages();
  }

  onTermScopeChange(value: ProgressionTermScope) {
    this.termScope.set(value);
    // Threshold is a server-side decision now; reload with the new scope.
    if (this.candidates().length || this.selectedSourceGradeLevelId()) {
      this.loadCandidates();
    }
  }

  /**
   * Selects all visible candidates whose academic status matches the given value.
   * Used by the "passed / failed / unpublished / no-grades" quick-select buttons.
   */
  selectVisibleByStatus(status: AcademicStatus) {
    const matched = this.filteredCandidates()
      .filter(c => c.academicStatus === status)
      .map(c => c.enrollmentId);

    const visibleIds = new Set(this.filteredCandidates().map(c => c.enrollmentId));
    const preservedHidden = this.selectedEnrollmentIds().filter(id => !visibleIds.has(id));
    this.selectedEnrollmentIds.set([...preservedHidden, ...matched]);
  }

  /** Quick auto-decide helper: leaves "no-grades/unpublished" unselected. */
  autoDecideSelection() {
    const ids = this.filteredCandidates()
      .filter(c => c.academicStatus === 2 || c.academicStatus === 3) // passed or failed
      .map(c => c.enrollmentId);

    const visibleIds = new Set(this.filteredCandidates().map(c => c.enrollmentId));
    const preservedHidden = this.selectedEnrollmentIds().filter(id => !visibleIds.has(id));
    this.selectedEnrollmentIds.set([...preservedHidden, ...ids]);
  }

  getStatusLabel(status: AcademicStatus): string {
    switch (status) {
      case 2: return 'ناجح';
      case 3: return 'راسب';
      case 1: return 'غير منشورة';
      default: return 'بدون درجات';
    }
  }

  getStatusBadgeClass(status: AcademicStatus): string {
    switch (status) {
      case 2: return 'bg-emerald-50 text-emerald-800 border-emerald-200';
      case 3: return 'bg-red-50 text-red-800 border-red-200';
      case 1: return 'bg-amber-50 text-amber-800 border-amber-200';
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  }

  getTermLabel(term: number | null | undefined): string {
    return term === 1 ? 'الترم الأول' : term === 2 ? 'الترم الثاني' : '—';
  }

  getScopeLabel(scope: ProgressionTermScope): string {
    return scope === 1 ? 'الترم الأول' : scope === 2 ? 'الترم الثاني' : 'الترمين';
  }

  toggleSubjectDetails(enrollmentId: number) {
    const next = new Set(this.expandedSubjectIds());
    if (next.has(enrollmentId)) {
      next.delete(enrollmentId);
    } else {
      next.add(enrollmentId);
    }
    this.expandedSubjectIds.set(next);
  }

  isSubjectDetailsOpen(enrollmentId: number): boolean {
    return this.expandedSubjectIds().has(enrollmentId);
  }

  onActionChanged(action: ProgressionAction) {
    this.action.set(action);
    this.selectedTargetClassId.set(null);

    if (action === 3) {
      this.selectedTargetAcademicYearId.set(null);
    } else if (!this.futureAcademicYears().some(year => year.id === this.selectedTargetAcademicYearId())) {
      this.selectedTargetAcademicYearId.set(this.futureAcademicYears()[0]?.id ?? null);
    }
  }

  onTargetAcademicYearChanged(value: number | null) {
    this.selectedTargetAcademicYearId.set(value);
    this.selectedTargetClassId.set(null);
  }

  toggleCandidateSelection(enrollmentId: number, checked: boolean) {
    const selected = new Set(this.selectedEnrollmentIds());

    if (checked) {
      selected.add(enrollmentId);
    } else {
      selected.delete(enrollmentId);
    }

    this.selectedEnrollmentIds.set(Array.from(selected));
  }

  toggleAllVisibleCandidates(checked: boolean) {
    const visibleIds = this.filteredCandidates().map(candidate => candidate.enrollmentId);
    const selected = new Set(this.selectedEnrollmentIds());

    for (const id of visibleIds) {
      if (checked) {
        selected.add(id);
      } else {
        selected.delete(id);
      }
    }

    this.selectedEnrollmentIds.set(Array.from(selected));
  }

  clearSelection() {
    this.selectedEnrollmentIds.set([]);
  }

  execute() {
    if (!this.canExecute()) {
      this.showError('أكمل البيانات المطلوبة وحدد الطلاب قبل التنفيذ.');
      return;
    }

    const request: StudentProgressionRequest = {
      enrollmentIds: this.selectedEnrollmentIds(),
      action: this.action(),
      effectiveDate: this.effectiveDate(),
      note: this.note().trim() || undefined,
      targetAcademicYearId: this.action() === 3 ? null : this.selectedTargetAcademicYearId(),
      targetClassId: this.action() === 3 ? null : this.selectedTargetClassId()
    };

    this.isExecuting.set(true);
    this.clearMessages();

    this.studentProgressionService.execute(request)
      .pipe(finalize(() => this.isExecuting.set(false)))
      .subscribe({
        next: res => {
          const result = this.unwrapData<StudentProgressionResult>(res);
          if (!result) {
            this.showError('تم استلام استجابة غير متوقعة من الخادم.');
            return;
          }

          this.lastResult.set(result);
          this.successMessage.set(res?.message || this.buildExecutionMessage(result));

          if (result.failureCount > 0) {
            const failedIds = new Set(result.failures.map(item => item.enrollmentId));
            this.candidates.set(this.candidates().filter(candidate => failedIds.has(candidate.enrollmentId)));
            this.selectedEnrollmentIds.set(Array.from(failedIds));
          } else {
            this.candidates.set([]);
            this.selectedEnrollmentIds.set([]);
          }
        },
        error: err => this.showError(this.extractErrorMessage(err, 'تعذر تنفيذ العملية المطلوبة.'))
      });
  }

  isCandidateSelected(enrollmentId: number): boolean {
    return this.selectedEnrollmentIds().includes(enrollmentId);
  }

  getActionLabel(action: ProgressionAction): string {
    if (action === 1) return 'ترقية';
    if (action === 2) return 'إبقاء';
    return 'تخرج';
  }

  trackByEnrollmentId(_: number, candidate: StudentProgressionCandidate): number {
    return candidate.enrollmentId;
  }

  private unwrapData<T>(response: any): T | null {
    if (response === null || response === undefined) return null;
    return (response.data ?? response) as T;
  }

  private extractErrorMessage(err: any, fallback: string): string {
    return err?.error?.message || err?.message || fallback;
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    this.successMessage.set('');
  }

  private clearMessages() {
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  private buildExecutionMessage(result: StudentProgressionResult): string {
    if (result.failureCount === 0) {
      return `تم تنفيذ العملية بنجاح على ${result.successCount} طالب.`;
    }

    return `تمت معالجة ${result.successCount} طالب، وتعذر تنفيذ العملية على ${result.failureCount} طالب.`;
  }

  private getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }
}
