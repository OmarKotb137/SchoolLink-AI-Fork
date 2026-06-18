import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';
import {
  StudentAssignmentAnswerPayload,
  StudentAssignmentDetails,
  StudentAssignmentDraft,
  StudentAssignmentListItem,
  StudentAssignmentSubmissionResult
} from '../models/student-assignment.models';

@Injectable({ providedIn: 'root' })
export class StudentAssignmentsService {
  private http = inject(HttpClient);
  private assignmentsBase = buildApiUrl('student/assignments');
  private submissionsBase = buildApiUrl('student/assignment-submissions');

  getMyAssignments(status?: string, subjectId?: number): Observable<OperationResult<StudentAssignmentListItem[]>> {
    let params = new HttpParams();
    if (status && status !== 'all') params = params.set('status', status);
    if (subjectId) params = params.set('subjectId', subjectId.toString());
    return this.http.get<OperationResult<StudentAssignmentListItem[]>>(this.assignmentsBase, { params });
  }

  getAssignmentDetails(assignmentId: number): Observable<OperationResult<StudentAssignmentDetails>> {
    return this.http.get<OperationResult<StudentAssignmentDetails>>(`${this.assignmentsBase}/${assignmentId}`);
  }

  submitAssignment(assignmentId: number, answers: StudentAssignmentAnswerPayload[]): Observable<OperationResult<StudentAssignmentSubmissionResult>> {
    return this.http.post<OperationResult<StudentAssignmentSubmissionResult>>(`${this.assignmentsBase}/${assignmentId}/submit`, { answers });
  }

  getSubmissionResult(submissionId: number): Observable<OperationResult<StudentAssignmentSubmissionResult>> {
    return this.http.get<OperationResult<StudentAssignmentSubmissionResult>>(`${this.submissionsBase}/${submissionId}`);
  }

  saveDraft(assignmentId: number, draft: StudentAssignmentDraft) {
    localStorage.setItem(this.getDraftKey(assignmentId), JSON.stringify(draft));
  }

  loadDraft(assignmentId: number): StudentAssignmentDraft | null {
    const raw = localStorage.getItem(this.getDraftKey(assignmentId));
    if (!raw) return null;

    try {
      return JSON.parse(raw) as StudentAssignmentDraft;
    } catch {
      localStorage.removeItem(this.getDraftKey(assignmentId));
      return null;
    }
  }

  clearDraft(assignmentId: number) {
    localStorage.removeItem(this.getDraftKey(assignmentId));
  }

  private getDraftKey(assignmentId: number): string {
    return `studentAssignmentDraft:${assignmentId}`;
  }
}
