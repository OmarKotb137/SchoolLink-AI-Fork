import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Criteria {
  id: string;
  name: string;
  max: number;
  autoCalcType?: number;
  absenceLinked?: boolean;
  absenceMax?: number;
}

export interface SumColumn {
  id: string;
  name: string;
  max: number;
}

export interface Template {
  id: number;
  apiId?: number;
  name: string;
  subjectName?: string;
  stage: string;
  subjectId?: number;
  weeks: number;
  start_date: string;
  school: string;
  directorate: string;
  administration: string;
  criteria: Criteria[];
  summary_columns: SumColumn[];
  week_names: string[];
  week_dates: string[];
  absent_days: string[];
  weekly_max: number;
  month_groups: { name: string; weeks: number[] }[];
}

export interface Student {
  id: number;
  name: string;
  enrollmentId?: number;
}

export interface ClassItem {
  linkId?: number;
  id: number;
  name: string;
  teacher: string;
  subject: string;
  year: string;
  template_id: number;
  students: Student[];
}

export interface SchoolProfile {
  schoolName: string;
  directorate: string;
  educationalAdministration: string;
}

export interface EvaluationPeriod {
  id: number;
  academicYearId: number;
  name: string;
  periodType: number;
  orderNum: number;
  startDate: string;
  endDate: string;
  monthName: string;
}

export interface EvaluationItem {
  id: number;
  templateId: number;
  name: string;
  maxScore: number;
  weight: number;
  itemType: number;
  autoCalcType: number;
  displayOrder: number;
  isVisible: boolean;
}

export interface EvaluationTemplate {
  id: number;
  name: string;
  gradeLevelName: string;
  subjectName: string;
  academicYearName: string;
  calculationType: number;
  isActive: boolean;
}

export interface StudentEvaluation {
  id: number;
  enrollmentId: number;
  evaluationItemId: number;
  periodId: number;
  score: number;
  itemName: string;
  maxScore: number;
  subjectName: string;
  periodName: string;
}

export interface ClassEvaluation {
  enrollmentId: number;
  studentName: string;
  evaluations: StudentEvaluation[];
}

@Injectable({ providedIn: 'root' })
export class GradeMonitorService {
  private http = inject(HttpClient);
  private base = 'http://localhost:5002/api';

  // ─── School Profile ─────────────────────────────────────
  getSchoolProfile() {
    return this.http.get<any>(`${this.base}/SchoolProfile`);
  }

  // ─── Subjects ───────────────────────────────────────────
  getSubjects() {
    return this.http.get<any>(`${this.base}/Subjects`);
  }

  // ─── Evaluation Templates ──────────────────────────────
  getTemplatesByGradeLevel(gradeLevelId: number, academicYearId: number) {
    return this.http.get<any>(`${this.base}/EvaluationTemplates/by-grade-level?gradeLevelId=${gradeLevelId}&academicYearId=${academicYearId}`);
  }

  getTemplateById(id: number) {
    return this.http.get<any>(`${this.base}/EvaluationTemplates/${id}`);
  }

  createTemplate(data: any) {
    return this.http.post<any>(`${this.base}/EvaluationTemplates`, data);
  }

  updateTemplate(id: number, data: any) {
    return this.http.put<any>(`${this.base}/EvaluationTemplates/${id}`, data);
  }

  deleteTemplate(id: number) {
    return this.http.delete<any>(`${this.base}/EvaluationTemplates/${id}`);
  }

  // ─── Evaluation Items ──────────────────────────────────
  getItemsByTemplate(templateId: number) {
    return this.http.get<any>(`${this.base}/EvaluationItems/by-template/${templateId}`);
  }

  createItem(data: any) {
    return this.http.post<any>(`${this.base}/EvaluationItems`, data);
  }

  updateItem(id: number, data: any) {
    return this.http.put<any>(`${this.base}/EvaluationItems/${id}`, data);
  }

  deleteItem(id: number) {
    return this.http.delete<any>(`${this.base}/EvaluationItems/${id}`);
  }

  // ─── Evaluation Periods ────────────────────────────────
  getPeriodsByAcademicYear(academicYearId: number, type?: number) {
    const params = type != null ? `?type=${type}` : '';
    return this.http.get<any>(`${this.base}/EvaluationPeriods/by-academic-year/${academicYearId}${params}`);
  }

