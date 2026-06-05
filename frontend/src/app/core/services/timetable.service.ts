import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Timetable {
  id: number;
  name?: string;
  isActive?: boolean;
  academicYearId?: number | string;
  classId?: number | string;
  slots?: any[];
}

export interface TimetableSlot {
  id?: number;
  timetableId?: number;
  dayOfWeek: string;
  periodNumber: number;
  classId?: number;
  subjectId?: number;
  teacherId?: number;
  roomId?: number;
  classSubjectTeacherId?: number | null;
  isBreak?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class TimetableService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/timetables`;

  // Timetable CRUD
  getAll(): Observable<Timetable[]> {
    return this.http.get<Timetable[]>(this.apiUrl);
  }

  getByClass(classId: number, academicYearId: number): Observable<Timetable[]> {
    let params = new HttpParams()
      .set('classId', classId.toString())
      .set('academicYearId', academicYearId.toString());
    return this.http.get<Timetable[]>(`${this.apiUrl}/by-class`, { params });
  }

  getTeacherSchedule(teacherId: number, academicYearId: number): Observable<any> {
    let params = new HttpParams().set('academicYearId', academicYearId.toString());
    return this.http.get<any>(`${this.apiUrl}/teacher-schedule/${teacherId}`, { params });
  }

  getMyScheduleCurrentYear(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-schedule/current-year`);
  }

  getMyStudentScheduleCurrentYear(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-student-schedule/current-year`);
  }

  getMyChildSchedulesCurrentYear(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/my-child-schedules/current-year`);
  }

  getActiveByClass(classId: number, academicYearId: number): Observable<Timetable> {
    let params = new HttpParams()
      .set('classId', classId.toString())
      .set('academicYearId', academicYearId.toString());
    return this.http.get<Timetable>(`${this.apiUrl}/active/by-class`, { params });
  }

  create(data: Partial<Timetable>): Observable<Timetable> {
    return this.http.post<Timetable>(this.apiUrl, data);
  }

  getById(id: number): Observable<Timetable> {
    return this.http.get<Timetable>(`${this.apiUrl}/${id}`);
  }

  update(id: number, data: Partial<Timetable>): Observable<Timetable> {
    return this.http.put<Timetable>(`${this.apiUrl}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  activate(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  // Slots CRUD
  addSlot(data: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/slots`, data);
  }

  updateSlot(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/slots/${id}`, data);
  }

  deleteSlot(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/slots/${id}`);
  }
}
