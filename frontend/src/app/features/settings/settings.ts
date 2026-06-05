import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { AcademicYearService, AcademicYear } from '../../core/services/academic-year.service';
import { GradeLevelService, GradeLevel } from '../../core/services/grade-level.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings implements OnInit {
  sidebarOpen = signal(false);
  activeTab = signal('academic'); // 'permissions' | 'academic'

  // Services
  private academicYearService = inject(AcademicYearService);
  private gradeLevelService = inject(GradeLevelService);

  // State
  academicYears = signal<AcademicYear[]>([]);
  gradeLevels = signal<GradeLevel[]>([]);
  editingYearId = signal<number | null>(null);

  // Error / Success messages
  yearError = signal<string | null>(null);
  gradeError = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Forms
  newYear = { name: '', startDate: '', endDate: '' };
  newGrade: { name: string; stage: string | null; levelOrder: number } = {
    name: '',
    stage: null,
    levelOrder: 1,
  };

  readonly stageOptions = ['ابتدائي', 'إعدادي', 'ثانوي'];

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.academicYearService.getAll().subscribe({
      next: (data) => this.academicYears.set(data),
      error: (err) => console.error('Failed to load academic years', err),
    });

    this.gradeLevelService.getAll().subscribe({
      next: (data) => this.gradeLevels.set(data),
      error: (err) => console.error('Failed to load grade levels', err),
    });
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  private extractErrorMessage(err: any, fallback: string): string {
    return err?.error?.message || err?.message || fallback;
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    setTimeout(() => this.successMessage.set(null), 3000);
  }

  // ─── Academic Year Methods ────────────────────────────────────────────────

  addAcademicYear() {
    if (!this.newYear.name || !this.newYear.startDate || !this.newYear.endDate) return;
    this.yearError.set(null);

    const request = { ...this.newYear };

    const action$ = this.editingYearId()
      ? this.academicYearService.update(this.editingYearId()!, {
          id: this.editingYearId()!,
          ...request,
        })
      : this.academicYearService.create(request);

    action$.subscribe({
      next: () => {
        this.loadData();
        this.cancelYearEdit();
        this.showSuccess(
          this.editingYearId() ? 'تم تحديث السنة الدراسية بنجاح' : 'تمت إضافة السنة الدراسية بنجاح'
        );
      },
      error: (err) => {
        this.yearError.set(this.extractErrorMessage(err, 'حدث خطأ أثناء حفظ السنة الدراسية'));
      },
    });
  }

  setYearActive(id: number) {
    this.yearError.set(null);
    this.academicYearService.setActive(id).subscribe({
      next: () => {
        this.loadData();
        this.showSuccess('تم تعيين السنة الدراسية الحالية بنجاح');
      },
      error: (err) => {
        this.yearError.set(this.extractErrorMessage(err, 'حدث خطأ أثناء تعيين السنة الحالية'));
      },
    });
  }

  editYear(year: AcademicYear) {
    this.editingYearId.set(year.id);
    this.newYear = {
      name: year.name,
      startDate: year.startDate,
      endDate: year.endDate,
    };
    this.yearError.set(null);
  }

  cancelYearEdit() {
    this.editingYearId.set(null);
    this.newYear = { name: '', startDate: '', endDate: '' };
    this.yearError.set(null);
  }

  deleteYear(id: number) {
    this.yearError.set(null);
    this.academicYearService.delete(id).subscribe({
      next: () => {
        this.loadData();
        this.showSuccess('تم حذف السنة الدراسية بنجاح');
      },
      error: (err) => {
        this.yearError.set(this.extractErrorMessage(err, 'حدث خطأ أثناء حذف السنة الدراسية'));
      },
    });
  }

  // ─── Grade Level Methods ──────────────────────────────────────────────────

  addGradeLevel() {
    if (!this.newGrade.name) return;
    this.gradeError.set(null);

    this.gradeLevelService.create(this.newGrade).subscribe({
      next: () => {
        this.loadData();
        this.newGrade = { name: '', stage: null, levelOrder: 1 };
        this.showSuccess('تمت إضافة المرحلة الدراسية بنجاح');
      },
      error: (err) => {
        this.gradeError.set(this.extractErrorMessage(err, 'حدث خطأ أثناء إضافة المرحلة الدراسية'));
      },
    });
  }

  deleteGradeLevel(id: number) {
    this.gradeError.set(null);
    this.gradeLevelService.delete(id).subscribe({
      next: () => {
        this.loadData();
        this.showSuccess('تم حذف المرحلة الدراسية بنجاح');
      },
      error: (err) => {
        this.gradeError.set(this.extractErrorMessage(err, 'حدث خطأ أثناء حذف المرحلة الدراسية'));
      },
    });
  }
}
