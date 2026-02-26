import {Injectable, computed, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, throwError} from 'rxjs';
import {switchMap, tap} from 'rxjs/operators';
import {User} from '@features/admin/users/models/user.model';
import {environment} from '@/environments/environment';

export interface JwtPayload {
  sub: string;
  name: string;
  email: string;
  roles: string | string[];
  permissions: string | string[];
  exp: number;
  is_superadmin?: string | boolean;
}

export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  token_type: string;
}

const TOKEN_KEY = 'access_token';
const REFRESH_KEY = 'refresh_token';

@Injectable({providedIn: 'root'})
export class AuthService {
  private http = inject(HttpClient);

  private _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  /** 從 JWT payload 衍生的目前使用者（signal） */
  currentUser = computed<User | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    // JWT 中 roles 可能是 string（單一角色）或 string[]（多角色）或 undefined
    const roles = !payload.roles ? [] : Array.isArray(payload.roles) ? payload.roles : [payload.roles];
    return {
      id: payload.sub,
      name: payload.name,
      email: payload.email,
      roleIds: roles,
      status: 'active',
      createdAt: new Date(),
    };
  });

  /** 是否為超管帳號（signal） */
  isSuperAdmin = computed<boolean>(() => {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1000 > Date.now()
      && (payload.is_superadmin === true || payload.is_superadmin === 'true');
  });

  get token(): string | null {
    return this._token();
  }

  get refreshTokenValue(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  isLoggedIn(): boolean {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1000 > Date.now();
  }

  hasPermission(permissionCode: string): boolean {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return false;
    if (payload.is_superadmin === true || payload.is_superadmin === 'true') return true;
    if (permissionCode === 'superadmin') return false;
    const perms = Array.isArray(payload.permissions) ? payload.permissions : [payload.permissions];
    return perms.includes(permissionCode);
  }

  /** 登入：向後端取得 JWT */
  login(email: string, password: string): Observable<LoginResponse> {
    if (!email || !password) {
      return throwError(() => new Error('Invalid credentials'));
    }
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, {email, password}).pipe(
      tap(res => {
        if (!res?.access_token) {
          console.error('[AuthService] No access_token in response!', res);
        }
        this._storeTokens(res.access_token, res.refresh_token);
      }),
    );
  }

  /** 使用 refresh token 取得新的 access token */
  refreshAccessToken(): Observable<LoginResponse> {
    const rt = this.refreshTokenValue;
    if (!rt) return throwError(() => new Error('No refresh token'));
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/refresh`, {refreshToken: rt}).pipe(
      tap(res => {
        this._storeTokens(res.access_token, res.refresh_token);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    this._token.set(null);
  }

  // ─── Private helpers ────────────────────────────────────

  private _storeTokens(accessToken: string, refreshToken?: string): void {
    localStorage.setItem(TOKEN_KEY, accessToken);
    this._token.set(accessToken);
    if (refreshToken) {
      localStorage.setItem(REFRESH_KEY, refreshToken);
    }
  }

  private _decode(token: string | null): JwtPayload | null {
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      return JSON.parse(atob(base64)) as JwtPayload;
    } catch {
      return null;
    }
  }
}
