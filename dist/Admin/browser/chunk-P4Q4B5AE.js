import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/holiday-travel-requests/services/holiday-travel-request.service.ts
var HolidayTravelRequestService = class _HolidayTravelRequestService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/holiday-travel-requests`);
  }
  getPaged(page, pageSize) {
    return this.http.get(`${environment.apiUrl}/holiday-travel-requests`, { params: { page, pageSize } });
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/holiday-travel-requests/${id}`);
  }
  /**
   * 新增假日執行活動申請（使用 FormData 以支援發票附件上傳）
   */
  create(data) {
    return this.http.post(`${environment.apiUrl}/holiday-travel-requests`, data);
  }
  /**
   * 更新假日執行活動申請（使用 FormData 以支援發票附件上傳）
   */
  update(id, data) {
    return this.http.patch(`${environment.apiUrl}/holiday-travel-requests/${id}`, data);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/holiday-travel-requests/${id}`);
  }
  /** 查詢日期範圍內的假日天數（依行事曆資料） */
  countHolidays(startDate, endDate) {
    return this.http.get(`${environment.apiUrl}/holiday-travel-requests/count-holidays`, { params: { startDate, endDate } });
  }
  /** 送出申請（draft → pending） */
  submit(id) {
    return this.http.patch(`${environment.apiUrl}/holiday-travel-requests/${id}/submit`, {});
  }
  /** 更新撥款日期（核准後財務部操作） */
  updatePaymentDate(id, estimatedPaymentDate, paidAt) {
    return this.http.patch(`${environment.apiUrl}/holiday-travel-requests/${id}/payment-date`, {
      estimatedPaymentDate: estimatedPaymentDate || null,
      paidAt: paidAt || null
    });
  }
  static \u0275fac = function HolidayTravelRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _HolidayTravelRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _HolidayTravelRequestService, factory: _HolidayTravelRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/holiday-travel-requests/models/holiday-travel-request.model.ts
var APPROVAL_STATUS_LABELS = {
  draft: "\u8349\u7A3F",
  pending: "\u5F85\u5BE9\u6838",
  approved: "\u5DF2\u6838\u51C6",
  rejected: "\u5DF2\u62D2\u7D55",
  returned: "\u9000\u56DE\u4FEE\u6539"
};
var APPROVAL_STATUS_CLASSES = {
  draft: "bg-blue-subtle text-blue-emphasis",
  pending: "bg-warning-subtle text-warning-emphasis",
  approved: "bg-success-subtle text-success",
  rejected: "bg-danger-subtle text-danger",
  returned: "bg-secondary-subtle text-secondary"
};

export {
  HolidayTravelRequestService,
  APPROVAL_STATUS_LABELS,
  APPROVAL_STATUS_CLASSES
};
//# sourceMappingURL=chunk-P4Q4B5AE.js.map
