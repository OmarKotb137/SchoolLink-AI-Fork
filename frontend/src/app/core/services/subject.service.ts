import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
export interface Subject {
  id: number;
  name: string;
  code: string;
}

@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('subjects');

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  search(term: string): Observable<any> {
    const params = new HttpParams().set('term', term);
    return this.http.get<any>(`${this.apiUrl}/search`, { params });
  }

  create(data: Partial<Subject>): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  update(id: number, data: Partial<Subject>): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, { ...data, id });
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
