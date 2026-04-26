import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-GQPYF5UN.js";

// src/app/features/admin/travel-payment-requests/services/travel-payment-request.service.ts
var TravelPaymentRequestService = class _TravelPaymentRequestService {
  http = inject(HttpClient);
  getPaged(page, pageSize) {
    return this.http.get(`${environment.apiUrl}/travel-payment-requests`, { params: { page, pageSize } });
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/travel-payment-requests/${id}`);
  }
  create(formData) {
    return this.http.post(`${environment.apiUrl}/travel-payment-requests`, formData);
  }
  update(id, formData) {
    return this.http.patch(`${environment.apiUrl}/travel-payment-requests/${id}`, formData);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/travel-payment-requests/${id}`);
  }
  /** 送出申請（draft → pending） */
  submit(id) {
    return this.http.patch(`${environment.apiUrl}/travel-payment-requests/${id}/submit`, {});
  }
  /** 更新撥款日期（核准後財務部操作） */
  updatePaymentDate(id, req) {
    return this.http.patch(`${environment.apiUrl}/travel-payment-requests/${id}/payment-date`, {
      estimatedPaymentDate: req.estimatedPaymentDate || null,
      paidAt: req.paidAt || null
    });
  }
  static \u0275fac = function TravelPaymentRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelPaymentRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _TravelPaymentRequestService, factory: _TravelPaymentRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelPaymentRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  TravelPaymentRequestService
};
//# sourceMappingURL=chunk-3653DQEP.js.map
