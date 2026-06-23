import { Component, signal, computed, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import {
  AssignmentService,
  AssignmentItem, AssignmentDetail, AssignmentStats, AssignmentFilter,
  AssignmentDraftQuestion, CreateAssignmentQuestionPayload,
  AssignmentSubmissionItem, AssignmentSubmissionDetail
} from '../../core/services/assignment.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-assignment-management',
  imports: [Sidebar, FormsModule, NgClass],
  templateUrl: './assignment-management.html',
  styleUrl: './assignment-management.css'
})
export class AssignmentManagement implements OnInit, OnDestroy {
  private api = inject(AssignmentService);
  private academicYearSvc = inject(AcademicYearService);
  private destroy$ = new Subject<void>();

  private currentAcademicYearId = signal<number | undefined>(undefined);

  sidebarOpen = signal(false);
  activeTab = signal<string>('all');

  searchTerm = signal('');
  subjectFilter = signal<number | undefined>(undefined);
  sortBy = signal('newest');
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  private searchSubject = new Subject<string>();

  showAddModal      = signal(false);
  showViewModal     = signal(false);
  showDeleteConfirm   = signal(false);
  showPublishConfirm  = signal(false);
  showResultsModal    = signal(false);
  showGradingModal    = signal(false);

  viewingAssignment     = signal<AssignmentDetail | null>(null);
  editingAssignment     = signal<AssignmentItem | null>(null);
  deletingAssignment    = signal<AssignmentItem | null>(null);
  publishingAssignment  = signal<AssignmentItem | null>(null);
  resultsAssignment     = signal<number | null>(null);
  resultsSubmissions    = signal<AssignmentSubmissionItem[] | null>(null);
  gradingSubmission     = signal<AssignmentSubmissionDetail | null>(null);

  formError  = signal('');
  loadError  = signal('');
  loading    = signal(false);

  newTitle      = signal('');
  newSubjectId  = signal<number | null>(null);
  newClassId    = signal<number | null>(null);
  newDeadline   = signal('');

  draftQuestions = signal<AssignmentDraftQuestion[]>([]);
  private _draftCounter = 0;

  assignments = signal<AssignmentItem[]>([]);
  stats       = signal<AssignmentStats>({ total: 0, active: 0, avgDelivery: 0, overdue: 0 });
  subjects    = signal<{ id: number; name: string }[]>([]);
  classes     = signal<{ id: number; name: string; gradeLevelId: number }[]>([]);

  readonly assignmentTabs = [
    { key: 'all',     label: 'الكل'      },
    { key: 'open',    label: 'نشطة'      },
    { key: 'draft',   label: 'مسودة'     },
    { key: 'closed',  label: 'مغلقة'     },
  ];

  tabCounts = computed<Record<string, number>>(() => {
    const all = this.assignments();
    return {
      all: all.length,
      open: all.filter(a => a.status === 'open').length,
      draft: all.filter(a => a.status === 'draft').length,
      closed: all.filter(a => a.status === 'closed').length,
    };
  });

  @HostListener('document:keydown.escape')
  handleEscape() {
    if (this.showAddModal() || this.showViewModal() || this.showDeleteConfirm() ||
        this.showPublishConfirm() || this.showResultsModal() || this.showGradingModal()) {
      this.closeModals();
    }
  }

  readonly sortOptions = [
    { value: 'newest',     label: 'الأحدث'              },
    { value: 'oldest',     label: 'الأقدم'              },
    { value: 'name-asc',   label: 'أ - ي'               },
    { value: 'name-desc',  label: 'ي - أ'               },
    { value: 'date-asc',   label: 'التسليم (تصاعدي)'    },
    { value: 'date-desc',  label: 'التسليم (تنازلي)'    },
  ];

