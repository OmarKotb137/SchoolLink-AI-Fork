import { Injectable, inject, signal, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { buildBackendUrl } from '../utils/api-url';

export interface NotificationDto {
  id: number;
  userId: number;
  title: string;
  body: string;
  type: number;
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

  private hubUrl = buildBackendUrl('/hubs/notifications');

  startConnection(): void {
    // Don't start if already connected
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;
    // Don't start if no token
    const token = this.auth.getToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
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
      this.hubConnection.stop().catch(() => {});
      this.hubConnection = null;
      this.isConnected.set(false);
    }
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }
}
