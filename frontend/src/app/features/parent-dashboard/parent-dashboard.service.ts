import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildApiUrl } from '../../core/utils/api-url';

export interface ParentDashboardData {
  children: ParentChild[];
  recentActivities: string[];
}

export interface ParentChild {
  name: string;
  grade: string;
  class: string;
  performance: number;
  grades: { last: string; total: string };
  absences: number;
}

@Injectable({ providedIn: 'root' })
export class ParentDashboardService {
  private http = inject(HttpClient);
  private base = buildApiUrl('parent-dashboard');

  get() {
    return this.http.get<ParentDashboardData>(`${this.base}`);
  }
}
