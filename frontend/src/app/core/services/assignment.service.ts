import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface Assignment {
  id?: number;
  title: string;
  description?: string;
  dueDate?: string;
  maxScore: number;
  isAutoGraded: boolean;
  isAIGenerated: boolean;
  category: string;
  subjectName?: string;
  className?: string;
  teacherName?: string;
  questionsCount?: number;
  createdAt?: string;
}

export interface AssignmentSubmission {
  id: number;
  studentName: string;
  submittedAt: string;
  score?: number;
  maxScore: number;
  isGraded: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {
  private http = inject(HttpClient);
  private assignmentsUrl = buildApiUrl('assignments');
  private submissionsUrl = buildApiUrl('assignment-submissions');

  getByTeacher(teacherId: number, academicYearId: number): Observable<any> {
    const params = new HttpParams().set('academicYearId', academicYearId.toString());
    return this.http.get<any>(`${this.assignmentsUrl}/by-teacher/${teacherId}`, { params });
  }

  getSubmissionsByAssignment(assignmentId: number): Observable<any> {
    return this.http.get<any>(`${this.submissionsUrl}/by-assignment/${assignmentId}`);
  }
}
