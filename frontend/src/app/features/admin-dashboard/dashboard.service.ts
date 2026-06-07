import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';
import { buildApiUrl } from '../../core/utils/api-url';

export interface AdminDashboardData {
  totalStudents: number;
  totalTeachers: number;
  totalClasses: number;
  successRate: number;
  weeklyActivity: { day: string; count: number }[];
  recentActivities: string[];
  recentUsers: { name: string; role: string; email: string; status: string }[];
}

interface OperationResult<T> {
  isSuccess: boolean;
  data: T;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private base = buildApiUrl('Dashboard');

  get(academicYearId?: number) {
    const params = academicYearId ? `?academicYearId=${academicYearId}` : '';
    return this.http.get<OperationResult<AdminDashboardData>>(`${this.base}${params}`).pipe(
      map(res => res.data)
    );
  }
}
