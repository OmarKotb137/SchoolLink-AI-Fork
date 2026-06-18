import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentExamAttemptResult } from '../../core/models/student-exam.models';
import { StudentExamsService } from '../../core/services/student-exams.service';

@Component({
  selector: 'app-exam-result',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './exam-result.html',
  styleUrl: './exam-result.css'
})
export class ExamResult implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examsService = inject(StudentExamsService);

  result = signal<StudentExamAttemptResult | null>(null);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  sidebarOpen = signal(false);

  private attemptId = Number(this.route.snapshot.paramMap.get('attemptId'));

  ngOnInit() {
    this.loadResult();
  }

  loadResult() {
    this.isLoading.set(true);
    this.examsService.getAttemptResult(this.attemptId).subscribe({
      next: response => {
        this.result.set(response.data);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(this.extractErrorMessage(err, 'تعذر تحميل حالة النتيجة'));
        this.isLoading.set(false);
      }
    });
  }

  backToExams() {
    this.router.navigate(['/my-exams']);
  }

  getScorePercent(result: StudentExamAttemptResult): number {
    if (!result.totalScore || result.score == null) return 0;
    return Math.round((result.score / result.totalScore) * 100);
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    const error = err as { error?: { message?: string }; message?: string };
    return error.error?.message || error.message || fallback;
  }
}
