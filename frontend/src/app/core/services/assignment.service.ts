import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult, PagedResult } from '../models/api.model';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface AssignmentFilter {
  search?: string;
  subjectId?: number;
  status?: string;
  sortBy?: string;
  page?: number;
  pageSize?: number;
  academicYearId?: number;
}

export interface AssignmentItem {
  id: number;
  title: string;
  subject: string;
  class: string;
  deadline: string;
  maxScore: number;
  isPublished: boolean;
  isAIGenerated: boolean;
  questionsCount: number;
  submitted: number;
  total: number;
  avgScore?: number;
  status: string;
}

export interface AssignmentQuestion {
  id: number;
  type: string;
  text: string;
  options?: string[];
  correctAnswer: string;
  points: number;
}

export interface AssignmentDraftQuestion {
  _localId: number;
  id?: number;
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options: string[];
  correctAnswer: string;
  points: number;
}

export interface CreateAssignmentQuestionPayload {
  type: 'mcq' | 'true-false' | 'essay';
  text: string;
  options?: string[];
  correctAnswer?: string;
  points: number;
}

export interface AssignmentDetail {
  id: number;
  title: string;
  subject: string;
  class: string;
  deadline: string;
  maxScore: number;
  isPublished: boolean;
  isAIGenerated: boolean;
  questionsCount: number;
  submitted: number;
  total: number;
  status: string;
  questions: AssignmentQuestion[];
}

export interface AssignmentStats {
  total: number;
  active: number;
  avgDelivery: number;
  overdue: number;
}

export interface CreateAssignmentPayload {
  title: string;
  subjectId: number;
  classId: number;
  deadline?: string;
  questions: CreateAssignmentQuestionPayload[];
}

export interface UpdateAssignmentPayload {
  title: string;
  deadline?: string;
  questions: CreateAssignmentQuestionPayload[];
}

export interface AssignmentSubmissionItem {
  submissionId: number;
  studentName: string;
  submittedAt: string;
  isGraded: boolean;
  score: number;
  maxScore: number;
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

export interface AssignmentSubmissionDetail {
  submissionId: number;
  studentName: string;
  score: number;
  maxScore: number;
  isGraded: boolean;
  answers: AssignmentSubmissionAnswer[];
}

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private http = inject(HttpClient);
  private base = buildApiUrl('assignment-manager');
  private assignmentsOldBase = buildApiUrl('assignments');

  getAll(filter: AssignmentFilter = {}): Observable<OperationResult<PagedResult<AssignmentItem>>> {
    let params = new HttpParams();
    if (filter.search) params = params.set('search', filter.search);
    if (filter.subjectId) params = params.set('subjectId', filter.subjectId);
    if (filter.status && filter.status !== 'all') params = params.set('status', filter.status);
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.page) params = params.set('page', filter.page);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);
    if (filter.academicYearId) params = params.set('academicYearId', filter.academicYearId);
    return this.http.get<OperationResult<PagedResult<AssignmentItem>>>(this.base, { params });
  }

  getById(id: number): Observable<AssignmentDetail> {
    return this.http.get<OperationResult<AssignmentDetail>>(`${this.base}/${id}`).pipe(
      map(r => {
        const data = r.data;
        return {
          ...data,
          maxScore: data.maxScore ?? 0,
          questionsCount: data.questionsCount ?? data.questions?.length ?? 0,
          questions: (data.questions ?? []).map((q: any) => ({
            id: q.id,
            type: q.type ?? 'mcq',
            text: q.text ?? q.questionText ?? '',
            options: q.options ?? [],
            correctAnswer: q.correctAnswer ?? '',
            points: q.points ?? 5,
          }))
        } as AssignmentDetail;
      })
    );
  }

  getStats(academicYearId?: number): Observable<OperationResult<AssignmentStats>> {
    let params = '';
    if (academicYearId) params = `?academicYearId=${academicYearId}`;
    return this.http.get<OperationResult<AssignmentStats>>(`${this.base}/stats${params}`);
  }

  create(dto: CreateAssignmentPayload): Observable<OperationResult<AssignmentItem>> {
    return this.http.post<OperationResult<AssignmentItem>>(this.base, dto);
  }

  update(id: number, dto: UpdateAssignmentPayload): Observable<OperationResult<any>> {
    return this.http.put<OperationResult<any>>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<OperationResult<null>> {
    return this.http.delete<OperationResult<null>>(`${this.base}/${id}`);
  }

  publish(id: number): Observable<OperationResult<null>> {
    return this.http.patch<OperationResult<null>>(`${this.assignmentsOldBase}/${id}/publish`, {});
  }

  unpublish(id: number): Observable<OperationResult<null>> {
    return this.http.patch<OperationResult<null>>(`${this.assignmentsOldBase}/${id}/unpublish`, {});
  }

  getSubjects(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/subjects`);
  }

  getClasses(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/classes`);
  }

  getSubmissions(assignmentId: number): Observable<OperationResult<AssignmentSubmissionItem[]>> {
    return this.http.get<OperationResult<AssignmentSubmissionItem[]>>(`${this.base}/${assignmentId}/submissions`);
  }

  getSubmissionDetail(assignmentId: number, submissionId: number): Observable<OperationResult<AssignmentSubmissionDetail>> {
    return this.http.get<OperationResult<AssignmentSubmissionDetail>>(`${this.base}/${assignmentId}/submissions/${submissionId}`);
  }

  gradeSubmission(assignmentId: number, submissionId: number, dto: { manualGrades: Record<number, number> }): Observable<OperationResult<null>> {
    return this.http.post<OperationResult<null>>(`${this.base}/${assignmentId}/submissions/${submissionId}/grade`, dto);
  }

  // Legacy — for teacher-dashboard
  getByTeacher(teacherId: number, academicYearId: number): Observable<any> {
    const params = new HttpParams().set('academicYearId', academicYearId.toString());
    return this.http.get<any>(`${this.assignmentsOldBase}/by-teacher/${teacherId}`, { params });
  }

  getSubmissionsByAssignment(assignmentId: number): Observable<any> {
    return this.http.get<any>(`${this.assignmentsOldBase.replace('assignments', 'assignment-submissions')}/by-assignment/${assignmentId}`);
  }
}
