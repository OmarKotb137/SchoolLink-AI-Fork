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
}

export interface ExamQuestion {
  id: number;
  type: string;
  text: string;
  options?: string[];
  correctAnswer: string;
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
}

@Injectable({ providedIn: 'root' })
export class ExamManagerService {
  private http = inject(HttpClient);
  private base = buildApiUrl('exam-manager');

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

  update(id: number, dto: CreateExamPayload): Observable<OperationResult<ExamDetail>> {
    return this.http.put<OperationResult<ExamDetail>>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<OperationResult<null>> {
    return this.http.delete<OperationResult<null>>(`${this.base}/${id}`);
  }

  publish(id: number): Observable<OperationResult<null>> {
    return this.http.put<OperationResult<null>>(`${this.base}/${id}/publish`, {});
  }

  getSubjects(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/subjects`);
  }

  getClasses(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.base}/classes`);
  }
}
