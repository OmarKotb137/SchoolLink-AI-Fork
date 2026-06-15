import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
}

@Injectable({ providedIn: 'root' })
export class AssignmentManagerService {
  private http = inject(HttpClient);
  private base = buildApiUrl('assignment-manager');

  getAll(teacherId?: number, academicYearId?: number): Observable<OperationResult<AssignmentItem[]>> {
    let params = '';
    if (teacherId && academicYearId) {
      params = `?teacherId=${teacherId}&academicYearId=${academicYearId}`;
    }
    return this.http.get<OperationResult<AssignmentItem[]>>(`${this.base}${params}`);
  }

  getById(id: number): Observable<AssignmentDetail> {
    return this.http.get<OperationResult<AssignmentDetail>>(`${this.base}/${id}`).pipe(
      map(r => r.data)
    );
  }

  getStats(teacherId?: number, academicYearId?: number): Observable<OperationResult<Stats>> {
    let params = '';
    if (teacherId && academicYearId) {
      params = `?teacherId=${teacherId}&academicYearId=${academicYearId}`;
    }
    return this.http.get<OperationResult<Stats>>(`${this.base}/stats${params}`);
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
}
