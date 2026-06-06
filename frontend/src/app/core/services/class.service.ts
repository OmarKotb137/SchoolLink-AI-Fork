import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ClassEntity {
  id: number;
  name: string;
  gradeLevelId: number;
  gradeLevelName?: string;
  academicYearId: number;
  academicYearName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ClassService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/class-management`;

  getAll(filter?: any): Observable<ClassEntity[]> {
    let params = new HttpParams();
    if (filter) {
      Object.keys(filter).forEach(key => {
        if (filter[key]) params = params.set(key, filter[key]);
      });
    }
    return this.http.get<ClassEntity[]>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ClassEntity> {
    return this.http.get<ClassEntity>(`${this.apiUrl}/${id}`);
  }

  getByGradeLevel(gradeLevelId: number): Observable<ClassEntity[]> {
    return this.http.get<ClassEntity[]>(`${this.apiUrl}/by-grade-level/${gradeLevelId}`);
  }

  getStudents(classId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${classId}/students`);
  }

  create(data: Partial<ClassEntity>): Observable<ClassEntity> {
    return this.http.post<ClassEntity>(this.apiUrl, data);
  }

  createWithStudents(data: any): Observable<ClassEntity> {
    return this.http.post<ClassEntity>(`${this.apiUrl}/with-students`, data);
  }

  update(id: number, data: Partial<ClassEntity>): Observable<ClassEntity> {
    return this.http.put<ClassEntity>(`${this.apiUrl}/${id}`, { ...data, id });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
