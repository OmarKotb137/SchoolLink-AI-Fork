import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { BookParserService, ParsedUnitDto, CreateUnitDto } from '../../core/services/book-parser.service';

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
        if (res.isSuccess) {
          this.parsedUnits.set(res.data);
          this.totalLessons.set(res.data.reduce((a, u) => a + u.lessons.length, 0));
          this.step.set('preview');
        } else {
          this.showToast(res.message || 'فشل تحليل الكتاب', 'error');
        }
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
      next: (res) => {
        if (res.isSuccess) {
          this.step.set('saved');
          this.showToast('تم حفظ هيكل الكتاب بنجاح!', 'success');
        } else {
          this.showToast(res.message || 'فشل حفظ الكتاب', 'error');
        }
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

  showToast(message: string, type: 'success' | 'error' = 'success') {
    this.toastMessage.set(message);
    this.toastType.set(type);
    setTimeout(() => this.toastMessage.set(''), 4000);
  }
}