import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface ClassStudentBrowserItem {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  gender?: number | null;
  isActive: boolean;
  enrolledAt: string;
}

export interface PagedItems<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ClassStudentsBrowserResult {
  classId: number;
  className: string;
  academicYearId: number;
  academicYearName: string;
  gradeLevelName: string;
  totalStudents: number;
  filteredStudentsCount: number;
  students: PagedItems<ClassStudentBrowserItem>;
}

@Injectable({
  providedIn: 'root'
})
export class ClassStudentsBrowserService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('class-students-browser');

  getClassStudents(
    classId: number,
    params: {
      academicYearId: number;
      page: number;
      pageSize: number;
      searchTerm?: string;
    }
  ): Observable<ClassStudentsBrowserResult> {
    let httpParams = new HttpParams()
      .set('academicYearId', params.academicYearId)
      .set('page', params.page)
      .set('pageSize', params.pageSize);

    if (params.searchTerm?.trim()) {
      httpParams = httpParams.set('searchTerm', params.searchTerm.trim());
    }

    return this.http.get<any>(`${this.apiUrl}/${classId}/students`, { params: httpParams }).pipe(
      map(response => response.data ?? response)
    );
  }
}
