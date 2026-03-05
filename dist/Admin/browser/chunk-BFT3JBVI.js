import {
  BehaviorSubject,
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  tap,
  ɵɵdefineInjectable
} from "./chunk-7JUZFUWV.js";

// src/app/features/dashboard/services/attendance.service.ts
var AttendanceService = class _AttendanceService {
  http = inject(HttpClient);
  today$ = new BehaviorSubject(null);
  getToday() {
    return this.http.get(`${environment.apiUrl}/attendances/today`).pipe(tap((record) => this.today$.next(record)));
  }
  clockIn(body) {
    return this.http.post(`${environment.apiUrl}/attendances/clock-in`, body).pipe(tap((record) => this.today$.next(record)));
  }
  clockOut(body) {
    return this.http.post(`${environment.apiUrl}/attendances/clock-out`, body).pipe(tap((record) => this.today$.next(record)));
  }
  overtimeStart(body) {
    return this.http.post(`${environment.apiUrl}/attendances/overtime-start`, body).pipe(tap((record) => this.today$.next(record)));
  }
  overtimeEnd(body) {
    return this.http.post(`${environment.apiUrl}/attendances/overtime-end`, body).pipe(tap((record) => this.today$.next(record)));
  }
  update(id, body) {
    return this.http.patch(`${environment.apiUrl}/attendances/${id}`, body);
  }
  static \u0275fac = function AttendanceService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _AttendanceService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _AttendanceService, factory: _AttendanceService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AttendanceService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/overtime-requests/services/overtime-request.service.ts
var OvertimeRequestService = class _OvertimeRequestService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/overtime-requests`);
  }
  getPaged(page, pageSize) {
    return this.http.get(`${environment.apiUrl}/overtime-requests`, { params: { page, pageSize } });
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/overtime-requests/${id}`);
  }
  create(data) {
    return this.http.post(`${environment.apiUrl}/overtime-requests`, data);
  }
  update(id, changes) {
    return this.http.patch(`${environment.apiUrl}/overtime-requests/${id}`, changes);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/overtime-requests/${id}`);
  }
  /** 送出申請（draft → pending） */
  submit(id) {
    return this.http.patch(`${environment.apiUrl}/overtime-requests/${id}/submit`, {});
  }
  /** 取得今日已核准的加班申請（打卡頁用） */
  getApprovedForToday() {
    const today = (/* @__PURE__ */ new Date()).toISOString().split("T")[0];
    return this.http.get(`${environment.apiUrl}/overtime-requests`, {
      params: { status: "approved", date: today }
    });
  }
  static \u0275fac = function OvertimeRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _OvertimeRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _OvertimeRequestService, factory: _OvertimeRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(OvertimeRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  AttendanceService,
  OvertimeRequestService
};
//# sourceMappingURL=chunk-BFT3JBVI.js.map
