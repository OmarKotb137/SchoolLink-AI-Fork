import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';
import {
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
