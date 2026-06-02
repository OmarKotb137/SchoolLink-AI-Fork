import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RoleService {
  currentRole = signal<string>(localStorage.getItem('role') ?? 'admin');

  setRole(role: string) {
    localStorage.setItem('role', role);
    this.currentRole.set(role);
  }
}
