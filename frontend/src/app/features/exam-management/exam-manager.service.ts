import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildApiUrl } from '../../core/utils/api-url';
import { OperationResult } from '../../core/models/api.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ExamItem {
  id: number;
  name: string;
  subject: string;
  class: string;
  date: string;
  startTime: string;
  endTime: string;
  duration: number;
  questionCount: number;
  status: string;
  avgScore?: number;
  submitted?: number;
  total?: number;
  isResultPublished: boolean;
  pendingGradingCount?: number;
}

export interface ExamQuestion {
  id: number;
  type: string;
  text: string;
  options?: string[];
  correctAnswer: string;
  points?: number;
}

/** سؤال مؤقت داخل مودال الإنشاء/التعديل (لم يُحفظ بعد) */
export interface ExamDraftQuestion {
  _localId: number;           // مُعرّف محلي فقط
  id?: number;                // مُعرّف السيرفر لو موجود (وقت التعديل)
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options: string[];
  correctAnswer: string;
  points: number;
}

export interface CreateExamQuestionPayload {
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options?: string[];
  correctAnswer?: string;
  points: number;
}

/** ملخص محاولة طالب (لمودال النتائج) */
export interface ExamAttemptSummary {
  id: number;
  studentName: string;
  score: number | null;
  totalScore: number;
  isGraded: boolean;
  submittedAt: string | null;
  status: string;       // 'submitted' | 'graded' | 'waitingGrade'
}

/** إجابة طالب (لمودال التصحيح) */
export interface ExamAttemptAnswerDetail {
  id: number;
  questionText: string;
  questionType: string;     // 'mcq' | 'true-false' | 'essay'
  questionPoints: number;
  answerText: string | null;
  isCorrect: boolean | null;
  pointsEarned: number;
  feedback?: string;
}

/** تفاصيل محاولة كاملة (لمودال التصحيح) */
export interface ExamAttemptGradingDetail {
  id: number;
  studentName: string;
  score: number | null;
  totalScore: number;
  isGraded: boolean;
  answers: ExamAttemptAnswerDetail[];
}

export interface GradeEssayAnswerPayload {
  answerId: number;
  pointsEarned: number;
  feedback?: string;
}

export interface GradeEssayAttemptPayload {
  answers: GradeEssayAnswerPayload[];
}

export interface ExamDetail {
  id: number;
  name: string;
  subject: string;
  class: string;
  date: string;
  startTime: string;
  endTime: string;
  duration: number;
  questionCount: number;
  status: string;
  isResultPublished: boolean;
  questions: ExamQuestion[];
}

export interface ExamStats {
  total: number;
  upcoming: number;
  ended: number;
  avgScore: number;
}

export interface CreateExamPayload {
  title: string;
  subjectId: number;
  classId: number;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  questions: CreateExamQuestionPayload[];
}

@Injectable({ providedIn: 'root' })
export class ExamManagerService {
  private http = inject(HttpClient);
  private base = buildApiUrl('exam-manager');
  private attemptsBase = buildApiUrl('exam-attempts');

  getAll(teacherId?: number, academicYearId?: number): Observable<OperationResult<ExamItem[]>> {
    let params = '';
    if (teacherId && academicYearId) {
      params = `?teacherId=${teacherId}&academicYearId=${academicYearId}`;
    }
    return this.http.get<OperationResult<ExamItem[]>>(`${this.base}${params}`);
  }

  getById(id: number): Observable<ExamDetail> {
    return this.http.get<OperationResult<ExamDetail>>(`${this.base}/${id}`).pipe(
      map(r => r.data)
    );
  }

  getStats(teacherId?: number, academicYearId?: number): Observable<OperationResult<ExamStats>> {
    let params = '';
    if (teacherId && academicYearId) {
      params = `?teacherId=${teacherId}&academicYearId=${academicYearId}`;
    }
    return this.http.get<OperationResult<ExamStats>>(`${this.base}/stats${params}`);
  }

  create(dto: CreateExamPayload): Observable<OperationResult<ExamDetail>> {
    return this.http.post<OperationResult<ExamDetail>>(this.base, dto);
  }

  update(id: number, dto: CreateExamPayload): Observable<OperationResult<any>> {
    return this.http.put<OperationResult<any>>(`${this.base}/${id}`, dto);
  }

  // ── Attempts / Grading ─────────────────────────────────────

  getAttemptsByExam(examId: number): Observable<OperationResult<ExamAttemptSummary[]>> {
    return this.http.get<OperationResult<ExamAttemptSummary[]>>(
      `${this.attemptsBase}/by-exam/${examId}`
    );
  }

  getAttemptDetail(attemptId: number): Observable<OperationResult<ExamAttemptGradingDetail>> {
    return this.http.get<OperationResult<ExamAttemptGradingDetail>>(
      `${this.attemptsBase}/${attemptId}`
    );
  }

  gradeEssayAnswers(attemptId: number, dto: GradeEssayAttemptPayload): Observable<OperationResult<any>> {
    return this.http.patch<OperationResult<any>>(
      `${this.attemptsBase}/${attemptId}/grade`, dto
    );
  }

  delete(id: number): Observable<OperationResult<null>> {
    return this.http.delete<OperationResult<null>>(`${this.base}/${id}`);
  }

  publish(id: number): Observable<OperationResult<null>> {
    return this.http.put<OperationResult<null>>(`${this.base}/${id}/publish`, {});
  }

  publishResults(id: number): Observable<OperationResult<null>> {
    return this.http.put<OperationResult<null>>(`${this.base}/${id}/publish-results`, {});
  }

  unpublishResults(id: number): Observable<OperationResult<null>> {
    return this.http.put<OperationResult<null>>(`${this.base}/${id}/unpublish-results`, {});
  }

  getSubjects(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/subjects`);
  }

  getClasses(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/classes`);
  }
}
