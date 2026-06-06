import { Injectable, signal } from '@angular/core';

export type AppRole = 'admin' | 'teacher' | 'student' | 'parent';

const ROLE_STORAGE_KEY = 'role';
const VALID_ROLES: readonly AppRole[] = ['admin', 'teacher', 'student', 'parent'];

@Injectable({ providedIn: 'root' })
export class RoleService {
  currentRole = signal<AppRole | null>(this.readStoredRole());

  setRole(role: AppRole) {
    localStorage.setItem(ROLE_STORAGE_KEY, role);
    this.currentRole.set(role);
  }

  clearRole() {
    localStorage.removeItem(ROLE_STORAGE_KEY);
    this.currentRole.set(null);
  }

  hasRole(): boolean {
    return this.currentRole() !== null;
  }

  canAccess(allowedRoles?: readonly AppRole[]): boolean {
    if (!allowedRoles?.length) {
      return this.hasRole();
    }

    const role = this.currentRole();
    return !!role && allowedRoles.includes(role);
  }

  getHomeRoute(role: AppRole | null = this.currentRole()): string {
    switch (role) {
      case 'admin':
        return '/admin';
      case 'teacher':
        return '/teacher';
      case 'student':
        return '/student';
      case 'parent':
        return '/parent';
      default:
        return '/index';
    }
  }

  getLoginRoute(role: AppRole | null = this.currentRole()): string {
    switch (role) {
      case 'admin':
      case 'teacher':
        return '/login-staff';
      case 'student':
      case 'parent':
        return '/login-guardian';
      default:
        return '/login';
    }
  }

  getLoginRouteForAllowedRoles(allowedRoles?: readonly AppRole[]): string {
    if (!allowedRoles?.length) {
      return this.getLoginRoute();
    }

    const hasStaffRole = allowedRoles.some(role => role === 'admin' || role === 'teacher');
    if (hasStaffRole) {
      return '/login-staff';
    }

    return '/login-guardian';
  }

  private readStoredRole(): AppRole | null {
    const stored = localStorage.getItem(ROLE_STORAGE_KEY);
    if (!stored || !VALID_ROLES.includes(stored as AppRole)) {
      return null;
    }

    return stored as AppRole;
  }
}
