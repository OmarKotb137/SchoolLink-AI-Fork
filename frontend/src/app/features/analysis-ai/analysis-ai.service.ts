import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../../core/utils/api-url';
import { OperationResult } from '../../core/models/api.model';

export interface TeacherGrowthWeek {
  periodId: number;
  label: string;
  orderNum: number;
  averageScore: number;
  evaluationsCount: number;
}

export interface TeacherWeeklyTrend {
  teacherId: number;
  teacherName: string;
  subjectName: string;
  color: string;
  firstHalfAverage: number;
  secondHalfAverage: number;
  weeklyScores: TeacherGrowthWeek[];
}

export interface TeacherGrowthSignal {
  title: string;
  description: string;
  severity: 'success' | 'warning' | 'critical' | 'info';
  teacherId?: number;
  teacherName?: string;
}

export interface TeacherGrowthCard {
  teacherId: number;
  teacherName: string;
  subjectId: number;
  subjectName: string;
  classId: number;
  className: string;
  gradeLevelName: string;
  studentsCount: number;
  evaluatedStudentsCount: number;
  evaluatedWeeks: number;
  totalConfiguredWeeks: number;
  firstHalfAverage: number;
  secondHalfAverage: number;
  averageChange: number;
  growthRate: number;
  improvedStudentsRate: number;
  declinedStudentsRate: number;
  stableStudentsRate: number;
  examGrowthRate: number;
  momentum: 'up' | 'down' | 'stable';
  riskLevel: 'healthy' | 'watch' | 'critical';
  improvedCount: number;
  declinedCount: number;
  stableCount: number;
}

export interface TeacherGrowthOverview {
  academicYearId: number;
  academicYearName: string;
  term?: number;
  teachersCount: number;
  evaluatedWeeks: number;
  totalConfiguredWeeks: number;
  schoolGrowthRate: number;
  schoolAverageChange: number;
  improvedStudentsRate: number;
  declinedStudentsRate: number;
  totalImprovedCount: number;
  totalDeclinedCount: number;
  totalEvaluatedCount: number;
  weeklyTrend: TeacherGrowthWeek[];
  teachersWeeklyTrend: TeacherWeeklyTrend[];
  signals: TeacherGrowthSignal[];
}

export interface TeacherGrowthTeachers {
  teachers: TeacherGrowthCard[];
}

export interface TeacherGrowthDashboard {
  academicYearId: number;
  academicYearName: string;
  term?: number;
  evaluatedWeeks: number;
  totalConfiguredWeeks: number;
  teachersCount: number;
  schoolGrowthRate: number;
  schoolAverageChange: number;
  improvedStudentsRate: number;
  declinedStudentsRate: number;
  totalImprovedCount: number;
  totalDeclinedCount: number;
  totalEvaluatedCount: number;
  teachers: TeacherGrowthCard[];
  weeklyTrend: TeacherGrowthWeek[];
  teachersWeeklyTrend: TeacherWeeklyTrend[];
  signals: TeacherGrowthSignal[];
}

export interface TeacherGrowthStudent {
  studentId: number;
  studentName: string;
  firstHalfAverage: number;
  secondHalfAverage: number;
  change: number;
  status: 'improved' | 'declined' | 'stable';
  evaluatedWeeks: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface TeacherGrowthStudentPage {
  summary: TeacherGrowthCard;
  students: PagedResult<TeacherGrowthStudent>;
}

export interface StudentGrowthWeek {
  periodName: string;
  orderNum: number;
  score: number;
  maxScore: number;
  percentage: number;
  isFirstHalf: boolean;
}

export interface StudentGrowthRankingItem {
  studentId: number;
  studentName: string;
  change: number;
  firstHalfAverage: number;
  secondHalfAverage: number;
  status: 'improved' | 'declined' | 'stable';
  subjectId: number;
  subjectName: string;
  teacherName: string;
  className: string;
  averageScore: number;
  averageMaxScore: number;
  maxPerPeriod: number;
  monthlyExam1Score: number;
  monthlyExam1Max: number;
  monthlyExam2Score: number;
  monthlyExam2Max: number;
}

export interface GradeLevelRankingGroup {
  gradeLevelId: number;
  gradeLevelName: string;
  students: StudentGrowthRankingItem[];
}

export interface StudentGrowthRanking {
  topImproved: StudentGrowthRankingItem[];
  topDeclined: StudentGrowthRankingItem[];
  topEvaluationStudents: StudentGrowthRankingItem[];
  topMonthlyExamStudents: StudentGrowthRankingItem[];
  topFinalExamStudentsByGrade: GradeLevelRankingGroup[];
}

export interface StudentSubjectExam {
  subjectId: number;
  subjectName: string;
  monthlyExam1Score?: number;
  monthlyExam1Max?: number;
  monthlyExam1Percent?: number;
  monthlyExam2Score?: number;
  monthlyExam2Max?: number;
  monthlyExam2Percent?: number;
  status: 'improved' | 'declined' | 'stable';
}

export interface StudentExamSummary {
  studentId: number;
  studentName: string;
  subjects: StudentSubjectExam[];
}

export interface StudentFinalGradeSubject {
  subjectId: number;
  subjectName: string;
  finalExamScore: number;
  writtenTotal: number;
  total: number;
  maxTotal: number;
  percentage: number;
}

export interface StudentFinalGradeSummary {
  studentId: number;
  studentName: string;
  subjects: StudentFinalGradeSubject[];
}

// ── Class × Subject × Teacher Board ─────────────────────────
export interface SubjectTeacherEntry {
  subjectId: number;
  subjectName: string;
  teacherId: number;
  teacherName: string;
  studentsCount: number;
}

export interface ClassSubjectTeacherBoardItem {
  classId: number;
  className: string;
  gradeLevelName: string;
  subjects: SubjectTeacherEntry[];
}

export interface ClassSubjectTeacherBoard {
  classes: ClassSubjectTeacherBoardItem[];
}

@Injectable({ providedIn: 'root' })
export class AnalysisAiService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('class-analysis');

