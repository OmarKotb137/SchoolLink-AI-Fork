import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';
import {
  SaveAnswerProgressPayload,
  SaveStatus,
  StudentExamAnswerPayload,
  StudentExamAttemptResult,
  StudentExamAttemptStarted,
  StudentExamDetails,
  StudentExamDraft,
  StudentExamListItem
} from '../models/student-exam.models';

@Injectable({ providedIn: 'root' })
export class StudentExamsService {
  private http = inject(HttpClient);
  private examsBase = buildApiUrl('student/exams');
  private attemptsBase = buildApiUrl('student/exam-attempts');

  // ===== Auto-Save: Save Queue =====
  // كل سؤال بياخد آخر قيمة بس — لو الطالب غيّر إجابته قبل ما القديمة تتبعت، الجديدة تستبدلها في المكان نفسه
  private pendingQueue = new Map<number, SaveAnswerProgressPayload>();
  // مؤقتات الـ debounce لأسئلة الكتابة (Essay / FillBlank) — القيمة لسه منتظرة قبل ما تنضم للـ Queue فعلياً
  private debounceTimers = new Map<number, ReturnType<typeof setTimeout>>();
  private debouncedPayloads = new Map<number, SaveAnswerProgressPayload>();

  private isFlushing = false;
  private queueStopped = false; // بيتفعّل لما السيرفر يرجع 409 (الامتحان اتسلّم بالفعل)
  private currentAttemptId: number | null = null;

  /** حالة الحفظ الحالية — تُعرض في الـ UI (idle / saving / saved / failed) */
  readonly saveStatus = signal<SaveStatus>('idle');

  getMyExams(): Observable<OperationResult<StudentExamListItem[]>> {
    return this.http.get<OperationResult<StudentExamListItem[]>>(this.examsBase);
  }

  getExamDetails(examId: number): Observable<OperationResult<StudentExamDetails>> {
    return this.http.get<OperationResult<StudentExamDetails>>(`${this.examsBase}/${examId}`);
  }

  startExam(examId: number): Observable<OperationResult<StudentExamAttemptStarted>> {
    return this.http.post<OperationResult<StudentExamAttemptStarted>>(`${this.examsBase}/${examId}/start`, {});
  }

  getActiveAttempt(examId: number): Observable<OperationResult<StudentExamAttemptStarted>> {
    return this.http.get<OperationResult<StudentExamAttemptStarted>>(`${this.examsBase}/${examId}/active-attempt`);
  }

  submitAttempt(attemptId: number, answers: StudentExamAnswerPayload[]): Observable<OperationResult<StudentExamAttemptResult>> {
    return this.http.post<OperationResult<StudentExamAttemptResult>>(`${this.attemptsBase}/${attemptId}/submit`, { answers });
  }

  getAttemptResult(attemptId: number): Observable<OperationResult<StudentExamAttemptResult>> {
    return this.http.get<OperationResult<StudentExamAttemptResult>>(`${this.attemptsBase}/${attemptId}/result`);
  }

  /** نداء الـ API لحفظ إجابة واحدة فوراً على السيرفر (Auto-Save) */
  saveAnswerProgress(attemptId: number, answer: SaveAnswerProgressPayload): Observable<OperationResult<void>> {
    return this.http.patch<OperationResult<void>>(`${this.attemptsBase}/${attemptId}/answers`, answer);
  }

  // ===== Save Queue API (تُستخدم من take-exam.ts) =====

  /** تصفير حالة الـ Queue عند بدء/استكمال محاولة جديدة، عشان مخلفات محاولة سابقة ما توقفش الجديدة */
  resetForAttempt(attemptId: number) {
    this.currentAttemptId = attemptId;
    this.queueStopped = false;
    this.pendingQueue.clear();
    this.debounceTimers.forEach(timer => clearTimeout(timer));
    this.debounceTimers.clear();
    this.debouncedPayloads.clear();
    this.saveStatus.set('idle');
  }

  /**
   * إضافة إجابة للـ Queue فوراً بدون انتظار — للأسئلة اللي بتُجاب بـ click واحد
   * (MultipleChoice / TrueFalse).
   */
  enqueueImmediate(attemptId: number, answer: SaveAnswerProgressPayload) {
    if (this.queueStopped) return;

    this.currentAttemptId = attemptId;
    this.pendingQueue.set(answer.questionId, answer);

    if (!this.isFlushing) {
      void this.startFlush();
    }
  }

  /**
   * إضافة إجابة بعد فترة debounce — للأسئلة اللي الطالب لسه بيكتب فيها
   * (Essay / FillBlank). كل تغيير جديد على نفس السؤال بيلغي المؤقت القديم ويبدأ من جديد.
   */
  enqueueDebounced(attemptId: number, answer: SaveAnswerProgressPayload, delayMs = 800) {
    if (this.queueStopped) return;

    const questionId = answer.questionId;
    const existingTimer = this.debounceTimers.get(questionId);
    if (existingTimer) {
      clearTimeout(existingTimer);
    }

    this.debouncedPayloads.set(questionId, answer);

    const timer = setTimeout(() => {
      this.debounceTimers.delete(questionId);
      this.debouncedPayloads.delete(questionId);
      this.enqueueImmediate(attemptId, answer);
    }, delayMs);

    this.debounceTimers.set(questionId, timer);
  }

