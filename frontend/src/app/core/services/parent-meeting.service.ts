import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { map } from 'rxjs/operators';

export interface CreateMeetingRequest {
  studentId: number;
  reason: string;
  preferredDate?: string;
  notes?: string;
}

export interface ParentMeetingRequestDto {
  id: number;
  parentId: number;
  parentName: string;
  studentId: number;
  studentName: string;
  handledById?: number;
  handledByName?: string;
  reason: string;
  preferredDate?: string;
  scheduledDate?: string;
  status: MeetingRequestStatus | string;
  notes?: string;
  createdAt: string;
}

export enum MeetingRequestStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Completed = 3
}

export function normalizeMeetingStatus(status: MeetingRequestStatus | string | number): number {
  if (typeof status === 'number') return status;
  const key = status as keyof typeof MeetingRequestStatus;
  return MeetingRequestStatus[key] ?? Number(status) ?? 0;
}

@Injectable({ providedIn: 'root' })
export class ParentMeetingService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('parent-meeting');

  createRequest(data: CreateMeetingRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  getAll(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/all`);
  }

  getByParent(parentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/parent/${parentId}`);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  approveRequest(id: number, scheduledDate: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/approve`, { scheduledDate });
  }

  rejectRequest(id: number, reason?: string): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/reject`, { reason });
  }

  completeRequest(id: number): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}/complete`, {});
  }
}
