import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface Announcement {
  id: number;
  authorId: number;
  authorName: string;
  title: string;
  body: string;
  targetRole?: string;
  targetClassId?: number;
  category?: number | string;  // API ترجع string (AnnouncementType enum)
  targetGradeLevelId?: number;
  isForAllUsers: boolean;
  isForAllStudents: boolean;
  isForAllParents: boolean;
  isForAllTeachers: boolean;
  expiresAt?: string;
  createdAt: string;
  targetedUserCount: number;
}

export interface CreateAnnouncementRequest {
  title: string;
  body: string;
  targetRole?: string;
  targetClassId?: number;
  category?: number;  // number OK — الباك إند بيفهم الرقم كمان
  targetGradeLevelId?: number;
  isForAllUsers: boolean;
  isForAllStudents: boolean;
  isForAllParents: boolean;
  isForAllTeachers: boolean;
  expiresAt?: string;
}

/** تحويل AnnouncementType من string (الوارد من API) إلى number */
export function normalizeCategory(cat: number | string | undefined | null): number {
  if (cat === undefined || cat === null) return 0;
  if (typeof cat === 'number') return cat;
  const map: Record<string, number> = {
    General: 0, Event: 1, Holiday: 2, Emergency: 3,
  };
  return map[cat] ?? 0;
}

@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('Announcement');

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  getExpired(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/expired`);
  }

  getById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateAnnouncementRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  update(id: number, data: CreateAnnouncementRequest): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  cleanupExpired(): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/cleanup-expired`, {});
  }
}
