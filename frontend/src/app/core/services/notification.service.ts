import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { OperationResult } from '../models/api.model';

export interface NotificationDto {
  id: number;
  userId: number;
  title: string;
  body: string;
  type: number | string;  // REST API ترجع string (بسبب JsonStringEnumConverter)، SignalR ترجع number
  typeName?: string;
  isRead: boolean;
  dataJson?: string;
  createdAt: string;
  createdSince?: string;
}

/** تحويل نوع الإشعار من string (الوارد من REST API) إلى number (الذي تفهمه دوال التصنيف) */
export function normalizeNotifType(type: number | string | undefined): number {
  if (type === undefined || type === null) return 0;
  if (typeof type === 'number') return type;
  // Map enum string → number
  const map: Record<string, number> = {
    GradeAlert: 1, BehaviorAlert: 2, AbsenceAlert: 3, NewAssignment: 4,
    ExamReminder: 5, MonthlyReport: 6, GradePublished: 7, SystemAlert: 8,
    ImprovementAlert: 9, PositiveBehavior: 10, DisciplinaryAction: 11,
    TopStudent: 16, GradeThresholdAlert: 26, AcademicProbation: 27,
    ExcessiveAbsenceWarning: 28, Announcement: 17, SchoolEvent: 18,
    Holiday: 19, EmergencyAlert: 20, ScheduleChanged: 21, SubstituteTeacher: 22,
    NewMessage: 23, GroupChatInvite: 24, ParentMeetingRequest: 29,
    HomeworkSubmitted: 12, HomeworkGraded: 13, Exam: 14, ExamResult: 15,
    ExamScheduleChanged: 32, ExamSchedulePublished: 33, ExamCheatingAlert: 34,
  };
  return map[type] ?? 0;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private base = buildApiUrl('notification');

  unreadCount = signal<number>(0);

  getNotifications(userId: number, onlyUnread = false): Observable<NotificationDto[]> {
    let params = new HttpParams();
    if (onlyUnread) params = params.set('onlyUnread', 'true');
    return this.http.get<OperationResult<NotificationDto[]>>(`${this.base}/user/${userId}`, { params })
      .pipe(map(res => res.data));
  }

  getUnreadCount(userId: number): Observable<number> {
    return this.http.get<OperationResult<number>>(`${this.base}/user/${userId}/unread-count`)
      .pipe(map(res => {
        this.unreadCount.set(res.data);
        return res.data;
      }));
  }

  markAsRead(notificationId: number): Observable<any> {
    return this.http.put(`${this.base}/${notificationId}/read`, {});
  }

  markAllAsRead(userId: number): Observable<any> {
    return this.http.put(`${this.base}/user/${userId}/read-all`, {});
  }

  deleteNotification(notificationId: number): Observable<any> {
    return this.http.delete(`${this.base}/${notificationId}`);
  }

  deleteBulkNotifications(ids: number[]): Observable<any> {
    return this.http.delete(`${this.base}/bulk`, { body: ids });
  }
}
