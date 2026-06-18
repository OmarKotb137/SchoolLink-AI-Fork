import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import {
  StudentAssignmentAnswerPayload,
  StudentAssignmentDetails,
  StudentAssignmentQuestion,
  StudentAssignmentQuestionType
} from '../../core/models/student-assignment.models';
import { StudentAssignmentsService } from '../../core/services/student-assignments.service';

@Component({
  selector: 'app-homework',
  standalone: true,
  imports: [CommonModule, Sidebar, DatePipe],
  templateUrl: './homework.html',
  styleUrl: './homework.css'
})
export class Homework implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private assignmentsService = inject(StudentAssignmentsService);

  readonly questionType = StudentAssignmentQuestionType;

  sidebarOpen = signal(false);
  assignment = signal<StudentAssignmentDetails | null>(null);
  answers = signal<Record<number, StudentAssignmentAnswerPayload>>({});
  isLoading = signal(false);
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);

  answeredCount = computed(() => {
    const assignment = this.assignment();
    if (!assignment) return 0;
    return assignment.questions.filter(q => this.hasAnswer(q)).length;
  });

  progressPercent = computed(() => {
    const assignment = this.assignment();
    if (!assignment || assignment.questions.length === 0) return 0;
    return Math.round((this.answeredCount() / assignment.questions.length) * 100);
  });

  ngOnInit() {
    const assignmentId = Number(this.route.snapshot.paramMap.get('assignmentId'));
    if (!assignmentId) {
      this.router.navigate(['/my-assignments']);
      return;
    }

    this.loadAssignment(assignmentId);
  }

  loadAssignment(assignmentId: number) {
    this.isLoading.set(true);
    this.assignmentsService.getAssignmentDetails(assignmentId).subscribe({
      next: result => {
        const assignment = result.data;
        this.assignment.set(assignment);
        this.loadDraft(assignment.assignmentId);
        this.isLoading.set(false);

        if (assignment.status === 'graded' || assignment.status === 'submittedWaitingGrade') {
          this.router.navigate(['/student-assignments/submissions', assignment.submissionId]);
        }
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الواجب'));
        this.isLoading.set(false);
      }
    });
  }

  selectOption(question: StudentAssignmentQuestion, optionId: number) {
    this.updateAnswer(question.id, {
      questionId: question.id,
      selectedOptionId: optionId,
      answerText: null,
      booleanAnswer: null
    });
  }

  selectBoolean(question: StudentAssignmentQuestion, value: boolean) {
    this.updateAnswer(question.id, {
      questionId: question.id,
      selectedOptionId: null,
      answerText: null,
      booleanAnswer: value
    });
  }

  updateText(question: StudentAssignmentQuestion, value: string) {
    this.updateAnswer(question.id, {
      questionId: question.id,
      selectedOptionId: null,
      answerText: value,
      booleanAnswer: null
    });
  }

  submit() {
    const assignment = this.assignment();
    if (!assignment || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    const answers = assignment.questions.map(q => this.answers()[q.id] ?? { questionId: q.id });

    this.assignmentsService.submitAssignment(assignment.assignmentId, answers).subscribe({
      next: result => {
        this.assignmentsService.clearDraft(assignment.assignmentId);
        this.isSubmitting.set(false);
        if (result.data?.submissionId) {
          this.router.navigate(['/student-assignments/submissions', result.data.submissionId]);
        } else {
          this.router.navigate(['/my-assignments']);
        }
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تسليم الواجب'));
        this.isSubmitting.set(false);
      }
    });
  }

  getAnswer(questionId: number): StudentAssignmentAnswerPayload | undefined {
    return this.answers()[questionId];
  }

  hasAnswer(question: StudentAssignmentQuestion): boolean {
    const answer = this.answers()[question.id];
    if (!answer) return false;

    if (question.questionType === StudentAssignmentQuestionType.MultipleChoice) return !!answer.selectedOptionId;
    if (question.questionType === StudentAssignmentQuestionType.TrueFalse) return answer.booleanAnswer !== null && answer.booleanAnswer !== undefined;
    return !!answer.answerText?.trim();
  }

  isPastDue(): boolean {
    const dueDate = this.assignment()?.dueDate;
    if (!dueDate) return false;
    // DueDate is stored as Egypt local time (UTC+3). The backend now compares correctly.
    // We use the backend's status field as the source of truth.
    return this.assignment()?.status === 'late';
  }

  private updateAnswer(questionId: number, answer: StudentAssignmentAnswerPayload) {
    this.answers.update(current => ({ ...current, [questionId]: answer }));
    const assignment = this.assignment();
    if (!assignment) return;

    this.assignmentsService.saveDraft(assignment.assignmentId, {
      assignmentId: assignment.assignmentId,
      updatedAt: new Date().toISOString(),
      answers: Object.values(this.answers())
    });
  }

  private loadDraft(assignmentId: number) {
    const draft = this.assignmentsService.loadDraft(assignmentId);
    if (!draft) return;

    const answerMap = draft.answers.reduce<Record<number, StudentAssignmentAnswerPayload>>((acc, answer) => {
      acc[answer.questionId] = answer;
      return acc;
    }, {});

    this.answers.set(answerMap);
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
