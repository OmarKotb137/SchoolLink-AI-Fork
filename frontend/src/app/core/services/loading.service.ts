import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  /** عدد الطلبات النشطة — يضمن عدم إخفاء الـ spinner لو أكتر من request شغال في نفس الوقت */
  private _activeRequests = 0;
  private _loading        = signal(false);
  readonly isLoading      = this._loading.asReadonly();

  show(): void {
    this._activeRequests++;
    this._loading.set(true);
  }

  hide(): void {
    this._activeRequests = Math.max(0, this._activeRequests - 1);
    if (this._activeRequests === 0) {
      this._loading.set(false);
    }
  }
}
