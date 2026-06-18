import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { AnnouncementService, Announcement, CreateAnnouncementRequest, normalizeCategory } from '../../core/services/announcement.service';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './announcements.html',
  styleUrl: './announcements.css',
})
export class Announcements implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  private announcementService = inject(AnnouncementService);

  announcements = signal<Announcement[]>([]);
  showExpired = signal(false);

  currentPage = signal(1);
  itemsPerPage = signal(10);

  filteredAnnouncements = computed(() => this.announcements());

  paginatedAnnouncements = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredAnnouncements().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.filteredAnnouncements().length / this.itemsPerPage()));
  });

  rangeStart = computed(() => {
    if (this.filteredAnnouncements().length === 0) return 0;
    return (this.currentPage() - 1) * this.itemsPerPage() + 1;
  });

  rangeEnd = computed(() => {
    return Math.min(this.currentPage() * this.itemsPerPage(), this.filteredAnnouncements().length);
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

  // ─── Form State ────────────────────────────────────────────
  editingId = signal<number | null>(null);
  errorMessage = signal('');
  successMessage = signal('');

  // New announcement form model
  newAnnouncement: CreateAnnouncementRequest = {
    title: '',
    body: '',
    category: 0,
    isForAllUsers: true,
    isForAllStudents: false,
    isForAllParents: false,
    isForAllTeachers: false,
    expiresAt: undefined,
  };

  categoryOptions = [
    { value: 0, label: 'عام' },
    { value: 1, label: 'فعالية' },
    { value: 2, label: 'إجازة' },
    { value: 3, label: 'طارئ' },
  ];

  ngOnInit() {
    this.loadAnnouncements();
  }

  loadAnnouncements() {
    if (this.showExpired()) {
      this.announcementService.getExpired().subscribe({
        next: (res) => {
          this.announcements.set(res.data ?? res ?? []);
          this.currentPage.set(1);
        },
        error: () => this.showError('فشل في تحميل الإعلانات'),
      });
    } else {
      this.announcementService.getAll().subscribe({
        next: (res) => {
          this.announcements.set(res.data ?? res ?? []);
          this.currentPage.set(1);
        },
        error: () => this.showError('فشل في تحميل الإعلانات'),
      });
    }
  }

  toggleExpired() {
    this.showExpired.update(v => !v);
    this.loadAnnouncements();
  }

  cleanupExpired() {
    if (!confirm('هل أنت متأكد من حذف جميع الإعلانات المنتهية صلاحيتها؟')) return;
    this.announcementService.cleanupExpired().subscribe({
      next: (res) => {
        this.showSuccess(res?.message || 'تم تنظيف الإعلانات المنتهية بنجاح');
        this.loadAnnouncements();
      },
      error: () => this.showError('فشل في تنظيف الإعلانات المنتهية'),
    });
  }

  isExpiringSoon(expiresAt: string): boolean {
    const diff = new Date(expiresAt).getTime() - Date.now();
    return diff > 0 && diff < 86400000 * 3; // أقل من 3 أيام
  }

  resetForm() {
    this.editingId.set(null);
    this.newAnnouncement = {
      title: '',
      body: '',
      category: 0,
      isForAllUsers: true,
      isForAllStudents: false,
      isForAllParents: false,
      isForAllTeachers: false,
      expiresAt: undefined,
    };
  }

  editAnnouncement(a: Announcement) {
    this.editingId.set(a.id);
    this.newAnnouncement = {
      title: a.title,
      body: a.body,
      category: normalizeCategory(a.category),
      isForAllUsers: a.isForAllUsers,
      isForAllStudents: a.isForAllStudents,
      isForAllParents: a.isForAllParents,
      isForAllTeachers: a.isForAllTeachers,
      expiresAt: a.expiresAt ? a.expiresAt.slice(0, 16) : undefined,
    };
  }

  cancelEdit() {
    this.resetForm();
  }

  saveAnnouncement() {
    if (!this.newAnnouncement.title?.trim() || !this.newAnnouncement.body?.trim()) {
      this.showError('العنوان والمحتوى مطلوبان');
      return;
    }

    const payload: CreateAnnouncementRequest = {
      ...this.newAnnouncement,
      expiresAt: this.newAnnouncement.expiresAt
        ? new Date(this.newAnnouncement.expiresAt).toISOString()
        : undefined,
    };

    if (this.editingId()) {
      this.announcementService.update(this.editingId()!, payload).subscribe({
        next: () => {
          this.loadAnnouncements();
          this.resetForm();
          this.showSuccess('تم تحديث الإعلان بنجاح');
        },
        error: (err) => {
          const msg = err.error?.message || err.error || 'فشل في تحديث الإعلان';
          this.showError(msg);
        },
      });
    } else {
      this.announcementService.create(payload).subscribe({
        next: () => {
          this.loadAnnouncements();
          this.resetForm();
          this.showSuccess('تم إنشاء الإعلان بنجاح');
        },
        error: (err) => {
          const msg = err.error?.message || err.error || 'فشل في إنشاء الإعلان';
          this.showError(msg);
        },
      });
    }
  }

  deleteConfirmId = signal<number | null>(null);

  deleteAnnouncement(id: number) {
    this.deleteConfirmId.set(id);
  }

  cancelDelete() {
    this.deleteConfirmId.set(null);
  }

  confirmDelete() {
    const id = this.deleteConfirmId();
    if (!id) return;
    this.deleteConfirmId.set(null);
    this.announcementService.delete(id).subscribe({
      next: () => {
        this.loadAnnouncements();
        this.showSuccess('تم حذف الإعلان بنجاح');
      },
      error: () => this.showError('فشل في حذف الإعلان'),
    });
  }

  getCategoryLabel(value: number | string | undefined): string {
    return this.categoryOptions.find(c => c.value === normalizeCategory(value))?.label ?? 'عام';
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
