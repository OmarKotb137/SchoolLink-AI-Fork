import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export type AcademicStatus = 0 | 1 | 2 | 3; // NoGrades=0, Unpublished=1, Passed=2, Failed=3
export type AcademicTermLabel = 1 | 2;      // FirstSemester=1, SecondSemester=2
export type ProgressionTermScope = 1 | 2 | 3; // First=1, Second=2, Both=3

export interface SubjectGrade {
  subjectId: number | null;
  subjectName: string;
  percentage: number;
  isPublished: boolean;
  isPassed: boolean;
  term?: AcademicTermLabel | null;
}

export interface StudentProgressionCandidate {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  currentClassId: number;
  currentClassName: string;
  currentGradeLevelId: number;
  currentGradeLevelName: string;
  academicYearId: number;
  academicYearName: string;
  studentIsActive: boolean;
  hasStudentAccount: boolean;
  hasFinalGrade: boolean;
  finalTotal?: number | null;
  hasPublishedFinalGrade: boolean;
  academicStatus: AcademicStatus;
  passedSubjectsCount: number;
  failedSubjectsCount: number;
  subjectGrades: SubjectGrade[];
}

export interface StudentProgressionRequest {
  enrollmentIds: number[];
  action: 1 | 2 | 3;
  targetClassId?: number | null;
  targetAcademicYearId?: number | null;
  effectiveDate: string;
  note?: string;
}

export interface StudentProgressionFailure {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  reason: string;
}

export interface StudentProgressionResult {
  totalRequested: number;
  successCount: number;
  promotedCount: number;
  retainedCount: number;
  graduatedCount: number;
  failureCount: number;
  failures: StudentProgressionFailure[];
  deactivatedStudents: string[];
  deactivatedParents: string[];
}

@Injectable({
  providedIn: 'root'
})
export class StudentProgressionService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('student-progression');

  getCandidates(
    gradeLevelId: number,
    academicYearId: number,
    termScope: ProgressionTermScope = 3,
    passingThreshold: number = 50
  ): Observable<any> {
    const params = new HttpParams()
      .set('gradeLevelId', gradeLevelId)
      .set('academicYearId', academicYearId)
      .set('termScope', termScope)
      .set('passingThreshold', passingThreshold);

    return this.http.get<any>(`${this.apiUrl}/candidates`, { params });
  }

  execute(request: StudentProgressionRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/execute`, request);
  }
}
