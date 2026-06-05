import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: number;
  fullName: string;
  email: string;
  username: string;
  role: string; // e.g. 'Admin', 'Teacher', 'Student', 'Parent'
  isActive: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/Users`;

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.apiUrl);
  }

  getById(id: number): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  // FIX: pass large pageSize so all teachers are returned (not just the first page)
  getByRole(role: string, pageSize: number = 1000): Observable<PagedResult<User>> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<User>>(`${this.apiUrl}/role/${role}`, { params });
  }
}
