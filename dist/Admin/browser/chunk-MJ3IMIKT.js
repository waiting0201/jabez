import {
  BehaviorSubject,
  HttpClient,
  Injectable,
  environment,
  inject,
  map,
  setClassMetadata,
  switchMap,
  tap,
  ɵɵdefineInjectable
} from "./chunk-7FYQHGNM.js";

// src/app/features/admin/approval-tasks/services/approval-task.service.ts
var ApprovalTaskService = class _ApprovalTaskService {
  http = inject(HttpClient);
  items$ = new BehaviorSubject([]);
  pendingCount$ = this.items$.pipe(map((tasks) => tasks.length));
  /** 拉取所有待審核任務（解包 PagedResult），更新 items$ 供 pendingCount$ 使用 */
  getAll() {
    return this.http.get(`${environment.apiUrl}/approval-tasks`, {
      params: { page: 1, pageSize: 100, status: "pending" }
    }).pipe(map((result) => result.items ?? []), tap((items) => this.items$.next(items)));
  }
  getPaged(page, pageSize, status, paymentStatus) {
    const params = { page, pageSize };
    if (status)
      params["status"] = status;
    if (paymentStatus)
      params["paymentStatus"] = paymentStatus;
    return this.http.get(`${environment.apiUrl}/approval-tasks`, { params });
  }
  getById(id, applicationType) {
    const path = applicationType ? `${environment.apiUrl}/approval-tasks/${applicationType}/${id}` : `${environment.apiUrl}/approval-tasks/${id}`;
    return this.http.get(path);
  }
  review(id, applicationType, action, reviewNote, estimatedPaymentDate, paidAt, closeAdvance) {
    return this.http.patch(`${environment.apiUrl}/approval-tasks/${applicationType}/${id}/review`, { action, reviewNote, applicationType, estimatedPaymentDate, paidAt, closeAdvance }).pipe(switchMap((updated) => this.getAll().pipe(map(() => updated))));
  }
  closeCase(id, applicationType) {
    return this.http.patch(`${environment.apiUrl}/approval-tasks/${applicationType}/${id}/close`, {});
  }
  static \u0275fac = function ApprovalTaskService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _ApprovalTaskService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _ApprovalTaskService, factory: _ApprovalTaskService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(ApprovalTaskService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  ApprovalTaskService
};
//# sourceMappingURL=chunk-MJ3IMIKT.js.map
