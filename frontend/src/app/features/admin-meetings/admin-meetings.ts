import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { ParentMeetingService, ParentMeetingRequestDto, MeetingRequestStatus, normalizeMeetingStatus } from '../../core/services/parent-meeting.service';

@Component({
  selector: 'app-admin-meetings',
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './admin-meetings.html',
  styleUrl: './admin-meetings.css',
})
export class AdminMeetings implements OnInit {
  private meetingService = inject(ParentMeetingService);

  sidebarOpen = signal(false);
  loading = signal(true);
  allRequests = signal<ParentMeetingRequestDto[]>([]);
  activeFilter = signal<'all' | 'pending' | 'approved' | 'rejected' | 'completed'>('all');

  // Expose normalize function for template
  normalizeStatus = normalizeMeetingStatus;

  // Approve modal
  showApproveModal = signal(false);
  approveRequestId = signal<number | null>(null);
  approveDate = signal('');
  approveTime = signal('');
  approveSubmitting = signal(false);

  // Reject modal
  showRejectModal = signal(false);
  rejectRequestId = signal<number | null>(null);
  rejectReason = signal('');
  rejectSubmitting = signal(false);

  filteredRequests = computed(() => {
    const filter = this.activeFilter();
    const all = this.allRequests();
    if (filter === 'all') return all;
    const statusMap: Record<string, number> = {
      pending: MeetingRequestStatus.Pending,
      approved: MeetingRequestStatus.Approved,
      rejected: MeetingRequestStatus.Rejected,
      completed: MeetingRequestStatus.Completed,
    };
    return all.filter(r => normalizeMeetingStatus(r.status) === statusMap[filter]);
  });

  get counts() {
    const all = this.allRequests();
    return {
      all: all.length,
      pending: all.filter(r => normalizeMeetingStatus(r.status) === MeetingRequestStatus.Pending).length,
      approved: all.filter(r => normalizeMeetingStatus(r.status) === MeetingRequestStatus.Approved).length,
      rejected: all.filter(r => normalizeMeetingStatus(r.status) === MeetingRequestStatus.Rejected).length,
      completed: all.filter(r => normalizeMeetingStatus(r.status) === MeetingRequestStatus.Completed).length,
    };
  }

  ngOnInit() {
    this.loadRequests();
  }

  private loadRequests() {
    this.loading.set(true);
    this.meetingService.getAll().subscribe({
      next: (result) => {
        const data = result.data ?? result ?? [];
        this.allRequests.set(Array.isArray(data) ? data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  setFilter(filter: 'all' | 'pending' | 'approved' | 'rejected' | 'completed') {
    this.activeFilter.set(filter);
  }

  // Approve
  openApproveModal(request: ParentMeetingRequestDto) {
    this.approveRequestId.set(request.id);
    this.approveDate.set('');
    this.approveTime.set('');
    this.showApproveModal.set(true);
  }

  closeApproveModal() {
    this.showApproveModal.set(false);
    this.approveRequestId.set(null);
  }

  onApproveDateChange(event: Event) {
    this.approveDate.set((event.target as HTMLInputElement).value);
  }

  onApproveTimeChange(event: Event) {
    this.approveTime.set((event.target as HTMLInputElement).value);
  }

  submitApprove() {
    if (!this.approveRequestId() || !this.approveDate() || !this.approveTime()) return;

    this.approveSubmitting.set(true);
    const scheduledDate = `${this.approveDate()}T${this.approveTime()}`;

    this.meetingService.approveRequest(this.approveRequestId()!, scheduledDate).subscribe({
      next: () => {
        this.approveSubmitting.set(false);
        this.closeApproveModal();
        this.loadRequests();
      },
      error: () => {
        this.approveSubmitting.set(false);
      },
    });
  }

  // Reject
  openRejectModal(request: ParentMeetingRequestDto) {
    this.rejectRequestId.set(request.id);
    this.rejectReason.set('');
    this.showRejectModal.set(true);
  }

  closeRejectModal() {
    this.showRejectModal.set(false);
    this.rejectRequestId.set(null);
  }

  onRejectReasonChange(event: Event) {
    this.rejectReason.set((event.target as HTMLTextAreaElement).value);
  }

  submitReject() {
    if (!this.rejectRequestId()) return;

    this.rejectSubmitting.set(true);
    this.meetingService.rejectRequest(this.rejectRequestId()!, this.rejectReason() || undefined).subscribe({
      next: () => {
        this.rejectSubmitting.set(false);
        this.closeRejectModal();
        this.loadRequests();
      },
      error: () => {
        this.rejectSubmitting.set(false);
      },
    });
  }

  // Complete
  completeRequest(request: ParentMeetingRequestDto) {
    if (!confirm('هل أنت متأكد من إنهاء طلب الاجتماع؟')) return;
    this.meetingService.completeRequest(request.id).subscribe({
      next: () => this.loadRequests(),
      error: () => {},
    });
  }

  getStatusLabel(status: MeetingRequestStatus | number | string): string {
    const labels: Record<number, string> = {
      [MeetingRequestStatus.Pending]: 'قيد الانتظار',
      [MeetingRequestStatus.Approved]: 'تمت الموافقة',
      [MeetingRequestStatus.Rejected]: 'مرفوض',
      [MeetingRequestStatus.Completed]: 'تم الإنتهاء',
    };
    return labels[normalizeMeetingStatus(status)] ?? 'غير معروف';
  }

  getStatusColor(status: MeetingRequestStatus | number | string): string {
    const colors: Record<number, string> = {
      [MeetingRequestStatus.Pending]: '#f59e0b',
      [MeetingRequestStatus.Approved]: '#0d7a5f',
      [MeetingRequestStatus.Rejected]: '#dc2626',
      [MeetingRequestStatus.Completed]: '#6b7280',
    };
    return colors[normalizeMeetingStatus(status)] ?? '#6b7280';
  }

  getStatusIcon(status: MeetingRequestStatus | number | string): string {
    const icons: Record<number, string> = {
      [MeetingRequestStatus.Pending]: 'schedule',
      [MeetingRequestStatus.Approved]: 'check_circle',
      [MeetingRequestStatus.Rejected]: 'cancel',
      [MeetingRequestStatus.Completed]: 'task_alt',
    };
    return icons[normalizeMeetingStatus(status)] ?? 'help';
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

  formatDate(dateStr?: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('ar-EG', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}
