import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { BookParserService, ParsedUnitDto, CreateUnitDto, ParsedLessonDto } from '../../core/services/book-parser.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-book-parser',
  standalone: true,
  imports: [Sidebar, CommonModule, FormsModule],
  templateUrl: './book-parser.html',
  styleUrl: './book-parser.css',
})
export class BookParser {
  sidebarOpen = signal(false);
  private service = inject(BookParserService);
  private academicYearService = inject(AcademicYearService);

  gradeLevels = signal<any[]>([]);
  selectedGradeLevelId = signal<number | null>(null);
  subjects = signal<any[]>([]);
  selectedSubjectId = signal<number | null>(null);
  selectedFile = signal<File | null>(null);

  step = signal<'upload' | 'preview' | 'saved'>('upload');

  parsedUnits = signal<ParsedUnitDto[]>([]);
  previewId = signal<string | null>(null);
  totalLessons = signal(0);
  loadingPreview = signal(false);
  saving = signal(false);
  expandedUnit = signal<number | null>(null);

  toastMessage = signal('');
  toastType = signal<'success' | 'error'>('success');

  parsedSubjects = signal<any[]>([]);
  loadingSubjects = signal(false);
  expandedSubjectId = signal<number | null>(null);
  subjectUnits = signal<any[]>([]);
  loadingSubjectUnits = signal(false);
  editingSavedUnitId = signal<number | null>(null);
  editingSavedUnitName = signal('');
  editingSavedLessonId = signal<number | null>(null);
  editingSavedLessonTitle = signal('');
  editingSavedUnitContent = signal<number | null>(null); // unitId being edited

  fileName = signal('');

  // Lesson Content Modal State
  selectedLesson = signal<ParsedLessonDto | null>(null);
  selectedLessonUnitIndex = signal<number | null>(null);
  selectedLessonIndex = signal<number | null>(null);
  showLessonModal = signal(false);
  generatingContent = signal(false);

  // Inline editing state
  editingUnitIndex = signal<number | null>(null);
  editingUnitName = signal('');
  editingLessonKey = signal<string | null>(null); // "unitIdx-lessonIdx"
  editingLessonTitle = signal('');

  // Inline page range editing state (combined "start - end")
  editingLessonRange = signal<{ unitIdx: number; lessonIdx: number } | null>(null);
  editingLessonRangeValue = signal<string>('');

  // Unit page range editing state
  editingUnitRange = signal<number | null>(null); // unit index being edited
  editingUnitRangeValue = signal<string>('');

  // Unit content editing in preview
  editingPreviewUnitContent = signal<number | null>(null); // unitIdx
  cleaningUnitContent = signal<number | null>(null); // unitIdx being AI-cleaned

  // Move lesson state
  moveLessonSrc = signal<{ unitIdx: number; lessonIdx: number } | null>(null);

  // Term filter for saved subjects view
  selectedTerm = signal<number>(1);

