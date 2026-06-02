import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _loading = signal(false);
  isLoading = this._loading.asReadonly();

  show() {
    this._loading.set(true);
  }

  hide() {
    this._loading.set(false);
  }
}