  /** يوقف أي مؤقتات debounce جارية، وينقل آخر قيمة فيها فوراً للـ Queue بدل ما تضيع */
  cancelPendingDebounces() {
    for (const [questionId, timer] of this.debounceTimers) {
      clearTimeout(timer);
      const pending = this.debouncedPayloads.get(questionId);
      if (pending) {
        this.pendingQueue.set(questionId, pending);
      }
    }
    this.debounceTimers.clear();
    this.debouncedPayloads.clear();
  }

  /** يبعت كل اللي في الـ Queue فوراً بالتسلسل — يُستدعى قبل أي submit (تلقائي أو يدوي) */
  async flushQueue(attemptId: number): Promise<void> {
    this.currentAttemptId = attemptId;

    if (this.isFlushing) {
      while (this.isFlushing) {
        await this.delay(50);
      }
      return;
    }

    await this.startFlush();
  }

  /** إعادة محاولة الإجابات اللي فشل حفظها بعد 3 مرات ومحفوظة محلياً كـ safety net */
  retryFailedSaves(attemptId: number) {
    const failed = this.loadFailedAnswers(attemptId);
    if (failed.length === 0) return;

    this.clearFailedAnswers(attemptId);
    failed.forEach(answer => this.enqueueImmediate(attemptId, answer));
  }

  private async startFlush(): Promise<void> {
    if (this.isFlushing) return;
    this.isFlushing = true;

    if (this.pendingQueue.size > 0) {
      this.saveStatus.set('saving');
    }

    while (this.pendingQueue.size > 0 && !this.queueStopped) {
      const next = this.pendingQueue.entries().next().value as [number, SaveAnswerProgressPayload];
      const [questionId, answer] = next;
      this.pendingQueue.delete(questionId);

      const attemptId = this.currentAttemptId;
      if (attemptId == null) continue;

      await this.sendWithRetry(attemptId, answer);
    }

    this.isFlushing = false;
    if (!this.queueStopped && this.saveStatus() === 'saving') {
      this.saveStatus.set('saved');
    }
  }

  private async sendWithRetry(attemptId: number, answer: SaveAnswerProgressPayload, attempt = 1): Promise<void> {
    try {
      await firstValueFrom(this.saveAnswerProgress(attemptId, answer));
    } catch (err) {
      const status = (err as HttpErrorResponse)?.status;

      if (status === 409) {
        // الامتحان اتسلّم بالفعل (يدوياً أو عن طريق الـ Background Service) — نوقف الـ Queue كله
        this.queueStopped = true;
        this.pendingQueue.clear();
        this.debounceTimers.forEach(timer => clearTimeout(timer));
        this.debounceTimers.clear();
        this.debouncedPayloads.clear();
        return;
      }

      if (attempt < 3) {
        await this.delay(2000);
        await this.sendWithRetry(attemptId, answer, attempt + 1);
        return;
      }

      // فشلت 3 مرات — نحفظها محلياً كـ safety net ونبلّغ الطالب بفشل الحفظ
      this.persistFailedAnswer(attemptId, answer);
      this.saveStatus.set('failed');
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private persistFailedAnswer(attemptId: number, answer: SaveAnswerProgressPayload) {
    const failed = this.loadFailedAnswers(attemptId).filter(a => a.questionId !== answer.questionId);
    failed.push(answer);
    localStorage.setItem(this.getFailedKey(attemptId), JSON.stringify(failed));
  }

  private loadFailedAnswers(attemptId: number): SaveAnswerProgressPayload[] {
    const raw = localStorage.getItem(this.getFailedKey(attemptId));
    if (!raw) return [];
    try {
      return JSON.parse(raw) as SaveAnswerProgressPayload[];
    } catch {
      return [];
    }
  }

  private clearFailedAnswers(attemptId: number) {
    localStorage.removeItem(this.getFailedKey(attemptId));
  }

  private getFailedKey(attemptId: number): string {
    return `studentExamFailedAnswers:${attemptId}`;
  }

  // ===== localStorage Draft (Safety Net فقط — مش المصدر الأساسي للتسليم) =====

  saveDraftToLocalStorage(attemptId: number, draft: StudentExamDraft) {
    localStorage.setItem(this.getDraftKey(attemptId), JSON.stringify(draft));
  }

  loadDraftFromLocalStorage(attemptId: number): StudentExamDraft | null {
    const raw = localStorage.getItem(this.getDraftKey(attemptId));
    if (!raw) return null;

    try {
      return JSON.parse(raw) as StudentExamDraft;
    } catch {
      localStorage.removeItem(this.getDraftKey(attemptId));
      return null;
    }
  }

  clearDraftFromLocalStorage(attemptId: number) {
    localStorage.removeItem(this.getDraftKey(attemptId));
  }

  private getDraftKey(attemptId: number): string {
    return `studentExamDraft:${attemptId}`;
  }
}