  subjectsGroupedByGrade = computed(() => {
    const subjects = this.parsedSubjects();
    const groups = new Map<string, typeof subjects>();
    for (const s of subjects) {
      const key = s.gradeLevelName || 'بدون صف دراسي';
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(s);
    }
    return Array.from(groups.entries()).sort(([a], [b]) => {
      const order = ['الأول', 'الثاني', 'الثالث', 'الرابع', 'الخامس', 'السادس'];
      const ai = order.findIndex(o => a.includes(o));
      const bi = order.findIndex(o => b.includes(o));
      return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi);
    });
  });

  formattedLessonContent = computed(() => this.formatMarkdown(this.selectedLesson()?.content));

  ngOnInit() {
    this.loadGradeLevels();
    this.loadCurrentTerm();
    this.loadParsedSubjects();
  }

  loadCurrentTerm() {
    this.academicYearService.getCurrentTerm().subscribe({
      next: (res) => {
        if (res?.data != null) this.selectedTerm.set(res.data);
      }
    });
  }

  onTermChange() {
    this.loadParsedSubjects();
    // Also reload subject structure if a subject is expanded
    const subjectId = this.expandedSubjectId();
    if (subjectId != null) {
      this.loadSubjectStructure(subjectId);
    }
  }

  loadParsedSubjects() {
    this.loadingSubjects.set(true);
    this.service.getParsedSubjects(this.selectedTerm()).subscribe({
      next: (res) => {
        this.parsedSubjects.set(res?.data ?? []);
        this.loadingSubjects.set(false);
      },
      error: () => this.loadingSubjects.set(false)
    });
  }

  loadSubjectStructure(subjectId: number) {
    this.loadingSubjectUnits.set(true);
    this.subjectUnits.set([]);
    this.service.getSubjectStructure(subjectId, this.selectedTerm()).subscribe({
      next: (res) => {
        this.subjectUnits.set(res?.data ?? []);
        this.loadingSubjectUnits.set(false);
      },
      error: () => this.loadingSubjectUnits.set(false)
    });
  }

  // ── Saved subjects inline editing ──
  startEditSavedUnit(unitId: number, name: string) {
    this.editingSavedUnitId.set(unitId);
    this.editingSavedUnitName.set(name);
  }

  saveSavedUnitName(unitId: number) {
    const name = this.editingSavedUnitName().trim();
    if (!name) { this.editingSavedUnitId.set(null); return; }
    this.service.updateUnit(unitId, name).subscribe({
      next: () => {
        const units = this.subjectUnits().map(u => u.id === unitId ? { ...u, name } : u);
        this.subjectUnits.set(units);
        this.showToast('تم تعديل اسم الوحدة', 'success');
      },
      error: () => this.showToast('فشل تعديل اسم الوحدة', 'error')
    });
    this.editingSavedUnitId.set(null);
  }

  cancelEditSavedUnit() {
    this.editingSavedUnitId.set(null);
    this.editingSavedUnitName.set('');
  }

  startEditSavedLesson(lessonId: number, title: string) {
    this.editingSavedLessonId.set(lessonId);
    this.editingSavedLessonTitle.set(title);
  }

  saveSavedLessonName(lessonId: number) {
    const title = this.editingSavedLessonTitle().trim();
    if (!title) { this.editingSavedLessonId.set(null); return; }
    this.service.updateLesson(lessonId, title).subscribe({
      next: () => {
        const units = this.subjectUnits().map(u => ({
          ...u,
          lessons: u.lessons?.map((l: any) => l.id === lessonId ? { ...l, title } : l)
        }));
        this.subjectUnits.set(units);
        this.showToast('تم تعديل اسم الدرس', 'success');
      },
      error: () => this.showToast('فشل تعديل اسم الدرس', 'error')
    });
    this.editingSavedLessonId.set(null);
  }

  cancelEditSavedLesson() {
    this.editingSavedLessonId.set(null);
    this.editingSavedLessonTitle.set('');
  }

  saveSavedUnitContent(unitId: number, name: string, content: string) {
    this.service.updateUnit(unitId, name, content).subscribe({
      next: () => {
        const units = this.subjectUnits().map(u => u.id === unitId ? { ...u, content } : u);
        this.subjectUnits.set(units);
        this.editingSavedUnitContent.set(null);
        this.showToast('تم حفظ محتوى الوحدة', 'success');
      },
      error: () => this.showToast('فشل حفظ المحتوى', 'error')
    });
  }

  addSavedUnit() {
    const subjectId = this.expandedSubjectId();
    if (!subjectId) return;
    const order = this.subjectUnits().length + 1;
    this.service.createUnit(subjectId, { name: `وحدة جديدة ${order}`, displayOrder: order }).subscribe({
      next: (res) => {
        const unit = res?.data;
        if (unit) this.subjectUnits.set([...this.subjectUnits(), { ...unit, lessons: [] }]);
        this.showToast('تم إضافة الوحدة', 'success');
      },
      error: () => this.showToast('فشل إضافة الوحدة', 'error')
    });
  }

  addSavedLesson(unitId: number) {
    const units = this.subjectUnits();
    const unit = units.find(u => u.id === unitId);
    const order = (unit?.lessons?.length || 0) + 1;
    this.service.createLesson(unitId, { title: `درس جديد ${order}`, displayOrder: order }).subscribe({
      next: (res) => {
        const lesson = res?.data;
        if (lesson) {
          const updated = units.map(u => u.id === unitId ? { ...u, lessons: [...(u.lessons || []), lesson] } : u);
          this.subjectUnits.set(updated);
        }
        this.showToast('تم إضافة الدرس', 'success');
      },
      error: () => this.showToast('فشل إضافة الدرس', 'error')
    });
  }

  deleteSavedUnit(unitId: number) {
    if (!confirm('هل أنت متأكد من حذف هذه الوحدة وجميع دروسها؟')) return;
    this.service.deleteUnit(unitId).subscribe({
      next: () => {
        this.subjectUnits.set(this.subjectUnits().filter(u => u.id !== unitId));
        this.showToast('تم حذف الوحدة', 'success');
      },
      error: () => this.showToast('فشل حذف الوحدة', 'error')
    });
  }

  deleteSavedLesson(lessonId: number) {
    if (!confirm('هل أنت متأكد من حذف هذا الدرس؟')) return;
    this.service.deleteLesson(lessonId).subscribe({
      next: () => {
        const units = this.subjectUnits().map(u => ({
          ...u,
          lessons: u.lessons?.filter((l: any) => l.id !== lessonId)
        }));
        this.subjectUnits.set(units);
        this.showToast('تم حذف الدرس', 'success');
      },
      error: () => this.showToast('فشل حذف الدرس', 'error')
    });
  }

  toggleSubject(subjectId: number) {
    if (this.expandedSubjectId() === subjectId) {
      this.expandedSubjectId.set(null);
      this.subjectUnits.set([]);
      return;
    }
    this.expandedSubjectId.set(subjectId);
    this.loadSubjectStructure(subjectId);
  }

  loadGradeLevels() {
    this.service.getGradeLevels().subscribe({
      next: (res) => {
        const data = res?.data ?? (Array.isArray(res) ? res : []);
        this.gradeLevels.set(data);
      },
      error: (err) => console.error('Error fetching grade levels', err)
    });
  }

  onGradeLevelChange() {
    const gradeLevelId = this.selectedGradeLevelId();
    this.selectedSubjectId.set(null);
    this.subjects.set([]);
    if (!gradeLevelId) return;
    this.service.getSubjects(gradeLevelId).subscribe({
      next: (res) => {
        const data = res?.data ?? (Array.isArray(res) ? res : []);
        this.subjects.set(data);
      },
      error: (err) => console.error('Error fetching subjects', err)
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files?.[0];
    if (file) {
      this.selectedFile.set(file);
      this.fileName.set(file.name);
      this.step.set('upload');
    }
  }

  preview() {
    const file = this.selectedFile();
    if (!file) return;

    this.loadingPreview.set(true);
    this.parsedUnits.set([]);
    this.previewId.set(null);
    this.service.preview(file).subscribe({
      next: (res) => {
        const data = res.data ?? res;
        const rawUnits = data?.units ?? (Array.isArray(data) ? data : []);
        const units = Array.isArray(rawUnits) ? rawUnits.map((u: any, i: number) => ({ ...u, displayOrder: u.displayOrder || i + 1 })) : [];
        this.parsedUnits.set(units);
        this.previewId.set(data?.previewId ?? null);
        this.recalcTotals();
        this.step.set('preview');
        this.loadingPreview.set(false);
      },
      error: (err) => {
        const msg = err.error?.message || err.error?.error || 'حدث خطأ في الاتصال';
        this.showToast(msg, 'error');
        this.loadingPreview.set(false);
      }
    });
  }

  recalcTotals() {
    const units = this.parsedUnits();
    this.totalLessons.set(Array.isArray(units) ? units.reduce((a, u) => a + (u.lessons?.length || 0), 0) : 0);
  }

  save() {
    const subjectId = this.selectedSubjectId();
    const gradeLevelId = this.selectedGradeLevelId();
    if (!subjectId) {
      this.showToast('اختر المادة أولاً', 'error');
      return;
    }
    if (!gradeLevelId) {
      this.showToast('اختر الصف الدراسي أولاً', 'error');
      return;
    }

    this.saving.set(true);
    const units: CreateUnitDto[] = this.parsedUnits().map(u => ({
      gradeLevelId: gradeLevelId,
      name: u.name,
      content: u.content || '',
      pageStart: u.pageStart,
      pageEnd: u.pageEnd,
      displayOrder: u.displayOrder,
      term: this.selectedTerm(),
      lessons: (u.lessons || []).map(l => ({
        title: l.title,
        content: l.content,
        pageStart: l.pageStart,
        pageEnd: l.pageEnd,
        displayOrder: l.displayOrder,
      }))
    }));

    this.service.save(subjectId, gradeLevelId, units, this.selectedTerm()).subscribe({
      next: () => {
        this.step.set('saved');
        this.showToast('تم حفظ هيكل الكتاب بنجاح!', 'success');
        this.saving.set(false);
        this.loadParsedSubjects();
      },
      error: (err) => {
        const msg = err.error?.message || err.error?.error || 'حدث خطأ في الاتصال';
        this.showToast(msg, 'error');
        this.saving.set(false);
      }
    });
  }

  reset() {
    this.step.set('upload');
    this.selectedFile.set(null);
    this.fileName.set('');
    this.parsedUnits.set([]);
    this.selectedGradeLevelId.set(null);
    this.selectedSubjectId.set(null);
    this.subjects.set([]);
  }

  toggleUnit(index: number) {
    this.expandedUnit.set(this.expandedUnit() === index ? null : index);
  }

  // ── Inline Editing: Unit Name ──
  startEditUnitName(index: number) {
    this.editingUnitIndex.set(index);
    this.editingUnitName.set(this.parsedUnits()[index]?.name || '');
  }

  saveUnitName(index: number) {
    const name = this.editingUnitName().trim();
    if (name) {
      const units = [...this.parsedUnits()];
      units[index] = { ...units[index], name };
      this.parsedUnits.set(units);
    }
    this.editingUnitIndex.set(null);
    this.editingUnitName.set('');
  }

  cancelEditUnitName() {
    this.editingUnitIndex.set(null);
    this.editingUnitName.set('');
  }

  // ── Inline Editing: Lesson Title ──
  startEditLessonTitle(unitIdx: number, lessonIdx: number) {
    const lesson = this.parsedUnits()[unitIdx]?.lessons?.[lessonIdx];
    if (!lesson) return;
    this.editingLessonKey.set(`${unitIdx}-${lessonIdx}`);
    this.editingLessonTitle.set(lesson.title);
  }

  saveLessonTitle(unitIdx: number, lessonIdx: number) {
    const title = this.editingLessonTitle().trim();
    if (title) {
      const units = [...this.parsedUnits()];
      const lessons = [...(units[unitIdx].lessons || [])];
      lessons[lessonIdx] = { ...lessons[lessonIdx], title };
      units[unitIdx] = { ...units[unitIdx], lessons };
      this.parsedUnits.set(units);
    }
    this.editingLessonKey.set(null);
    this.editingLessonTitle.set('');
  }

  cancelEditLessonTitle() {
    this.editingLessonKey.set(null);
    this.editingLessonTitle.set('');
  }

  // ── Inline Editing: Lesson Page Range (combined "start-end") ──
  startEditLessonRange(unitIdx: number, lessonIdx: number) {
    const lesson = this.parsedUnits()[unitIdx]?.lessons?.[lessonIdx];
    if (!lesson) return;
    const val = [lesson.pageStart, lesson.pageEnd].filter(v => v != null).join(' - ');
    this.editingLessonRange.set({ unitIdx, lessonIdx });
    this.editingLessonRangeValue.set(val);
  }

  saveLessonRange(unitIdx: number, lessonIdx: number) {
    const raw = this.editingLessonRangeValue();
    if (!raw.trim()) {
      this.editingLessonRange.set(null);
      return;
    }
    this.editingLessonRangeValue.set('');
    const parts = raw.split(/[-\u2013\u2014]/).map(s => s.trim());
    const toNum = (s: string) => { const n = parseInt(s, 10); return isNaN(n) ? null : n; };
    const pageStart = toNum(parts[0]) ?? null;
    const pageEnd = parts.length > 1 ? (toNum(parts[1]) ?? null) : null;
    const units = [...this.parsedUnits()];
    const lessons = [...(units[unitIdx].lessons || [])];
    const lesson = lessons[lessonIdx];
    lessons[lessonIdx] = { ...lesson, pageStart, pageEnd };
    units[unitIdx] = { ...units[unitIdx], lessons };
    this.parsedUnits.set(units);
    this.editingLessonRange.set(null);

    // Auto re-extract content from OCR based on new page range
    const pid = this.previewId();
    if (pid && pageStart && lesson.title) {
      this.service.reExtractLessonContent(pid, lesson.title, pageStart, pageEnd).subscribe({
        next: (res) => {
          const content = typeof res === 'string' ? res : (res?.data ?? '');
          if (content) {
            const updatedUnits = [...this.parsedUnits()];
            const updatedLessons = [...(updatedUnits[unitIdx].lessons || [])];
            updatedLessons[lessonIdx] = { ...updatedLessons[lessonIdx], content };
            updatedUnits[unitIdx] = { ...updatedUnits[unitIdx], lessons: updatedLessons };
            this.parsedUnits.set(updatedUnits);
          }
        }
      });
    }
  }

  cancelEditLessonRange() {
    this.editingLessonRange.set(null);
    this.editingLessonRangeValue.set('');
  }

  startEditUnitRange(unitIdx: number) {
    const unit = this.parsedUnits()[unitIdx];
    this.editingUnitRangeValue.set(unit.pageStart + (unit.pageEnd ? ` - ${unit.pageEnd}` : ''));
    this.editingUnitRange.set(unitIdx);
  }

  saveUnitRange(unitIdx: number) {
    const raw = this.editingUnitRangeValue();
    if (!raw.trim()) {
      this.editingUnitRange.set(null);
      return;
    }
    this.editingUnitRangeValue.set('');
    const parts = raw.split(/[-\u2013\u2014]/).map(s => s.trim());
    const toNum = (s: string) => { const n = parseInt(s, 10); return isNaN(n) ? null : n; };
    const pageStart = toNum(parts[0]) ?? null;
    const pageEnd = parts.length > 1 ? (toNum(parts[1]) ?? null) : null;
    const units = [...this.parsedUnits()];
    units[unitIdx] = { ...units[unitIdx], pageStart, pageEnd };
    this.parsedUnits.set(units);
    this.editingUnitRange.set(null);

    // Auto re-extract content from OCR
    const pid = this.previewId();
    if (pid && pageStart) {
      const unitName = units[unitIdx].name;
      this.service.reExtractUnitContent(pid, unitName, pageStart, pageEnd).subscribe({
        next: (res) => {
          const content = typeof res === 'string' ? res : (res?.data ?? '');
          if (content) {
            const updatedUnits = [...this.parsedUnits()];
            updatedUnits[unitIdx] = { ...updatedUnits[unitIdx], content };
            this.parsedUnits.set(updatedUnits);
          }
        }
      });
    }
  }

  cancelEditUnitRange() {
    this.editingUnitRange.set(null);
    this.editingUnitRangeValue.set('');
  }

  // ── Edit Unit Content (English books) ──
  editUnitContent(unitIdx: number, content: string) {
    const units = [...this.parsedUnits()];
    units[unitIdx] = { ...units[unitIdx], content };
    this.parsedUnits.set(units);
    this.editingPreviewUnitContent.set(null);
  }

  cleanUnitContentWithAi(unitIdx: number) {
    const unit = this.parsedUnits()[unitIdx];
    if (!unit?.content) return;
    this.cleaningUnitContent.set(unitIdx);
    this.service.generateLessonContent(unit.content, unit.name).subscribe({
      next: (res) => {
        const clean = typeof res === 'string' ? res : (res?.data ?? '');
        if (clean) {
          const units = [...this.parsedUnits()];
          units[unitIdx] = { ...units[unitIdx], content: clean };
          this.parsedUnits.set(units);
        }
        this.cleaningUnitContent.set(null);
      },
      error: () => {
        this.cleaningUnitContent.set(null);
        this.showToast('فشل تنظيف المحتوى بالذكاء الاصطناعي', 'error');
      }
    });
  }

  // ── Move Lesson ──
  startMoveLesson(unitIdx: number, lessonIdx: number) {
    this.moveLessonSrc.set({ unitIdx, lessonIdx });
  }

  cancelMoveLesson() {
    this.moveLessonSrc.set(null);
  }

  moveLessonTo(targetUnitIdx: number) {
    const src = this.moveLessonSrc();
    if (!src) return;
    if (src.unitIdx === targetUnitIdx) { this.moveLessonSrc.set(null); return; }

    const units = this.parsedUnits().map(u => ({ ...u, lessons: [...(u.lessons || [])] }));
    const [lesson] = units[src.unitIdx].lessons.splice(src.lessonIdx, 1);
    units[targetUnitIdx].lessons.push(lesson);

    // Fix display orders
    units[src.unitIdx].lessons.forEach((l, i) => l.displayOrder = i + 1);
    units[targetUnitIdx].lessons.forEach((l, i) => l.displayOrder = i + 1);

    this.parsedUnits.set(units);
    this.moveLessonSrc.set(null);
    this.recalcTotals();
  }

  // ── Add/Delete ──
  addUnit() {
    const units = [...this.parsedUnits()];
    const order = units.length + 1;
    units.push({
      name: `وحدة جديدة ${order}`,
      content: '',
      pageStart: null,
      pageEnd: null,
      displayOrder: order,
      lessons: []
    });
    this.parsedUnits.set(units);
    this.expandedUnit.set(units.length - 1);
    this.recalcTotals();
  }

  addLesson(unitIdx: number) {
    const units = [...this.parsedUnits()];
    const unit = { ...units[unitIdx], lessons: [...(units[unitIdx].lessons || [])] };
    const order = unit.lessons.length + 1;
    unit.lessons.push({
      title: `درس جديد ${order}`,
      content: '',
      pageStart: null,
      pageEnd: null,
      displayOrder: order
    });
    units[unitIdx] = unit;
    this.parsedUnits.set(units);
    this.expandedUnit.set(unitIdx);
    this.recalcTotals();
  }

  deleteUnit(index: number) {
    const units = this.parsedUnits().filter((_, i) => i !== index).map((u, i) => ({ ...u, displayOrder: i + 1 }));
    this.parsedUnits.set(units);
    if (this.expandedUnit() === index) this.expandedUnit.set(null);
    this.recalcTotals();
  }

  deleteLesson(unitIdx: number, lessonIdx: number) {
    const units = [...this.parsedUnits()];
    const lessons = units[unitIdx].lessons.filter((_, i) => i !== lessonIdx).map((l, i) => ({ ...l, displayOrder: i + 1 }));
    units[unitIdx] = { ...units[unitIdx], lessons };
    this.parsedUnits.set(units);
    this.recalcTotals();
  }

  // ── Lesson Content Modal ──
  openLessonModal(unitIndex: number, lessonIndex: number) {
    const unit = this.parsedUnits()[unitIndex];
    const lesson = unit.lessons[lessonIndex];
    this.selectedLesson.set({ ...lesson });
    this.selectedLessonUnitIndex.set(unitIndex);
    this.selectedLessonIndex.set(lessonIndex);
    this.showLessonModal.set(true);
  }

  openSavedUnitContent(unit: any) {
    this.selectedLesson.set({ title: unit.name, content: unit.content, pageStart: unit.pageStart, pageEnd: unit.pageEnd } as any);
    this.selectedLessonUnitIndex.set(null);
    this.selectedLessonIndex.set(null);
    this.showLessonModal.set(true);
  }

  openSavedLesson(lesson: any) {
    this.selectedLesson.set({ ...lesson });
    this.selectedLessonUnitIndex.set(null);
    this.selectedLessonIndex.set(null);
    this.showLessonModal.set(true);
  }

  closeLessonModal() {
    this.showLessonModal.set(false);
    this.selectedLesson.set(null);
    this.selectedLessonUnitIndex.set(null);
    this.selectedLessonIndex.set(null);
  }

  generateLessonContent() {
    const lesson = this.selectedLesson();
    if (!lesson || !lesson.content) {
      this.showToast('لا يوجد محتوى خام لهذا الدرس لمعالجته.', 'error');
      return;
    }

    this.generatingContent.set(true);
    this.service.generateLessonContent(lesson.content, lesson.title).subscribe({
      next: (res) => {
        const content = typeof res === 'string' ? res : (res?.data ?? '');
        const updatedLesson = { ...lesson, content };
        this.selectedLesson.set(updatedLesson);

        const uIdx = this.selectedLessonUnitIndex()!;
        const lIdx = this.selectedLessonIndex()!;
        const currentUnits = [...this.parsedUnits()];
        currentUnits[uIdx].lessons[lIdx] = updatedLesson;
        this.parsedUnits.set(currentUnits);

        this.showToast('تم تنسيق محتوى الدرس بنجاح!', 'success');
        this.generatingContent.set(false);
      },
      error: (err) => {
        const msg = err.error?.message || err.error?.error || 'حدث خطأ في الاتصال';
        this.showToast(msg, 'error');
        this.generatingContent.set(false);
      }
    });
  }

  // ── Reorder ──
  moveUnit(fromIdx: number, toIdx: number) {
    if (toIdx < 0 || toIdx >= this.parsedUnits().length) return;
    const units = [...this.parsedUnits()];
    const [moved] = units.splice(fromIdx, 1);
    units.splice(toIdx, 0, moved);
    units.forEach((u, i) => u.displayOrder = i + 1);
    this.parsedUnits.set(units);
  }

  moveLessonInUnit(unitIdx: number, fromIdx: number, toIdx: number) {
    const units = [...this.parsedUnits()];
    const lessons = [...(units[unitIdx].lessons || [])];
    if (toIdx < 0 || toIdx >= lessons.length) return;
    const [moved] = lessons.splice(fromIdx, 1);
    lessons.splice(toIdx, 0, moved);
    lessons.forEach((l, i) => l.displayOrder = i + 1);
    units[unitIdx] = { ...units[unitIdx], lessons };
    this.parsedUnits.set(units);
  }

  showToast(message: string, type: 'success' | 'error' = 'success') {
    this.toastMessage.set(message);
    this.toastType.set(type);
    setTimeout(() => this.toastMessage.set(''), 4000);
  }

  formatMarkdown(text: string | undefined): string {
    if (!text) return '';
    let html = text;

    html = html.replace(/^# (.*$)/gim, '<h1 class="text-2xl font-bold mt-6 mb-4 text-primary">$1</h1>');
    html = html.replace(/^## (.*$)/gim, '<h2 class="text-xl font-bold mt-5 mb-3 text-secondary">$1</h2>');
    html = html.replace(/^### (.*$)/gim, '<h3 class="text-lg font-bold mt-4 mb-2 text-on-surface">$1</h3>');
    html = html.replace(/^#### (.*$)/gim, '<h4 class="text-base font-bold mt-3 mb-2 text-on-surface-variant">$1</h4>');
    html = html.replace(/\*\*(.*?)\*\*/gim, '<strong>$1</strong>');
    html = html.replace(/\*(.*?)\*/gim, '<em>$1</em>');
    html = html.replace(/^\- (.*$)/gim, '<li class="ml-6 mb-1 list-disc" style="margin-right: 1.5rem;">$1</li>');
    html = html.replace(/^\d+\. (.*$)/gim, '<li class="ml-6 mb-1 list-decimal" style="margin-right: 1.5rem;">$1</li>');
    html = html.replace(/^\> (.*$)/gim, '<blockquote class="border-r-4 border-primary bg-surface-container-lowest py-2 pr-4 pl-2 my-4 italic text-on-surface-variant">$1</blockquote>');
    html = html.replace(/\n\n/gim, '<br><br>');
    html = html.replace(/\n/gim, '<br>');

    return html;
  }
}
