import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ExamGeneratorService, AiGenerateExamRequest, AiExamPreviewDto, AiExamPreviewQuestionDto, ExamSummaryDto } from '../../core/services/exam-generator.service';
import { GradeLevelService } from '../../core/services/grade-level.service';
import { buildApiUrl } from '../../core/utils/api-url';

export interface ClassSubjectTeacherDto {
  id: number;
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  teacherId: number;
  teacherName: string;
  academicYearId: number;
  academicYearName: string;
  weeklyPeriods: number;
}

export interface UnitDto {
  id: number;
  subjectId: number;
  gradeLevelId: number;
  name: string;
  content?: string;
  displayOrder: number;
  pageStart?: number;
  pageEnd?: number;
  subjectName?: string;
  gradeLevelName?: string;
  lessons?: LessonDto[];
}

export interface LessonDto {
  id: number;
  unitId: number;
  title: string;
  content?: string;
  displayOrder: number;
  pageStart?: number;
  pageEnd?: number;
}

@Component({
  selector: 'app-exam-generator',
  imports: [Sidebar, Topbar, FormsModule],
  templateUrl: './exam-generator.html',
  styleUrl: './exam-generator.css',
})
export class ExamGenerator implements OnInit {
  private genSvc = inject(ExamGeneratorService);
  private gradeLevelSvc = inject(GradeLevelService);
  private http = inject(HttpClient);

  sidebarOpen = signal(false);
  loading = signal(false);
  generating = signal(false);
  saving = signal(false);
  embedding = signal(false);
  embedMsg = signal<string | null>(null);
  errorMsg = signal('');

  gradeLevels = signal<any[]>([]);
  selectedGradeLevelId = signal<number | null>(null);
  assignments = signal<ClassSubjectTeacherDto[]>([]);
  units = signal<UnitDto[]>([]);
  lessons = signal<LessonDto[]>([]);
  history = signal<ExamSummaryDto[]>([]);

  selectedCstId = signal<number | null>(null);
  selectedSubjectId = signal<number | null>(null);
  selectedSubjectName = signal<string>('');
  selectedClassName = signal<string>('');
  title = signal('');
  durationMinutes = signal<number>(60);
  totalScore = signal<number>(100);
  questionCounts = signal<Record<number, number>>({ 1: 5, 2: 3, 3: 2, 4: 0 });
  topic = signal('');
  selectedUnitId = signal<number | null>(null);
  selectedLessonIds = signal<Set<number>>(new Set());

  previewExam = signal<AiExamPreviewDto | null>(null);
  previewQuestions = signal<AiExamPreviewQuestionDto[]>([]);
  savedExamId = signal<number | null>(null);
  savedExamUid = signal<string | null>(null);
  viewingHistoryId = signal<number | null>(null);

  showConfirm = signal(false);
  showDeleteConfirm = signal(false);
  deleteTargetId = signal<number | null>(null);

  // Edit Mode
  editMode = signal(false);
  editData = signal<{ questionText: string; points: number; correctAnswer: string | null; options: { optionText: string; isCorrect: boolean }[] }[]>([]);
  editTitle = signal('');
  editDuration = signal<number>(60);
  editTotalScore = signal<number>(100);
  editCstId = signal<number | null>(null);
  editSubjectName = signal<string>('');
  editSubjectId = signal<number | null>(null);
  /** SubjectId محفوظ عند تحميل الامتحان (للمعاينة فقط) */
  savedSubjectId = signal<number | null>(null);
  savedClassSubjectTeacherId = signal<number | null>(null);

  /** الفصول المتاحة للمادة المختارة في edit mode */
  editClassOptions = computed(() => {
    const name = this.editSubjectName();
    if (!name) return [];
    return this.assignments().filter(a => a.subjectName === name);
  });

  /** هل للمادة المختارة في edit mode أكثر من فصل؟ */
  editHasMultipleClasses = computed(() => this.editClassOptions().length > 1);

