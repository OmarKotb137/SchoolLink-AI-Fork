import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: number;
  fullName: string;
  email: string;
  username?: string;
  role: string;
  phone?: string;
  isActive: boolean;
  profilePictureUrl?: string;
  createdAt?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page?: number;
  pageSize?: number;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
  role: string;
}

export interface UpdateUserRequest {
  fullName: string;
  phone?: string;
  profilePictureUrl?: string;
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

  getByRole(role: string, pageSize: number = 1000): Observable<PagedResult<User>> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<User>>(`${this.apiUrl}/role/${role}`, { params });
  }

  createUser(data: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.apiUrl, data);
  }

  updateUser(id: number, data: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/${id}`, data);
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
