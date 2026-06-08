import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

@Injectable({ providedIn: 'root' })
export class StudyPlannerService {
  private http = inject(HttpClient);
  private plansBase = buildApiUrl('study-plans');
  private aiBase = buildApiUrl('ai/study-schedule');

  getActivePlan(enrollmentId: number): Observable<any> {
    return this.http.get(`${this.plansBase}/active/${enrollmentId}`);
  }

  getAllPlans(enrollmentId: number): Observable<any> {
    return this.http.get(`${this.plansBase}/${enrollmentId}`);
  }

  generatePlan(body: any): Observable<any> {
    return this.http.post(`${this.plansBase}/generate`, body);
  }

  createManualPlan(body: any): Observable<any> {
    return this.http.post(`${this.plansBase}/manual`, body);
  }

  markComplete(itemId: number, enrollmentId: number): Observable<any> {
    const params = new HttpParams().set('enrollmentId', enrollmentId);
    return this.http.patch(`${this.plansBase}/sessions/${itemId}/complete`, null, { params });
  }

  markIncomplete(itemId: number, enrollmentId: number): Observable<any> {
    const params = new HttpParams().set('enrollmentId', enrollmentId);
    return this.http.patch(`${this.plansBase}/sessions/${itemId}/incomplete`, null, { params });
  }

  updateSession(itemId: number, body: any): Observable<any> {
    return this.http.put(`${this.plansBase}/sessions/${itemId}`, body);
  }

  deactivatePlan(id: number): Observable<any> {
    return this.http.delete(`${this.plansBase}/${id}`);
  }

  updateRestDay(planId: number, restDay: number | null): Observable<any> {
    return this.http.patch(`${this.plansBase}/${planId}/rest-day`, { restDay });
  }

  deleteSession(itemId: number, enrollmentId: number): Observable<any> {
    const params = new HttpParams().set('enrollmentId', enrollmentId);
    return this.http.delete(`${this.plansBase}/sessions/${itemId}`, { params });
  }

  optimizeSchedule(body: any): Observable<any> {
    return this.http.post(`${this.aiBase}/optimize`, body);
  }

  getRecommended(enrollmentId: number): Observable<any> {
    return this.http.get(`${this.aiBase}/recommended/${enrollmentId}`);
  }
}