  readonly QUESTION_TYPE_LABELS: { value: number; label: string; icon: string }[] = [
    { value: 1, label: 'اختيار من متعدد', icon: 'quiz' },
    { value: 2, label: 'صح/خطأ', icon: 'check_circle' },
    { value: 3, label: 'أكمل الفراغ', icon: 'pending_actions' },
    { value: 4, label: 'مقالي', icon: 'edit_note' },
  ];

  qTypeName(v: number): string {
    return this.QUESTION_TYPE_LABELS.find(q => q.value === v)?.label ?? '';
  }

  totalQuestionCount(): number {
    const qc = this.questionCounts();
    return Object.values(qc).reduce((a, b) => a + b, 0);
  }

  /** قائمة المواد الفريدة من بيانات المدرس */
  subjectOptions = computed(() => {
    const seen = new Map<string, { subjectName: string; subjectId: number; count: number }>();
    for (const a of this.assignments()) {
      if (!seen.has(a.subjectName)) {
        seen.set(a.subjectName, { subjectName: a.subjectName, subjectId: a.subjectId, count: 0 });
      }
      seen.get(a.subjectName)!.count++;
    }
    return [...seen.values()].sort((a, b) => a.subjectName.localeCompare(b.subjectName));
  });

  /** الفصول المتاحة للمادة المختارة */
  classOptions = computed(() => {
    const name = this.selectedSubjectName();
    if (!name) return [];
    return this.assignments().filter(a => a.subjectName === name);
  });

  /** هل للمادة أكثر من فصل؟ */
  hasMultipleClasses = computed(() => this.classOptions().length > 1);

  ngOnInit() {
    this.loadGradeLevels();
    this.loadMyAssignments();
    this.loadHistory();
  }

  private loadGradeLevels() {
    this.gradeLevelSvc.getAll().subscribe({
      next: (res: any) => {
        const data = res?.data ?? (Array.isArray(res) ? res : []);
        this.gradeLevels.set(data);
      },
      error: () => {}
    });
  }

  private loadMyAssignments() {
    this.loading.set(true);
    this.genSvc.getMyAssignments().subscribe({
      next: (r: any) => {
        const list = Array.isArray(r.data) ? r.data : Array.isArray(r) ? r : [];
        this.assignments.set(list.map((x: any) => ({
          id: x.id,
          classId: x.classId,
          className: x.className,
          subjectId: x.subjectId,
          subjectName: x.subjectName,
          teacherId: x.teacherId,
          teacherName: x.teacherName,
          academicYearId: x.academicYearId,
          academicYearName: x.academicYearName,
          weeklyPeriods: x.weeklyPeriods,
        })));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.errorMsg.set('فشل تحميل المواد');
      }
    });
  }

  private loadHistory() {
    this.genSvc.getHistory().subscribe({
      next: (r: any) => {
        const list = Array.isArray(r.data) ? r.data : [];
        this.history.set(list);
      },
      error: () => {}
    });
  }

  onSubjectChange() {
    const name = this.selectedSubjectName();
    this.selectedClassName.set('');
    this.selectedCstId.set(null);
    this.units.set([]);
    this.lessons.set([]);
    this.selectedUnitId.set(null);
    this.selectedLessonIds.set(new Set());

    if (!name) {
      this.selectedSubjectId.set(null);
      return;
    }

    const classes = this.classOptions();
    const first = classes[0];
    if (!first) {
      this.selectedSubjectId.set(null);
      return;
    }

    this.selectedSubjectId.set(first.subjectId);
    this.loadUnits(first.subjectId);

    // Auto-resolve if only one class, or let user pick
    if (classes.length === 1) {
      this.selectedCstId.set(first.id);
      this.selectedClassName.set(first.className);
    }
  }

  onEditSubjectChange() {
    const name = this.editSubjectName();
    this.editCstId.set(null);
    this.editSubjectId.set(null);

    if (!name) return;

    const classes = this.editClassOptions();
    const first = classes[0];
    if (!first) return;

    this.editSubjectId.set(first.subjectId);

    if (classes.length === 1) {
      this.editCstId.set(first.id);
    }
  }

