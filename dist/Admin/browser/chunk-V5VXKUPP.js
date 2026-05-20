import {
  HttpClient,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/payment-requests/services/payment-request.service.ts
var PaymentRequestService = class _PaymentRequestService {
  http = inject(HttpClient);
  getAll() {
    return this.http.get(`${environment.apiUrl}/payment-requests`);
  }
  getPaged(page, pageSize) {
    return this.http.get(`${environment.apiUrl}/payment-requests`, { params: { page, pageSize } });
  }
  getById(id) {
    return this.http.get(`${environment.apiUrl}/payment-requests/${id}`);
  }
  createWithFiles(formData) {
    return this.http.post(`${environment.apiUrl}/payment-requests`, formData);
  }
  updateWithFiles(id, formData) {
    return this.http.patch(`${environment.apiUrl}/payment-requests/${id}`, formData);
  }
  delete(id) {
    return this.http.delete(`${environment.apiUrl}/payment-requests/${id}`);
  }
  /** 送出申請（draft → pending） */
  submit(id) {
    return this.http.patch(`${environment.apiUrl}/payment-requests/${id}/submit`, {});
  }
  /** 發票 / 交通票根 OCR 辨識（後端透過 Google Gemini API） */
  ocrInvoice(file) {
    const fd = new FormData();
    fd.append("file", file, file.name);
    return this.http.post(`${environment.apiUrl}/invoice-ocr`, fd);
  }
  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id, body) {
    return this.http.patch(`${environment.apiUrl}/payment-requests/${id}/installments`, body);
  }
  static \u0275fac = function PaymentRequestService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentRequestService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PaymentRequestService, factory: _PaymentRequestService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentRequestService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  PaymentRequestService
};
//# sourceMappingURL=chunk-V5VXKUPP.js.map
