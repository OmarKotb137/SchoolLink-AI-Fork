import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ClassSubjectTeacherService, ClassSubjectTeacher } from '../../core/services/class-subject-teacher.service';
import { ClassService, ClassEntity } from '../../core/services/class.service';
import { SubjectService, Subject } from '../../core/services/subject.service';
import { UserService, User } from '../../core/services/user.service';
import { AcademicYearService } from '../../core/services/academic-year.service';

@Component({
  selector: 'app-teacher-assignments',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './teacher-assignments.html',
  styleUrl: './teacher-assignments.css',
})
export class TeacherAssignments implements OnInit {
  sidebarOpen = signal(false);

  private assignmentService = inject(ClassSubjectTeacherService);
  private classService = inject(ClassService);
  private subjectService = inject(SubjectService);
  private userService = inject(UserService);
  private academicYearService = inject(AcademicYearService);

  assignments = signal<ClassSubjectTeacher[]>([]);
  classes = signal<ClassEntity[]>([]);
  subjects = signal<Subject[]>([]);
  teachers = signal<User[]>([]);

  // FIX 1: plain property instead of signal — [(ngModel)] needs a plain value, not a WritableSignal
  selectedClassFilter: number | null = null;

  currentAcademicYearId: number | null = null;

  newAssignment: Partial<ClassSubjectTeacher> = { classId: 0, subjectId: 0, teacherId: 0, weeklyPeriods: 1 };

  // Edit state
  editingAssignmentId = signal<number | null>(null);
  editForm: { teacherId: number; weeklyPeriods: number } = { teacherId: 0, weeklyPeriods: 1 };

  // UI state
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  ngOnInit() {
    // FIX 2: load academic year first so assignTeacher() always has currentAcademicYearId ready
    this.loadAcademicYear();
    this.loadClasses();
    this.loadSubjects();
    this.loadTeachers();
  }

  loadAcademicYear() {
    this.academicYearService.getAll().subscribe({
      next: (years) => {
        const current = years.find(y => y.isCurrent);
        if (current) this.currentAcademicYearId = current.id;
      },
      error: () => this.showError('تعذر تحميل السنة الدراسية الحالية')
    });
  }

  loadClasses() {
    this.classService.getAll().subscribe({
      next: (data) => this.classes.set(data),
      error: () => this.showError('تعذر تحميل بيانات الفصول')
    });
  }

  loadSubjects() {
    this.subjectService.getAll().subscribe({
      next: (data) => this.subjects.set(data),
      error: () => this.showError('تعذر تحميل بيانات المواد')
    });
  }

  loadTeachers() {
    // FIX 3: pass pageSize=1000 via user.service so all teachers are fetched (not just page 1)
    this.userService.getByRole('Teacher', 1000).subscribe({
      next: (res) => this.teachers.set(res.items || []),
      error: () => this.showError('تعذر تحميل بيانات المعلمين')
    });
  }

  loadAssignments() {
    if (!this.selectedClassFilter) {
      this.assignments.set([]);
      return;
    }

    this.isLoading.set(true);
    // FIX 4: pass academicYearId to filter by current year only
    this.assignmentService.getByClass(this.selectedClassFilter, this.currentAcademicYearId ?? undefined).subscribe({
      next: (data) => {
        this.assignments.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.assignments.set([]);
        this.isLoading.set(false);
        this.showError('تعذر تحميل التعيينات');
      }
    });
  }

  onClassFilterChange() {
    this.loadAssignments();
  }

  // Lookup helpers (fallback if DTO name fields are empty)
  getTeacherName(teacherId: number): string {
    return this.teachers().find(t => t.id == teacherId)?.fullName || 'غير محدد';
  }

  getSubjectName(subjectId: number): string {
    return this.subjects().find(s => s.id == subjectId)?.name || 'غير محدد';
  }

  getClassName(classId: number): string {
    return this.classes().find(c => c.id == classId)?.name || 'غير محدد';
  }

  assignTeacher() {
    if (!this.newAssignment.classId || !this.newAssignment.subjectId || !this.newAssignment.teacherId) {
      this.showError('يرجى تعبئة جميع الحقول المطلوبة');
      return;
    }

    if (!this.currentAcademicYearId) {
      this.showError('لم يتم تحديد السنة الدراسية الحالية، يرجى إعداد سنة دراسية نشطة أولاً');
      return;
    }

    const payload = {
      classId: Number(this.newAssignment.classId),
      subjectId: Number(this.newAssignment.subjectId),
      teacherId: Number(this.newAssignment.teacherId),
      weeklyPeriods: Number(this.newAssignment.weeklyPeriods),
      // FIX 5: include academicYearId in the create payload
      academicYearId: this.currentAcademicYearId,
    };

    this.assignmentService.assignTeacherToClass(payload).subscribe({
      next: () => {
        // keep same classId selected, reset rest
        this.newAssignment = { classId: this.newAssignment.classId, subjectId: 0, teacherId: 0, weeklyPeriods: 1 };
        this.loadAssignments();
        this.showSuccess('تم تعيين المعلم بنجاح');
      },
      // FIX 6: replace alert() with inline error banner
      error: (err) => {
        const msg = err?.error?.message || err?.error || 'تأكد من عدم تكرار التعيين لنفس المادة والفصل.';
        this.showError('حدث خطأ أثناء التعيين: ' + msg);
      }
    });
  }

  removeAssignment(id: number) {
    if (!confirm('هل أنت متأكد من حذف هذا التعيين؟ سيؤدي ذلك إلى إزالة المعلم من تدريس هذه المادة لهذا الفصل.')) return;

    // FIX 7: add error handler for delete
    this.assignmentService.delete(id).subscribe({
      next: () => {
        this.loadAssignments();
        this.showSuccess('تم حذف التعيين بنجاح');
      },
      error: () => this.showError('تعذر حذف التعيين، حاول مرة أخرى')
    });
  }

  // ── Edit functionality ──────────────────────────────────────────────────────

  startEdit(assignment: ClassSubjectTeacher) {
    this.editingAssignmentId.set(assignment.id!);
    this.editForm = { teacherId: assignment.teacherId, weeklyPeriods: assignment.weeklyPeriods };
  }

  cancelEdit() {
    this.editingAssignmentId.set(null);
  }

  saveEdit(id: number) {
    if (!this.editForm.teacherId) {
      this.showError('يرجى اختيار معلم');
      return;
    }
    this.assignmentService.update(id, {
      teacherId: Number(this.editForm.teacherId),
      weeklyPeriods: Number(this.editForm.weeklyPeriods)
    }).subscribe({
      next: () => {
        this.editingAssignmentId.set(null);
        this.loadAssignments();
        this.showSuccess('تم تحديث التعيين بنجاح');
      },
      error: () => this.showError('تعذر تحديث التعيين')
    });
  }

  // ── Notification helpers ────────────────────────────────────────────────────

  private showError(msg: string) {
    this.errorMessage.set(msg);
    this.successMessage.set(null);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    this.errorMessage.set(null);
    setTimeout(() => this.successMessage.set(null), 3000);
  }
}