  onClassChange() {
    const cstId = this.selectedCstId();
    if (cstId) {
      const cst = this.assignments().find(a => a.id === cstId);
      if (cst) {
        this.selectedClassName.set(cst.className);
      }
    }
  }

  private loadUnits(subjectId: number) {
    this.genSvc.getUnitsWithLessons(subjectId, this.selectedGradeLevelId() ?? undefined).subscribe({
      next: (r: any) => {
        const list = Array.isArray(r.data) ? r.data : Array.isArray(r) ? r : [];
        this.units.set(list.map((u: any) => ({
          id: u.id,
          subjectId: u.subjectId,
          name: u.name,
          content: u.content,
          displayOrder: u.displayOrder,
          pageStart: u.pageStart,
          pageEnd: u.pageEnd,
          subjectName: u.subjectName,
          lessons: (u.lessons ?? []).map((l: any) => ({
            id: l.id,
            unitId: l.unitId,
            title: l.title,
            content: l.content,
            displayOrder: l.displayOrder,
            pageStart: l.pageStart,
            pageEnd: l.pageEnd,
          })),
        })));
      },
      error: () => this.errorMsg.set('فشل تحميل الوحدات')
    });
  }

  onUnitChange() {
    const uid = this.selectedUnitId();
    if (!uid) {
      this.lessons.set([]);
      this.selectedLessonIds.set(new Set());
      return;
    }
    const unit = this.units().find(u => u.id === uid);
    this.lessons.set(unit?.lessons ?? []);
    this.selectedLessonIds.set(new Set());
  }

  setQuestionCount(type: number, count: number) {
    this.questionCounts.update(m => ({ ...m, [type]: Math.max(0, count) }));
  }

