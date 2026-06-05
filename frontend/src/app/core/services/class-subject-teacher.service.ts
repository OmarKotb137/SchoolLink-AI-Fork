import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ClassSubjectTeacher {
  id?: number;
  classId: number;
  // FIX Issue 4: backend ClassSubjectTeacherDto already returns these name fields —
  //              added to the interface so we can use them directly in the template
  //              instead of doing redundant lookups in local arrays.
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
  private apiUrl = '/api/class-subject-teachers';

  getAll(): Observable<ClassSubjectTeacher[]> {
    return this.http.get<ClassSubjectTeacher[]>(this.apiUrl);
  }

  getById(id: number): Observable<ClassSubjectTeacher> {
    return this.http.get<ClassSubjectTeacher>(`${this.apiUrl}/${id}`);
  }

  getByClass(classId: number, academicYearId?: number): Observable<ClassSubjectTeacher[]> {
    let url = `${this.apiUrl}/by-class/${classId}`;
    if (academicYearId) url += `?academicYearId=${academicYearId}`;
    return this.http.get<ClassSubjectTeacher[]>(url);
  }

  getByTeacher(teacherId: number): Observable<ClassSubjectTeacher[]> {
    return this.http.get<ClassSubjectTeacher[]>(`${this.apiUrl}/by-teacher/${teacherId}`);
  }

  assignTeacherToClass(data: Partial<ClassSubjectTeacher>): Observable<ClassSubjectTeacher> {
    return this.http.post<ClassSubjectTeacher>(this.apiUrl, data);
  }

  assignBulk(data: Partial<ClassSubjectTeacher>[]): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/bulk`, data);
  }

  // FIX: ClassSubjectTeacherController.UpdateAssignment checks `if (id != request.AssignmentId)`
  //      so the body must carry `assignmentId` matching the URL segment.
  //      Only teacherId and weeklyPeriods are editable (class & subject are immutable).
  update(id: number, data: { teacherId: number; weeklyPeriods: number }): Observable<ClassSubjectTeacher> {
    return this.http.put<ClassSubjectTeacher>(`${this.apiUrl}/${id}`, { ...data, assignmentId: id });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
