import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { SubjectService, Subject } from '../../core/services/subject.service';

@Component({
  selector: 'app-subject-management',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './subject-management.html',
  styleUrl: './subject-management.css',
})
export class SubjectManagement implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  private subjectService = inject(SubjectService);
  subjects = signal<Subject[]>([]);

  currentPage = signal(1);
  itemsPerPage = signal(10);

  paginatedSubjects = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.subjects().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.subjects().length / this.itemsPerPage()));
  });

  rangeStart = computed(() => {
    if (this.subjects().length === 0) return 0;
    return (this.currentPage() - 1) * this.itemsPerPage() + 1;
  });

  rangeEnd = computed(() => {
    return Math.min(this.currentPage() * this.itemsPerPage(), this.subjects().length);
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

  // FIX #1: كانت signal('') — [(ngModel)] بتبطل تشتغل مع signals
  // الحل: plain property عادية
  searchTerm = '';

  editingSubjectId = signal<number | null>(null);

  errorMessage = signal('');
  successMessage = signal('');
  deleteSubjectConfirmId = signal<number | null>(null);

  newSubject: Partial<Subject> = { name: '', code: '' };

  ngOnInit() {
    this.loadSubjects();
  }

  loadSubjects() {
    this.subjectService.getAll().subscribe({
      next: (data) => {
        this.subjects.set(data.data ?? data);
        this.currentPage.set(1);
      },
      // FIX #2: error handler مع رسالة للمستخدم
      error: (err) => {
        console.error('Failed to load subjects', err);
        this.showError('فشل في تحميل المواد الدراسية. تأكد من الاتصال بالخادم.');
      }
    });
  }

  onSearch() {
    this.errorMessage.set('');
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) {
      this.loadSubjects();
      return;
    }
    // البحث client-side على كل الـ subjects بالاسم والكود مع بعض
    // لأن الـ backend endpoint بيبحث بالاسم فقط
    this.subjectService.getAll().subscribe({
      next: (data) => {
        const filtered = (data.data ?? data).filter((s: any) =>
          s.name.toLowerCase().includes(term) ||
          s.code.toLowerCase().includes(term)
        );
        this.subjects.set(filtered);
        this.currentPage.set(1);
      },
      error: (err) => {
        console.error('Search failed', err);
        this.showError('فشل في البحث. حاول مرة أخرى.');
      }
    });
  }

  // FIX #4: clear البحث + auto-reset للـ subjects list
  clearSearch() {
    this.searchTerm = '';
    this.currentPage.set(1);
    this.loadSubjects();
  }

  editSubject(subject: Subject) {
    this.editingSubjectId.set(subject.id);
    // نرسل فقط الحقول القابلة للتعديل هنا، والخدمة نفسها تضيف `id`
    // حتى يطابق الـ URL ويقبلها الـ backend.
    this.newSubject = { name: subject.name, code: subject.code };
  }

  cancelEdit() {
    this.editingSubjectId.set(null);
    this.newSubject = { name: '', code: '' };
  }

  saveSubject() {
    if (!this.newSubject.name?.trim() || !this.newSubject.code?.trim()) return;

    if (this.editingSubjectId()) {
      // نمرر الحقول القابلة للتعديل فقط، والخدمة تضيف `id` تلقائيا.
      const { name, code } = this.newSubject;
      this.subjectService.update(this.editingSubjectId()!, { name, code }).subscribe({
        next: () => {
          this.loadSubjects();
          this.cancelEdit();
          this.showSuccess('تم تحديث المادة بنجاح!');
        },
        // FIX #2: error handler مع رسالة للمستخدم
        error: (err) => {
          console.error('Update failed', err);
          this.showError('فشل في تحديث المادة. حاول مرة أخرى.');
        }
      });
    } else {
      this.subjectService.create({ name: this.newSubject.name, code: this.newSubject.code }).subscribe({
        next: () => {
          this.loadSubjects();
          this.newSubject = { name: '', code: '' };
          this.showSuccess('تم إضافة المادة بنجاح!');
        },
        // FIX #2: error handler مع رسالة للمستخدم
        error: (err) => {
          console.error('Create failed', err);
          this.showError('فشل في إضافة المادة. حاول مرة أخرى.');
        }
      });
    }
  }

  deleteSubject(id: number) {
    this.deleteSubjectConfirmId.set(id);
  }

  cancelDeleteSubject() {
    this.deleteSubjectConfirmId.set(null);
  }

  confirmDeleteSubject() {
    const id = this.deleteSubjectConfirmId();
    if (!id) return;
    this.deleteSubjectConfirmId.set(null);
    this.subjectService.delete(id).subscribe({
      next: () => {
        this.loadSubjects();
        this.showSuccess('تم حذف المادة بنجاح!');
      },
      error: (err) => {
        console.error('Delete failed', err);
        this.showError('فشل في حذف المادة. حاول مرة أخرى.');
      }
    });
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    this.successMessage.set('');
    setTimeout(() => this.errorMessage.set(''), 4000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    this.errorMessage.set('');
    setTimeout(() => this.successMessage.set(''), 3000);
  }
}