  getTeacherGrowth(term?: number, teacherId?: number, classId?: number): Observable<OperationResult<TeacherGrowthDashboard>> {
    let params = new HttpParams();
    if (term) params = params.set('term', term);
    if (teacherId) params = params.set('teacherId', teacherId);
    if (classId) params = params.set('classId', classId);
    return this.http.get<OperationResult<TeacherGrowthDashboard>>(`${this.apiUrl}/teacher-growth`, { params });
  }

  getTeacherGrowthOverview(term?: number, teacherId?: number, classId?: number): Observable<OperationResult<TeacherGrowthOverview>> {
    let params = new HttpParams();
    if (term) params = params.set('term', term);
    if (teacherId) params = params.set('teacherId', teacherId);
    if (classId) params = params.set('classId', classId);
    return this.http.get<OperationResult<TeacherGrowthOverview>>(`${this.apiUrl}/teacher-growth/overview`, { params });
  }

  getTeacherGrowthTeachers(term?: number, teacherId?: number, classId?: number): Observable<OperationResult<TeacherGrowthTeachers>> {
    let params = new HttpParams();
    if (term) params = params.set('term', term);
    if (teacherId) params = params.set('teacherId', teacherId);
    if (classId) params = params.set('classId', classId);
    return this.http.get<OperationResult<TeacherGrowthTeachers>>(`${this.apiUrl}/teacher-growth/teachers`, { params });
  }

  getTeacherGrowthStudents(teacherId: number, classId?: number, subjectId?: number, term?: number, page = 1, pageSize = 20): Observable<OperationResult<TeacherGrowthStudentPage>> {
    let params = new HttpParams()
      .set('teacherId', teacherId)
      .set('page', page)
      .set('pageSize', pageSize);
    if (classId) params = params.set('classId', classId);
    if (subjectId) params = params.set('subjectId', subjectId);
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<TeacherGrowthStudentPage>>(`${this.apiUrl}/teacher-growth/students`, { params });
  }

  getStudentWeeks(studentId: number, classId?: number, subjectId?: number, teacherId?: number, term?: number): Observable<OperationResult<StudentGrowthWeek[]>> {
    let params = new HttpParams()
      .set('studentId', studentId);
    if (classId) params = params.set('classId', classId);
    if (subjectId) params = params.set('subjectId', subjectId);
    if (teacherId) params = params.set('teacherId', teacherId);
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<StudentGrowthWeek[]>>(`${this.apiUrl}/teacher-growth/student-weeks`, { params });
  }

  getStudentGrowthRankings(term?: number): Observable<OperationResult<StudentGrowthRanking>> {
    let params = new HttpParams();
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<StudentGrowthRanking>>(`${this.apiUrl}/teacher-growth/student-rankings`, { params });
  }

  getStudentExamSummary(studentId: number, term?: number): Observable<OperationResult<StudentExamSummary>> {
    let params = new HttpParams()
      .set('studentId', studentId);
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<StudentExamSummary>>(`${this.apiUrl}/teacher-growth/student-exams`, { params });
  }

  getStudentFinalGrades(studentId: number, term?: number): Observable<OperationResult<StudentFinalGradeSummary>> {
    let params = new HttpParams()
      .set('studentId', studentId);
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<StudentFinalGradeSummary>>(`${this.apiUrl}/teacher-growth/student-final-grades`, { params });
  }

  getClassSubjectTeacherBoard(term?: number): Observable<OperationResult<ClassSubjectTeacherBoard>> {
    let params = new HttpParams();
    if (term) params = params.set('term', term);
    return this.http.get<OperationResult<ClassSubjectTeacherBoard>>(`${this.apiUrl}/subject-teacher-board`, { params });
  }
}
