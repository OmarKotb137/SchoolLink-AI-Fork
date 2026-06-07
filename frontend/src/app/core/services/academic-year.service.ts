import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
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
  private apiUrl = buildApiUrl('academic-years');

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  create(data: Partial<AcademicYear>): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  update(id: number, data: Partial<AcademicYear>): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  setActive(id: number): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}/set-current`, {});
  }

  archive(id: string): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}/archive`, {});
  }
}
