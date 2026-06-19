import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface ResultVisibilityDto {
  id: number;
  academicYearId: number;
  term: string; // API returns enum as string: "FirstSemester", "SecondSemester"
  isVisible: boolean;
  visibleFrom: string | null;
  visibleUntil: string | null;
  controlledById: number;
  createdAt: string;
  updatedAt: string;
}

export interface SetVisibilityRequest {
  academicYearId: number;
  term: number;
  isVisible: boolean;
  visibleFrom?: string | null;
  visibleUntil?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ResultVisibilityService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('resultvisibility');

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getByAcademicYear(academicYearId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/academic-year/${academicYearId}`);
  }

  setVisibility(data: SetVisibilityRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  update(id: number, data: { isVisible: boolean; visibleFrom?: string | null; visibleUntil?: string | null }): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
