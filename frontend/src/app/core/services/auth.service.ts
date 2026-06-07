import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap, catchError, throwError, map, finalize } from 'rxjs';
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

    throw new Error('ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â§ÃƒËœÃ‚Â³ÃƒËœÃ‚ÂªÃƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚Â¯Ãƒâ„¢Ã‹â€ ÃƒËœÃ‚Â± Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â³ÃƒËœÃ‚ÂªÃƒËœÃ‚Â®ÃƒËœÃ‚Â¯Ãƒâ„¢Ã¢â‚¬Â¦ ÃƒËœÃ‚ÂºÃƒâ„¢Ã…Â ÃƒËœÃ‚Â± Ãƒâ„¢Ã¢â‚¬Â¦ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â¹Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Â¦');
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  login(role: AppRole, email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.base}/login/${role}`, { email, password }).pipe(
      map(res => {
        const data = res.data ?? res;
        return {
          ...data,
          role: this.normalizeRole(data.role),
        };
      }),
      tap(session => {
        this.persist(session);
      }),
      catchError(err => {
        const msg = err.error?.message || err.error?.title || err.message || 'Ãƒâ„¢Ã‚ÂÃƒËœÃ‚Â´Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚ÂªÃƒËœÃ‚Â³ÃƒËœÃ‚Â¬Ãƒâ„¢Ã…Â Ãƒâ„¢Ã¢â‚¬Å¾ ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¯ÃƒËœÃ‚Â®Ãƒâ„¢Ã‹â€ Ãƒâ„¢Ã¢â‚¬Å¾';
        return throwError(() => new Error(msg));
      })
    );
  }

  refreshToken(): Observable<any> {
    const expiredAccessToken = this.getToken();
    const refreshToken = localStorage.getItem(this.REFRESH_KEY);

    if (!expiredAccessToken || !refreshToken) {
      return throwError(() => new Error('Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â§ ÃƒËœÃ‚ÂªÃƒâ„¢Ã‹â€ ÃƒËœÃ‚Â¬ÃƒËœÃ‚Â¯ ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â³ÃƒËœÃ‚Â© ÃƒËœÃ‚ÂµÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â­ÃƒËœÃ‚Â© Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â«Ãƒâ„¢Ã¢â‚¬Â¡ÃƒËœÃ‚Â§'));
    }

    return this.http.post<any>(`${this.base}/refresh-token`, { expiredAccessToken, refreshToken }).pipe(
      map(res => {
        const data = res.data ?? res;
        return {
          ...data,
          role: this.normalizeRole(data.role),
        };
      }),
      tap(session => {
        this.persist(session);
      }),
      catchError(err => {
        const msg = err.error?.message || err.error?.title || err.message || 'ÃƒËœÃ‚ÂªÃƒËœÃ‚Â¹ÃƒËœÃ‚Â°ÃƒËœÃ‚Â± ÃƒËœÃ‚ÂªÃƒËœÃ‚Â­ÃƒËœÃ‚Â¯Ãƒâ„¢Ã…Â ÃƒËœÃ‚Â« ÃƒËœÃ‚Â§Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â¬Ãƒâ„¢Ã¢â‚¬Å¾ÃƒËœÃ‚Â³ÃƒËœÃ‚Â©';
        return throwError(() => new Error(msg));
      })
    );
  }

  clearSession() {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.roleService.clearRole();
    this.token.set(null);
    this.user.set(null);
  }

  logout(): Observable<any> {
    const refreshToken = this.getRefreshToken();

    if (!refreshToken) {
      this.clearSession();
      return of(void 0);
    }

    return this.http.post<any>(`${this.base}/logout`, { refreshToken }).pipe(
      map(() => void 0),
      catchError(() => of(void 0)),
      finalize(() => {
        this.clearSession();
      })
    );
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
