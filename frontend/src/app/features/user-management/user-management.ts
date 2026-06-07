import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ParentStudentLink, ParentStudentService, RelationshipType } from '../../core/services/parent-student.service';
import { Student, StudentService } from '../../core/services/student.service';
import { User, UserService } from '../../core/services/user.service';

type AccountTab = 'all' | 'admins' | 'parents' | 'students';
type ManagedRole = 'Admin' | 'Parent' | 'Student';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './user-management.html',
  styleUrl: './user-management.css'
})
export class UserManagement implements OnInit {
  private userService = inject(UserService);
  private studentService = inject(StudentService);
  private parentStudentService = inject(ParentStudentService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  activeTab = signal<AccountTab>('all');
  searchQuery = signal('');
  currentPage = signal(1);
  itemsPerPage = signal(10);
  users = signal<User[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  showCreateModal = signal(false);
  showEditModal = signal(false);
  showParentLinksModal = signal(false);
  editingUserId = signal<number | null>(null);
  managingParent = signal<User | null>(null);
  students = signal<Student[]>([]);
  parentLinkedStudents = signal<Array<{ student: Student; link: ParentStudentLink | null }>>([]);
  selectedStudentToLink = signal<number | null>(null);
  selectedRelationship = signal<RelationshipType>(1);

  formName = signal('');
  formEmail = signal('');
  formPassword = signal('');
  formPhone = signal('');
  formRole = signal<ManagedRole>('Parent');

  filteredUsers = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const tab = this.activeTab();

    return this.users().filter(user => {
      const role = user.role.toLowerCase();
      const matchesTab =
        tab === 'all' ||
        (tab === 'admins' && role === 'admin') ||
        (tab === 'parents' && role === 'parent') ||
        (tab === 'students' && role === 'student');

      const matchesQuery =
        !query ||
        user.fullName.toLowerCase().includes(query) ||
        user.email.toLowerCase().includes(query) ||
        (user.phone ?? '').toLowerCase().includes(query);

      return matchesTab && matchesQuery;
    });
  });

