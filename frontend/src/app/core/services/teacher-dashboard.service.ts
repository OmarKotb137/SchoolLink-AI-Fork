import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface TeacherClassDto {
  classId: number;
  className: string;
  subjectName: string;
  studentCount: number;
}

export interface TeacherDashboardDto {
  userName: string;
  todayClassesCount: number;
  totalStudentsCount: number;
  pendingSubmissionsCount: number;
  classes: TeacherClassDto[];
  tasks: string[];
}

@Injectable({ providedIn: 'root' })
export class TeacherDashboardService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('teacher-dashboard');

  getDashboard(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }
}
