import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { StudentAssignmentSubmissionResult } from '../../core/models/student-assignment.models';
import { StudentAssignmentsService } from '../../core/services/student-assignments.service';

@Component({
  selector: 'app-assignment-submission-result',
  standalone: true,
  imports: [CommonModule, Sidebar, DatePipe],
  templateUrl: './assignment-submission-result.html',
  styleUrl: './assignment-submission-result.css'
})
export class AssignmentSubmissionResult implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private assignmentsService = inject(StudentAssignmentsService);

  sidebarOpen = signal(false);
  result = signal<StudentAssignmentSubmissionResult | null>(null);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  ngOnInit() {
    const submissionId = Number(this.route.snapshot.paramMap.get('submissionId'));
    if (!submissionId) {
      this.router.navigate(['/my-assignments']);
      return;
    }

    this.loadResult(submissionId);
  }

  loadResult(submissionId: number) {
    this.isLoading.set(true);
    this.assignmentsService.getSubmissionResult(submissionId).subscribe({
      next: result => {
        this.result.set(result.data);
        this.isLoading.set(false);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل نتيجة التسليم'));
        this.isLoading.set(false);
      }
    });
  }

  getScorePercent(r: StudentAssignmentSubmissionResult): number {
    if (!r.maxScore || r.score == null) return 0;
    return Math.round((r.score / r.maxScore) * 100);
  }

  backToAssignments() {
    this.router.navigate(['/my-assignments']);
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
