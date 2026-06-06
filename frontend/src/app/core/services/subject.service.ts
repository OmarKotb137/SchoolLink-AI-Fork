import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

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

  getAll(): Observable<Subject[]> {
    return this.http.get<Subject[]>(this.apiUrl);
  }

  getById(id: number): Observable<Subject> {
    return this.http.get<Subject>(`${this.apiUrl}/${id}`);
  }

  search(term: string): Observable<Subject[]> {
    const params = new HttpParams().set('term', term);
    return this.http.get<Subject[]>(`${this.apiUrl}/search`, { params });
  }

  create(data: Partial<Subject>): Observable<Subject> {
    return this.http.post<Subject>(this.apiUrl, data);
  }

  update(id: number, data: Partial<Subject>): Observable<Subject> {
    return this.http.put<Subject>(`${this.apiUrl}/${id}`, { ...data, id });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
