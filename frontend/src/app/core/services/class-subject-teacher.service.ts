import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';import { Teacher } from './teacher.service';

export interface ClassSubjectTeacher {
  id?: number;
  classId: number;
  className?: string;
  subjectId: number;
  subjectName?: string;
  teacherId: number;
  teacherName?: string;
  academicYearId?: number;
  academicYearName?: string;
  weeklyPeriods: number;
}

@Injectable({
  providedIn: 'root'
})
export class ClassSubjectTeacherService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('class-subject-teachers');

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getByClass(classId: number, academicYearId?: number): Observable<any> {
    let url = `${this.apiUrl}/by-class/${classId}`;
    if (academicYearId) url += `?academicYearId=${academicYearId}`;
    return this.http.get<any>(url);
  }

  getByTeacher(teacherId: number, academicYearId?: number): Observable<any> {
    let url = `${this.apiUrl}/by-teacher/${teacherId}`;
    if (academicYearId) url += `?academicYearId=${academicYearId}`;
    return this.http.get<any>(url);
  }

  getAvailableTeachers(subjectId: number, classId: number, academicYearId: number): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/available-teachers?subjectId=${subjectId}&classId=${classId}&academicYearId=${academicYearId}`
    );
  }

  assignTeacherToClass(data: Partial<ClassSubjectTeacher>): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  assignBulk(data: Partial<ClassSubjectTeacher>[]): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/bulk`, data);
  }

  update(id: number, data: { teacherId: number; weeklyPeriods: number }): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, { ...data, assignmentId: id });
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  getMyAssignmentsCurrentYear(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-assignments/current-year`);
  }
}
