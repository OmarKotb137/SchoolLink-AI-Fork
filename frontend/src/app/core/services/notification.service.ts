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
  type: number;
  typeName?: string;
  isRead: boolean;
  dataJson?: string;
  createdAt: string;
  createdSince?: string;
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
