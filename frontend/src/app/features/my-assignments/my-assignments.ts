import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentAssignmentListItem, StudentAssignmentStatus } from '../../core/models/student-assignment.models';
import { StudentAssignmentsService } from '../../core/services/student-assignments.service';

type AssignmentTab = 'all' | 'pending' | 'submitted' | 'graded' | 'late';

@Component({
  selector: 'app-my-assignments',
  standalone: true,
  imports: [CommonModule, Sidebar, DatePipe],
  templateUrl: './my-assignments.html',
  styleUrl: './my-assignments.css'
})
export class MyAssignments implements OnInit {
  private assignmentsService = inject(StudentAssignmentsService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  activeTab = signal<AssignmentTab>('all');
  assignments = signal<StudentAssignmentListItem[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  totalCount = computed(() => this.assignments().length);
  pendingCount = computed(() => this.assignments().filter(a => a.status === 'pending').length);
  submittedCount = computed(() => this.assignments().filter(a => a.status === 'submittedWaitingGrade').length);
  gradedCount = computed(() => this.assignments().filter(a => a.status === 'graded').length);
  lateCount = computed(() => this.assignments().filter(a => a.status === 'late').length);

  filteredAssignments = computed(() => {
    const tab = this.activeTab();
    const items = this.assignments();

    if (tab === 'all') return items;
    if (tab === 'submitted') return items.filter(a => a.status === 'submittedWaitingGrade');
    return items.filter(a => a.status === tab);
  });

  ngOnInit() {
    this.loadAssignments();
  }

  loadAssignments() {
    this.isLoading.set(true);
    this.assignmentsService.getMyAssignments().subscribe({
      next: result => {
        this.assignments.set(result.data ?? []);
        this.isLoading.set(false);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الواجبات'));
        this.isLoading.set(false);
      }
    });
  }

  openAssignment(assignment: StudentAssignmentListItem) {
    if (assignment.status !== 'pending') return;
    this.router.navigate(['/student-assignments', assignment.assignmentId, 'take']);
  }

  openSubmission(assignment: StudentAssignmentListItem) {
    if (!assignment.submissionId) return;
    this.router.navigate(['/student-assignments/submissions', assignment.submissionId]);
  }

  getStatusText(status: StudentAssignmentStatus): string {
    const map: Record<StudentAssignmentStatus, string> = {
      pending: 'لم يسلم',
      late: 'انتهى الموعد',
      submittedWaitingGrade: 'بانتظار التصحيح',
      graded: 'تم التصحيح'
    };

    return map[status] ?? status;
  }

  getStatusClass(status: StudentAssignmentStatus): string {
    const map: Record<StudentAssignmentStatus, string> = {
      pending: 'bg-amber-50 text-amber-700 border-amber-100',
      late: 'bg-red-50 text-red-700 border-red-100',
      submittedWaitingGrade: 'bg-indigo-50 text-indigo-700 border-indigo-100',
      graded: 'bg-green-50 text-green-700 border-green-100'
    };

    return map[status] ?? 'bg-gray-50 text-gray-700 border-gray-100';
  }

  getActionLabel(assignment: StudentAssignmentListItem): string {
    if (assignment.status === 'pending') return 'ابدأ الحل';
    if (assignment.status === 'graded') return 'عرض الدرجة';
    if (assignment.status === 'submittedWaitingGrade') return 'حالة التسليم';
    return 'انتهى الموعد';
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    const error = err as { error?: { message?: string }; message?: string };
    return error.error?.message || error.message || fallback;
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }
}
