import { Component, OnInit, inject, signal } from '@angular/core';
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

  // FIX #1: كانت signal('') — [(ngModel)] بتبطل تشتغل مع signals
  // الحل: plain property عادية
  searchTerm = '';

  editingSubjectId = signal<number | null>(null);

  // FIX #2: رسائل feedback للمستخدم
  errorMessage = signal('');
  successMessage = signal('');

  newSubject: Partial<Subject> = { name: '', code: '' };

  ngOnInit() {
    this.loadSubjects();
  }

  loadSubjects() {
    this.subjectService.getAll().subscribe({
      next: (data) => this.subjects.set(data),
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
        const filtered = data.filter(s =>
          s.name.toLowerCase().includes(term) ||
          s.code.toLowerCase().includes(term)
        );
        this.subjects.set(filtered);
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
    if (confirm('هل أنت متأكد من حذف هذه المادة؟')) {
      this.subjectService.delete(id).subscribe({
        next: () => {
          this.loadSubjects();
          this.showSuccess('تم حذف المادة بنجاح!');
        },
        // FIX #2: error handler مع رسالة للمستخدم
        error: (err) => {
          console.error('Delete failed', err);
          this.showError('فشل في حذف المادة. حاول مرة أخرى.');
        }
      });
    }
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
