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

export interface ChildSubject {
  subjectName: string;
  score: number;
  maxScore: number;
}

export interface UpcomingExam {
  title: string;
  subjectName: string;
  startTime: string | null;
  totalScore: number;
}

export interface WeeklyPerformance {
  periodName: string;
  weekNumber: number;
  startDate?: string;
  endDate?: string;
  avgScore: number;
  maxScore: number;
  totalScore: number;
  totalMaxScore: number;
  subjectPerformances?: ChildSubject[];
}

export interface MonthlyExamResult {
  subjectName: string;
  title: string;
  score: number;
  maxScore: number;
}

export interface FinalExamResult {
  subjectName: string;
  title: string;
  score: number;
  maxScore: number;
}

export interface RecSection {
  title: string;
  items: string[];
}

export interface ParentChildStats {
  name: string;
  grade: string;
  class: string;
  performance: number;
  grades: { last: string; total: string };
  absences: number;
  attendanceRate: number;
  excusedAbsences: number;
  unexcusedAbsences: number;
  subjectPerformances: ChildSubject[];
  recommendationsText?: string | null;
  recommendationSections: RecSection[];
  upcomingExams: UpcomingExam[];
  weeklyPerformances: WeeklyPerformance[];
  monthlyExams: MonthlyExamResult[];
  finalExams: FinalExamResult[];
  currentTermName?: string;
}

export interface ParentDashboardData {
  children: ParentChildStats[];
  recentActivities: string[];
}

@Injectable({ providedIn: 'root' })
export class ParentDashboardService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('parent-dashboard');

  getMyChildren(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-children`);
  }

  getDashboard(term?: number): Observable<any> {
    const params = term ? `?term=${term}` : '';
    return this.http.get<any>(`${this.apiUrl}${params}`);
  }
}
