import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';
import { Student } from './student.service';

export type RelationshipType = 1 | 2 | 3 | 4 | 5;

export interface ParentStudentLink {
  id: number;
  parentId: number;
  parentName: string;
  parentEmail: string;
  studentId: number;
  studentName: string;
  relationship: RelationshipType;
}

export interface LinkParentStudentRequest {
  parentId: number;
  studentId: number;
  relationship: RelationshipType;
}

@Injectable({
  providedIn: 'root'
})
export class ParentStudentService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('parent-students');

  link(data: LinkParentStudentRequest): Observable<ParentStudentLink> {
    return this.http
      .post<OperationResult<ParentStudentLink>>(this.apiUrl, data)
      .pipe(map(res => res.data));
  }

  unlink(id: number): Observable<void> {
    return this.http
      .delete<OperationResult<unknown>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  getStudentsByParent(parentId: number): Observable<Student[]> {
    return this.http
      .get<OperationResult<Student[]>>(`${this.apiUrl}/by-parent/${parentId}`)
      .pipe(map(res => res.data));
  }

  getParentsByStudent(studentId: number): Observable<ParentStudentLink[]> {
    return this.http
      .get<OperationResult<ParentStudentLink[]>>(`${this.apiUrl}/by-student/${studentId}`)
      .pipe(map(res => res.data));
  }

  updateRelationship(id: number, relationship: RelationshipType): Observable<ParentStudentLink> {
    return this.http
      .put<OperationResult<ParentStudentLink>>(`${this.apiUrl}/${id}/relationship`, relationship)
      .pipe(map(res => res.data));
  }
}
