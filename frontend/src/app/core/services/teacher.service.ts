import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { PagedResult } from '../models/api.model';

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
    return this.http.get<PagedResult<Teacher>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<Teacher> {
    return this.http.get<Teacher>(`${this.apiUrl}/${id}`);
  }

  createTeacher(data: CreateTeacherRequest): Observable<Teacher> {
    return this.http.post<Teacher>(this.apiUrl, data);
  }

  updateTeacher(id: number, data: UpdateTeacherRequest): Observable<Teacher> {
    return this.http.put<Teacher>(`${this.apiUrl}/${id}`, data);
  }

  deleteTeacher(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
