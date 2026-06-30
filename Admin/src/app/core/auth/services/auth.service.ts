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
  department_name?: string;
  department_code?: string;
  job_title_name?: string;
  job_title_level?: string | number;
  avatar?: string;
  avatar_x?: string | number;
  avatar_y?: string | number;
  avatar_scale?: string | number;
}

export interface AutoClockOutInfo {
  count: number;
  dates: string[];
}

/**
 * 財務體系部門 Code（須與後端 Constants.cs 的 DepartmentCodes.FinancialAndAbove 同步）：
 * 成員可執行撥款 / 退款 / 結案 / 批次核准等業務操作，亦顯示撥款/退款子篩選。
 * 含舊短碼與 2026 改制後英文全名，避免改組織就失效。
 * 注意：此為「使用者撥款權限」廣集合（含 CEO/總監/HQ/會計），與「財務撥款步驟」窄集合
 *（approval-task-review.ts 的 FINANCE_STEP_DEPT_CODES，只財務管理部）用途不同，勿混用。
 */
export const FINANCIAL_AND_ABOVE_DEPT_CODES = new Set([
  'CEO', 'FIN', 'AC', 'Jabez HQ',       // 舊短碼
  'Office of the Director',             // 總監室
  'Financial Management Department',    // 財務管理部
  'Accounting Department',              // 會計室
]);

export interface AutoOvertimeEndInfo {
  count: number;
  dates: string[];
}

export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  token_type: string;
  must_change_password?: boolean;
  auto_clock_out?: AutoClockOutInfo | null;
  auto_overtime_end?: AutoOvertimeEndInfo | null;
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
      avatar: payload.avatar,
      roleIds: roles,
      status: 'active',
      createdAt: new Date(),
    };
  });

  /** 當前使用者的頭像 URL（signal）— 透過 API 代理路徑顯示；無頭像則回傳 null */
  avatarUrl = computed<string | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    const raw = payload.avatar;
    if (!raw) return null;
    return raw.startsWith('http') ? raw : `${environment.apiUrl}/${raw}`;
  });

  /** 頭像 X 位置（百分比 0-100），預設 50 */
  avatarPositionX = computed<number>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return 50;
    return this._parseAvatarNumber(payload.avatar_x, 50);
  });

  /** 頭像 Y 位置（百分比 0-100），預設 50 */
  avatarPositionY = computed<number>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return 50;
    return this._parseAvatarNumber(payload.avatar_y, 50);
  });

  /** 頭像縮放倍率（1.0-3.0），預設 1.0 */
  avatarScale = computed<number>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return 1;
    return this._parseAvatarNumber(payload.avatar_scale, 1);
  });

  /** 是否為超管帳號（signal） */
  isSuperAdmin = computed<boolean>(() => {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1000 > Date.now()
      && (payload.is_superadmin === true || payload.is_superadmin === 'true');
  });

  /** 當前使用者的部門名稱（signal） */
  departmentName = computed<string | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    return payload.department_name ?? null;
  });

  /** 當前使用者的部門代碼（signal） */
  departmentCode = computed<string | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    return payload.department_code ?? null;
  });

  /** 當前使用者的職稱名稱（signal） */
  jobTitleName = computed<string | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    return payload.job_title_name ?? null;
  });

  /** 當前使用者的職級（signal），Level 數字越小 = 層級越高；找不到回傳 null */
  jobTitleLevel = computed<number | null>(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1000 <= Date.now()) return null;
    const raw = payload.job_title_level;
    if (raw === undefined || raw === null) return null;
    const n = typeof raw === 'number' ? raw : parseInt(raw, 10);
    return Number.isFinite(n) ? n : null;
  });

  /** 是否為協理以上（Level ≤ 3），用於高階主管假權限判斷 */
  isSeniorExecutive = computed<boolean>(() => {
    const level = this.jobTitleLevel();
    return level !== null && level <= 3;
  });

  /** 是否屬財務體系部門（signal）：以 FINANCIAL_AND_ABOVE_DEPT_CODES 判斷，對齊後端結案/撥款授權 */
  isFinanceDept = computed<boolean>(() =>
    FINANCIAL_AND_ABOVE_DEPT_CODES.has(this.departmentCode() ?? ''),
  );

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

  /** 修改密碼 */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/change-password`, {currentPassword, newPassword});
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

  private _parseAvatarNumber(raw: string | number | undefined, fallback: number): number {
    if (raw === undefined || raw === null) return fallback;
    const n = typeof raw === 'number' ? raw : parseFloat(raw);
    return Number.isFinite(n) ? n : fallback;
  }

  private _decode(token: string | null): JwtPayload | null {
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const binary = atob(base64);
      const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
      const json = new TextDecoder().decode(bytes);
      return JSON.parse(json) as JwtPayload;
    } catch {
      return null;
    }
  }
}
