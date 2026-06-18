import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { AuthService } from '../../core/services/auth.service';
import { ParentStudentService, ParentStudentLink } from '../../core/services/parent-student.service';
import { ParentMeetingService, ParentMeetingRequestDto, MeetingRequestStatus, normalizeMeetingStatus } from '../../core/services/parent-meeting.service';

@Component({
  selector: 'app-parent-meeting-request',
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './parent-meeting-request.html',
  styleUrl: './parent-meeting-request.css',
})
export class ParentMeetingRequest implements OnInit {
  private auth = inject(AuthService);
  private parentStudentService = inject(ParentStudentService);
  private meetingService = inject(ParentMeetingService);

  sidebarOpen = signal(false);
  loading = signal(true);
  submitting = signal(false);
  submitted = signal(false);
  error = signal('');

  // بيانات النموذج
  studentId = signal<number | null>(null);
  reason = signal('');
  preferredDate = signal('');
  notes = signal('');

  // قائمة الأبناء
  children = signal<{ id: number; name: string }[]>([]);

  // طلباتي السابقة
  myRequests = signal<ParentMeetingRequestDto[]>([]);
  requestsLoading = signal(false);

  get statusLabels(): Record<number, string> {
    return {
      [MeetingRequestStatus.Pending]: 'قيد الانتظار',
      [MeetingRequestStatus.Approved]: 'تمت الموافقة',
      [MeetingRequestStatus.Rejected]: 'مرفوض',
      [MeetingRequestStatus.Completed]: 'تم الإنتهاء',
    };
  }

  get statusColors(): Record<number, string> {
    return {
      [MeetingRequestStatus.Pending]: '#f59e0b',
      [MeetingRequestStatus.Approved]: '#0d7a5f',
      [MeetingRequestStatus.Rejected]: '#dc2626',
      [MeetingRequestStatus.Completed]: '#6b7280',
    };
  }

  get statusIcons(): Record<number, string> {
    return {
      [MeetingRequestStatus.Pending]: 'schedule',
      [MeetingRequestStatus.Approved]: 'check_circle',
      [MeetingRequestStatus.Rejected]: 'cancel',
      [MeetingRequestStatus.Completed]: 'task_alt',
    };
  }

  canSubmit = computed(() => {
    return this.studentId() !== null && this.reason().trim().length > 0 && !this.submitting();
  });

  ngOnInit() {
    this.loadChildren();
    this.loadMyRequests();
  }

  private loadChildren() {
    const user = this.auth.user();
    if (!user?.userId) {
      this.loading.set(false);
      return;
    }

    this.parentStudentService.getStudentsByParent(user.userId).subscribe({
      next: (result) => {
        const data = result.data ?? result ?? [];
        const list = Array.isArray(data)
          ? data.map((s: any) => ({ id: s.id, name: s.fullName }))
          : [];
        this.children.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  private loadMyRequests() {
    const user = this.auth.user();
    if (!user?.userId) return;

    this.requestsLoading.set(true);
    this.meetingService.getByParent(user.userId).subscribe({
      next: (result) => {
        const data = result.data ?? result ?? [];
        this.myRequests.set(Array.isArray(data) ? data : []);
        this.requestsLoading.set(false);
      },
      error: () => {
        this.requestsLoading.set(false);
      },
    });
  }

  onStudentChange(event: Event) {
    const val = (event.target as HTMLSelectElement).value;
    this.studentId.set(val ? Number(val) : null);
  }

  onReasonInput(event: Event) {
    this.reason.set((event.target as HTMLTextAreaElement).value);
  }

  onDateInput(event: Event) {
    this.preferredDate.set((event.target as HTMLInputElement).value);
  }

  onNotesInput(event: Event) {
    this.notes.set((event.target as HTMLTextAreaElement).value);
  }

  submitRequest() {
    if (!this.canSubmit()) return;

    const user = this.auth.user();
    if (!user?.userId) return;

    this.submitting.set(true);
    this.error.set('');
    this.submitted.set(false);

    this.meetingService.createRequest({
      studentId: this.studentId()!,
      reason: this.reason().trim(),
      preferredDate: this.preferredDate() || undefined,
      notes: this.notes().trim() || undefined,
    }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.submitted.set(true);
        // Reset form
        this.studentId.set(null);
        this.reason.set('');
        this.preferredDate.set('');
        this.notes.set('');
        // Reload requests
        this.loadMyRequests();
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err.error?.message || err.message || 'حدث خطأ أثناء إرسال الطلب');
      },
    });
  }

  getStatusLabel(status: MeetingRequestStatus | number | string): string {
    return this.statusLabels[normalizeMeetingStatus(status)] ?? 'غير معروف';
  }

  getStatusColor(status: MeetingRequestStatus | number | string): string {
    return this.statusColors[normalizeMeetingStatus(status)] ?? '#6b7280';
  }

  getStatusIcon(status: MeetingRequestStatus | number | string): string {
    return this.statusIcons[normalizeMeetingStatus(status)] ?? 'help';
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ar-EG', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }

  formatDateTime(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ar-EG', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
}
