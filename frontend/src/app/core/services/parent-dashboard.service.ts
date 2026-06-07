import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface ParentDashboardChild {
  studentId: number;
  studentName: string;
  className?: string | null;
  gradeLevelName?: string | null;
  isActive: boolean;
  relationship: string;
}

@Injectable({
  providedIn: 'root'
})
export class ParentDashboardService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('parent-dashboard');

  getMyChildren(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-children`);
  }
}
