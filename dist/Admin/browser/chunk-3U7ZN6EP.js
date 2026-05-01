import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  signal,
  tap,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/core/auth/services/line.service.ts
var LineService = class _LineService {
  http = inject(HttpClient);
  apiUrl = environment.apiUrl;
  /** 共享綁定狀態 signal — ProfileDropdown 與 LineBindCallback 共用 */
  isBound = signal(false, ...ngDevMode ? [{ debugName: "isBound" }] : []);
  /** OA 好友狀態（綁定完成但未加 OA 為好友時推播會失敗） */
  isBotFriend = signal(false, ...ngDevMode ? [{ debugName: "isBotFriend" }] : []);
  /** 取得 LINE OAuth 綁定 URL */
  getBindUrl() {
    return this.http.get(`${this.apiUrl}/line/bind-url`);
  }
  /** 用 OAuth code 完成綁定 */
  bind(code, redirectUri) {
    return this.http.post(`${this.apiUrl}/line/bind`, { code, redirectUri }).pipe(tap((status) => this.applyStatus(status)));
  }
  /** 解除 LINE 綁定 */
  unbind() {
    return this.http.post(`${this.apiUrl}/line/unbind`, {}).pipe(tap((status) => this.applyStatus(status)));
  }
  /** 查詢 LINE 綁定狀態（同步更新 signal） */
  refreshStatus() {
    this.http.get(`${this.apiUrl}/line/binding-status`).subscribe({
      next: (status) => this.applyStatus(status),
      error: () => {
      }
    });
  }
  applyStatus(status) {
    this.isBound.set(status.isBound);
    this.isBotFriend.set(status.isBotFriend);
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
//# sourceMappingURL=chunk-3U7ZN6EP.js.map
