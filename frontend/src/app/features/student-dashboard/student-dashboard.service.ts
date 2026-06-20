import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map, of, catchError } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { buildApiUrl } from '../../core/utils/api-url';

// ── Rich data types matching ParentChildDto from backend ──────────────────

export interface SubjectPerformance {
  subjectName: string;
  score: number;
  maxScore: number;
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
  subjectPerformances: SubjectPerformance[];
}

export interface MonthlyExam {
  subjectName: string;
  title: string;
  score: number;
  maxScore: number;
}

export interface FinalExam {
  subjectName: string;
  title: string;
  score: number;
  maxScore: number;
}

export interface UpcomingExam {
  title: string;
  subjectName: string;
  startTime?: string;
  totalScore: number;
}

export interface RecommendationSection {
  title: string;
  items: string[];
}

export interface StudentDashboardData {
  name: string;
  grade: string;
  class: string;
  performance: number;
  grades: { last: string; total: string };
  absences: number;
  attendanceRate: number;
  excusedAbsences: number;
  unexcusedAbsences: number;
  currentTermName: string;
  subjectPerformances: SubjectPerformance[];
  weeklyPerformances: WeeklyPerformance[];
  monthlyExams: MonthlyExam[];
  finalExams: FinalExam[];
  upcomingExams: UpcomingExam[];
  recommendationSections: RecommendationSection[];
  // legacy (assignments, sessions still loaded separately)
  assignments: AssignmentItem[];
  sessions: { id: number; subject: string; title: string; duration: number; isCompleted: boolean }[];
}

export interface AssignmentItem {
  id: number;
  subject: string;
  title: string;
  dueDate?: string;
  maxScore: number;
  score?: number;
  status: string;
}

interface OperationResult<T> {
  isSuccess: boolean;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class StudentDashboardService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  /** Load complete student dashboard from unified endpoint */
  loadDashboard(term?: number | null) {
    let url = buildApiUrl('student-dashboard');
    if (term != null) url += `?term=${term}`;

    return this.http.get<OperationResult<any>>(url).pipe(
      map(r => r.data as any),
      catchError(() => of(null))
    );
  }

  /** Load student enrollment to get assignments & study sessions */
  loadSideData(enrollmentId: number) {
    const assignments$ = this.http
      .get<OperationResult<AssignmentItem[]>>(buildApiUrl(`assignments/by-enrollment/${enrollmentId}`))
      .pipe(map(r => r.data ?? []), catchError(() => of([])));

    const plans$ = this.http
      .get<OperationResult<any>>(buildApiUrl(`study-plans/active/${enrollmentId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));

    return forkJoin({ assignments: assignments$, plan: plans$ }).pipe(
      map(({ assignments, plan }) => ({
        assignments: (assignments as any[]).map((a: any) => ({
          id: a.id, subject: a.subject, title: a.title,
          dueDate: a.dueDate, maxScore: a.maxScore, score: a.score, status: a.status,
        })),
        sessions: (plan?.items ?? []).map((i: any) => ({
          id: i.id, subject: i.subjectName ?? '', title: i.title,
          duration: i.durationMinutes ?? 30, isCompleted: i.isCompleted,
        })),
      })),
      catchError(() => of({ assignments: [], sessions: [] }))
    );
  }


  /** Resolve enrollment ID for a student */
  getEnrollmentId(studentId: number, academicYearId: number) {
    return this.http
      .get<OperationResult<any>>(buildApiUrl(`enrollments/active/${studentId}?academicYearId=${academicYearId}`))
      .pipe(
        map(r => {
          const d = r.data;
          // API returns EnrollmentDto object — extract id
          if (d == null) return null;
          if (typeof d === 'number') return d as number;
          if (typeof d === 'object' && d.id != null) return d.id as number;
          return null;
        }),
        catchError(() => of(null))
      );
  }


  /** Fetch student + academic year in one shot */
  getStudentContext() {
    const user = this.auth.user();
    if (!user) return of(null);

    const student$ = this.http
      .get<OperationResult<any>>(buildApiUrl(`students/by-user/${user.userId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));

    const year$ = this.http
      .get<OperationResult<any>>(buildApiUrl('academic-years/current'))
      .pipe(map(r => r.data), catchError(() => of(null)));

    return forkJoin({ student: student$, year: year$ }).pipe(
      map(({ student, year }) => {
        if (!student?.id) return null;
        return { studentId: student.id as number, academicYearId: (year?.id ?? 0) as number };
      }),
      catchError(() => of(null))
    );
  }
}
