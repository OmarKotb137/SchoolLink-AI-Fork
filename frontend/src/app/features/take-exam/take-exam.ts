import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import {
  StudentExamAnswerPayload,
  StudentExamAttemptStarted,
  StudentExamDetails,
  StudentExamQuestion,
  StudentQuestionType
} from '../../core/models/student-exam.models';
import { StudentExamsService } from '../../core/services/student-exams.service';

@Component({
  selector: 'app-take-exam',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './take-exam.html',
  styleUrl: './take-exam.css'
})
export class TakeExam implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examsService = inject(StudentExamsService);

  readonly QuestionType = StudentQuestionType;

  sidebarOpen = signal(false);
  exam = signal<StudentExamDetails | null>(null);
  attempt = signal<StudentExamAttemptStarted | null>(null);
  answers = signal<Record<number, StudentExamAnswerPayload>>({});
  isLoading = signal(false);
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);
  confirmSubmitOpen = signal(false);
  remainingSeconds = signal(0);

  private examId = Number(this.route.snapshot.paramMap.get('examId'));
  private attemptId = Number(this.route.snapshot.paramMap.get('attemptId'));
  private timerId: ReturnType<typeof setInterval> | null = null;

  answeredCount = computed(() => {
    const values = Object.values(this.answers());
    return values.filter(answer =>
      !!answer.answerText?.trim() ||
      answer.selectedOptionId != null ||
      answer.booleanAnswer != null
    ).length;
  });

  totalQuestions = computed(() => this.exam()?.questions.length ?? 0);
  progressPercent = computed(() => {
    const total = this.totalQuestions();
    return total ? Math.round((this.answeredCount() / total) * 100) : 0;
  });

  timeLabel = computed(() => {
    const value = Math.max(0, this.remainingSeconds());
    const minutes = Math.floor(value / 60).toString().padStart(2, '0');
    const seconds = (value % 60).toString().padStart(2, '0');
    return `${minutes}:${seconds}`;
  });

  ngOnInit() {
    this.loadExam();
  }

  ngOnDestroy() {
    this.stopTimer();
  }

  loadExam() {
    this.isLoading.set(true);
    this.examsService.getExamDetails(this.examId).subscribe({
      next: detailsResult => {
        this.exam.set(detailsResult.data);
        this.examsService.getActiveAttempt(this.examId).subscribe({
          next: attemptResult => {
            const attempt = attemptResult.data;
            this.attempt.set(attempt);
            if (attempt?.attemptId !== this.attemptId) {
              this.router.navigate(['/student-exams', this.examId, 'take', attempt?.attemptId ?? this.attemptId]);
              return;
            }
            this.restoreDraft();
            this.startTimer(attempt);
            this.isLoading.set(false);
          },
          error: err => {
            this.showError(this.extractErrorMessage(err, 'تعذر تحميل محاولة الامتحان'));
            this.isLoading.set(false);
          }
        });
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الامتحان'));
        this.isLoading.set(false);
      }
    });
  }

  updateOption(question: StudentExamQuestion, optionId: number) {
    this.setAnswer(question.id, { questionId: question.id, selectedOptionId: optionId, answerText: null, booleanAnswer: null });
  }

  updateBoolean(question: StudentExamQuestion, value: boolean) {
    this.setAnswer(question.id, { questionId: question.id, booleanAnswer: value, selectedOptionId: null, answerText: null });
  }

  updateText(question: StudentExamQuestion, value: string) {
    this.setAnswer(question.id, { questionId: question.id, answerText: value, selectedOptionId: null, booleanAnswer: null });
  }

  getAnswer(questionId: number): StudentExamAnswerPayload | undefined {
    return this.answers()[questionId];
  }

  openSubmitConfirm() {
    this.confirmSubmitOpen.set(true);
  }

  cancelSubmit() {
    this.confirmSubmitOpen.set(false);
  }

  submitExam() {
    if (this.isSubmitting()) return;

    const payload = this.buildPayload();
    this.isSubmitting.set(true);
    this.confirmSubmitOpen.set(false);

    this.examsService.submitAttempt(this.attemptId, payload).subscribe({
      next: () => {
        this.examsService.clearDraftFromLocalStorage(this.attemptId);
        this.isSubmitting.set(false);
        this.router.navigate(['/student-exams/results', this.attemptId]);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تسليم الامتحان'));
        this.isSubmitting.set(false);
      }
    });
  }

  scrollToQuestion(questionId: number) {
    document.getElementById(`question-${questionId}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  private setAnswer(questionId: number, answer: StudentExamAnswerPayload) {
    this.answers.update(current => ({ ...current, [questionId]: answer }));
    this.saveDraft();
  }

  private buildPayload(): StudentExamAnswerPayload[] {
    const questions = this.exam()?.questions ?? [];
    const current = this.answers();
    return questions.map(question => current[question.id] ?? { questionId: question.id, answerText: null, selectedOptionId: null, booleanAnswer: null });
  }

  private restoreDraft() {
    const draft = this.examsService.loadDraftFromLocalStorage(this.attemptId);
    if (!draft?.answers) return;

    const restored = draft.answers.reduce<Record<number, StudentExamAnswerPayload>>((acc, answer) => {
      acc[answer.questionId] = answer;
      return acc;
    }, {});
    this.answers.set(restored);
  }

  private saveDraft() {
    this.examsService.saveDraftToLocalStorage(this.attemptId, {
      attemptId: this.attemptId,
      examId: this.examId,
      updatedAt: new Date().toISOString(),
      answers: this.buildPayload()
    });
  }

  private startTimer(attempt: StudentExamAttemptStarted | null | undefined) {
    this.stopTimer();
    if (!attempt?.endsAt) return;

    const serverNow = new Date(attempt.serverNow).getTime();
    const localNow = Date.now();
    const offset = serverNow - localNow;
    const end = new Date(attempt.endsAt).getTime();

    const tick = () => {
      const remaining = Math.max(0, Math.floor((end - (Date.now() + offset)) / 1000));
      this.remainingSeconds.set(remaining);
      if (remaining === 0) {
        this.stopTimer();
        this.submitExam();
      }
    };

    tick();
    this.timerId = setInterval(tick, 1000);
  }

  private stopTimer() {
    if (this.timerId) {
      clearInterval(this.timerId);
      this.timerId = null;
    }
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
