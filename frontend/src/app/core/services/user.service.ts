import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult, PagedResult } from '../models/api.model';

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

  getAll(filter?: GetUsersFilter): Observable<PagedResult<User>> {
    let params = new HttpParams();

    if (filter?.role) params = params.set('role', filter.role);
    if (filter?.isActive !== undefined) params = params.set('isActive', filter.isActive);
    if (filter?.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter?.page) params = params.set('page', filter.page);
    if (filter?.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http
      .get<OperationResult<PagedResult<User>>>(this.apiUrl, { params })
      .pipe(map(res => res.data));
  }

  getById(id: number): Observable<User> {
    return this.http
      .get<OperationResult<User>>(`${this.apiUrl}/${id}`)
      .pipe(map(res => res.data));
  }

  getByRole(role: string, pageSize: number = 1000): Observable<PagedResult<User>> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http
      .get<OperationResult<PagedResult<User>>>(`${this.apiUrl}/role/${role}`, { params })
      .pipe(map(res => res.data));
  }

  createUser(data: CreateUserRequest): Observable<User> {
    return this.http
      .post<OperationResult<User>>(this.apiUrl, data)
      .pipe(map(res => res.data));
  }

  updateUser(id: number, data: UpdateUserRequest): Observable<User> {
    return this.http
      .put<OperationResult<User>>(`${this.apiUrl}/${id}`, data)
      .pipe(map(res => res.data));
  }

  setActiveStatus(id: number, isActive: boolean): Observable<void> {
    return this.http
      .patch<OperationResult<unknown>>(`${this.apiUrl}/${id}/active-status`, isActive)
      .pipe(map(() => void 0));
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
