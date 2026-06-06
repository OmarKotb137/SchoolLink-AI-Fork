import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError, map } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { AppRole, RoleService } from '../../shared/role.service';

export interface UserInfo {
  userId: number;
  fullName: string;
  role: AppRole;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiry: string;
  userId: number;
  fullName: string;
  role: AppRole;
}

type RawAuthSession = Omit<AuthSession, 'role'> & { role: string };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private base = buildApiUrl('Auth');
  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_KEY = 'refresh_token';
  private readonly USER_KEY = 'user_info';

  token = signal<string | null>(localStorage.getItem(this.TOKEN_KEY));
  user = signal<UserInfo | null>(this.loadUser());

  constructor(
    private http: HttpClient,
    private roleService: RoleService,
  ) {}

  private loadUser(): UserInfo | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as UserInfo;
      return {
        userId: parsed.userId,
        fullName: parsed.fullName,
        role: this.normalizeRole(parsed.role),
      };
    } catch {
      localStorage.removeItem(this.USER_KEY);
      return null;
    }
  }

  private persist(session: AuthSession) {
    const user: UserInfo = {
      userId: session.userId,
      fullName: session.fullName,
      role: this.normalizeRole(session.role),
    };

    localStorage.setItem(this.TOKEN_KEY, session.accessToken);
    localStorage.setItem(this.REFRESH_KEY, session.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.token.set(session.accessToken);
    this.user.set(user);
    this.roleService.setRole(user.role);
  }

  private normalizeRole(role: string): AppRole {
    const normalized = role.trim().toLowerCase();

    if (normalized === 'admin' || normalized === 'teacher' || normalized === 'student' || normalized === 'parent') {
      return normalized;
    }

    throw new Error('تم استلام دور مستخدم غير مدعوم');
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  login(role: AppRole, email: string, password: string): Observable<AuthSession> {
    return this.http.post<RawAuthSession>(`${this.base}/login/${role}`, { email, password }).pipe(
      map(res => ({
        ...res,
        role: this.normalizeRole(res.role),
      })),
      tap(session => {
        this.persist(session);
      }),
      catchError(err => {
        const msg = err.error?.message || err.error?.title || err.message || 'فشل تسجيل الدخول';
        return throwError(() => new Error(msg));
      })
    );
  }

  refreshToken(): Observable<AuthSession> {
    const expiredAccessToken = this.getToken();
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);

    if (!expiredAccessToken || !refreshToken) {
      return throwError(() => new Error('لا توجد جلسة صالحة لتحديثها'));
    }

    return this.http.post<RawAuthSession>(`${this.base}/refresh-token`, { expiredAccessToken, refreshToken }).pipe(
      map(res => ({
        ...res,
        role: this.normalizeRole(res.role),
      })),
      tap(session => {
        this.persist(session);
      }),
      catchError(err => {
        const msg = err.error?.message || err.error?.title || err.message || 'تعذر تحديث الجلسة';
        return throwError(() => new Error(msg));
      })
    );
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.roleService.clearRole();
    this.token.set(null);
    this.user.set(null);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
