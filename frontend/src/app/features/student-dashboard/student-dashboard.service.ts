import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { forkJoin, map, of, switchMap, catchError } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { buildApiUrl } from '../../core/utils/api-url';

export interface PeriodAverage {
  avgScore: number;
  maxScore: number;
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

export interface StudentDashboardData {
  overallPercentage: number;
  completedTasksPercent: number;
  periodAverages: PeriodAverage[];
  sessions: { id: number; subject: string; title: string; duration: number; isCompleted: boolean }[];
  absencesCount: number;
  assignments: AssignmentItem[];
}

interface OperationResult<T> {
  isSuccess: boolean;
  data: T;
}

@Injectable({ providedIn: 'root' })
export class StudentDashboardService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  get() {
    const user = this.auth.user();
    if (!user) return of(null);

    const student$ = this.http.get<OperationResult<any>>(buildApiUrl(`students/by-user/${user.userId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));

    const year$ = this.http.get<OperationResult<any>>(buildApiUrl('academic-years/current'))
      .pipe(map(r => r.data), catchError(() => of(null)));

    return forkJoin({ student: student$, year: year$ }).pipe(
      map(({ student, year }) => {
        if (!student?.id) return null;
        return { student, academicYearId: year?.id ?? 0 };
      }),
      catchError(() => of(null)),
    );
  }

  loadDetails(studentId: number, academicYearId: number) {
    return this.http.get<OperationResult<any>>(buildApiUrl(`enrollments/active/${studentId}?academicYearId=${academicYearId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));
  }

  loadStats(enrollmentId: number, term?: number | null) {
    let averagesUrl = buildApiUrl(`PeriodAverages/by-enrollment/${enrollmentId}`);
    if (term != null) {
      averagesUrl += `?term=${term}`;
    }
    const averages$ = this.http.get<OperationResult<any[]>>(averagesUrl)
      .pipe(map(r => r.data ?? []), catchError(() => of([])));

    const submissions$ = this.http.get<OperationResult<any[]>>(buildApiUrl(`assignment-submissions/by-student/${enrollmentId}`))
      .pipe(map(r => r.data ?? []), catchError(() => of([])));

    const plans$ = this.http.get<OperationResult<any>>(buildApiUrl(`study-plans/active/${enrollmentId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));

    const absences$ = this.http.get<OperationResult<any>>(buildApiUrl(`DailyAbsences/summary/${enrollmentId}`))
      .pipe(map(r => r.data), catchError(() => of(null)));

    const assignments$ = this.http.get<OperationResult<AssignmentItem[]>>(buildApiUrl(`assignments/by-enrollment/${enrollmentId}`))
      .pipe(map(r => r.data ?? []), catchError(() => of([])));

    return forkJoin({ averages: averages$, submissions: submissions$, plan: plans$, absences: absences$, assignments: assignments$ }).pipe(
      map(({ averages, submissions, plan, absences, assignments }) => {
        const avgPct = averages.length > 0
          ? Math.round(averages.reduce((s: any, a: any) => s + (a.maxScore > 0 ? (a.avgScore / a.maxScore) * 100 : 0), 0) / averages.length)
          : 0;

        const graded = submissions.filter((s: any) => s.isGraded);
        const total = submissions.length || 1;
        const completedPct = Math.round((graded.length / total) * 100);

        const sessions = (plan?.items ?? []).map((i: any) => ({
          id: i.id,
          subject: i.subjectName ?? '',
          title: i.title,
          duration: i.durationMinutes ?? 30,
          isCompleted: i.isCompleted,
        }));

        return {
          overallPercentage: avgPct,
          completedTasksPercent: completedPct,
          periodAverages: averages,
          sessions,
          absencesCount: absences?.totalAbsences ?? 0,
          assignments: assignments.map((a: any) => ({
            id: a.id,
            subject: a.subject,
            title: a.title,
            dueDate: a.dueDate,
            maxScore: a.maxScore,
            score: a.score,
            status: a.status,
          })),
        };
      }),
      catchError(() => of({ overallPercentage: 0, completedTasksPercent: 0, periodAverages: [], sessions: [], absencesCount: 0, assignments: [] })),
    );
  }
}
