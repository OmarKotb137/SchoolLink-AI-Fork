import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { BookParserService, ParsedUnitDto, CreateUnitDto, ParsedLessonDto } from '../../core/services/book-parser.service';

@Component({
  selector: 'app-book-parser',
  standalone: true,
  imports: [Sidebar, Topbar, CommonModule, FormsModule],
  templateUrl: './book-parser.html',
  styleUrl: './book-parser.css',
})
export class BookParser {
  sidebarOpen = signal(false);
  private service = inject(BookParserService);

  gradeLevels = signal<any[]>([]);
  selectedGradeLevelId = signal<number | null>(null);
  subjects = signal<any[]>([]);
  selectedSubjectId = signal<number | null>(null);
  selectedFile = signal<File | null>(null);

  step = signal<'upload' | 'preview' | 'saved'>('upload');

  parsedUnits = signal<ParsedUnitDto[]>([]);
  totalLessons = signal(0);
  loadingPreview = signal(false);
  saving = signal(false);
  expandedUnit = signal<number | null>(null);

  toastMessage = signal('');
  toastType = signal<'success' | 'error'>('success');

  fileName = signal('');

  // Lesson Content Modal State
  selectedLesson = signal<ParsedLessonDto | null>(null);
  selectedLessonUnitIndex = signal<number | null>(null);
  selectedLessonIndex = signal<number | null>(null);
  showLessonModal = signal(false);
  generatingContent = signal(false);

  formattedLessonContent = computed(() => this.formatMarkdown(this.selectedLesson()?.content));

  ngOnInit() {
    this.loadGradeLevels();
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
    this.service.preview(file).subscribe({
      next: (res) => {
        this.parsedUnits.set(res);
        this.totalLessons.set(res.reduce((a, u) => a + u.lessons.length, 0));
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

  save() {
    const subjectId = this.selectedSubjectId();
    if (!subjectId) {
      this.showToast('اختر المادة أولاً', 'error');
      return;
    }

    this.saving.set(true);
    const units: CreateUnitDto[] = this.parsedUnits().map(u => ({
      name: u.name,
      content: u.content,
      pageStart: u.pageStart,
      pageEnd: u.pageEnd,
      displayOrder: u.displayOrder,
      lessons: u.lessons.map(l => ({
        title: l.title,
        pageStart: l.pageStart,
        pageEnd: l.pageEnd,
        displayOrder: l.displayOrder,
      }))
    }));

    this.service.save(subjectId, units).subscribe({
      next: () => {
        this.step.set('saved');
        this.showToast('تم حفظ هيكل الكتاب بنجاح!', 'success');
        this.saving.set(false);
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

  openLessonModal(unitIndex: number, lessonIndex: number) {
    const unit = this.parsedUnits()[unitIndex];
    const lesson = unit.lessons[lessonIndex];
    this.selectedLesson.set({ ...lesson });
    this.selectedLessonUnitIndex.set(unitIndex);
    this.selectedLessonIndex.set(lessonIndex);
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
        const updatedLesson = { ...lesson, content: res };
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

  showToast(message: string, type: 'success' | 'error' = 'success') {
    this.toastMessage.set(message);
    this.toastType.set(type);
    setTimeout(() => this.toastMessage.set(''), 4000);
  }

  formatMarkdown(text: string | undefined): string {
    if (!text) return '';
    let html = text;

    // Headings
    html = html.replace(/^# (.*$)/gim, '<h1 class="text-2xl font-bold mt-6 mb-4 text-primary">$1</h1>');
    html = html.replace(/^## (.*$)/gim, '<h2 class="text-xl font-bold mt-5 mb-3 text-secondary">$1</h2>');
    html = html.replace(/^### (.*$)/gim, '<h3 class="text-lg font-bold mt-4 mb-2 text-on-surface">$1</h3>');
    html = html.replace(/^#### (.*$)/gim, '<h4 class="text-base font-bold mt-3 mb-2 text-on-surface-variant">$1</h4>');

    // Bold
    html = html.replace(/\*\*(.*?)\*\*/gim, '<strong>$1</strong>');
    
    // Italic
    html = html.replace(/\*(.*?)\*/gim, '<em>$1</em>');
    
    // Lists (unordered)
    html = html.replace(/^\- (.*$)/gim, '<li class="ml-6 mb-1 list-disc" style="margin-right: 1.5rem;">$1</li>');
    
    // Lists (ordered)
    html = html.replace(/^\d+\. (.*$)/gim, '<li class="ml-6 mb-1 list-decimal" style="margin-right: 1.5rem;">$1</li>');
    
    // Blockquotes
    html = html.replace(/^\> (.*$)/gim, '<blockquote class="border-r-4 border-primary bg-surface-container-lowest py-2 pr-4 pl-2 my-4 italic text-on-surface-variant">$1</blockquote>');

    // Line breaks
    html = html.replace(/\n\n/gim, '<br><br>');
    html = html.replace(/\n/gim, '<br>');

    return html;
  }
}