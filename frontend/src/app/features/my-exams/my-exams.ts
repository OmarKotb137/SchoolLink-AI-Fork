import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { StudentExamListItem, StudentExamStatus } from '../../core/models/student-exam.models';
import { StudentExamsService } from '../../core/services/student-exams.service';

type ExamTab = 'all' | 'available' | 'upcoming' | 'submitted' | 'results';

@Component({
  selector: 'app-my-exams',
  standalone: true,
  imports: [CommonModule, Sidebar, Topbar, DatePipe],
  templateUrl: './my-exams.html',
  styleUrl: './my-exams.css'
})
export class MyExams implements OnInit {
  private examsService = inject(StudentExamsService);
  private router = inject(Router);

  sidebarOpen = signal(false);
  activeTab = signal<ExamTab>('all');
  exams = signal<StudentExamListItem[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  totalCount = computed(() => this.exams().length);
  availableCount = computed(() => this.exams().filter(e => e.status === 'available' || e.status === 'inProgress').length);
  submittedCount = computed(() => this.exams().filter(e => e.status === 'submittedWaitingGrade' || e.status === 'gradedHidden').length);
  resultCount = computed(() => this.exams().filter(e => e.status === 'resultVisible').length);

  filteredExams = computed(() => {
    const tab = this.activeTab();
    const items = this.exams();

    if (tab === 'all') return items;
    if (tab === 'available') return items.filter(e => e.status === 'available' || e.status === 'inProgress');
    if (tab === 'upcoming') return items.filter(e => e.status === 'upcoming');
    if (tab === 'submitted') return items.filter(e => e.status === 'submittedWaitingGrade' || e.status === 'gradedHidden');
    return items.filter(e => e.status === 'resultVisible');
  });

  ngOnInit() {
    this.loadExams();
  }

  loadExams() {
    this.isLoading.set(true);
    this.examsService.getMyExams().subscribe({
      next: result => {
        this.exams.set(result.data ?? []);
        this.isLoading.set(false);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الامتحانات'));
        this.isLoading.set(false);
      }
    });
  }

  startExam(exam: StudentExamListItem) {
    if (exam.status === 'inProgress' && exam.attemptId) {
      this.router.navigate(['/student-exams', exam.examId, 'take', exam.attemptId]);
      return;
    }

    this.isLoading.set(true);
    this.examsService.startExam(exam.examId).subscribe({
      next: result => {
        const attempt = result.data;
        this.isLoading.set(false);
        if (attempt) {
          this.router.navigate(['/student-exams', exam.examId, 'take', attempt.attemptId]);
        }
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر بدء الامتحان'));
        this.isLoading.set(false);
      }
    });
  }

  openResult(exam: StudentExamListItem) {
    if (!exam.attemptId) return;
    this.router.navigate(['/student-exams/results', exam.attemptId]);
  }

  getStatusText(status: StudentExamStatus): string {
    const map: Record<StudentExamStatus, string> = {
      upcoming: 'قادم',
      available: 'متاح الآن',
      inProgress: 'قيد الحل',
      submittedWaitingGrade: 'بانتظار التصحيح',
      gradedHidden: 'لم تعلن النتيجة',
      resultVisible: 'النتيجة متاحة',
      expired: 'انتهى'
    };

    return map[status] ?? status;
  }

  getStatusClass(status: StudentExamStatus): string {
    const map: Record<StudentExamStatus, string> = {
      upcoming: 'bg-blue-50 text-blue-700 border-blue-100',
      available: 'bg-emerald-50 text-emerald-700 border-emerald-100',
      inProgress: 'bg-amber-50 text-amber-700 border-amber-100',
      submittedWaitingGrade: 'bg-slate-50 text-slate-700 border-slate-200',
      gradedHidden: 'bg-indigo-50 text-indigo-700 border-indigo-100',
      resultVisible: 'bg-green-50 text-green-700 border-green-100',
      expired: 'bg-red-50 text-red-700 border-red-100'
    };

    return map[status] ?? 'bg-gray-50 text-gray-700 border-gray-100';
  }

  canStart(exam: StudentExamListItem): boolean {
    return exam.status === 'available' || exam.status === 'inProgress';
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    const error = err as { error?: { message?: string }; message?: string };
    return error.error?.message || error.message || fallback;
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    this.successMessage.set(null);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }
}
