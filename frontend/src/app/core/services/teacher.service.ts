import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult, PagedResult } from '../models/api.model';

export interface Teacher {
  id: number;
  fullName: string;
  email: string;
  phone?: string;
  isActive: boolean;
  profilePictureUrl?: string;
  createdAt?: string;
  subjectIds?: number[];
  subjectNames?: string[];
}

export interface CreateTeacherRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
  subjectIds: number[];
}

export interface UpdateTeacherRequest {
  fullName: string;
  phone?: string;
  profilePictureUrl?: string;
  subjectIds: number[];
}

@Injectable({
  providedIn: 'root'
})
export class TeacherService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('Teachers');

  getAll(pageSize: number = 1000): Observable<PagedResult<Teacher>> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http
      .get<OperationResult<PagedResult<Teacher>>>(this.apiUrl, { params })
      .pipe(map(res => res.data));
  }

  getById(id: number): Observable<Teacher> {
    return this.http
      .get<OperationResult<Teacher>>(`${this.apiUrl}/${id}`)
      .pipe(map(res => res.data));
  }

  createTeacher(data: CreateTeacherRequest): Observable<Teacher> {
    return this.http
      .post<OperationResult<Teacher>>(this.apiUrl, data)
      .pipe(map(res => res.data));
  }

  updateTeacher(id: number, data: UpdateTeacherRequest): Observable<Teacher> {
    return this.http
      .put<OperationResult<Teacher>>(`${this.apiUrl}/${id}`, data)
      .pipe(map(res => res.data));
  }

  deleteTeacher(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
