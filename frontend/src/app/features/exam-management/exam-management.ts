import { Component, signal, computed, inject, OnInit, OnDestroy } from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import {
  ExamManagerService,
  ExamItem, ExamDetail, ExamStats, ExamFilter,
  ExamDraftQuestion, CreateExamQuestionPayload,
  ExamAttemptSummary, ExamAttemptGradingDetail
} from './exam-manager.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-exam-management',
  imports: [Sidebar, Topbar, FormsModule, NgClass],
  templateUrl: './exam-management.html',
  styleUrl: './exam-management.css'
})
export class ExamManagement implements OnInit, OnDestroy {
  private router = inject(Router);
  private api = inject(ExamManagerService);
  private academicYearSvc = inject(AcademicYearService);
  private destroy$ = new Subject<void>();

  // ── Academic Year ─────────────────────────────────────────────
  private currentAcademicYearId = signal<number | undefined>(undefined);

  // ── Layout ────────────────────────────────────────────────────
  sidebarOpen = signal(false);
  activeTab = signal<string>('all');

  // ── Filters & Pagination ──────────────────────────────────────
  searchTerm = signal('');
  subjectFilter = signal<number | undefined>(undefined);
  sortBy = signal('newest');
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  private searchSubject = new Subject<string>();

  // ── Modals ────────────────────────────────────────────────────
  showAddModal    = signal(false);
  showViewModal   = signal(false);
  showPublishConfirm = signal(false);
  showDeleteConfirm  = signal(false);
  showResultsModal   = signal(false);
  showGradingModal   = signal(false);

  // ── Modal state ───────────────────────────────────────────────
  viewingExam    = signal<ExamDetail | null>(null);
  editingExam    = signal<ExamItem | null>(null);
  publishingExam = signal<ExamItem | null>(null);
  deletingExam   = signal<ExamItem | null>(null);
  resultsExam    = signal<ExamAttemptSummary[] | null>(null);
  resultsExamId  = signal<number | null>(null);
  gradingAttempt = signal<ExamAttemptGradingDetail | null>(null);

  // ── Form fields ───────────────────────────────────────────────
  formError  = signal('');
  loadError  = signal('');
  loading    = signal(false);

  newName      = signal('');
  newSubjectId = signal<number | null>(null);
  newClassId   = signal<number | null>(null);
  newDate      = signal('');
  newStart     = signal('');
  newEnd       = signal('');
  newTotalScore = signal<number>(100);
  draftTotal    = computed(() => this.draftQuestions().reduce((sum, q) => sum + (q.points || 0), 0));

  // ── Draft questions (for create / edit modal) ─────────────────
  draftQuestions = signal<ExamDraftQuestion[]>([]);
  private _draftCounter = 0;

  // ── Data ──────────────────────────────────────────────────────
  exams    = signal<ExamItem[]>([]);
  stats    = signal<ExamStats>({ total: 0, upcoming: 0, ended: 0, avgScore: 0 });
  subjects = signal<{ id: number; name: string }[]>([]);
  classes  = signal<{ id: number; name: string }[]>([]);

  // ── Static tabs ───────────────────────────────────────────────
  readonly examTabs = [
    { key: 'all',      label: 'الكل'       },
    { key: 'upcoming', label: 'قادمة'      },
    { key: 'active',   label: 'جاري الآن'  },
    { key: 'ended',    label: 'منتهية'     },
    { key: 'draft',    label: 'مسودة'      },
  ];

  readonly sortOptions = [
    { value: 'newest',    label: 'الأحدث'   },
    { value: 'oldest',    label: 'الأقدم'   },
    { value: 'name-asc',  label: 'أ - ي'    },
    { value: 'name-desc', label: 'ي - أ'    },
    { value: 'date-asc',  label: 'التاريخ (تصاعدي)' },
    { value: 'date-desc', label: 'التاريخ (تنازلي)' },
  ];

