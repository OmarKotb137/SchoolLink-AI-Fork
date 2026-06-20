import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentAssignmentListItem, StudentAssignmentStatus } from '../../core/models/student-assignment.models';
import { StudentAssignmentsService } from '../../core/services/student-assignments.service';

type AssignmentTab = 'all' | 'pending' | 'submitted' | 'graded' | 'late';

@Component({
  selector: 'app-my-assignments',
  standalone: true,
  imports: [CommonModule, Sidebar, DatePipe, FormsModule],
  templateUrl: './my-assignments.html',
  styleUrl: './my-assignments.css'
})
export class MyAssignments implements OnInit {
  private assignmentsService = inject(StudentAssignmentsService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  activeTab = signal<string>('all');
  subjectFilter = signal<number | undefined>(undefined);
  sortBy = signal<string>('newest');
  subjectList = signal<{ id: number; name: string }[]>([]);
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
    const subj = this.subjectFilter();
    const sort = this.sortBy();
    let items = this.assignments();

    if (tab === 'submitted') items = items.filter(a => a.status === 'submittedWaitingGrade');
    else if (tab !== 'all') items = items.filter(a => a.status === tab);

    if (subj) {
      const name = this.subjectList().find(s => s.id === subj)?.name;
      if (name) items = items.filter(a => a.subjectName === name);
    }

    return [...items].sort((a, b) => {
      if (sort === 'oldest') return compDate(a.dueDate, b.dueDate);
      if (sort === 'name-asc') return a.title.localeCompare(b.title);
      if (sort === 'name-desc') return b.title.localeCompare(a.title);
      if (sort === 'score-desc') return (b.score ?? 0) - (a.score ?? 0);
      return compDate(b.dueDate, a.dueDate); // newest = default
    });

    function compDate(da: string | null | undefined, db: string | null | undefined): number {
      if (!da && !db) return 0;
      if (!da) return 1;
      if (!db) return -1;
      return new Date(da).getTime() - new Date(db).getTime();
    }
  });

  readonly assignmentTabs = [
    { key: 'all', label: 'الكل' },
    { key: 'pending', label: 'لم يسلم' },
    { key: 'submitted', label: 'بانتظار التصحيح' },
    { key: 'graded', label: 'تم التصحيح' },
    { key: 'late', label: 'انتهى الموعد' },
  ];

  readonly sortOptions = [
    { value: 'newest',     label: 'الأحدث'          },
    { value: 'oldest',     label: 'الأقدم'          },
    { value: 'name-asc',   label: 'أ - ي'           },
    { value: 'name-desc',  label: 'ي - أ'           },
    { value: 'score-desc', label: 'الدرجة (تنازلي)' },
  ];

  ngOnInit() {
    this.loadAssignments();
  }

  onSubjectFilterChange(value: number | undefined) {
    this.subjectFilter.set(value);
  }

  loadAssignments() {
    this.isLoading.set(true);
    this.assignmentsService.getMyAssignments().subscribe({
      next: result => {
        this.assignments.set(result.data ?? []);
        this.loadSubjects();
        this.isLoading.set(false);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الواجبات'));
        this.isLoading.set(false);
      }
    });
  }

  private loadSubjects() {
    const seen = new Set<string>();
    const list: { id: number; name: string }[] = [];
    for (const a of this.assignments()) {
      if (a.subjectName && !seen.has(a.subjectName)) {
        seen.add(a.subjectName);
        list.push({ id: list.length + 1, name: a.subjectName });
      }
    }
    this.subjectList.set(list);
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
