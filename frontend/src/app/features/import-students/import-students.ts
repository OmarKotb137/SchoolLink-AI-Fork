import { Component, inject, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentImportService, ImportedStudent } from '../../core/services/student-import.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-import-students',
  imports: [Sidebar],
  templateUrl: './import-students.html',
  styleUrl: './import-students.css'
})
export class ImportStudents {
  private importSvc = inject(StudentImportService);
  private yearSvc = inject(AcademicYearService);

  sidebarOpen = signal(false);
  students = signal<ImportedStudent[]>([]);
  selectedYearId = signal<number | null>(null);
  isDragging = signal(false);
  showSuccess = signal(false);
  showError = signal(false);
  errorMessage = signal('');
  isLoading = signal(false);
  fileNames = signal<string[]>([]);

  private nextId = 1;

  constructor() {
    this.yearSvc.getCurrent().subscribe((res: any) => {
      const yearId = res?.data?.id;
      if (yearId) this.selectedYearId.set(yearId);
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
        const imported: ImportedStudent[] = list.map((s: any) => ({
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

  updateStudentNationalId(id: number, val: string) {
    this.students.update(list => list.map(s => s.id === id ? { ...s, nationalId: val || null } : s));
  }

  updateStudentGender(id: number, val: string) {
    this.students.update(list => list.map(s => s.id === id ? { ...s, gender: val || null } : s));
  }

  updateStudentBirthDate(id: number, val: string) {
    this.students.update(list => list.map(s => s.id === id ? { ...s, birthDate: val || null } : s));
  }

  saveStudents() {
    this.isLoading.set(true);
    const body = {
      students: this.students().map(s => ({
        fullName: s.fullName,
        nationalId: s.nationalId,
        gender: s.gender,
        birthDate: s.birthDate,
      })),
      academicYearId: this.selectedYearId(),
    };
    this.importSvc.import(body).subscribe({
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
    this.selectedYearId.set(null);
    this.showSuccess.set(false);
    this.showError.set(false);
    this.errorMessage.set('');
    this.fileNames.set([]);
  }
}
