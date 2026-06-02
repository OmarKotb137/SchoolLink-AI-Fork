import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  addMessage(message: string) {
    console.log('[Notification]', message);
  }
}
