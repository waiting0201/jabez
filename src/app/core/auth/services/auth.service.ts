import {Injectable, computed, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, of, throwError} from 'rxjs';
import {tap} from 'rxjs/operators';
import {User} from '@features/admin/users/models/user.model';

export interface JwtPayload {
  sub: string;
  name: string;
  email: string;
  roles: string[];
  permissions: string[];
  exp: number;
}

export interface LoginResponse {
  access_token: string;
  token_type: string;
}

const TOKEN_KEY = 'access_token';

@Injectable({providedIn: 'root'})
export class AuthService {
  private http = inject(HttpClient);

  private _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  /** 從 JWT payload 衍生的目前使用者（signal） */
  currentUser = computed<User | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    return {
      id: payload.sub,
      name: payload.name,
      email: payload.email,
      roleIds: payload.roles,
      status: 'active',
      createdAt: new Date(),
    };
  });

  get token(): string | null {
    return this._token();
  }

  isLoggedIn(): boolean {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1000 > Date.now();
  }

  hasPermission(permissionCode: string): boolean {
    const payload = this._decode(this._token());
    return !!payload && payload.permissions.includes(permissionCode);
  }

  /**
   * 登入：向後端取得 JWT。
   * 後端就緒時取消下方注解並移除 mock 區塊即可。
   */
  login(email: string, password: string): Observable<LoginResponse> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.post<LoginResponse>('/api/auth/login', {email, password}).pipe(
    //   tap(res => this._store(res.access_token)),
    // );

    // ─── Mock JWT（前端開發用）──────────────────────────────
    if (email && password) {
      const token = this._mockJwt(email);
      this._store(token);
      return of({access_token: token, token_type: 'Bearer'});
    }
    return throwError(() => new Error('Invalid credentials'));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
  }

  // ─── Private helpers ────────────────────────────────────

  private _store(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
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

  /** 產生 mock JWT（header.payload.signature），僅供本地開發使用 */
  private _mockJwt(email: string): string {
    const header  = btoa(JSON.stringify({alg: 'HS256', typ: 'JWT'})).replace(/=+$/, '');
    const payload = btoa(JSON.stringify({
      sub: '1',
      name: 'Alice Chen',
      email,
      roles: ['admin'],
      permissions: [
        'admin:access',
        'users:read',       'users:write',       'users:delete',
        'roles:read',       'roles:write',       'roles:delete',
        'permissions:read', 'permissions:write', 'permissions:delete',
        'departments:read', 'departments:write', 'departments:delete',
        'job-titles:read',  'job-titles:write',  'job-titles:delete',
        'approvals:read',   'approvals:write',   'approvals:delete',
        'projects:read',    'projects:write',    'projects:delete',
        'payment-requests:read', 'payment-requests:write', 'payment-requests:delete',
        'approval-tasks:read', 'approval-tasks:write',
        'leave-requests:read',  'leave-requests:write',  'leave-requests:delete',
        'travel-requests:read', 'travel-requests:write', 'travel-requests:delete',
      ],
      exp: Math.floor(Date.now() / 1000) + 86400,
    })).replace(/=+$/, '');
    return `${header}.${payload}.mock_signature`;
  }
}
