import { Component, signal, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ExamGeneratorService, AiGenerateExamRequest, AiExamPreviewDto, AiExamPreviewQuestionDto, ExamSummaryDto } from '../../core/services/exam-generator.service';

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
  name: string;
  content?: string;
  displayOrder: number;
  pageStart?: number;
  pageEnd?: number;
  subjectName?: string;
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

  sidebarOpen = signal(false);
  loading = signal(false);
  generating = signal(false);
  saving = signal(false);
  errorMsg = signal('');

  assignments = signal<ClassSubjectTeacherDto[]>([]);
  units = signal<UnitDto[]>([]);
  lessons = signal<LessonDto[]>([]);
  history = signal<ExamSummaryDto[]>([]);

  selectedCstId = signal<number | null>(null);
  selectedSubjectId = signal<number | null>(null);
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
  viewingHistoryId = signal<number | null>(null);

  showConfirm = signal(false);

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

  ngOnInit() {
    this.loadMyAssignments();
    this.loadHistory();
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

  onCstChange() {
    const cstId = this.selectedCstId();
    if (!cstId) {
      this.selectedSubjectId.set(null);
      this.units.set([]);
      this.lessons.set([]);
      this.selectedUnitId.set(null);
      this.selectedLessonIds.set(new Set());
      return;
    }
    const cst = this.assignments().find(a => a.id === cstId);
    if (cst) {
      this.selectedSubjectId.set(cst.subjectId);
      this.loadUnits(cst.subjectId);
    }
  }

  private loadUnits(subjectId: number) {
    this.genSvc.getUnitsWithLessons(subjectId).subscribe({
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
    const cstId = this.selectedCstId();
    if (!cstId) { this.errorMsg.set('اختر المادة والفصل'); return; }
    const totalQ = this.totalQuestionCount();
    if (!totalQ) { this.errorMsg.set('اختر عدداً من الأسئلة'); return; }

    this.generating.set(true);
    this.errorMsg.set('');
    this.previewExam.set(null);
    this.previewQuestions.set([]);
    this.savedExamId.set(null);
    this.showConfirm.set(false);

    const body: AiGenerateExamRequest = {
      classSubjectTeacherId: cstId,
      title: this.title() || `امتحان ${this.assignments().find(a => a.id === cstId)?.subjectName ?? ''}`,
      durationMinutes: this.durationMinutes() || undefined,
      totalScore: this.totalScore(),
      category: 1,
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

    const saveBody: any = {
      classSubjectTeacherId: this.selectedCstId(),
      title: preview.title,
      durationMinutes: preview.durationMinutes,
      totalScore: preview.totalScore,
      category: 1,
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

  viewHistoryExam(id: number) {
    this.viewingHistoryId.set(id);
    this.generating.set(false);
    this.saving.set(false);
    this.previewExam.set(null);
    this.previewQuestions.set([]);
    this.savedExamId.set(null);
    this.showConfirm.set(false);

    this.genSvc.getExamById(id).subscribe({
      next: (r: any) => {
        const saved = r?.data;
        if (saved) {
          const mapped: AiExamPreviewDto = {
            subjectName: saved.subjectName || '',
            className: saved.className || '',
            teacherName: saved.teacherName || '',
            title: saved.title,
            durationMinutes: saved.durationMinutes,
            totalScore: saved.totalScore,
            questionsCount: saved.questionsCount || 0,
            standaloneQuestions: [],
          };

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
                      isCorrect: false,
                      displayOrder: o.displayOrder,
                    })),
                    correctAnswer: null,
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
                  isCorrect: false,
                  displayOrder: o.displayOrder,
                })),
                correctAnswer: null,
                points: q.points,
                displayOrder: q.displayOrder,
              });
            }
          }

          mapped.standaloneQuestions = allQ;
          this.previewExam.set(mapped);
          this.previewQuestions.set(allQ);
          this.savedExamId.set(saved.id);
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
    this.selectedCstId.set(null);
    this.selectedSubjectId.set(null);
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
    this.viewingHistoryId.set(null);
    this.showConfirm.set(false);
    this.errorMsg.set('');
  }

  dismissError() { this.errorMsg.set(''); }
}
