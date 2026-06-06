import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface GradeLevel {
  id: number;
  name: string;
  stage?: string | null;
  levelOrder: number;
}

@Injectable({
  providedIn: 'root'
})
export class GradeLevelService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('grade-levels');

  getAll(): Observable<GradeLevel[]> {
    return this.http.get<GradeLevel[]>(this.apiUrl);
  }

  getById(id: number): Observable<GradeLevel> {
    return this.http.get<GradeLevel>(`${this.apiUrl}/${id}`);
  }

  create(data: Partial<GradeLevel>): Observable<GradeLevel> {
    return this.http.post<GradeLevel>(this.apiUrl, data);
  }

  update(id: number, data: Partial<GradeLevel>): Observable<GradeLevel> {
    return this.http.put<GradeLevel>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
