import { Component, signal, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { NotificationService, NotificationDto } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule, Sidebar, Topbar],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css',
})
export class Notifications implements OnInit {
  private notifService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  loading = signal(true);
  notifications = signal<NotificationDto[]>([]);
  filteredNotifications = signal<NotificationDto[]>([]);
  activeFilter = signal<'all' | 'academic' | 'general' | 'system'>('all');
  userId!: number;

  // Notification type ranges for filtering
  private academicTypes = [1, 2, 3, 7, 9, 10, 11, 16, 26, 27, 28];
  private generalTypes = [17, 18, 19, 20, 21, 22, 23, 24, 29];
  private systemTypes = [8];

  ngOnInit() {
    const user = this.authService.user();
    if (!user) {
      this.loading.set(false);
      return;
    }
    this.userId = user.userId;
    this.loadNotifications();
  }

  loadNotifications() {
    this.loading.set(true);
    this.notifService.getNotifications(this.userId).subscribe({
      next: (data) => {
        this.notifications.set(data);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  applyFilter() {
    const filter = this.activeFilter();
    let items = this.notifications();
    switch (filter) {
      case 'academic':
        items = items.filter(n => this.academicTypes.includes(n.type));
        break;
      case 'general':
        items = items.filter(n => this.generalTypes.includes(n.type));
        break;
      case 'system':
        items = items.filter(n => this.systemTypes.includes(n.type));
        break;
    }
    this.filteredNotifications.set(items);
  }

  setFilter(filter: 'all' | 'academic' | 'general' | 'system') {
    this.activeFilter.set(filter);
    this.applyFilter();
  }

  markAsRead(notif: NotificationDto) {
    if (notif.isRead) return;
    this.notifService.markAsRead(notif.id).subscribe({
      next: () => {
        notif.isRead = true;
        this.notifService.getUnreadCount(this.userId).subscribe();
      }
    });
  }

  markAllAsRead() {
    this.notifService.markAllAsRead(this.userId).subscribe({
      next: () => {
        this.notifications.update(items => items.map(n => ({ ...n, isRead: true })));
        this.notifService.unreadCount.set(0);
      }
    });
  }

  deleteNotification(id: number, event: Event) {
    event.stopPropagation();
    this.notifService.deleteNotification(id).subscribe({
      next: () => {
        this.notifications.update(items => items.filter(n => n.id !== id));
        this.applyFilter();
      }
    });
  }

  getTypeIcon(type: number): string {
    if (type === 1 || type === 7 || type === 16) return 'grade';
    if (type === 2 || type === 11) return 'gavel';
    if (type === 3 || type === 28) return 'person_off';
    if (type === 9 || type === 10) return 'trending_up';
    if (type === 26 || type === 27) return 'warning';
    if (type === 17 || type === 18) return 'campaign';
    if (type === 19) return 'celebration';
    if (type === 20) return 'emergency';
    if (type === 21) return 'calendar_changed';
    if (type === 22) return 'swap_horiz';
    if (type === 23) return 'chat_bubble';
    if (type === 24) return 'group_add';
    if (type === 29) return 'event';
    if (type === 8) return 'notification_important';
    return 'notifications';
  }

  getTypeColor(type: number): string {
    if ([1, 7, 16].includes(type)) return '#dc2626';
    if ([2, 11].includes(type)) return '#ea580c';
    if ([3, 28].includes(type)) return '#6b7280';
    if ([9, 10].includes(type)) return '#0d7a5f';
    if ([26, 27].includes(type)) return '#d97706';
    if (type === 20) return '#b91c1c';
    return '#00236f';
  }

  getTimeAgo(dateStr: string): string {
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const mins = Math.floor(diffMs / 60000);
    if (mins < 1) return 'الآن';
    if (mins < 60) return `منذ ${mins} دقيقة`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `منذ ${hours} ساعة`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `منذ ${days} أيام`;
    return date.toLocaleDateString('ar-EG');
  }

  getStatusLabel(type: number): string {
    if ([1, 7, 16, 9, 10].includes(type)) return 'إنجاز';
    if ([2, 11, 26, 27].includes(type)) return 'تنبيه';
    if ([3, 28].includes(type)) return 'متوسط';
    if (type === 20) return 'حرج';
    return 'عام';
  }

  getStatusClass(type: number): string {
    if ([1, 7, 16, 9, 10].includes(type)) return 'badge-success';
    if ([2, 11, 26, 27].includes(type)) return 'badge-warning';
    if ([3, 28].includes(type)) return 'badge-secondary';
    if (type === 20) return 'badge-danger';
    return 'badge-info';
  }
}
