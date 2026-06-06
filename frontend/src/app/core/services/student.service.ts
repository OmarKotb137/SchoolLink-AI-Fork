import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';

export interface Student {
  id: number;
  fullName: string;
  nationalId?: string | null;
  gender?: number | null;
  birthDate?: string | null;
  userId?: number | null;
  userName?: string | null;
  userEmail?: string | null;
  isActive: boolean;
  createdAt?: string;
}

export interface CreateStudentRequest {
  fullName: string;
  nationalId?: string;
  gender?: number | null;
  birthDate?: string | null;
}

export interface UpdateStudentRequest {
  id: number;
  fullName: string;
  gender?: number | null;
  birthDate?: string | null;
}

export interface StudentSearchFilter {
  searchTerm?: string;
  classId?: number;
  academicYearId?: number;
  isActive?: boolean;
}

export interface LinkStudentUserRequest {
  studentId: number;
  userId: number;
}

@Injectable({
  providedIn: 'root'
})
export class StudentService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('students');

  getAll(): Observable<Student[]> {
    return this.http
      .get<OperationResult<Student[]>>(this.apiUrl)
      .pipe(map(res => res.data));
  }

  search(filter: StudentSearchFilter): Observable<Student[]> {
    let params = new HttpParams();
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    if (filter.classId) params = params.set('classId', filter.classId);
    if (filter.academicYearId) params = params.set('academicYearId', filter.academicYearId);
    if (filter.isActive !== undefined) params = params.set('isActive', filter.isActive);

    return this.http
      .get<OperationResult<Student[]>>(`${this.apiUrl}/search`, { params })
      .pipe(map(res => res.data));
  }

  create(data: CreateStudentRequest): Observable<Student> {
    return this.http
      .post<OperationResult<Student>>(this.apiUrl, data)
      .pipe(map(res => res.data));
  }

  update(id: number, data: UpdateStudentRequest): Observable<Student> {
    return this.http
      .put<OperationResult<Student>>(`${this.apiUrl}/${id}`, data)
      .pipe(map(res => res.data));
  }

  delete(id: number): Observable<void> {
    return this.http
      .delete<OperationResult<unknown>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  linkUser(data: LinkStudentUserRequest): Observable<void> {
    return this.http
      .post<OperationResult<unknown>>(`${this.apiUrl}/link-user`, data)
      .pipe(map(() => void 0));
  }
}
