import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { PagedResult } from '../models/api.model';

export interface Teacher {
  id: number;
  fullName: string;
  username: string;
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
  username: string;
  contactEmail?: string;
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

  getAll(pageSize: number = 1000): Observable<any> {
    const params = new HttpParams().set('pageSize', pageSize.toString());
    return this.http.get<any>(this.apiUrl, { params });
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  createTeacher(data: CreateTeacherRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateTeacher(id: number, data: UpdateTeacherRequest): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  deleteTeacher(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
