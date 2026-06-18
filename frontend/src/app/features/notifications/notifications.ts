import { Component, signal, computed, OnInit, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { NotificationService, NotificationDto } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationSignalRService } from '../../core/services/notification-signalr.service';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule, Sidebar],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css',
})
export class Notifications implements OnInit {
  private notifService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notifSignalR = inject(NotificationSignalRService);

  sidebarOpen = signal(false);
  loading = signal(true);
  notifications = signal<NotificationDto[]>([]);
  filteredNotifications = signal<NotificationDto[]>([]);
  activeFilter = signal<'all' | 'academic' | 'general' | 'system'>('all');
  userId!: number;
  userName = computed(() => this.authService.user()?.fullName ?? 'ولي الأمر');

  ngOnInit() {
    const user = this.authService.user();
    if (user?.userId) {
      this.userId = user.userId;
      this.loadNotifications();
    }

    // Start SignalR connection for real-time notifications
    this.notifSignalR.startConnection();
  }

  constructor() {
    // React to new notifications from SignalR - prepend to list
    effect(() => {
      const notif = this.notifSignalR.newNotification();
      if (notif) {
        this.notifications.update(list => [notif, ...list]);
        this.applyFilter();
      }
    });
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
      },
    });
  }

  applyFilter() {
    const filter = this.activeFilter();
    let filtered = this.notifications();
    if (filter !== 'all') {
      filtered = filtered.filter((n) => this.getCategory(n.type) === filter);
    }
    this.filteredNotifications.set(filtered);
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
        this.notifService.unreadCount.update((c) => Math.max(0, c - 1));
      },
    });
  }

  markAllAsRead() {
    this.notifService.markAllAsRead(this.userId).subscribe({
      next: () => {
        this.notifications.update((list) =>
          list.map((n) => ({ ...n, isRead: true }))
        );
        this.applyFilter();
        this.notifService.unreadCount.set(0);
      },
    });
  }

  deleteNotification(id: number, event: MouseEvent) {
    event.stopPropagation();
    this.notifService.deleteNotification(id).subscribe({
      next: () => {
        this.notifications.update((list) => list.filter((n) => n.id !== id));
        this.applyFilter();
        this.notifService.unreadCount.update((c) => Math.max(0, c - 1));
      },
    });
  }

  private getCategory(type: number): 'academic' | 'general' | 'system' {
    const academic = [1, 2, 3, 6, 7, 9, 10, 11, 16, 26, 27, 28];
    const training = [4, 5, 12, 13, 14, 15, 32, 33, 34];
    const general = [17, 18, 19, 20, 21, 22, 23, 24, 29];
    const system = [8];

    if (academic.includes(type) || training.includes(type)) return 'academic';
    if (general.includes(type)) return 'general';
    if (system.includes(type)) return 'system';
    return 'general';
  }

  getTypeIcon(type: number): string {
    const icons: Record<number, string> = {
      1: 'grade',               // GradeAlert
      2: 'error',               // BehaviorAlert
      3: 'event_busy',          // AbsenceAlert
      4: 'assignment',          // NewAssignment
      5: 'quiz',                // ExamReminder
      6: 'summarize',           // MonthlyReport
      7: 'check_circle',        // GradePublished
      8: 'info',                // SystemAlert
      9: 'trending_up',         // ImprovementAlert
      10: 'mood',               // PositiveBehavior
      11: 'gavel',              // DisciplinaryAction
      12: 'home_work',          // HomeworkSubmitted
      13: 'task_alt',           // HomeworkGraded
      14: 'fact_check',         // Exam
      15: 'score',              // ExamResult
      16: 'emoji_events',       // TopStudent
      17: 'campaign',           // Announcement
      18: 'event',              // SchoolEvent
      19: 'celebration',        // Holiday
      20: 'warning',            // EmergencyAlert
      21: 'calendar_month',     // ScheduleChanged
      22: 'swap_horiz',         // SubstituteTeacher
      23: 'chat',               // NewMessage
      24: 'group_add',          // GroupChatInvite
      26: 'trending_down',      // GradeThresholdAlert
      27: 'school',             // AcademicProbation
      28: 'warning_amber',      // ExcessiveAbsenceWarning
      29: 'meeting_room',       // ParentMeetingRequest
      32: 'update',             // ExamScheduleChanged
      33: 'publish',            // ExamSchedulePublished
      34: 'block',              // ExamCheatingAlert
    };
    return icons[type] ?? 'notifications';
  }

  getTypeColor(type: number): string {
    const academicColors = [1, 2, 3, 6, 7, 9, 10, 11, 16, 26, 27, 28];
    const trainingColors = [4, 5, 12, 13, 14, 15, 32, 33, 34];
    const generalColors = [17, 18, 19, 20, 21, 22, 23, 24, 29];

    if (academicColors.includes(type)) return '#2563eb';
    if (trainingColors.includes(type)) return '#7c3aed';
    if (generalColors.includes(type)) return '#059669';
    return '#6b7280';
  }

  getStatusLabel(type: number): string {
    const category = this.getCategory(type);
    switch (category) {
      case 'academic': return 'أكاديمي';
      case 'general': return 'عام';
      case 'system': return 'النظام';
    }
  }

  getStatusClass(type: number): string {
    const category = this.getCategory(type);
    switch (category) {
      case 'academic': return 'badge-academic';
      case 'general': return 'badge-general';
      case 'system': return 'badge-system';
    }
  }

  getTimeAgo(dateStr: string): string {
    if (!dateStr) return '';
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'الآن';
    if (diffMins < 60) return `منذ ${diffMins} دقيقة`;
    if (diffHours < 24) return `منذ ${diffHours} ساعة`;
    if (diffDays < 7) return `منذ ${diffDays} يوم`;
    return date.toLocaleDateString('ar-EG');
  }
}
