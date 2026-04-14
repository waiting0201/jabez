import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  signal,
  tap,
  ɵɵdefineInjectable
} from "./chunk-7FYQHGNM.js";

// src/app/core/auth/services/line.service.ts
var LineService = class _LineService {
  http = inject(HttpClient);
  apiUrl = environment.apiUrl;
  /** 共享綁定狀態 signal — ProfileDropdown 與 LineBindCallback 共用 */
  isBound = signal(false, ...ngDevMode ? [{ debugName: "isBound" }] : []);
  /** 取得 LINE OAuth 綁定 URL */
  getBindUrl() {
    return this.http.get(`${this.apiUrl}/line/bind-url`);
  }
  /** 用 OAuth code 完成綁定 */
  bind(code, redirectUri) {
    return this.http.post(`${this.apiUrl}/line/bind`, { code, redirectUri }).pipe(tap((status) => this.isBound.set(status.isBound)));
  }
  /** 解除 LINE 綁定 */
  unbind() {
    return this.http.post(`${this.apiUrl}/line/unbind`, {}).pipe(tap((status) => this.isBound.set(status.isBound)));
  }
  /** 查詢 LINE 綁定狀態（同步更新 signal） */
  refreshStatus() {
    this.http.get(`${this.apiUrl}/line/binding-status`).subscribe({
      next: (status) => this.isBound.set(status.isBound),
      error: () => {
      }
    });
  }
  static \u0275fac = function LineService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _LineService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _LineService, factory: _LineService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(LineService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  LineService
};
//# sourceMappingURL=chunk-EPER4WTO.js.map
