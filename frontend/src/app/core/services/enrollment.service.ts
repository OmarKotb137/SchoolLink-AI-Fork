import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult, PagedResult } from '../models/api.model';

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

export interface GetEnrollmentsFilter {
  academicYearId: number;
  page?: number;
  pageSize?: number;
  activeOnly?: boolean;
  searchTerm?: string;
}

export interface BulkTransferRequest {
  enrollmentIds: number[];
  newClassId: number;
  transferDate: string;
  transferReason?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('enrollments');

  getByClass(classId: number, academicYearId: number, activeOnly = true): Observable<Enrollment[]> {
    let params = new HttpParams()
      .set('academicYearId', academicYearId)
      .set('activeOnly', activeOnly);
    return this.http.get<Enrollment[]>(`${this.apiUrl}/by-class/${classId}`, { params });
  }

  getByClassPaged(filter: GetEnrollmentsFilter & { classId: number }): Observable<OperationResult<PagedResult<Enrollment>>> {
    let params = new HttpParams()
      .set('academicYearId', filter.academicYearId)
      .set('page', (filter.page ?? 1).toString())
      .set('pageSize', (filter.pageSize ?? 20).toString())
      .set('activeOnly', (filter.activeOnly ?? true).toString());
    if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
    
    return this.http.get<OperationResult<PagedResult<Enrollment>>>(`${this.apiUrl}/by-class/${filter.classId}/paged`, { params });
  }

  getActiveByStudent(studentId: number, academicYearId: number): Observable<Enrollment> {
    let params = new HttpParams().set('academicYearId', academicYearId);
    return this.http.get<Enrollment>(`${this.apiUrl}/active/${studentId}`, { params });
  }

  transferStudent(request: TransferStudentRequest): Observable<OperationResult<Enrollment>> {
    return this.http.put<OperationResult<Enrollment>>(`${this.apiUrl}/transfer`, request);
  }

  bulkTransfer(request: BulkTransferRequest): Observable<OperationResult<any>> {
    return this.http.post<OperationResult<any>>(`${this.apiUrl}/transfer/bulk`, request);
  }

  getTransferHistory(academicYearId: number, page = 1, pageSize = 20): Observable<OperationResult<PagedResult<TransferHistory>>> {
    let params = new HttpParams()
      .set('academicYearId', academicYearId)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<OperationResult<PagedResult<TransferHistory>>>(`${this.apiUrl}/transfers-history`, { params });
  }
}
