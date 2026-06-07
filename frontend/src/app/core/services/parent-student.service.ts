import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
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
    return this.http.post<ParentStudentLink>(this.apiUrl, data);
  }

  unlink(id: number): Observable<void> {
    return this.http
      .delete<unknown>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  getStudentsByParent(parentId: number): Observable<Student[]> {
    return this.http.get<Student[]>(`${this.apiUrl}/by-parent/${parentId}`);
  }

  getParentsByStudent(studentId: number): Observable<ParentStudentLink[]> {
    return this.http.get<ParentStudentLink[]>(`${this.apiUrl}/by-student/${studentId}`);
  }

  updateRelationship(id: number, relationship: RelationshipType): Observable<ParentStudentLink> {
    return this.http.put<ParentStudentLink>(`${this.apiUrl}/${id}/relationship`, relationship);
  }
}
