import { Component, inject, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { StudentImportService, ImportedStudent, ClassInfo } from '../../core/services/student-import.service';
import { ClassService } from '../../core/services/class.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-import-students',
  imports: [Sidebar, Topbar],
  templateUrl: './import-students.html',
  styleUrl: './import-students.css'
})
export class ImportStudents {
  private importSvc = inject(StudentImportService);
  private classSvc = inject(ClassService);
  private yearSvc = inject(AcademicYearService);

  sidebarOpen = signal(false);
  students = signal<ImportedStudent[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedYearId = signal<number | null>(null);
  isDragging = signal(false);
  showSuccess = signal(false);
  showError = signal(false);
  errorMessage = signal('');
  isLoading = signal(false);
  classes = signal<ClassInfo[]>([]);
  fileNames = signal<string[]>([]);

  private nextId = 1;

  constructor() {
    this.loadClasses();
  }

  private loadClasses() {
    this.yearSvc.getCurrent().subscribe((res: any) => {
      const yearId = res?.data?.id;
      if (yearId) {
        this.selectedYearId.set(yearId);
        this.classSvc.getAll({ academicYearId: yearId }).subscribe((res2: any) => {
          const list = res2?.data ?? res2 ?? [];
          this.classes.set(list.map((c: any) => ({ id: c.id, name: c.name })));
        });
      }
    });
  }

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(false);
    if (e.dataTransfer?.files?.length) this.parseFiles(e.dataTransfer.files);
  }

  onFileSelected(e: Event) {
    const input = e.target as HTMLInputElement;
    if (input.files?.length) this.parseFiles(input.files);
    input.value = '';
  }

  parseFiles(files: FileList) {
    if (!files.length) return;
    this.showSuccess.set(false);
    this.showError.set(false);
    this.isLoading.set(true);
    this.fileNames.set(Array.from(files).map(f => f.name));

    this.importSvc.preview(files).subscribe({
      next: (res: any) => {
        this.isLoading.set(false);
        const list = res?.data?.students ?? [];
        const imported: ImportedStudent[] = list.map((s: any, i: number) => ({
          id: this.nextId++,
          fullName: s.fullName ?? '',
          nationalId: s.nationalId,
          gender: s.gender,
          birthDate: s.birthDate,
        }));
        this.students.set(imported);
        if (res?.data?.errors?.length) {
          console.warn('تحذيرات:', res.data.errors);
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.showError.set(true);
        this.errorMessage.set('فشل تحليل الملفات. تأكد من صيغ الملفات وحاول مجدداً.');
      }
    });
  }

  removeStudent(id: number) {
    this.students.update(list => list.filter(s => s.id !== id));
  }

  updateStudentName(id: number, name: string) {
    this.students.update(list => list.map(s => s.id === id ? { ...s, fullName: name } : s));
  }

  saveStudents() {
    if (!this.selectedClassId()) {
      this.showError.set(true);
      this.errorMessage.set('يرجى اختيار الفصل قبل الحفظ');
      return;
    }
    this.isLoading.set(true);
    this.importSvc.import(this.students(), this.selectedClassId()!, this.selectedYearId()!).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.showSuccess.set(true);
        this.showError.set(false);
        this.students.set([]);
        this.fileNames.set([]);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.showError.set(true);
        this.errorMessage.set(err?.error?.message || err?.error?.error || 'فشل الحفظ');
      }
    });
  }

  resetAll() {
    this.students.set([]);
    this.selectedClassId.set(null);
    this.selectedYearId.set(null);
    this.showSuccess.set(false);
    this.showError.set(false);
    this.errorMessage.set('');
    this.fileNames.set([]);
  }
}
