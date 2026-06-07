import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { map } from 'rxjs/operators';

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

  link(data: LinkParentStudentRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  unlink(id: number): Observable<any> {
    return this.http
      .delete<any>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  getStudentsByParent(parentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/by-parent/${parentId}`);
  }

  getParentsByStudent(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/by-student/${studentId}`);
  }

  updateRelationship(id: number, relationship: RelationshipType): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/relationship`, relationship);
  }
}
