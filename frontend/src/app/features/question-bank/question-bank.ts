import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { QuestionBankService, QuestionBankItemDto, SearchQuestionBankDto, AddQuestionDto, AddOptionDto } from '../../core/services/question-bank.service';
import { GradeLevelService } from '../../core/services/grade-level.service';
import { HttpClient } from '@angular/common/http';
import { buildApiUrl } from '../../core/utils/api-url';

@Component({
  selector: 'app-question-bank',
  imports: [Sidebar, FormsModule],
  templateUrl: './question-bank.html',
  styleUrl: './question-bank.css',
})
export class QuestionBank implements OnInit {
  private qbSvc = inject(QuestionBankService);
  private gradeLevelSvc = inject(GradeLevelService);
  private http = inject(HttpClient);

  sidebarOpen = signal(false);
  loading = signal(false);
  deleting = signal(false);
  saving = signal(false);
  errorMsg = signal('');
  successMsg = signal('');

  // Filters
  searchText = signal('');
  selectedSubjectId = signal<number | null>(null);
  selectedGradeLevelId = signal<number | null>(null);
  selectedQuestionType = signal<number | null>(null);

  // Subject & Grade options
  subjects = signal<{ id: number; name: string }[]>([]);
  gradeLevels = signal<any[]>([]);
  filteredSubjects = computed(() => {
    const glId = this.selectedGradeLevelId();
    if (!glId) return this.subjects();
    return this.subjects();
  });

  // Results
  questions = signal<QuestionBankItemDto[]>([]);
  totalCount = signal(0);
  page = signal(1);
  pageSize = signal(20);

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  readonly QUESTION_TYPE_LABELS: { value: number; label: string; icon: string; cls: string }[] = [
    { value: 1, label: 'اختيار من متعدد', icon: 'quiz', cls: 'type-mcq' },
    { value: 2, label: 'صح/خطأ', icon: 'check_circle', cls: 'type-tf' },
    { value: 3, label: 'أكمل الفراغ', icon: 'pending_actions', cls: 'type-fill' },
    { value: 4, label: 'مقالي', icon: 'edit_note', cls: 'type-essay' },
  ];

  qTypeName(v: number): string {
    return this.QUESTION_TYPE_LABELS.find(q => q.value === v)?.label ?? '';
  }

  qTypeClass(v: number): string {
    return this.QUESTION_TYPE_LABELS.find(q => q.value === v)?.cls ?? '';
  }

  qTypeIcon(v: number): string {
    return this.QUESTION_TYPE_LABELS.find(q => q.value === v)?.icon ?? 'help';
  }

  // ── Add/Edit Modal ──
  showModal = signal(false);
  editMode = signal(false); // false = add, true = edit
  editId = signal<number | null>(null);

  // Form fields
  formSubjectId = signal<number | null>(null);
  formGradeLevelId = signal<number | null>(null);
  formQuestionType = signal<number>(1);
  formQuestionText = signal('');
  formCorrectAnswer = signal('');

  // Options for MCQ
  formOptions = signal<{ text: string; isCorrect: boolean }[]>([
    { text: '', isCorrect: false },
    { text: '', isCorrect: false },
    { text: '', isCorrect: false },
    { text: '', isCorrect: false },
  ]);

  // Confirm delete
  showDeleteConfirm = signal(false);
  deleteTarget: QuestionBankItemDto | null = null;

  ngOnInit() {
    this.loadGradeLevels();
    this.loadSubjects();
    this.search();
  }

  private loadGradeLevels() {
    this.gradeLevelSvc.getAll().subscribe({
      next: (res: any) => {
        const data = res?.data ?? (Array.isArray(res) ? res : []);
        this.gradeLevels.set(data);
      },
    });
  }

  private loadSubjects() {
    this.http.get(buildApiUrl('subjects')).subscribe({
      next: (res: any) => {
        const list = res?.data ?? (Array.isArray(res) ? res : []);
        this.subjects.set(list.map((s: any) => ({ id: s.id, name: s.name })));
      },
    });
  }

  onGradeLevelChange() {
    this.page.set(1);
    this.search();
  }

  onQuestionTypeChange() {
    this.page.set(1);
    this.search();
  }

  onSubjectChange() {
    this.page.set(1);
    this.search();
  }

  onSearchInput() {
    this.page.set(1);
    this.search();
  }

