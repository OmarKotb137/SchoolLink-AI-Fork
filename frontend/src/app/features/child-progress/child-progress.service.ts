import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';
import { buildApiUrl } from '../../core/utils/api-url';

export interface ChildProgressItem {
  studentId: number;
  studentName: string;
  className: string;
  gradeLevelName: string;
  avgScore: number;
  attendancePercentage: number;
  assignments: AssignmentProgress[];
  exams: ExamProgress[];
}

export interface AssignmentProgress {
  id: number;
  subject: string;
  title: string;
  deadline?: string;
  status: string;
  score?: number;
  maxScore: number;
}

export interface ExamProgress {
  id: number;
  subject: string;
  date?: string;
  status: string;
  score?: number;
  maxScore: number;
}

interface OperationResult<T> {
  isSuccess: boolean;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class ChildProgressService {
  private http = inject(HttpClient);
  private base = buildApiUrl('child-progress');

  get(term?: number | null) {
    let url = this.base;
    if (term != null) {
      url += `?term=${term}`;
    }
    return this.http.get<OperationResult<ChildProgressItem[]>>(url).pipe(
      map(res => res.data)
    );
  }
}