  ngOnInit() {
    this.academicYearSvc.getCurrent().subscribe({
      next: r => {
        const year = r?.data ?? r;
        if (year?.id) this.currentAcademicYearId.set(year.id);
      },
      error: () => {}
    });

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.page.set(1);
      this.loadAssignments();
    });

    this.loadAll();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchInput(value: string) {
    this.searchTerm.set(value);
    this.searchSubject.next(value);
  }

  onSubjectChange(subjectId: number | string) {
    this.subjectFilter.set(subjectId ? Number(subjectId) : undefined);
    this.page.set(1);
    this.loadAssignments();
  }

  onSortChange(sort: string) {
    this.sortBy.set(sort);
    this.page.set(1);
    this.loadAssignments();
  }

  setActiveTab(tab: string) {
    this.activeTab.set(tab);
    this.page.set(1);
    this.loadAssignments();
    this.scrollToTop();
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadAssignments();
    this.scrollToTop();
  }

  private scrollToTop() {
    const el = document.querySelector('.content-area');
    if (el) el.scrollTop = 0;
  }

  pageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.page();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: number[] = [];
    if (current > 2) pages.push(1);
    if (current > 3) pages.push(-1);
    if (current > 1) pages.push(current - 1);
    pages.push(current);
    if (current < total) pages.push(current + 1);
    if (current < total - 2) pages.push(-2);
    if (current < total - 1) pages.push(total);
    return pages;
  }

  setPageSize(size: number) {
    this.pageSize.set(size);
    this.page.set(1);
    this.loadAssignments();
  }

  private buildFilter(): AssignmentFilter {
    return {
      search: this.searchTerm() || undefined,
      subjectId: this.subjectFilter(),
      status: this.activeTab() === 'all' ? undefined : this.activeTab(),
      sortBy: this.sortBy(),
      page: this.page(),
      pageSize: this.pageSize(),
      academicYearId: this.currentAcademicYearId(),
    };
  }

  loadAssignments() {
    this.loading.set(true);
    this.loadError.set('');
    this.api.getAll(this.buildFilter()).subscribe({
      next: r => {
        if (r.isSuccess) {
          this.assignments.set(r.data.items);
          this.totalCount.set(r.data.totalCount);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('تعذّر تحميل الواجبات، تحقق من الاتصال وأعد المحاولة.');
        this.loading.set(false);
      }
    });
  }

  loadAll() {
    this.loadAssignments();

    const yearId = this.currentAcademicYearId();
    this.api.getStats(yearId).subscribe({
      next:  r => { if (r.isSuccess) this.stats.set(r.data); },
      error: () => {}
    });

    this.api.getSubjects().subscribe({
      next:  r => this.subjects.set(r),
      error: () => this.subjects.set([])
    });

    // الفصول بتتحمل لما المعلم يختار مادة (loadClassesForSubject)
    this.classes.set([]);
  }

  /** تحميل قائمة الفصول الخاصة بمادة معيّنة (المعلم + المادة معاً) */
  private loadClassesForSubject(subjectId: number | null, onLoaded?: () => void) {
    if (!subjectId) {
      this.classes.set([]);
      onLoaded?.();
      return;
    }
    this.api.getClasses(subjectId).subscribe({
      next:  r => { this.classes.set(r); onLoaded?.(); },
      error: () => { this.classes.set([]); onLoaded?.(); }
    });
  }

  /** لما يبدّل المعلم المادة في فورم إنشاء/تعديل واجب — نعيد تحميل الفصول المرتبطة بها ونصفّر اختيار الفصل السابق */
  onFormSubjectChange(value: number | string | null) {
    const id = value ? Number(value) : null;
    this.newSubjectId.set(id);
    this.newClassId.set(null);
    this.loadClassesForSubject(id);
  }

  openAddModal() {
    this.editingAssignment.set(null);
    this.newTitle.set('');
    this.newSubjectId.set(null);
    this.newClassId.set(null);
    this.newDeadline.set('');
    this.formError.set('');
    this.draftQuestions.set([]);
    this.classes.set([]);
    this.showAddModal.set(true);
  }

  openEditModal(e: AssignmentItem) {
    this.editingAssignment.set(e);
    this.newTitle.set(e.title);
    this.newDeadline.set(this.formatDeadlineForInput(e.deadline));
    this.formError.set('');
    this.draftQuestions.set([]);

    const subj = this.subjects().find(s => s.name === e.subject);
    const subjectId = subj ? subj.id : null;
    this.newSubjectId.set(subjectId);
    this.newClassId.set(null);

    // حمّل تفاصيل الواجب (الأسئلة)
    this.api.getById(e.id).subscribe({
      next: detail => {
        const drafts: AssignmentDraftQuestion[] = (detail.questions ?? []).map(q => ({
          _localId: ++this._draftCounter,
          id: q.id,
          type: q.type as 'mcq' | 'true-false' | 'essay',
          text: q.text,
          options: q.options ? [...q.options] : ['', '', '', ''],
          correctAnswer: q.correctAnswer ?? '',
          points: q.points ?? 5,
        }));
        this.draftQuestions.set(drafts);
        this.showAddModal.set(true);
      },
      error: () => {
        this.showAddModal.set(true);
      }
    });

    // حمّل فصول المادة، وبعد ما تخلص نطابق الفصل الحالي بالاسم
    this.loadClassesForSubject(subjectId, () => {
      const cls = this.classes().find(c => c.name === e.class);
      this.newClassId.set(cls ? cls.id : null);
    });
  }

  openViewModal(e: AssignmentItem) {
    this.api.getById(e.id).subscribe({
      next: detail => {
        this.viewingAssignment.set(detail);
        this.showViewModal.set(true);
      },
      error: () => {}
    });
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.showDeleteConfirm.set(false);
    this.showPublishConfirm.set(false);
    this.showResultsModal.set(false);
    this.showGradingModal.set(false);
    this.viewingAssignment.set(null);
    this.editingAssignment.set(null);
    this.deletingAssignment.set(null);
    this.publishingAssignment.set(null);
    this.resultsAssignment.set(null);
    this.resultsSubmissions.set(null);
    this.gradingSubmission.set(null);
    this.formError.set('');
    this.draftQuestions.set([]);
  }

  saveAssignment() {
    if (!this.newTitle().trim() || !this.newSubjectId() || !this.newClassId()) {
      this.formError.set('من فضلك اكمل بيانات الواجب قبل الحفظ');
      return;
    }

    const drafts = this.draftQuestions();
    for (const q of drafts) {
      if (!q.text.trim()) {
        this.formError.set('من فضلك اكتب نص كل سؤال قبل الحفظ');
        return;
      }
      if (!q.points || q.points <= 0) {
        this.formError.set('من فضلك أدخل درجة صحيحة (أكبر من صفر) لكل سؤال');
        return;
      }
      if (q.type === 'mcq') {
        const filledOptions = q.options.filter(o => o.trim());
        if (filledOptions.length < 2) {
          this.formError.set('سؤال الاختيار من متعدد يجب أن يحتوي على خيارين على الأقل');
          return;
        }
        if (!q.correctAnswer.trim() || !filledOptions.includes(q.correctAnswer)) {
          this.formError.set('من فضلك حدد الإجابة الصحيحة لكل سؤال اختيار من متعدد');
          return;
        }
      }
      if (q.type === 'true-false' && !q.correctAnswer.trim()) {
        this.formError.set('من فضلك حدد الإجابة الصحيحة لكل سؤال صح وخطأ');
        return;
      }
    }

    const questions: CreateAssignmentQuestionPayload[] = drafts.map(q => ({
      type: q.type,
      text: q.text.trim(),
      options: q.type !== 'essay' ? q.options.filter(o => o.trim()) : undefined,
      correctAnswer: q.correctAnswer || undefined,
      points: q.points,
    }));

    const existing = this.editingAssignment();
    if (existing) {
      this.api.update(existing.id, {
        title: this.newTitle().trim(),
        deadline: this.newDeadline() || undefined,
        questions,
      }).subscribe({
        next: r => {
          if (r.isSuccess) { this.closeModals(); this.loadAll(); }
          else this.formError.set(r.message ?? 'حدث خطأ أثناء التحديث');
        },
        error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة، حاول مرة أخرى')
      });
    } else {
      this.api.create({
        title: this.newTitle().trim(),
        subjectId: this.newSubjectId() ?? 0,
        classId: this.newClassId() ?? 0,
        deadline: this.newDeadline() || undefined,
        questions,
      }).subscribe({
        next: r => {
          if (r.isSuccess) { this.closeModals(); this.loadAll(); }
          else this.formError.set(r.message ?? 'حدث خطأ أثناء الإنشاء');
        },
        error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة، حاول مرة أخرى')
      });
    }
  }

  requestDeleteAssignment(e: AssignmentItem) {
    this.deletingAssignment.set(e);
    this.showDeleteConfirm.set(true);
  }

  confirmDeleteAssignment() {
    const assignment = this.deletingAssignment();
    if (!assignment) return;
    this.api.delete(assignment.id).subscribe({
      next: r => {
        if (r.isSuccess) { this.closeModals(); this.loadAll(); }
      },
      error: () => this.closeModals()
    });
  }

  // ── Publish ───────────────────────────────────────────────────

  publishAssignment(a: AssignmentItem) {
    this.publishingAssignment.set(a);
    this.showPublishConfirm.set(true);
  }

  confirmPublish() {
    const a = this.publishingAssignment();
    if (!a) return;
    this.api.publish(a.id).subscribe({
      next: r => {
        if (r.isSuccess) { this.closeModals(); this.loadAll(); }
        else this.formError.set(r.message ?? 'تعذّر النشر');
      },
      error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة')
    });
  }

  // ── Draft questions helpers ───────────────────────────────────

  addQuestion() {
    this.draftQuestions.update(qs => [
      ...qs,
      {
        _localId: ++this._draftCounter,
        id: undefined,
        type: 'mcq',
        text: '',
        options: ['', '', '', ''],
        correctAnswer: '',
        points: 5,
      }
    ]);
  }

  removeQuestion(localId: number) {
    this.draftQuestions.update(qs => qs.filter(q => q._localId !== localId));
  }

  updateQuestionType(localId: number, type: 'mcq' | 'true-false' | 'essay') {
    this.draftQuestions.update(qs => qs.map(q => {
      if (q._localId !== localId) return q;
      const options = type === 'true-false' ? ['صواب', 'خطأ']
                    : type === 'mcq'       ? ['', '', '', '']
                    : [];
      return { ...q, type, options, correctAnswer: '' };
    }));
  }

  updateQuestionText(localId: number, text: string) {
    this.draftQuestions.update(qs =>
      qs.map(q => q._localId === localId ? { ...q, text } : q)
    );
  }

  updateQuestionOption(localId: number, idx: number, value: string) {
    this.draftQuestions.update(qs => qs.map(q => {
      if (q._localId !== localId) return q;
      const options = [...q.options];
      options[idx] = value;
      return { ...q, options };
    }));
  }

  updateQuestionAnswer(localId: number, answer: string) {
    this.draftQuestions.update(qs =>
      qs.map(q => q._localId === localId ? { ...q, correctAnswer: answer } : q)
    );
  }

  updateQuestionPoints(localId: number, pts: number) {
    this.draftQuestions.update(qs =>
      qs.map(q => q._localId === localId ? { ...q, points: pts } : q)
    );
  }

  // ── Results / Grading ─────────────────────────────────────────

  openResultsModal(assignmentId: number) {
    this.resultsAssignment.set(assignmentId);
    this.resultsSubmissions.set(null);
    this.showResultsModal.set(true);

    this.api.getSubmissions(assignmentId).subscribe({
      next:  r => { if (r.isSuccess) this.resultsSubmissions.set(r.data); },
      error: () => this.resultsSubmissions.set([])
    });
  }

  openGradingModal(assignmentId: number, submissionId: number) {
    this.gradingSubmission.set(null);
    this.showGradingModal.set(true);

    this.api.getSubmissionDetail(assignmentId, submissionId).subscribe({
      next:  r => { if (r.isSuccess) this.gradingSubmission.set(r.data); },
      error: () => this.closeModals()
    });
  }

  saveGrading() {
    const submission = this.gradingSubmission();
    if (!submission) return;

    const manualGrades: Record<number, number> = {};
    for (const ans of submission.answers) {
      if (ans.type === 'essay') {
        manualGrades[ans.questionId] = ans.pointsEarned;
      }
    }

    const assignmentId = this.resultsAssignment();
    if (!assignmentId) return;

    this.api.gradeSubmission(assignmentId, submission.submissionId, { manualGrades }).subscribe({
      next: r => {
        if (r.isSuccess) {
          this.showGradingModal.set(false);
          this.gradingSubmission.set(null);
          this.openResultsModal(assignmentId);
        } else {
          alert(r.message ?? 'حدث خطأ أثناء الحفظ');
        }
      },
      error: err => alert(err?.error?.message ?? 'خطأ في الشبكة')
    });
  }

  setEssayGrade(questionId: number, pts: number) {
    this.gradingSubmission.update(s => {
      if (!s) return s;
      return {
        ...s,
        answers: s.answers.map(ans =>
          ans.questionId === questionId ? { ...ans, pointsEarned: pts } : ans
        )
      };
    });
  }

  // ── Helpers ───────────────────────────────────────────────────

  getStatusText(status: string): string {
    const map: Record<string, string> = { open: 'نشط', draft: 'مسودة', closed: 'مغلق' };
    return map[status] || status;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      open:   'bg-green-50 text-green-700',
      draft:  'bg-tertiary-fixed/20 text-tertiary',
      closed: 'bg-surface-container-high text-outline',
    };
    return map[status] || '';
  }

  hasDeadline(e: AssignmentItem): boolean {
    return !!(e.deadline && e.deadline !== '');
  }

  formatDeadline(iso: string): string {
    if (!iso) return '—';
    // Deadline بيجي بتوقيت القاهرة من الـ Manager (string بدون Z للـ datetime-local input).
    // نفسره كـ local browser time — للمعلم المصري = توقيت القاهرة صح.
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    const datePart = d.toLocaleDateString('ar-EG', { day: 'numeric', month: 'numeric', year: 'numeric' });
    const timePart = d.toLocaleTimeString('ar-EG', { hour: 'numeric', minute: '2-digit' });
    return `${datePart}، ${timePart}`;
  }

  private formatDeadlineForInput(iso: string): string {
    if (!iso) return '';
    if (iso.length >= 16) return iso.substring(0, 16);
    return iso;
  }

  deadlineRemaining(iso: string): string {
    if (!iso) return '';
    // Deadline بيجي بتوقيت القاهرة (string بدون Z) — نفسره كـ local browser time
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    const now = new Date();
    const diff = d.getTime() - now.getTime();
    if (diff < 0) {
      const days = Math.abs(Math.ceil(diff / (1000 * 60 * 60 * 24)));
      return `فات موعده ب ${days} أيام`;
    }
    const days = Math.ceil(diff / (1000 * 60 * 60 * 24));
    return days === 0 ? 'ينتهي اليوم' : `متبقي ${days} أيام`;
  }

  isOverdue(iso: string): boolean {
    if (!iso) return false;
    // Deadline بيجي بتوقيت القاهرة (string بدون Z) — نفسره كـ local browser time
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return false;
    return d.getTime() < Date.now();
  }

  submissionRate(a: AssignmentItem): number {
    return a.total > 0 ? Math.round((a.submitted / a.total) * 100) : 0;
  }

  needsAttention(a: AssignmentItem): boolean {
    return a.isAIGenerated && !this.hasDeadline(a);
  }

  formatSubmittedAt(iso: string): string {
    // الـ API بترجع DateTimeOffset بصيغة "2026-06-23T10:00:00+00:00" وهي صالحة
    // لـ Date مباشرة. لا نضيف 'Z' لأن لو القيمة فيها offset بالفعل، الناتج
    // "...+00:00Z" غير صالح ويرجع NaN.
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    const datePart = d.toLocaleDateString('ar-EG', { day: 'numeric', month: 'numeric', year: 'numeric' });
    const timePart = d.toLocaleTimeString('ar-EG', { hour: 'numeric', minute: '2-digit' });
    return `${datePart}، ${timePart}`;
  }
}
