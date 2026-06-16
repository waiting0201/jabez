import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

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
  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id, body) {
    return this.http.patch(`${environment.apiUrl}/travel-payment-requests/${id}/installments`, body);
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
//# sourceMappingURL=chunk-MKTMSFDP.js.map
