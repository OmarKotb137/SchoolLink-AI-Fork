import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../../core/utils/api-url';
import { OperationResult } from '../../core/models/api.model';
import { ClassAnalysisFull, ClassAnalysisOverview, SubjectPerformance, AttendanceTrend, TopStudent, AtRiskStudent, Weakness, ClassStudent } from './class-analysis.models';

export interface ClassInfo {
  id: number;
  name: string;
  gradeLevelName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ClassAnalysisService {
  private http = inject(HttpClient);
  private apiUrl = buildApiUrl('class-analysis');

  /** Fetch all class analysis data in one call */
  getFullAnalysis(classId: number, term?: number): Observable<OperationResult<ClassAnalysisFull>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<ClassAnalysisFull>>(`${this.apiUrl}/${classId}/full`, { params });
  }

  /** Individual endpoints for granular loading */
  getOverview(classId: number, term?: number): Observable<OperationResult<ClassAnalysisOverview>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<ClassAnalysisOverview>>(`${this.apiUrl}/${classId}/overview`, { params });
  }

  getSubjectPerformance(classId: number, term?: number): Observable<OperationResult<SubjectPerformance[]>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<SubjectPerformance[]>>(`${this.apiUrl}/${classId}/subjects`, { params });
  }

  getAttendanceTrends(classId: number, term?: number): Observable<OperationResult<AttendanceTrend[]>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<AttendanceTrend[]>>(`${this.apiUrl}/${classId}/attendance`, { params });
  }

  getTopStudents(classId: number, count = 10, term?: number): Observable<OperationResult<TopStudent[]>> {
    let params = new HttpParams().set('count', count);
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<TopStudent[]>>(`${this.apiUrl}/${classId}/top-students`, { params });
  }

  getAtRiskStudents(classId: number, term?: number): Observable<OperationResult<AtRiskStudent[]>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<AtRiskStudent[]>>(`${this.apiUrl}/${classId}/at-risk`, { params });
  }

  getWeaknessAnalysis(classId: number, term?: number): Observable<OperationResult<Weakness[]>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<Weakness[]>>(`${this.apiUrl}/${classId}/weakness`, { params });
  }

  getStudents(classId: number, term?: number): Observable<OperationResult<ClassStudent[]>> {
    let params = new HttpParams();
    if (term != null) params = params.set('term', term);
    return this.http.get<OperationResult<ClassStudent[]>>(`${this.apiUrl}/${classId}/students`, { params });
  }
}