  getCurrentWeek(academicYearId: number) {
    return this.http.get<any>(`${this.base}/EvaluationPeriods/current-week/${academicYearId}`);
  }

  getDistinctMonthNames(academicYearId: number) {
    return this.http.get<any>(`${this.base}/EvaluationPeriods/month-names/${academicYearId}`);
  }

  getPeriodsByMonth(academicYearId: number, monthName: string) {
    return this.http.get<any>(`${this.base}/EvaluationPeriods/by-month/${academicYearId}?monthName=${monthName}`);
  }

  createPeriod(data: any) {
    return this.http.post<any>(`${this.base}/EvaluationPeriods`, data);
  }

  // ─── Student Evaluations ───────────────────────────────
  getEvaluationsByClassPeriod(classId: number, periodId: number) {
    return this.http.get<any>(`${this.base}/StudentEvaluations/by-class-period?classId=${classId}&periodId=${periodId}`);
  }

  recordEvaluation(data: any) {
    return this.http.post<any>(`${this.base}/StudentEvaluations`, data);
  }

  updateEvaluation(data: any) {
    return this.http.put<any>(`${this.base}/StudentEvaluations`, data);
  }

  deleteEvaluation(id: number) {
    return this.http.delete<any>(`${this.base}/StudentEvaluations/${id}`);
  }

  autoFillAttendance(classId: number, periodId: number) {
    return this.http.post<any>(`${this.base}/StudentEvaluations/auto-fill-attendance?classId=${classId}&periodId=${periodId}`, {});
  }

  // ─── Daily Absences ────────────────────────────────────
  getAbsencesByEnrollment(enrollmentId: number, fromDate?: string, toDate?: string) {
    let url = `${this.base}/DailyAbsences/by-enrollment/${enrollmentId}`;
    const params: string[] = [];
    if (fromDate) params.push(`fromDate=${fromDate}`);
    if (toDate) params.push(`toDate=${toDate}`);
    if (params.length) url += '?' + params.join('&');
    return this.http.get<any>(url);
  }

  getAbsencesByEnrollments(enrollmentIds: number[], fromDate: string, toDate: string) {
    return this.http.get<any>(`${this.base}/DailyAbsences/by-enrollments?enrollmentIds=${enrollmentIds.join(',')}&fromDate=${fromDate}&toDate=${toDate}`);
  }

  recordAbsence(data: any) {
    return this.http.post<any>(`${this.base}/DailyAbsences`, data);
  }

  // ─── Periodic Assessments ──────────────────────────────
  recordAssessment(data: any) {
    return this.http.post<any>(`${this.base}/PeriodicAssessments`, data);
  }

  getAssessmentsByEnrollment(enrollmentId: number) {
    return this.http.get<any>(`${this.base}/PeriodicAssessments/by-enrollment/${enrollmentId}`);
  }

  // ─── Period Averages ──────────────────────────────────
  calculatePeriodAverage(data: any) {
    return this.http.post<any>(`${this.base}/PeriodAverages/calculate`, data);
  }

  // ─── Final Grades ──────────────────────────────────────
  calculateFinalGrade(enrollmentId: number) {
    return this.http.post<any>(`${this.base}/FinalGrades/calculate/${enrollmentId}`, {});
  }

  publishGrades(data: any) {
    return this.http.post<any>(`${this.base}/FinalGrades/publish`, data);
  }

  // ─── Real Classes (for dropdown picker) ──────────────
  getClasses() {
    return this.http.get<any>(`${this.base}/Classes`);
  }

  // ─── Bulk Save ─────────────────────────────────────────
  bulkSaveEvaluations(entries: {
    evaluationId?: number;
    enrollmentId: number;
    evaluationItemId: number;
    periodId: number;
    score: number | null;
  }[]) {
    return this.http.post<any>(`${this.base}/StudentEvaluations/bulk`, { entries });
  }

  // ─── Class-Template Links ─────────────────────────────
  getLinks() {
    return this.http.get<any>(`${this.base}/ClassTemplateLinks`);
  }

  createLink(data: { classId: number; templateId: number; academicYearId: number }) {
    return this.http.post<any>(`${this.base}/ClassTemplateLinks`, data);
  }

  deleteLink(id: number) {
    return this.http.delete<any>(`${this.base}/ClassTemplateLinks/${id}`);
  }
}
