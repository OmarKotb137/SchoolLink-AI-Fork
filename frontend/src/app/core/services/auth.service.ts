import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private token = signal<string | null>(null);

  getToken(): string | null {
    return this.token();
  }

  setToken(token: string) {
    this.token.set(token);
  }

  logout() {
    this.token.set(null);
  }

  isAuthenticated(): boolean {
    return !!this.token();
  }
}
