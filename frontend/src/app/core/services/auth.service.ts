import { Injectable, signal } from '@angular/core';

const TOKEN_STORAGE_KEY = 'auth_token';
const DISPLAY_NAME_STORAGE_KEY = 'display_name';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _token       = signal<string | null>(localStorage.getItem(TOKEN_STORAGE_KEY));
  private _displayName = signal<string>(localStorage.getItem(DISPLAY_NAME_STORAGE_KEY) || 'المستخدم');

  /* ── Token ────────────────────────────────────────────── */

  getToken(): string | null {
    return this._token();
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
    this._token.set(token);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this._token.set(null);
  }

  /* ── Display Name ─────────────────────────────────────── */

  readonly displayName = this._displayName.asReadonly();

  setDisplayName(name: string): void {
    localStorage.setItem(DISPLAY_NAME_STORAGE_KEY, name);
    this._displayName.set(name);
  }

  /* ── Session ──────────────────────────────────────────── */

  logout(): void {
    this.clearToken();
    localStorage.removeItem(DISPLAY_NAME_STORAGE_KEY);
    this._displayName.set('المستخدم');
  }

  isAuthenticated(): boolean {
    return !!this._token();
  }
}
