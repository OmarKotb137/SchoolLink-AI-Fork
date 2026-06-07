import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { buildApiUrl } from '../utils/api-url';

export interface Enrollment {
  id: number;
  studentId: number;
  classId: number;
  academicYearId: number;
  enrolledAt: string;
  leftAt?: string;
  transferReason?: string;
  status: number;
  studentName?: string;
  className?: string;
  academicYearName?: string;
}

export interface TransferStudentRequest {
  currentEnrollmentId: number;
  newClassId: number;
  transferDate: string;
  transferReason?: string;
}

export interface TransferHistory {
  id: number;
  studentName: string;
  fromClass: string;
  toClass: string;
  transferDate?: string;
  reason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('enrollments');

  getByClass(classId: number, academicYearId: number, activeOnly: boolean = true): Observable<Enrollment[]> {
    let params = new HttpParams()
      .set('academicYearId', academicYearId)
      .set('activeOnly', activeOnly);
    return this.http.get<Enrollment[]>(`${this.apiUrl}/by-class/${classId}`, { params });
  }

  getActiveByStudent(studentId: number, academicYearId: number): Observable<Enrollment> {
    let params = new HttpParams().set('academicYearId', academicYearId);
    return this.http.get<Enrollment>(`${this.apiUrl}/active/${studentId}`, { params });
  }

  transferStudent(request: TransferStudentRequest): Observable<Enrollment> {
    return this.http.put<Enrollment>(`${this.apiUrl}/transfer`, request);
  }

  getTransferHistory(academicYearId: number): Observable<TransferHistory[]> {
    let params = new HttpParams().set('academicYearId', academicYearId);
    return this.http.get<TransferHistory[]>(`${this.apiUrl}/transfers-history`, { params });
  }
}