  search() {
    this.loading.set(true);
    this.errorMsg.set('');

    const dto: SearchQuestionBankDto = {
      searchText: this.searchText() || undefined,
      subjectId: this.selectedSubjectId() ?? undefined,
      gradeLevelId: this.selectedGradeLevelId() ?? undefined,
      questionType: this.selectedQuestionType() ?? undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    };

    this.qbSvc.search(dto).subscribe({
      next: (res: any) => {
        const data = res?.data;
        if (data) {
          this.questions.set(data.items || []);
          this.totalCount.set(data.totalCount || 0);
        } else {
          this.questions.set([]);
          this.totalCount.set(0);
        }
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.message || 'فشل تحميل بنك الأسئلة');
      },
    });
  }

  goToPage(p: number) {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.search();
  }

  pageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.page();
    const pages: number[] = [];
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  // ── Modal: Open / Close ──

  openAddModal() {
    this.editMode.set(false);
    this.editId.set(null);
    this.resetForm();
    this.showModal.set(true);
  }

  openEditModal(q: QuestionBankItemDto) {
    this.editMode.set(true);
    this.editId.set(q.id);
    this.formSubjectId.set(q.subjectId);
    this.formGradeLevelId.set(q.gradeLevelId);
    this.formQuestionType.set(q.questionType);
    this.formQuestionText.set(q.questionText);
    this.formCorrectAnswer.set(q.correctAnswer || '');

    if (q.questionType === 1 && q.options.length > 0) {
      this.formOptions.set(q.options.map(o => ({ text: o.optionText, isCorrect: o.isCorrect })));
    } else {
      this.formOptions.set([
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
      ]);
    }

    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.resetForm();
  }

  private resetForm() {
    this.formSubjectId.set(null);
    this.formGradeLevelId.set(null);
    this.formQuestionType.set(1);
    this.formQuestionText.set('');
    this.formCorrectAnswer.set('');
    this.formOptions.set([
      { text: '', isCorrect: false },
      { text: '', isCorrect: false },
      { text: '', isCorrect: false },
      { text: '', isCorrect: false },
    ]);
  }

  // ── Form helpers ──

  isFormValid(): boolean {
    if (!this.formSubjectId() || !this.formGradeLevelId() || !this.formQuestionText()) return false;
    if (this.formQuestionType() === 1) {
      const validOpts = this.formOptions().filter(o => o.text.trim());
      if (validOpts.length < 2) return false;
      if (!validOpts.some(o => o.isCorrect)) return false;
    }
    return true;
  }

  setCorrectOption(index: number) {
    this.formOptions.update(opts => opts.map((o, i) => ({ ...o, isCorrect: i === index })));
  }

  addOption() {
    this.formOptions.update(opts => [...opts, { text: '', isCorrect: false }]);
  }

  removeOption(index: number) {
    if (this.formOptions().length <= 2) return;
    this.formOptions.update(opts => opts.filter((_, i) => i !== index));
  }

  markAllCorrect() {
    this.formOptions.update(opts => opts.map(o => ({ ...o, isCorrect: true })));
  }

  // ── Save (Add / Update) ──

  saveQuestion() {
    if (!this.isFormValid()) return;

    this.saving.set(true);
    this.errorMsg.set('');

    const options: AddOptionDto[] = this.formQuestionType() === 1
      ? this.formOptions()
          .filter(o => o.text.trim())
          .map((o, i) => ({ text: o.text.trim(), isCorrect: o.isCorrect, displayOrder: i + 1 }))
      : [];

    const dto: AddQuestionDto = {
      questionText: this.formQuestionText().trim(),
      questionType: this.formQuestionType(),
      correctAnswer: this.formCorrectAnswer().trim() || null,
      options,
      subjectId: this.formSubjectId()!,
      gradeLevelId: this.formGradeLevelId()!,
    };

    if (this.editMode() && this.editId()) {
      // Update
      this.qbSvc.update(this.editId()!, dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.successMsg.set('تم تحديث السؤال بنجاح');
          setTimeout(() => this.successMsg.set(''), 3000);
          this.search();
        },
        error: (err) => {
          this.saving.set(false);
          this.errorMsg.set(err?.error?.message || err?.error?.title || 'فشل تحديث السؤال');
        },
      });
    } else {
      // Add
      this.qbSvc.add(dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.successMsg.set('تم إضافة السؤال بنجاح');
          setTimeout(() => this.successMsg.set(''), 3000);
          this.search();
        },
        error: (err) => {
          this.saving.set(false);
          this.errorMsg.set(err?.error?.message || err?.error?.title || 'فشل إضافة السؤال');
        },
      });
    }
  }

  // ── Delete ──

  confirmDelete(q: QuestionBankItemDto) {
    this.deleteTarget = q;
    this.showDeleteConfirm.set(true);
  }

  cancelDelete() {
    this.showDeleteConfirm.set(false);
    this.deleteTarget = null;
  }

  doDelete() {
    const target = this.deleteTarget;
    if (!target) return;

    this.deleting.set(true);
    this.qbSvc.delete(target.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.showDeleteConfirm.set(false);
        this.deleteTarget = null;
        this.successMsg.set('تم حذف السؤال بنجاح');
        setTimeout(() => this.successMsg.set(''), 3000);
        this.search();
      },
      error: (err) => {
        this.deleting.set(false);
        this.errorMsg.set(err?.error?.message || 'فشل حذف السؤال');
      },
    });
  }

  dismissError() {
    this.errorMsg.set('');
  }

  getOptionLabel(index: number): string {
    return 'ABCDEFGH'[index] || `${index + 1}`;
  }
}
