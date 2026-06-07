import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { PagedResult } from '../models/api.model';

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

export interface GetUsersFilter {
  role?: 'Admin' | 'Teacher' | 'Parent' | 'Student';
  isActive?: boolean;
  searchTerm?: string;
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
  private apiUrl = buildApiUrl('Users');

  getAll(filter?: GetUsersFilter): Observable<any> {
    let params = new HttpParams();

    if (filter?.role) params = params.set('role', filter.role);
    if (filter?.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter?.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter?.page) params = params.set('page', filter.page);
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<any>(this.apiUrl, { params });
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getByRole(role: string, pageSize: number = 1000): Observable<any> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http.get<any>(`${this.apiUrl}/role/${role}`, { params });
  }

  createUser(data: CreateUserRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateUser(id: number, data: UpdateUserRequest): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  setActiveStatus(id: number, isActive: boolean): Observable<any> {
    return this.http.patch<any>(`${this.apiUrl}/${id}/active-status`, isActive);
  }

  deleteUser(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
