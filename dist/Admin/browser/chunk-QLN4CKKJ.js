import {
  HttpClient,
  Injectable,
  computed,
  environment,
  inject,
  setClassMetadata,
  signal,
  tap,
  throwError,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/core/auth/services/auth.service.ts
var TOKEN_KEY = "access_token";
var REFRESH_KEY = "refresh_token";
var AuthService = class _AuthService {
  http = inject(HttpClient);
  _token = signal(localStorage.getItem(TOKEN_KEY), ...ngDevMode ? [{ debugName: "_token" }] : []);
  /** 從 JWT payload 衍生的目前使用者（signal） */
  currentUser = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    const roles = !payload.roles ? [] : Array.isArray(payload.roles) ? payload.roles : [payload.roles];
    return {
      id: payload.sub,
      name: payload.name,
      email: payload.email,
      avatar: payload.avatar,
      roleIds: roles,
      status: "active",
      createdAt: /* @__PURE__ */ new Date()
    };
  }, ...ngDevMode ? [{ debugName: "currentUser" }] : []);
  /** 當前使用者的頭像 URL（signal）— 透過 API 代理路徑顯示；無頭像則回傳 null */
  avatarUrl = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    const raw = payload.avatar;
    if (!raw)
      return null;
    return raw.startsWith("http") ? raw : `${environment.apiUrl}/${raw}`;
  }, ...ngDevMode ? [{ debugName: "avatarUrl" }] : []);
  /** 是否為超管帳號（signal） */
  isSuperAdmin = computed(() => {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1e3 > Date.now() && (payload.is_superadmin === true || payload.is_superadmin === "true");
  }, ...ngDevMode ? [{ debugName: "isSuperAdmin" }] : []);
  /** 當前使用者的部門名稱（signal） */
  departmentName = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    return payload.department_name ?? null;
  }, ...ngDevMode ? [{ debugName: "departmentName" }] : []);
  /** 當前使用者的部門代碼（signal） */
  departmentCode = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    return payload.department_code ?? null;
  }, ...ngDevMode ? [{ debugName: "departmentCode" }] : []);
  /** 當前使用者的職稱名稱（signal） */
  jobTitleName = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    return payload.job_title_name ?? null;
  }, ...ngDevMode ? [{ debugName: "jobTitleName" }] : []);
  /** 當前使用者的職級（signal），Level 數字越小 = 層級越高；找不到回傳 null */
  jobTitleLevel = computed(() => {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return null;
    const raw = payload.job_title_level;
    if (raw === void 0 || raw === null)
      return null;
    const n = typeof raw === "number" ? raw : parseInt(raw, 10);
    return Number.isFinite(n) ? n : null;
  }, ...ngDevMode ? [{ debugName: "jobTitleLevel" }] : []);
  /** 是否為協理以上（Level ≤ 3），用於高階主管假權限判斷 */
  isSeniorExecutive = computed(() => {
    const level = this.jobTitleLevel();
    return level !== null && level <= 3;
  }, ...ngDevMode ? [{ debugName: "isSeniorExecutive" }] : []);
  /** 是否為財務部（signal），以部門代碼 'FIN' 判斷 */
  isFinanceDept = computed(() => this.departmentCode() === "FIN", ...ngDevMode ? [{ debugName: "isFinanceDept" }] : []);
  get token() {
    return this._token();
  }
  get refreshTokenValue() {
    return localStorage.getItem(REFRESH_KEY);
  }
  isLoggedIn() {
    const payload = this._decode(this._token());
    return !!payload && payload.exp * 1e3 > Date.now();
  }
  hasPermission(permissionCode) {
    const payload = this._decode(this._token());
    if (!payload || payload.exp * 1e3 <= Date.now())
      return false;
    if (payload.is_superadmin === true || payload.is_superadmin === "true")
      return true;
    if (permissionCode === "superadmin")
      return false;
    const perms = Array.isArray(payload.permissions) ? payload.permissions : [payload.permissions];
    return perms.includes(permissionCode);
  }
  /** 登入：向後端取得 JWT */
  login(email, password) {
    if (!email || !password) {
      return throwError(() => new Error("Invalid credentials"));
    }
    return this.http.post(`${environment.apiUrl}/auth/login`, { email, password }).pipe(tap((res) => {
      if (!res?.access_token) {
        console.error("[AuthService] No access_token in response!", res);
      }
      this._storeTokens(res.access_token, res.refresh_token);
    }));
  }
  /** 使用 refresh token 取得新的 access token */
  refreshAccessToken() {
    const rt = this.refreshTokenValue;
    if (!rt)
      return throwError(() => new Error("No refresh token"));
    return this.http.post(`${environment.apiUrl}/auth/refresh`, { refreshToken: rt }).pipe(tap((res) => {
      this._storeTokens(res.access_token, res.refresh_token);
    }));
  }
  /** 修改密碼 */
  changePassword(currentPassword, newPassword) {
    return this.http.post(`${environment.apiUrl}/auth/change-password`, { currentPassword, newPassword });
  }
  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    this._token.set(null);
  }
  // ─── Private helpers ────────────────────────────────────
  _storeTokens(accessToken, refreshToken) {
    localStorage.setItem(TOKEN_KEY, accessToken);
    this._token.set(accessToken);
    if (refreshToken) {
      localStorage.setItem(REFRESH_KEY, refreshToken);
    }
  }
  _decode(token) {
    if (!token)
      return null;
    try {
      const parts = token.split(".");
      if (parts.length !== 3)
        return null;
      const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
      const binary = atob(base64);
      const bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
      const json = new TextDecoder().decode(bytes);
      return JSON.parse(json);
    } catch {
      return null;
    }
  }
  static \u0275fac = function AuthService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _AuthService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _AuthService, factory: _AuthService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AuthService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  AuthService
};
//# sourceMappingURL=chunk-QLN4CKKJ.js.map
