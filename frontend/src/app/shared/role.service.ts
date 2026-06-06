import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly USER_KEY = 'user_info';

  currentRole = signal<string>(this.loadInitialRole());

  private loadInitialRole(): string {
    const raw = localStorage.getItem(this.USER_KEY);
    if (raw) {
      try {
        const u = JSON.parse(raw);
        return (u.role ?? '').toLowerCase();
      } catch { /* ignore */ }
    }
    return '';
  }

  setRole(role: string) {
    localStorage.setItem('role', role.toLowerCase());
    this.currentRole.set(role.toLowerCase());
  }
}
