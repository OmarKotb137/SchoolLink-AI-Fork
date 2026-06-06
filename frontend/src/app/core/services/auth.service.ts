import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RoleService } from '../../shared/role.service';

export interface UserInfo {
  userId: number;
  fullName: string;
  role: string;
}

export interface AuthResponse {
  isSuccess: boolean;
  data?: {
    accessToken: string;
    refreshToken: string;
    expiry: string;
    userId: number;
    fullName: string;
    role: string;
  };
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private base = `${environment.apiUrl}/Auth`;
  private readonly TOKEN_KEY = 'access_token';
  private readonly REFRESH_KEY = 'refresh_token';
  private readonly USER_KEY = 'user_info';

  token = signal<string | null>(localStorage.getItem(this.TOKEN_KEY));
  user = signal<UserInfo | null>(this.loadUser());

  private roleService = inject(RoleService);

  constructor(private http: HttpClient) {}

  private loadUser(): UserInfo | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (raw) {
      const u = JSON.parse(raw) as UserInfo;
      u.role = u.role.toLowerCase();
      return u;
    }
    return null;
  }

  private persist(token: string, refreshToken: string, user: UserInfo) {
    user.role = user.role.toLowerCase();
    localStorage.setItem(this.TOKEN_KEY, token);
    localStorage.setItem(this.REFRESH_KEY, refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.token.set(token);
    this.user.set(user);
    this.roleService.setRole(user.role);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  login(role: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login/${role}`, { email, password }).pipe(
      tap(res => {
        if (res.isSuccess && res.data) {
          this.persist(res.data.accessToken, res.data.refreshToken, {
            userId: res.data.userId,
            fullName: res.data.fullName,
            role: res.data.role,
          });
        }
      }),
      catchError(err => {
        const msg = err.error?.message || err.error?.title || 'فشل تسجيل الدخول';
        return throwError(() => new Error(msg));
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    const expiredAccessToken = this.getToken()!;
    const refreshToken = localStorage.getItem(this.REFRESH_KEY)!;
    return this.http.post<AuthResponse>(`${this.base}/refresh-token`, { expiredAccessToken, refreshToken }).pipe(
      tap(res => {
        if (res.isSuccess && res.data) {
          this.persist(res.data.accessToken, res.data.refreshToken, {
            userId: res.data.userId,
            fullName: res.data.fullName,
            role: res.data.role,
          });
        }
      })
    );
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.token.set(null);
    this.user.set(null);
    this.roleService.setRole('');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
