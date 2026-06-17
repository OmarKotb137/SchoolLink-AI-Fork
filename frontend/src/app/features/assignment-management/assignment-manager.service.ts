import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { buildApiUrl } from '../../core/utils/api-url';
import { OperationResult } from '../../core/models/api.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface AssignmentItem {
  id: number;
  title: string;
  subject: string;
  class: string;
  deadline: string;
  submitted: number;
  total: number;
  status: string;
}

export interface Question {
  id: number;
  type: string;
  text: string;
  options?: string[];
  correctAnswer: string;
  points?: number;
}

export interface AssignmentDetail {
  id: number;
  title: string;
  subject: string;
  class: string;
  deadline: string;
  submitted: number;
  total: number;
  status: string;
  questions: Question[];
}

export interface Stats {
  total: number;
  active: number;
  avgDelivery: number;
  overdue: number;
}

export interface CreatePayload {
  title: string;
  subjectId: number;
  classId: number;
  deadline: string;
  questions: CreateQuestion[];
}

export interface CreateQuestion {
  type: string;
  text: string;
  options: string[];
  correctAnswer: string;
  points?: number;
}

@Injectable({ providedIn: 'root' })
export class AssignmentManagerService {
  private http = inject(HttpClient);
  private base = buildApiUrl('assignment-manager');

  // FIX: Use HttpParams so teacherId or academicYearId can be sent independently
  getAll(teacherId?: number, academicYearId?: number): Observable<OperationResult<AssignmentItem[]>> {
    let params = new HttpParams();
    if (teacherId) params = params.set('teacherId', teacherId);
    if (academicYearId) params = params.set('academicYearId', academicYearId);
    return this.http.get<OperationResult<AssignmentItem[]>>(this.base, { params });
  }

  // FIX: Throw a proper error if isSuccess=false so the component's error handler fires
  getById(id: number): Observable<AssignmentDetail> {
    return this.http.get<OperationResult<AssignmentDetail>>(`${this.base}/${id}`).pipe(
      map(r => {
        if (!r.isSuccess || !r.data) {
          throw new Error(r.message || 'تعذر تحميل بيانات الواجب');
        }
        return r.data;
      })
    );
  }

  // FIX: Same independent-params approach for stats
  getStats(teacherId?: number, academicYearId?: number): Observable<OperationResult<Stats>> {
    let params = new HttpParams();
    if (teacherId) params = params.set('teacherId', teacherId);
    if (academicYearId) params = params.set('academicYearId', academicYearId);
    return this.http.get<OperationResult<Stats>>(`${this.base}/stats`, { params });
  }

  create(dto: CreatePayload): Observable<OperationResult<AssignmentDetail>> {
    return this.http.post<OperationResult<AssignmentDetail>>(this.base, dto);
  }

  update(id: number, dto: CreatePayload): Observable<OperationResult<AssignmentDetail>> {
    return this.http.put<OperationResult<AssignmentDetail>>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<OperationResult<null>> {
    return this.http.delete<OperationResult<null>>(`${this.base}/${id}`);
  }

  getSubjects(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/subjects`);
  }

  getClasses(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/classes`);
  }

  getSubmissions(assignmentId: number): Observable<OperationResult<AssignmentSubmissionListItem[]>> {
    return this.http.get<OperationResult<AssignmentSubmissionListItem[]>>(`${this.base}/${assignmentId}/submissions`);
  }

  getSubmissionDetail(assignmentId: number, submissionId: number): Observable<OperationResult<AssignmentSubmissionDetail>> {
    return this.http.get<OperationResult<AssignmentSubmissionDetail>>(`${this.base}/${assignmentId}/submissions/${submissionId}`);
  }

  gradeSubmission(assignmentId: number, submissionId: number, dto: GradeAssignmentSubmissionDto): Observable<OperationResult<void>> {
    return this.http.post<OperationResult<void>>(`${this.base}/${assignmentId}/submissions/${submissionId}/grade`, dto);
  }
}

export interface AssignmentSubmissionListItem {
  submissionId: number;
  studentName: string;
  submittedAt: string;
  isGraded: boolean;
  score: number;
  maxScore: number;
}

export interface AssignmentSubmissionDetail {
  submissionId: number;
  studentName: string;
  score: number;
  maxScore: number;
  isGraded: boolean;
  answers: AssignmentSubmissionAnswer[];
}

export interface AssignmentSubmissionAnswer {
  questionId: number;
  questionText: string;
  type: string;
  studentAnswer: string;
  correctAnswer: string;
  pointsEarned: number;
  maxPoints: number;
  isCorrect?: boolean;
}

export interface GradeAssignmentSubmissionDto {
  manualGrades: Record<number, number>;
}
