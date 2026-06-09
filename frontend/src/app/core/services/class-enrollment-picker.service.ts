import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult, PagedResult } from '../models/api.model';

// ── Interfaces خاصة بهذه الميزة فقط ──────────────────────────────────────────

export interface ClassPickerStudent {
  id: number;
  fullName: string;
  nationalId?: string;
  gender?: number | null;
  birthDate?: string | null;
}

export interface ClassPickerFilter {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  birthDateFrom?: string;
  birthDateTo?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface ClassPickerBulkEnrollRequest {
  classId: number;
  studentIds: number[];
  enrolledAt: string;
}

export interface ClassPickerEnrollResult {
  totalRequested: number;
  successCount: number;
  failureCount: number;
  failures: { studentId: number; studentName: string; reason: string }[];
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class ClassEnrollmentPickerService {
  private http = inject(HttpClient);
  private baseUrl = buildApiUrl('class-enrollment-picker');

  getAvailableStudents(
    classId: number,
    filter: ClassPickerFilter
  ): Observable<OperationResult<PagedResult<ClassPickerStudent>>> {
    let params = new HttpParams()
      .set('page',     (filter.page     ?? 1).toString())
      .set('pageSize', (filter.pageSize ?? 20).toString());

    if (filter.searchTerm)
      params = params.set('searchTerm', filter.searchTerm);
    if (filter.birthDateFrom)
      params = params.set('birthDateFrom', filter.birthDateFrom);
    if (filter.birthDateTo)
      params = params.set('birthDateTo', filter.birthDateTo);
    if (filter.sortBy)
      params = params.set('sortBy', filter.sortBy);
    if (filter.sortDescending !== undefined)
      params = params.set('sortDescending', filter.sortDescending.toString());

    return this.http.get<OperationResult<PagedResult<ClassPickerStudent>>>(
      `${this.baseUrl}/${classId}/available-students`,
      { params }
    );
  }

  bulkEnroll(
    request: ClassPickerBulkEnrollRequest
  ): Observable<OperationResult<ClassPickerEnrollResult>> {
    return this.http.post<OperationResult<ClassPickerEnrollResult>>(
      `${this.baseUrl}/bulk-enroll`,
      request
    );
  }
}
