import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { buildBackendUrl } from '../utils/api-url';

export interface NotificationDto {
  id: number;
  userId: number;
  title: string;
  body: string;
  type: number | string;  // SignalR ترجع number، REST API ترجع string
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationSignalRService implements OnDestroy {
  private auth = inject(AuthService);
  private hubConnection: signalR.HubConnection | null = null;

  /** Emitted whenever a new notification arrives via SignalR */
  newNotification = signal<NotificationDto | null>(null);
  isConnected = signal(false);

  /** آخر userId اتصلنا بيه عشان نتأكد إن مفيش تغيير */
  private lastUserId: number | null = null;

  /** IDs الإشعارات اللي اتبعتت بالفعل — لمنع إعادة بعت نفس الإشعار */
  private emittedIds = new Set<number>();

  private hubUrl = buildBackendUrl('/hubs/notifications');

  startConnection(): void {
    const token = this.auth.getToken();
    if (!token) return;

    // لو في userId جديد — نمسح سجل الإشعارات القديمة
    const currentUserId = this.auth.user()?.userId ?? null;
    if (currentUserId !== null && currentUserId !== this.lastUserId) {
      this.emittedIds.clear();
    }

    // لو الاتصال شغال لنفس المستخدم — مفيش داعي نعيد التشغيل
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected && this.lastUserId === currentUserId) return;

    // لو الاتصال شغال بس لمستخدم تاني — نوقف القديم ونبدا جديد
    if (this.hubConnection) {
      this.hubConnection.off('ReceiveNotification');
      this.hubConnection.stop().catch(() => {});
      this.hubConnection = null;
      this.isConnected.set(false);
    }

    this.lastUserId = currentUserId;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => this.auth.getToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      // منع إعادة بعت نفس الإشعار (لو جاي اتنين push لنفس الـ id)
      if (this.emittedIds.has(notification.id)) return;
      this.emittedIds.add(notification.id);
      this.newNotification.set(notification);
    });

    this.hubConnection.onreconnecting(() => {
      this.isConnected.set(false);
    });

    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
    });

    this.hubConnection.onclose(() => {
      this.isConnected.set(false);
    });

    this.hubConnection.start()
      .then(() => this.isConnected.set(true))
      .catch(() => { /* silent */ });
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.off('ReceiveNotification');
      this.hubConnection.stop().catch(() => {});
      this.hubConnection = null;
      this.isConnected.set(false);
    }
    this.lastUserId = null;
    this.emittedIds.clear();
  }

  /** مسح الإشعار المخزّن لمنع إعادة معالجته */
  clearNotification(): void {
    this.newNotification.set(null);
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }
}
