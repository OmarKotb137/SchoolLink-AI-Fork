import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ParentStudentLink, ParentStudentService, RelationshipType } from '../../core/services/parent-student.service';
import { CreateStudentRequest, Student, StudentService, UpdateStudentRequest } from '../../core/services/student.service';
import { User, UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-student-management',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './student-management.html',
  styleUrl: './student-management.css'
})
export class StudentManagement implements OnInit {
  private studentService = inject(StudentService);
  private userService = inject(UserService);
  private parentStudentService = inject(ParentStudentService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  students = signal<Student[]>([]);
  parentLinks = signal<ParentStudentLink[]>([]);
  studentAccounts = signal<User[]>([]);
  parentAccounts = signal<User[]>([]);

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  deleteStudentConfirm = signal<Student | null>(null);
  unlinkParentConfirm = signal<ParentStudentLink | null>(null);
  searchQuery = signal('');

  currentPage = signal(1);
  itemsPerPage = signal(10);

  paginatedStudents = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredStudents().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredStudents().length / this.itemsPerPage()));
  });

  rangeStart = computed(() => {
    if (this.filteredStudents().length === 0) return 0;
    return (this.currentPage() - 1) * this.itemsPerPage() + 1;
  });

  rangeEnd = computed(() => {
    return Math.min(this.currentPage() * this.itemsPerPage(), this.filteredStudents().length);
  });

  pages = computed<(number | string)[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const result: (number | string)[] = [];
    result.push(1);
    if (current > 3) result.push('...');
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      result.push(i);
    }
    if (current < total - 2) result.push('...');
    if (total > 1) result.push(total);
    return result;
  });

  trackByPageIndex = (_: number, item: number | string) =>
    typeof item === 'string' ? `dot-${_}` : `page-${item}`;

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  goToPage(page: number) {
    this.currentPage.set(page);
  }

  selectedStudentId = signal<number | null>(null);
  selectedStudentUserId = signal<number | null>(null);
  selectedParentId = signal<number | null>(null);
  selectedRelationship = signal<RelationshipType>(1);

  showCreateModal = signal(false);
  showEditModal = signal(false);
  editingStudentId = signal<number | null>(null);

  formName = signal('');
  formNationalId = signal('');
  formGender = signal<number | null>(null);
  formBirthDate = signal('');

  filteredStudents = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    if (!query) return this.students();

    return this.students().filter(student =>
      student.fullName.toLowerCase().includes(query) ||
      (student.nationalId ?? '').toLowerCase().includes(query) ||
      (student.userEmail ?? '').toLowerCase().includes(query)
    );
  });

  selectedStudent = computed(() =>
    this.students().find(student => student.id === this.selectedStudentId()) ?? null
  );

  linkedStudentUserIds = computed(() =>
    new Set(this.students().map(student => student.userId).filter((id): id is number => !!id))
  );

  availableStudentAccounts = computed(() => {
    const selected = this.selectedStudent();
    const linkedIds = this.linkedStudentUserIds();

    return this.studentAccounts().filter(account =>
      !linkedIds.has(account.id) || account.id === selected?.userId
    );
  });

  ngOnInit() {
    this.loadAllData();
  }

  loadAllData() {
    this.loadStudents();
    this.loadParentAccounts();
    this.loadStudentAccounts();
  }

  loadStudents() {
    this.isLoading.set(true);

    this.studentService.getAll().subscribe({
      next: students => {
        const s = students.data ?? students;
        this.students.set(s);
        this.currentPage.set(1);

        const selectedId = this.selectedStudentId();
        if (selectedId && s.some((student: any) => student.id === selectedId)) {
          this.loadParentLinks(selectedId);
        } else {
          this.selectedStudentId.set(null);
          this.parentLinks.set([]);
        }

        this.isLoading.set(false);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحميل ملفات الطلاب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  loadStudentAccounts() {
    this.userService.getByRole('Student', 1000).subscribe({
      next: result => this.studentAccounts.set(result.data?.items ?? []),
      error: err => this.showError(err?.error?.message || 'تعذر تحميل حسابات الطلاب')
    });
  }

  loadParentAccounts() {
    this.userService.getByRole('Parent', 1000).subscribe({
      next: result => this.parentAccounts.set(result.data?.items ?? []),
      error: err => this.showError(err?.error?.message || 'تعذر تحميل حسابات أولياء الأمور')
    });
  }

  selectStudent(student: Student) {
    this.selectedStudentId.set(student.id);
    this.selectedStudentUserId.set(student.userId ?? null);
    this.selectedParentId.set(null);
    this.loadParentLinks(student.id);
  }

  loadParentLinks(studentId: number) {
    this.parentStudentService.getParentsByStudent(studentId).subscribe({
      next: links => this.parentLinks.set(links.data ?? links),
      error: err => {
        this.parentLinks.set([]);
        this.showError(err?.error?.message || 'تعذر تحميل روابط أولياء الأمور');
      }
    });
  }

  openCreateModal() {
    this.resetForm();
    this.showCreateModal.set(true);
  }

  openEditModal(student: Student) {
    this.editingStudentId.set(student.id);
    this.formName.set(student.fullName);
    this.formNationalId.set(student.nationalId ?? '');
    this.formGender.set(student.gender ?? null);
    this.formBirthDate.set(student.birthDate ?? '');
    this.showEditModal.set(true);
  }

  closeModals() {
    this.showCreateModal.set(false);
    this.showEditModal.set(false);
    this.editingStudentId.set(null);
    this.resetForm();
  }

  saveStudent() {
    if (!this.formName()) {
      this.showError('يرجى إدخال اسم الطالب');
      return;
    }

    this.isLoading.set(true);

    const payload: CreateStudentRequest = {
      fullName: this.formName(),
      nationalId: this.formNationalId() || undefined,
      gender: this.formGender(),
      birthDate: this.formBirthDate() || null
    };

    this.studentService.create(payload).subscribe({
      next: student => {
        const s = student.data ?? student;
        this.showSuccess('تم إنشاء ملف الطالب بنجاح');
        this.closeModals();
        this.loadStudents();
        this.selectStudent(s);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر إنشاء ملف الطالب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  updateStudent() {
    const studentId = this.editingStudentId();
    if (!studentId || !this.formName()) {
      this.showError('يرجى إدخال اسم الطالب');
      return;
    }

    this.isLoading.set(true);

    const payload: UpdateStudentRequest = {
      id: studentId,
      fullName: this.formName(),
      gender: this.formGender(),
      birthDate: this.formBirthDate() || null
    };

    this.studentService.update(studentId, payload).subscribe({
      next: () => {
        this.showSuccess('تم تحديث بيانات الطالب بنجاح');
        this.closeModals();
        this.loadStudents();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحديث بيانات الطالب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  deleteStudent(student: Student) {
    this.deleteStudentConfirm.set(student);
  }

  cancelDeleteStudent() {
    this.deleteStudentConfirm.set(null);
  }

  confirmDeleteStudent() {
    const student = this.deleteStudentConfirm();
    if (!student) return;
    this.deleteStudentConfirm.set(null);
    this.isLoading.set(true);

    this.studentService.delete(student.id).subscribe({
      next: () => {
        if (this.selectedStudentId() === student.id) {
          this.selectedStudentId.set(null);
          this.parentLinks.set([]);
        }

        this.showSuccess('تم حذف ملف الطالب بنجاح');
        this.loadStudents();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر حذف ملف الطالب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  linkStudentAccount() {
    const student = this.selectedStudent();
    const userId = this.selectedStudentUserId();

    if (!student || !userId) {
      this.showError('اختر الطالب ثم حساب الطالب المطلوب ربطه');
      return;
    }

    this.isLoading.set(true);

    this.studentService.linkUser({
      studentId: student.id,
      userId
    }).subscribe({
      next: () => {
        this.showSuccess('تم ربط حساب الطالب بنجاح');
        this.loadStudents();
        this.loadStudentAccounts();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر ربط حساب الطالب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  linkParentToStudent() {
    const student = this.selectedStudent();
    const parentId = this.selectedParentId();

    if (!student || !parentId) {
      this.showError('اختر الطالب وولي الأمر أولًا');
      return;
    }

    this.isLoading.set(true);

    this.parentStudentService.link({
      parentId,
      studentId: student.id,
      relationship: this.selectedRelationship()
    }).subscribe({
      next: () => {
        this.showSuccess('تم ربط ولي الأمر بالطالب بنجاح');
        this.selectedParentId.set(null);
        this.loadParentLinks(student.id);
        this.isLoading.set(false);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر ربط ولي الأمر بالطالب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  unlinkParent(link: ParentStudentLink) {
    this.unlinkParentConfirm.set(link);
  }

  cancelUnlinkParent() {
    this.unlinkParentConfirm.set(null);
  }

  confirmUnlinkParent() {
    const link = this.unlinkParentConfirm();
    if (!link) return;
    this.unlinkParentConfirm.set(null);
    this.isLoading.set(true);

    this.parentStudentService.unlink(link.id).subscribe({
      next: () => {
        this.showSuccess('تم إلغاء ربط ولي الأمر بنجاح');
        if (this.selectedStudentId()) {
          this.loadParentLinks(this.selectedStudentId()!);
        }
        this.isLoading.set(false);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر إلغاء الربط';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  navigateTo(route: string) {
    this.router.navigate([route]);
  }

  getGenderLabel(gender?: number | null): string {
    if (gender === 1) return 'ذكر';
    if (gender === 2) return 'أنثى';
    return 'غير محدد';
  }

  getRelationshipLabel(relationship: RelationshipType): string {
    switch (relationship) {
      case 1:
        return 'الأب';
      case 2:
        return 'الأم';
      case 3:
        return 'الوصي';
      case 4:
        return 'أخ / أخت';
      case 5:
        return 'أخرى';
      default:
        return 'غير محدد';
    }
  }

  canSaveStudent(): boolean {
    return !!this.formName() && !this.isLoading();
  }

  canUpdateStudent(): boolean {
    return !!this.formName() && !this.isLoading();
  }

  setRelationshipFromValue(value: string) {
    const relationship = Number(value) as RelationshipType;
    this.selectedRelationship.set(relationship);
  }

  private resetForm() {
    this.formName.set('');
    this.formNationalId.set('');
    this.formGender.set(null);
    this.formBirthDate.set('');
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    this.successMessage.set(null);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  private showSuccess(message: string) {
    this.successMessage.set(message);
    this.errorMessage.set(null);
    setTimeout(() => this.successMessage.set(null), 3000);
  }
}