  calculatedDuration = computed(() => this.calculateDurationMinutes(this.newStart(), this.newEnd()));

  ngOnInit() {
    this.academicYearSvc.getCurrent().subscribe({
      next: r => {
        const year = r?.data ?? r;
        if (year?.id) this.currentAcademicYearId.set(year.id);
      },
      error: () => {}
    });

    // Debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.page.set(1);
      this.loadExams();
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
    this.loadExams();
  }

  onSortChange(sort: string) {
    this.sortBy.set(sort);
    this.page.set(1);
    this.loadExams();
  }

  setActiveTab(tab: string) {
    this.activeTab.set(tab);
    this.page.set(1);
    this.loadExams();
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadExams();
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
    this.loadExams();
  }

  private buildFilter(): ExamFilter {
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

  loadExams() {
    this.loading.set(true);
    this.loadError.set('');
    this.api.getAll(this.buildFilter()).subscribe({
      next: r => {
        if (r.isSuccess) {
          this.exams.set(r.data.items);
          this.totalCount.set(r.data.totalCount);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('تعذّر تحميل الامتحانات، تحقق من الاتصال وأعد المحاولة.');
        this.loading.set(false);
      }
    });
  }

  loadAll() {
    this.loadExams();

    const yearId = this.currentAcademicYearId();
    this.api.getStats(undefined, yearId).subscribe({
      next:  r => { if (r.isSuccess) this.stats.set(r.data); },
      error: () => {}
    });

    this.api.getSubjects().subscribe({
      next:  r => this.subjects.set(r),
      error: () => this.subjects.set([])
    });

    this.api.getClasses().subscribe({
      next:  r => this.classes.set(r),
      error: () => this.classes.set([])
    });
  }

  goToGenerator() {
    this.router.navigate(['/exam-generator']);
  }

  openAddModal() {
    this.editingExam.set(null);
    this.newName.set('');
    this.newSubjectId.set(null);
    this.newClassId.set(null);
    this.newDate.set('');
    this.newStart.set('');
    this.newEnd.set('');
    this.newTotalScore.set(100);
    this.formError.set('');
    this.draftQuestions.set([]);
    this.showAddModal.set(true);
  }

  openEditModal(e: ExamItem) {
    this.editingExam.set(e);
    this.newName.set(e.name);
    const subj = this.subjects().find(s => s.name === e.subject);
    this.newSubjectId.set(subj ? subj.id : null);
    const cls = this.classes().find(c => c.name === e.class);
    this.newClassId.set(cls ? cls.id : null);
    this.newDate.set(e.date);
    this.newStart.set(e.startTime);
    this.newEnd.set(e.endTime);
    this.formError.set('');
    this.draftQuestions.set([]);

    // جلب الأسئلة الحالية للامتحان وتعبئة draftQuestions
    this.api.getById(e.id).subscribe({
      next: detail => {
        this.newTotalScore.set(detail.totalScore ?? 100);
        const drafts: ExamDraftQuestion[] = detail.questions.map(q => ({
          _localId: ++this._draftCounter,
          id: q.id,
          type: q.type as 'mcq' | 'true-false' | 'essay',
          text: q.text,
          options: q.options ? [...q.options] : ['', '', '', ''],
          correctAnswer: q.correctAnswer,
          points: q.points ?? 10,
        }));
        this.draftQuestions.set(drafts);
        this.showAddModal.set(true);
      },
      error: () => {
        // فتح المودال بدون أسئلة لو فشل الجلب
        this.showAddModal.set(true);
      }
    });
  }

  openViewModal(e: ExamItem) {
    this.api.getById(e.id).subscribe(detail => {
      this.viewingExam.set(detail);
      this.showViewModal.set(true);
    });
  }

  closeModals() {
    this.showAddModal.set(false);
    this.showViewModal.set(false);
    this.showPublishConfirm.set(false);
    this.showDeleteConfirm.set(false);
    this.showResultsModal.set(false);
    this.showGradingModal.set(false);
    this.viewingExam.set(null);
    this.publishingExam.set(null);
    this.deletingExam.set(null);
    this.resultsExam.set(null);
    this.resultsExamId.set(null);
    this.gradingAttempt.set(null);
    this.formError.set('');
    this.draftQuestions.set([]);
  }

  saveExam() {
    const durationMinutes = this.calculatedDuration();
    if (!this.newName().trim() || !this.newSubjectId() || !this.newClassId() || !this.newDate() || !this.newStart() || !this.newEnd()) {
      this.formError.set('من فضلك اكمل بيانات الامتحان قبل الحفظ');
      return;
    }

    if (durationMinutes <= 0) {
      this.formError.set('وقت النهاية يجب ان يكون بعد وقت البداية');
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

    const questions: CreateExamQuestionPayload[] = drafts.map(q => ({
      type: q.type,
      text: q.text.trim(),
      options: q.type !== 'essay' ? q.options.filter(o => o.trim()) : undefined,
      correctAnswer: q.correctAnswer || undefined,
      points: q.points,
    }));

    const payload = {
      title: this.newName().trim(),
      subjectId: this.newSubjectId() ?? 0,
      classId: this.newClassId() ?? 0,
      date: this.newDate(),
      startTime: this.newStart(),
      endTime: this.newEnd(),
      durationMinutes,
      totalScore: this.newTotalScore(),
      questions,
    };

    const existing = this.editingExam();
    if (existing) {
      this.api.update(existing.id, payload).subscribe({
        next: r => {
          if (r.isSuccess) { this.closeModals(); this.loadAll(); }
          else this.formError.set(r.message ?? 'حدث خطأ أثناء التحديث');
        },
        error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة، حاول مرة أخرى')
      });
    } else {
      this.api.create(payload).subscribe({
        next: r => {
          if (r.isSuccess) { this.closeModals(); this.loadAll(); }
          else this.formError.set(r.message ?? 'حدث خطأ أثناء الإنشاء');
        },
        error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة، حاول مرة أخرى')
      });
    }
  }

  requestDeleteExam(e: ExamItem) {
    this.deletingExam.set(e);
    this.showDeleteConfirm.set(true);
  }

  confirmDeleteExam() {
    const exam = this.deletingExam();
    if (!exam) return;
    this.api.delete(exam.id).subscribe({
      next: r => {
        if (r.isSuccess) { this.closeModals(); this.loadAll(); }
      },
      error: () => this.closeModals()
    });
  }

  publishExam(id: number) {
    const exam = this.exams().find(e => e.id === id);
    if (!exam) return;
    this.publishingExam.set(exam);
    this.showPublishConfirm.set(true);
  }

  confirmPublish() {
    const exam = this.publishingExam();
    if (!exam) return;

    this.api.publish(exam.id).subscribe({
      next: r => {
        if (r.isSuccess) { this.closeModals(); this.loadAll(); }
        else this.formError.set(r.message ?? 'تعذّر النشر');
      },
      error: err => this.formError.set(err?.error?.message ?? 'خطأ في الشبكة')
    });
  }

  togglePublishResults(examId: number, publish: boolean) {
    const call = publish
      ? this.api.publishResults(examId)
      : this.api.unpublishResults(examId);

    call.subscribe({
      next:  r => { if (r.isSuccess) this.loadAll(); else alert(r.message); },
      error: err => alert(err?.error?.message ?? 'خطأ في الشبكة')
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
        points: 10,
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

  openResultsModal(examId: number) {
    this.resultsExamId.set(examId);
    this.resultsExam.set(null);
    this.showResultsModal.set(true);

    this.api.getAttemptsByExam(examId).subscribe({
      next:  r => { if (r.isSuccess) this.resultsExam.set(r.data); },
      error: () => this.resultsExam.set([])
    });
  }

  openGradingModal(attemptId: number) {
    this.gradingAttempt.set(null);
    this.showGradingModal.set(true);

    this.api.getAttemptDetail(attemptId).subscribe({
      next:  r => { if (r.isSuccess) this.gradingAttempt.set(r.data); },
      error: () => this.closeModals()
    });
  }

  saveGrading() {
    const attempt = this.gradingAttempt();
    if (!attempt) return;

    const answers = attempt.answers
      .filter(a => a.questionType === 'essay')
      .map(a => ({
        answerId:     a.id,
        pointsEarned: a.pointsEarned ?? 0,
        feedback:     a.feedback ?? '',
      }));

    this.api.gradeEssayAnswers(attempt.id, { answers }).subscribe({
      next: r => {
        if (r.isSuccess) {
          const examId = this.resultsExamId();
          this.showGradingModal.set(false);
          this.gradingAttempt.set(null);
          if (examId) this.openResultsModal(examId); // refresh results
        } else {
          alert(r.message ?? 'حدث خطأ أثناء الحفظ');
        }
      },
      error: err => alert(err?.error?.message ?? 'خطأ في الشبكة')
    });
  }

  setEssayGrade(answerId: number, pts: number) {
    this.gradingAttempt.update(a => {
      if (!a) return a;
      return {
        ...a,
        answers: a.answers.map(ans =>
          ans.id === answerId ? { ...ans, pointsEarned: pts } : ans
        )
      };
    });
  }

  setEssayFeedback(answerId: number, feedback: string) {
    this.gradingAttempt.update(a => {
      if (!a) return a;
      return {
        ...a,
        answers: a.answers.map(ans =>
          ans.id === answerId ? { ...ans, feedback } : ans
        )
      };
    });
  }

  private calculateDurationMinutes(start: string, end: string): number {
    if (!start || !end) return 0;

    const [startHours, startMinutes] = start.split(':').map(Number);
    const [endHours, endMinutes] = end.split(':').map(Number);
    if ([startHours, startMinutes, endHours, endMinutes].some(value => Number.isNaN(value))) return 0;

    return (endHours * 60 + endMinutes) - (startHours * 60 + startMinutes);
  }

  getStatusText(status: string): string {
    const map: Record<string, string> = { upcoming: 'قادم', active: 'جاري الآن', ended: 'منتهٍ', draft: 'مسودة' };
    return map[status] || status;
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = { upcoming: 'bg-secondary/10 text-secondary', active: 'bg-green-50 text-green-700', ended: 'bg-surface-container-high text-outline', draft: 'bg-tertiary-fixed/20 text-tertiary' };
    return map[status] || '';
  }

  hasSchedule(e: ExamItem): boolean {
    return !!(e.date && e.date !== '' && e.startTime && e.startTime !== '' && e.endTime && e.endTime !== '');
  }

  isExamIncomplete(e: ExamItem): boolean {
    return e.isAIGenerated && !this.hasSchedule(e);
  }

  getNeedsAttentionMessage(e: ExamItem): string {
    if (!e.isAIGenerated) return '';
    const missing: string[] = [];
    if (!e.date || e.date === '') missing.push('التاريخ');
    if (!e.startTime || e.startTime === '') missing.push('وقت البداية');
    if (!e.endTime || e.endTime === '') missing.push('وقت النهاية');
    if (missing.length > 0) return `أضف ${missing.join('، ')}`;
    return '';
  }

  formatSubmittedAt(iso: string): string {
    if (!iso.endsWith('Z')) {
      iso += 'Z';
    }
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    const datePart = d.toLocaleDateString('ar-EG', { day: 'numeric', month: 'numeric', year: 'numeric' });
    const timePart = d.toLocaleTimeString('ar-EG', { hour: 'numeric', minute: '2-digit' });
    return `${datePart}، ${timePart}`;
  }
}