  paginatedUsers = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredUsers().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredUsers().length / this.itemsPerPage()));
  });

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

  totalCount = computed(() => this.users().length);
  adminCount = computed(() => this.users().filter(user => user.role.toLowerCase() === 'admin').length);
  parentCount = computed(() => this.users().filter(user => user.role.toLowerCase() === 'parent').length);
  studentCount = computed(() => this.users().filter(user => user.role.toLowerCase() === 'student').length);
  activeCount = computed(() => this.users().filter(user => user.isActive).length);
  linkedStudentIdsForManagingParent = computed(() =>
    new Set(this.parentLinkedStudents().map(item => item.student.id))
  );
  availableStudentsForParent = computed(() =>
    this.students().filter(student => !this.linkedStudentIdsForManagingParent().has(student.id))
  );
  managedParentChildrenCount = computed(() => this.parentLinkedStudents().length);

  ngOnInit() {
    this.loadUsers();
    this.loadStudents();
  }

  loadUsers() {
    this.isLoading.set(true);

    this.userService.getAll({ pageSize: 1000 }).subscribe({
      next: result => {
        this.users.set(result.data?.items ?? []);
        this.isLoading.set(false);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحميل الحسابات';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  loadStudents() {
    this.studentService.getAll().subscribe({
      next: students => this.students.set(students.data ?? []),
      error: err => {
        const msg = err?.error?.message || 'تعذر تحميل قائمة الطلاب';
        this.showError(msg);
      }
    });
  }

  openCreateModal(role?: ManagedRole) {
    this.resetForm();
    if (role) {
      this.formRole.set(role);
    }
    this.showCreateModal.set(true);
  }

  openEditModal(user: User) {
    this.editingUserId.set(user.id);
    this.formName.set(user.fullName);
    this.formEmail.set(user.email);
    this.formPhone.set(user.phone ?? '');
    this.formRole.set(this.toManagedRole(user.role));
    this.formPassword.set('');
    this.showEditModal.set(true);
  }

  closeModals() {
    this.showCreateModal.set(false);
    this.showEditModal.set(false);
    this.editingUserId.set(null);
    this.resetForm();
  }

  openParentLinksModal(user: User) {
    if (user.role.toLowerCase() !== 'parent') {
      return;
    }

    this.managingParent.set(user);
    this.selectedStudentToLink.set(null);
    this.selectedRelationship.set(1);
    this.showParentLinksModal.set(true);
    this.loadParentChildren(user.id);
  }

  closeParentLinksModal() {
    this.showParentLinksModal.set(false);
    this.managingParent.set(null);
    this.parentLinkedStudents.set([]);
    this.selectedStudentToLink.set(null);
    this.selectedRelationship.set(1);
  }

  loadParentChildren(parentId: number) {
    this.isLoading.set(true);

    this.parentStudentService.getStudentsByParent(parentId).subscribe({
      next: students => {
        if ((students.data ?? students).length === 0) {
          this.parentLinkedStudents.set([]);
          this.isLoading.set(false);
          return;
        }

        forkJoin(
          students.map(student =>
            this.parentStudentService.getParentsByStudent(student.id).pipe(
              map(links => ({
                student,
                link: (links.data ?? links).find(link => link.parentId === parentId) ?? null
              })),
              catchError(() => of({ student, link: null }))
            )
          )
        ).subscribe({
          next: rows => {
            this.parentLinkedStudents.set(rows);
            this.isLoading.set(false);
          },
          error: err => {
            const msg = err?.error?.message || 'تعذر تحميل أبناء ولي الأمر';
            this.showError(msg);
            this.isLoading.set(false);
          }
        });
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحميل أبناء ولي الأمر';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  saveAccount() {
    if (!this.formName() || !this.formEmail() || !this.formPassword()) {
      this.showError('يرجى إدخال الاسم والبريد الإلكتروني وكلمة المرور');
      return;
    }

    this.isLoading.set(true);
    this.userService.createUser({
      fullName: this.formName(),
      email: this.formEmail(),
      password: this.formPassword(),
      phone: this.formPhone() || undefined,
      role: this.formRole()
    }).subscribe({
      next: () => {
        this.showSuccess('تم إنشاء الحساب بنجاح');
        this.closeModals();
        this.loadUsers();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر إنشاء الحساب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  updateAccount() {
    const userId = this.editingUserId();
    if (!userId || !this.formName()) {
      this.showError('يرجى إدخال الاسم الكامل');
      return;
    }

    this.isLoading.set(true);
    this.userService.updateUser(userId, {
      fullName: this.formName(),
      phone: this.formPhone() || undefined
    }).subscribe({
      next: () => {
        this.showSuccess('تم تحديث الحساب بنجاح');
        this.closeModals();
        this.loadUsers();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحديث الحساب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  toggleAccountStatus(user: User) {
    const nextStatus = !user.isActive;
    this.isLoading.set(true);

    this.userService.setActiveStatus(user.id, nextStatus).subscribe({
      next: () => {
        this.showSuccess(nextStatus ? 'تم تفعيل الحساب بنجاح' : 'تم تعطيل الحساب بنجاح');
        this.loadUsers();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تغيير حالة الحساب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  deleteAccount(user: User) {
    if (!confirm(`هل أنت متأكد من حذف حساب ${user.fullName}؟`)) {
      return;
    }

    this.isLoading.set(true);
    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.showSuccess('تم حذف الحساب بنجاح');
        this.loadUsers();
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر حذف الحساب';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  navigateTo(route: string) {
    this.router.navigate([route]);
  }

  linkStudentToParent() {
    const parent = this.managingParent();
    const studentId = this.selectedStudentToLink();

    if (!parent || !studentId) {
      this.showError('اختر الطالب المراد ربطه أولًا');
      return;
    }

    this.isLoading.set(true);
    this.parentStudentService.link({
      parentId: parent.id,
      studentId,
      relationship: this.selectedRelationship()
    }).subscribe({
      next: () => {
        this.showSuccess('تم ربط الطالب بولي الأمر بنجاح');
        this.selectedStudentToLink.set(null);
        this.loadParentChildren(parent.id);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر ربط الطالب بولي الأمر';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  unlinkStudentFromParent(link: ParentStudentLink) {
    const parent = this.managingParent();
    if (!parent) {
      return;
    }

    if (!confirm(`هل تريد فك ربط الطالب ${link.studentName} من ولي الأمر ${link.parentName}؟`)) {
      return;
    }

    this.isLoading.set(true);
    this.parentStudentService.unlink(link.id).subscribe({
      next: () => {
        this.showSuccess('تم فك الربط بنجاح');
        this.loadParentChildren(parent.id);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر فك الربط';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  updateParentRelationship(link: ParentStudentLink, value: string) {
    const parent = this.managingParent();
    if (!parent) {
      return;
    }

    const relationship = Number(value) as RelationshipType;
    this.isLoading.set(true);
    this.parentStudentService.updateRelationship(link.id, relationship).subscribe({
      next: () => {
        this.showSuccess('تم تحديث صلة القرابة بنجاح');
        this.loadParentChildren(parent.id);
      },
      error: err => {
        const msg = err?.error?.message || 'تعذر تحديث صلة القرابة';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  setSelectedRelationshipFromValue(value: string) {
    this.selectedRelationship.set(Number(value) as RelationshipType);
  }

  getRoleLabel(role: string): string {
    switch (role.toLowerCase()) {
      case 'admin':
        return 'أدمن';
      case 'parent':
        return 'ولي أمر';
      case 'student':
        return 'حساب طالب';
      case 'teacher':
        return 'معلم';
      default:
        return role;
    }
  }

  getRelationshipLabel(relationship?: RelationshipType | null): string {
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

  canSaveAccount(): boolean {
    return !!this.formName() && !!this.formEmail() && !!this.formPassword() && !this.isLoading();
  }

  canUpdateAccount(): boolean {
    return !!this.formName() && !this.isLoading();
  }

  private resetForm() {
    this.formName.set('');
    this.formEmail.set('');
    this.formPassword.set('');
    this.formPhone.set('');
    this.formRole.set('Parent');
  }

  private toManagedRole(role: string): ManagedRole {
    switch (role.toLowerCase()) {
      case 'admin':
        return 'Admin';
      case 'student':
        return 'Student';
      default:
        return 'Parent';
    }
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
