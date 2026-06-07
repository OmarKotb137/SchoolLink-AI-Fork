import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TimetableDto,
  TimetableSlotDto,
  TeacherScheduleSlotDto,
  ChildScheduleDto,
} from '../models/timetable.models';

/* ── Admin / Shared DTOs ────────────────────────────────── */
export interface TimetableListItem {
  id:             number;
  classId:        number | string;
  className?:     string;
  academicYearId: number | string;
  isActive?:      boolean;
  createdAt?:     string;
  updatedAt?:     string;
  slots?:         TimetableSlotDto[];
}

export interface TimetableValidationIssue {
  severity:               string;
  code:                   string;
  message:                string;
  slotId?:                number | null;
  dayOfWeek?:             string | null;
  periodNumber?:          number | null;
  classSubjectTeacherId?: number | null;
}

export interface TimetableValidationResult {
  timetableId:                     number;
  canActivate:                     boolean;
  totalSlots:                      number;
  lessonSlots:                     number;
  breakSlots:                      number;
  missingAssignmentsCount:         number;
  overScheduledAssignmentsCount:   number;
  errors:                          TimetableValidationIssue[];
  warnings:                        TimetableValidationIssue[];
}

/* ── Re-export shared types so consumers import from one place ── */
export type { TimetableDto, TimetableSlotDto, TeacherScheduleSlotDto, ChildScheduleDto };

@Injectable({
  providedIn: 'root'
})
export class TimetableService {
  private http   = inject(HttpClient);
  private apiUrl = buildApiUrl('timetables');

  /* ── Admin: CRUD ──────────────────────────────────────── */

  getAll(): Observable<TimetableListItem[]> {
    return this.http.get<TimetableListItem[]>(this.apiUrl);
  }

  getByClass(classId: number, academicYearId: number): Observable<TimetableListItem[]> {
    const params = new HttpParams()
      .set('classId',        classId.toString())
      .set('academicYearId', academicYearId.toString());
    return this.http.get<TimetableListItem[]>(`${this.apiUrl}/by-class`, { params });
  }

  getById(id: number): Observable<TimetableDto> {
    return this.http.get<TimetableDto>(`${this.apiUrl}/${id}`);
  }

  getActiveByClass(classId: number, academicYearId: number): Observable<TimetableDto> {
    const params = new HttpParams()
      .set('classId',        classId.toString())
      .set('academicYearId', academicYearId.toString());
    return this.http.get<TimetableDto>(`${this.apiUrl}/active/by-class`, { params });
  }

  create(data: Partial<TimetableListItem>): Observable<TimetableDto> {
    return this.http.post<TimetableDto>(this.apiUrl, data);
  }

  cloneDraft(classId: number, academicYearId: number): Observable<TimetableDto> {
    const params = new HttpParams()
      .set('classId',        classId.toString())
      .set('academicYearId', academicYearId.toString());
    return this.http.post<TimetableDto>(`${this.apiUrl}/clone-draft`, {}, { params });
  }

  validate(id: number): Observable<TimetableValidationResult> {
    return this.http.get<TimetableValidationResult>(`${this.apiUrl}/${id}/validate`);
  }

  activate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  update(id: number, data: Partial<TimetableListItem>): Observable<TimetableDto> {
    return this.http.put<TimetableDto>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /* ── Admin: Slots ─────────────────────────────────────── */

  addSlot(data: unknown): Observable<TimetableSlotDto> {
    return this.http.post<TimetableSlotDto>(`${this.apiUrl}/slots`, data);
  }

  updateSlot(id: number, data: unknown): Observable<TimetableSlotDto> {
    return this.http.put<TimetableSlotDto>(`${this.apiUrl}/slots/${id}`, data);
  }

  deleteSlot(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/slots/${id}`);
  }

  /* ── Teacher ──────────────────────────────────────────── */

  getTeacherSchedule(teacherId: number, academicYearId: number): Observable<TeacherScheduleSlotDto[]> {
    const params = new HttpParams().set('academicYearId', academicYearId.toString());
    return this.http.get<TeacherScheduleSlotDto[]>(
      `${this.apiUrl}/teacher-schedule/${teacherId}`, { params }
    );
  }

  /** جدول المعلم الحالي (السنة الدراسية الحالية تلقائياً) */
  getMyScheduleCurrentYear(): Observable<TeacherScheduleSlotDto[]> {
    return this.http.get<TeacherScheduleSlotDto[]>(`${this.apiUrl}/my-schedule/current-year`);
  }

  /* ── Student ──────────────────────────────────────────── */

  /** جدول فصل الطالب للسنة الدراسية الحالية */
  getMyStudentScheduleCurrentYear(): Observable<TimetableDto> {
    return this.http.get<TimetableDto>(`${this.apiUrl}/my-student-schedule/current-year`);
  }

  /* ── Parent ───────────────────────────────────────────── */

  /** جداول جميع الأبناء للسنة الدراسية الحالية */
  getMyChildSchedulesCurrentYear(): Observable<ChildScheduleDto[]> {
    return this.http.get<ChildScheduleDto[]>(`${this.apiUrl}/my-child-schedules/current-year`);
  }
}
