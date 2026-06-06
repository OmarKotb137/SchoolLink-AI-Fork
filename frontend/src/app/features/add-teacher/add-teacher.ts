import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { SubjectService, Subject } from '../../core/services/subject.service';
import { TeacherService, Teacher, CreateTeacherRequest, UpdateTeacherRequest } from '../../core/services/teacher.service';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-add-teacher',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './add-teacher.html',
  styleUrl: './add-teacher.css'
})
export class AddTeacher implements OnInit {
  private subjectService = inject(SubjectService);
  private teacherService = inject(TeacherService);
  private userService = inject(UserService);

  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';
  showEditModal = signal(false);
  editingTeacherId = signal<number | null>(null);
  modalErrorMessage = signal<string | null>(null);
  modalSuccessMessage = signal<string | null>(null);
  searchQuery = signal('');
  statusFilter = signal<'all' | 'active' | 'inactive'>('all');
  subjectFilter = signal<number | 'all'>('all');

  currentPage = signal(1);
  itemsPerPage = signal(10);

  newName = signal('');
  newEmail = signal('');
  newPassword = signal('');
  newPhone = signal('');
  selectedSubjectIds = signal<number[]>([]);

  allSubjects = signal<Subject[]>([]);
  teachers = signal<Teacher[]>([]);

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  filteredTeachers = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const status = this.statusFilter();
    const subjectId = this.subjectFilter();

