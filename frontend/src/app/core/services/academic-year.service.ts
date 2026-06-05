import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AcademicYear {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AcademicYearService {
  private http = inject(HttpClient);
  private apiUrl = '/api/academic-years';

  getAll(): Observable<AcademicYear[]> {
    return this.http.get<AcademicYear[]>(this.apiUrl);
  }

  getById(id: number): Observable<AcademicYear> {
    return this.http.get<AcademicYear>(`${this.apiUrl}/${id}`);
  }

  create(data: Partial<AcademicYear>): Observable<AcademicYear> {
    return this.http.post<AcademicYear>(this.apiUrl, data);
  }

  update(id: number, data: Partial<AcademicYear>): Observable<AcademicYear> {
    return this.http.put<AcademicYear>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  setActive(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/set-current`, {});
  }

  archive(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/archive`, {});
  }
}