  toggleLesson(id: number) {
    this.selectedLessonIds.update(s => {
      const next = new Set(s);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  preview() {
    let cstId = this.selectedCstId();
    if (!cstId) {
      // خلّي cstId = null عشان الامتحان مش هيتقيد بفصل
    }
    const totalQ = this.totalQuestionCount();
    if (!totalQ) { this.errorMsg.set('اختر عدداً من الأسئلة'); return; }

    this.generating.set(true);
    this.errorMsg.set('');
    this.previewExam.set(null);
    this.previewQuestions.set([]);
    this.savedExamId.set(null);
    this.savedExamUid.set(null);
    this.embedMsg.set(null);
    this.embedding.set(false);
    this.showConfirm.set(false);

    const body: AiGenerateExamRequest = {
      classSubjectTeacherId: cstId,
      subjectId: this.selectedSubjectId(),
      gradeLevelId: this.selectedGradeLevelId(),
      title: this.title() || `امتحان ${this.selectedSubjectName() || ''}`,
      durationMinutes: this.durationMinutes() || undefined,
      totalScore: this.totalScore(),
      category: 2,
      questionCounts: { ...this.questionCounts() },
      topic: this.topic() || undefined,
      unitId: this.selectedUnitId() ?? undefined,
      lessonIds: [...this.selectedLessonIds()],
    };

    this.genSvc.preview(body).subscribe({
      next: (r: any) => {
        const preview = r?.data as AiExamPreviewDto;
        if (preview) {
          this.previewExam.set(preview);
          this.previewQuestions.set(preview.standaloneQuestions || []);
        }
        this.generating.set(false);
      },
      error: (err) => {
        this.generating.set(false);
        this.errorMsg.set(err?.error?.message || err?.message || 'فشل إنشاء الامتحان');
      }
    });
  }

  confirmSave() {
    this.showConfirm.set(true);
  }

  cancelSave() {
    this.showConfirm.set(false);
  }

  saveExam() {
    const preview = this.previewExam();
    if (!preview) return;

    this.saving.set(true);
    this.showConfirm.set(false);
    this.editMode.set(false);
    this.editData.set([]);

    const saveBody: any = {
      classSubjectTeacherId: this.selectedCstId() ?? null,
      subjectId: this.selectedSubjectId(),
      gradeLevelId: this.selectedGradeLevelId(),
      title: preview.title,
      durationMinutes: preview.durationMinutes,
      totalScore: preview.totalScore,
      category: 2,
      standaloneQuestions: preview.standaloneQuestions.map((q, i) => ({
        questionText: q.questionText,
        questionType: q.questionType,
        options: q.options?.map(o => ({
          text: o.optionText,
          isCorrect: o.isCorrect,
          displayOrder: o.displayOrder,
        })) || [],
        correctAnswer: q.correctAnswer,
        points: q.points,
        displayOrder: i + 1,
      })),
    };

    this.genSvc.save(saveBody).subscribe({
      next: (r: any) => {
        const saved = r?.data;
        if (saved?.id) {
          this.savedExamId.set(saved.id);
          this.savedExamUid.set(saved.uid || null);
          // Update previewExam with saved data so gradeLevelName etc. are shown
          this.previewExam.update(p => p ? {
            ...p,
            subjectName: saved.subjectName || p.subjectName,
            gradeLevelName: saved.gradeLevelName || p.gradeLevelName,
            classSubjectTeacherId: saved.classSubjectTeacherId ?? p.classSubjectTeacherId,
          } : p);
        }
        this.saving.set(false);
        this.loadHistory();
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMsg.set(err?.error?.message || err?.message || 'فشل حفظ الامتحان');
      }
    });
  }

  /** حفظ أسئلة الامتحان في بنك الأسئلة مع إنشاء البحث الدلالي (MongoDB embedding) */
  embedToQuestionBank() {
    const uid = this.savedExamUid();
    const examId = this.savedExamId();
    if (!uid || !examId) return;

    this.embedding.set(true);
    this.embedMsg.set('جاري الحفظ في بنك الأسئلة...');

    this.http.post(buildApiUrl(`question-embedding/from-exam/${examId}`), {}).subscribe({
      next: (r: any) => {
        this.embedding.set(false);
        const count = r?.data || r?.embeddedCount || '';
        this.embedMsg.set(`✅ تم حفظ ${count} سؤال في بنك الأسئلة`);
      },
      error: (err) => {
        this.embedding.set(false);
        const msg = err?.error?.message || err?.error?.title || err?.message || JSON.stringify(err?.error) || 'خطأ غير معروف';
        console.error('Embed error:', err);
        this.embedMsg.set('❌ ' + msg);
      }
    });
  }

  deleteExam(id: number) {
    this.deleteTargetId.set(id);
    this.showDeleteConfirm.set(true);
  }

  confirmDelete() {
    const id = this.deleteTargetId();
    if (!id) return;

    this.loading.set(true);
    this.showDeleteConfirm.set(false);
    this.genSvc.deleteExamById(id).subscribe({
      next: () => {
        this.loading.set(false);
        this.deleteTargetId.set(null);

        // Clear preview if it was the deleted exam
        if (this.savedExamId() === id) {
          this.previewExam.set(null);
          this.previewQuestions.set([]);
          this.savedExamId.set(null);
          this.savedExamUid.set(null);
        }

        this.loadHistory();
      },
      error: (err) => {
        this.loading.set(false);
        this.deleteTargetId.set(null);
        this.errorMsg.set(err?.error?.message || err?.message || 'فشل حذف الامتحان');
      }
    });
  }

  cancelDelete() {
    this.showDeleteConfirm.set(false);
    this.deleteTargetId.set(null);
  }

  viewHistoryExam(id: number) {
    this.viewingHistoryId.set(id);
    this.generating.set(false);
    this.saving.set(false);
    this.previewExam.set(null);
    this.previewQuestions.set([]);
    this.savedExamId.set(null);
    this.savedExamUid.set(null);
    this.embedMsg.set(null);
    this.embedding.set(false);
    this.showConfirm.set(false);
    this.showDeleteConfirm.set(false);
    this.editMode.set(false);
    this.editData.set([]);

    this.genSvc.getExamById(id).subscribe({
      next: (r: any) => {
        const saved = r?.data;
        if (saved) {
          const mapped: AiExamPreviewDto = {
            subjectName: saved.subjectName || '',
            className: saved.className || '',
            teacherName: saved.teacherName || '',
            gradeLevelName: saved.gradeLevelName || '',
            gradeLevelId: saved.gradeLevelId,
            title: saved.title,
            durationMinutes: saved.durationMinutes,
            totalScore: saved.totalScore,
            questionsCount: saved.questionsCount || 0,
            classSubjectTeacherId: saved.classSubjectTeacherId ?? null,
            standaloneQuestions: [],
          };
          this.savedClassSubjectTeacherId.set(saved.classSubjectTeacherId ?? null);

          const allQ: AiExamPreviewQuestionDto[] = [];
          if (saved.groups) {
            for (const g of saved.groups) {
              if (g.questions) {
                for (const q of g.questions) {
                  allQ.push({
                    questionText: q.questionText,
                    questionType: q.questionType,
                    options: (q.options || []).map((o: any) => ({
                      optionText: o.optionText,
                      isCorrect: o.isCorrect || false,
                      displayOrder: o.displayOrder,
                    })),
                    correctAnswer: q.correctAnswer || null,
                    points: q.points,
                    displayOrder: q.displayOrder,
                  });
                }
              }
            }
          }
          if (saved.standaloneQuestions) {
            for (const q of saved.standaloneQuestions) {
              allQ.push({
                questionText: q.questionText,
                questionType: q.questionType,
                options: (q.options || []).map((o: any) => ({
                  optionText: o.optionText,
                  isCorrect: o.isCorrect || false,
                  displayOrder: o.displayOrder,
                })),
                correctAnswer: q.correctAnswer || null,
                points: q.points,
                displayOrder: q.displayOrder,
              });
            }
          }

          mapped.standaloneQuestions = allQ;
          this.previewExam.set(mapped);
          this.previewQuestions.set(allQ);
          this.savedExamId.set(saved.id);
          this.savedExamUid.set(saved.uid || null);
        }
        this.viewingHistoryId.set(null);
      },
      error: () => {
        this.viewingHistoryId.set(null);
        this.errorMsg.set('فشل تحميل الامتحان');
      }
    });
  }

  reset() {
    this.selectedGradeLevelId.set(null);
    this.selectedCstId.set(null);
    this.selectedSubjectId.set(null);
    this.selectedSubjectName.set('');
    this.selectedClassName.set('');
    this.title.set('');
    this.durationMinutes.set(60);
    this.totalScore.set(100);
    this.questionCounts.set({ 1: 5, 2: 3, 3: 2, 4: 0 });
    this.topic.set('');
    this.selectedUnitId.set(null);
    this.selectedLessonIds.set(new Set());
    this.units.set([]);
    this.lessons.set([]);
    this.previewExam.set(null);
    this.previewQuestions.set([]);
    this.savedExamId.set(null);
    this.savedExamUid.set(null);
    this.embedMsg.set(null);
    this.embedding.set(false);
    this.viewingHistoryId.set(null);
    this.showConfirm.set(false);
    this.showDeleteConfirm.set(false);
    this.deleteTargetId.set(null);
    this.editMode.set(false);
    this.editData.set([]);
    this.editTitle.set('');
    this.editDuration.set(60);
    this.editTotalScore.set(100);
    this.editCstId.set(null);
    this.editSubjectName.set('');
    this.editSubjectId.set(null);
    this.savedSubjectId.set(null);
    this.savedClassSubjectTeacherId.set(null);
    this.errorMsg.set('');
  }

  dismissError() { this.errorMsg.set(''); }

  // ── Edit Mode Helpers ──

  getEditPoints(i: number): number {
    return this.editData()[i]?.points ?? 0;
  }

  setEditPoints(i: number, val: any) {
    const data = this.editData();
    if (data[i]) data[i].points = +val;
  }

  getEditQuestionText(i: number): string {
    return this.editData()[i]?.questionText ?? '';
  }

  setEditQuestionText(i: number, val: string) {
    const data = this.editData();
    if (data[i]) data[i].questionText = val;
  }

  getEditCorrectAnswer(i: number): string {
    return this.editData()[i]?.correctAnswer ?? '';
  }

  setEditCorrectAnswer(i: number, val: string) {
    const data = this.editData();
    if (data[i]) data[i].correctAnswer = val || null;
  }

  getEditOptionText(i: number, oi: number): string {
    return this.editData()[i]?.options[oi]?.optionText ?? '';
  }

  setEditOptionText(i: number, oi: number, val: string) {
    const data = this.editData();
    if (data[i]?.options[oi]) data[i].options[oi].optionText = val;
  }

  isEditOptCorrect(i: number, oi: number): boolean {
    return this.editData()[i]?.options[oi]?.isCorrect ?? false;
  }

  setEditCorrectOption(i: number, oi: number) {
    const data = this.editData();
    if (!data[i]?.options) return;
    data[i].options.forEach((o, idx) => {
      o.isCorrect = idx === oi;
    });
  }

  // ── Edit Mode ──

  toggleEdit() {
    const next = !this.editMode();
    this.editMode.set(next);
    if (next) {
      const preview = this.previewExam();
      this.editTitle.set(preview?.title ?? '');
      this.editDuration.set(preview?.durationMinutes ?? 60);
      this.editTotalScore.set(preview?.totalScore ?? 100);
      this.editCstId.set(this.savedClassSubjectTeacherId());
      const cstId = this.savedClassSubjectTeacherId();
      const cst = cstId ? this.assignments().find(a => a.id === cstId) : null;
      this.editSubjectName.set(cst?.subjectName ?? '');
      this.editSubjectId.set(cst?.subjectId ?? null);
      this.editData.set(this.previewQuestions().map(q => ({
        questionText: q.questionText,
        points: q.points,
        correctAnswer: q.correctAnswer,
        options: (q.options || []).map(o => ({
          optionText: o.optionText,
          isCorrect: o.isCorrect,
        })),
      })));
    }
  }

  cancelEdit() {
    this.editMode.set(false);
    this.editData.set([]);
    this.editTitle.set('');
    this.editDuration.set(60);
    this.editTotalScore.set(100);
    this.editCstId.set(null);
    this.editSubjectName.set('');
    this.editSubjectId.set(null);
    // Reload from saved/history state
    const savedId = this.savedExamId();
    if (savedId) {
      this.viewHistoryExam(savedId);
    }
  }

  saveEdits() {
    const uid = this.savedExamUid();
    if (!uid) return;

    const preview = this.previewExam();
    if (!preview) return;

    const edited = this.editData();
    const questions = this.previewQuestions().map((q, i) => ({
      id: 0,
      questionText: edited[i]?.questionText || q.questionText,
      points: edited[i]?.points ?? q.points,
      displayOrder: i + 1,
      correctAnswer: edited[i]?.correctAnswer ?? q.correctAnswer,
      options: (edited[i]?.options || q.options || []).map((o, oi) => ({
        id: 0,
        optionText: o.optionText,
        isCorrect: o.isCorrect,
        displayOrder: oi + 1,
      })),
    }));

    const dto = {
      uid: uid,
      title: this.editTitle() || preview.title,
      durationMinutes: this.editDuration() || preview.durationMinutes,
      totalScore: this.editTotalScore() || preview.totalScore,
      classSubjectTeacherId: this.editCstId() ?? null,
      questions: questions,
    };

    this.saving.set(true);
    this.genSvc.saveExistingExam(uid, dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.editMode.set(false);
        this.editData.set([]);
        // Reload
        const savedId = this.savedExamId();
        if (savedId) this.viewHistoryExam(savedId);
        this.errorMsg.set('');
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMsg.set(err?.error?.message || err?.message || 'فشل حفظ التعديلات');
      }
    });
  }
}