    return this.teachers().filter(teacher => {
      const matchesQuery =
        !query ||
        teacher.fullName.toLowerCase().includes(query) ||
        teacher.email.toLowerCase().includes(query) ||
        (teacher.phone ?? '').toLowerCase().includes(query);

      const matchesStatus =
        status === 'all' ||
        (status === 'active' && teacher.isActive) ||
        (status === 'inactive' && !teacher.isActive);

      const matchesSubject =
        subjectId === 'all' ||
        (teacher.subjectIds ?? []).includes(subjectId);

      return matchesQuery && matchesStatus && matchesSubject;
    });
  });

  paginatedTeachers = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredTeachers().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredTeachers().length / this.itemsPerPage()));
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

  ngOnInit() {
    this.loadSubjects();
    this.loadTeachers();
  }

  loadSubjects() {
    this.subjectService.getAll().subscribe({
      next: (data) => this.allSubjects.set(data.data ?? data),
      error: () => this.showError('تعذر تحميل المواد الدراسية')
    });
  }

  loadTeachers() {
    this.teacherService.getAll(1000).subscribe({
      next: (res) => this.teachers.set(res.data?.items ?? []),
      error: () => this.showError('تعذر تحميل قائمة المعلمين')
    });
  }

  toggleSubject(id: number) {
    this.selectedSubjectIds.update(list =>
      list.includes(id) ? list.filter(s => s !== id) : [...list, id]
    );
  }

  clearSelectedSubjects() {
    this.selectedSubjectIds.set([]);
  }

  resetFilters() {
    this.searchQuery.set('');
    this.statusFilter.set('all');
    this.subjectFilter.set('all');
    this.currentPage.set(1);
  }

  resetForm() {
    this.newName.set('');
    this.newEmail.set('');
    this.newPassword.set('');
    this.newPhone.set('');
    this.selectedSubjectIds.set([]);
    this.editingTeacherId.set(null);
  }

  saveTeacher() {
    if (!this.newName() || !this.newEmail() || !this.newPassword()) {
      this.showError('يرجى تعبئة الاسم والبريد الإلكتروني وكلمة المرور');
      return;
    }

    if (this.selectedSubjectIds().length === 0) {
      this.showError('يرجى اختيار مادة واحدة على الأقل للمعلم');
      return;
    }

    this.isLoading.set(true);

    const payload: CreateTeacherRequest = {
      fullName: this.newName(),
      email: this.newEmail(),
      password: this.newPassword(),
      phone: this.newPhone() || undefined,
      subjectIds: this.selectedSubjectIds()
    };

    this.teacherService.createTeacher(payload).subscribe({
      next: () => {
        this.showSuccess('تم إضافة المعلم بنجاح');
        this.resetForm();
        this.loadTeachers();
        this.isLoading.set(false);
      },
      error: (err) => {
        const msg = err?.error?.message || err?.error?.errors?.[0] || 'حدث خطأ أثناء الإضافة، تحقق من البيانات';
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  editTeacher(t: Teacher) {
    this.editingTeacherId.set(t.id);
    this.newName.set(t.fullName);
    this.newEmail.set(t.email);
    this.newPassword.set('');
    this.newPhone.set(t.phone || '');
    this.selectedSubjectIds.set(t.subjectIds || []);
    this.showEditModal.set(true);
  }

  updateTeacher() {
    const id = this.editingTeacherId();
    if (!id) return;

    if (!this.newName()) {
      this.showModalError('يرجى إدخال الاسم');
      return;
    }

    if (this.selectedSubjectIds().length === 0) {
      this.showModalError('يجب أن يكون للمعلم مادة واحدة على الأقل');
      return;
    }

    this.isLoading.set(true);

    const payload: UpdateTeacherRequest = {
      fullName: this.newName(),
      phone: this.newPhone() || undefined,
      subjectIds: this.selectedSubjectIds()
    };

    this.teacherService.updateTeacher(id, payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.showSuccess('تم تحديث بيانات المعلم بنجاح');
        this.closeEditModal();
        this.loadTeachers();
      },
      error: (err) => {
        const msg = err?.error?.message || 'حدث خطأ أثناء التحديث';
        this.showModalError(msg);
        this.isLoading.set(false);
      }
    });
  }

  closeEditModal() {
    this.showEditModal.set(false);
    this.editingTeacherId.set(null);
    this.modalErrorMessage.set(null);
    this.modalSuccessMessage.set(null);
    this.resetForm();
  }

  deleteTeacher(id: number) {
    if (!confirm('هل أنت متأكد من حذف هذا المعلم؟ إذا كان لديه تعيينات فعالة فسيتم تعطيله بدلا من حذفه.')) return;

    this.teacherService.deleteTeacher(id).subscribe({
      next: () => {
        this.showSuccess('تم تنفيذ حذف أو تعطيل المعلم بنجاح');
        this.loadTeachers();
      },
      error: () => this.showError('تعذر حذف أو تعطيل المعلم، حاول مرة أخرى')
    });
  }

  toggleTeacherStatus(teacher: Teacher) {
    const nextStatus = !teacher.isActive;
    const actionLabel = nextStatus ? 'تفعيل' : 'تعطيل';

    if (!confirm(`هل تريد ${actionLabel} المعلم ${teacher.fullName}؟`)) {
      return;
    }

    this.isLoading.set(true);
    this.userService.setActiveStatus(teacher.id, nextStatus).subscribe({
      next: () => {
        this.showSuccess(nextStatus ? 'تم تفعيل المعلم بنجاح' : 'تم تعطيل المعلم بنجاح');
        this.isLoading.set(false);
        this.loadTeachers();
      },
      error: err => {
        const msg = err?.error?.message || `تعذر ${actionLabel} المعلم`;
        this.showError(msg);
        this.isLoading.set(false);
      }
    });
  }

  getTeacherSubjectsText(teacher: Teacher): string {
    const subjectNames = teacher.subjectNames || [];
    return subjectNames.length > 0 ? subjectNames.join('، ') : 'غير محدد';
  }

  getSelectedSubjectsText(): string {
    const names = this.allSubjects()
      .filter(subject => this.selectedSubjectIds().includes(subject.id))
      .map(subject => subject.name);

    return names.length > 0 ? names.join('، ') : 'لم يتم اختيار مواد بعد';
  }

  getTeachersCount(): number {
    return this.teachers().length;
  }

  getActiveTeachersCount(): number {
    return this.teachers().filter(teacher => teacher.isActive).length;
  }

  getInactiveTeachersCount(): number {
    return this.teachers().filter(teacher => !teacher.isActive).length;
  }

  getFilteredTeachersCount(): number {
    return this.filteredTeachers().length;
  }

  canSaveTeacher(): boolean {
    return !!this.newName() && !!this.newEmail() && !!this.newPassword() && this.selectedSubjectIds().length > 0 && !this.isLoading();
  }

  canUpdateTeacher(): boolean {
    return !!this.newName() && this.selectedSubjectIds().length > 0 && !this.isLoading();
  }

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

  private showModalError(msg: string) {
    this.modalErrorMessage.set(msg);
    this.modalSuccessMessage.set(null);
    setTimeout(() => this.modalErrorMessage.set(null), 5000);
  }
}
