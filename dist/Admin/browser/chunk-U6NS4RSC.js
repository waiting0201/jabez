import {
  AuthService
} from "./chunk-ZSGTQ3YJ.js";
import {
  HttpClient,
  Injectable,
  computed,
  environment,
  inject,
  of,
  setClassMetadata,
  signal,
  tap,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/notifications/services/notification.service.ts
var NotificationService = class _NotificationService {
  http = inject(HttpClient);
  auth = inject(AuthService);
  approvalCounts = signal({}, ...ngDevMode ? [{ debugName: "approvalCounts" }] : []);
  myRequestCounts = signal({}, ...ngDevMode ? [{ debugName: "myRequestCounts" }] : []);
  totalCount = computed(() => {
    const sum = (m) => Object.values(m).reduce((a, b) => a + (b ?? 0), 0);
    return sum(this.approvalCounts()) + sum(this.myRequestCounts());
  }, ...ngDevMode ? [{ debugName: "totalCount" }] : []);
  refresh() {
    if (!this.auth.currentUser())
      return of(null);
    return this.http.get(`${environment.apiUrl}/me/notification-counts`).pipe(tap((data) => {
      if (data) {
        this.approvalCounts.set(data.approvals);
        this.myRequestCounts.set(data.myRequests);
      }
    }));
  }
  static \u0275fac = function NotificationService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _NotificationService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _NotificationService, factory: _NotificationService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(NotificationService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  NotificationService
};
//# sourceMappingURL=chunk-U6NS4RSC.js.map
