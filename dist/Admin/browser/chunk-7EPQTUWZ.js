import {
  HttpClient,
  HttpParams,
  Injectable,
  environment,
  inject,
  setClassMetadata,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/attendance-reminder-logs/services/attendance-reminder-log.service.ts
var AttendanceReminderLogService = class _AttendanceReminderLogService {
  http = inject(HttpClient);
  base = `${environment.apiUrl}/admin/attendance-reminder-logs`;
  /** 列表（分頁 + 篩選） */
  getPaged(query) {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== null && v !== void 0 && v !== "") {
        params = params.set(k, String(v));
      }
    }
    return this.http.get(this.base, { params });
  }
  /** 統計卡（今日推播 / 失敗 / 批次 + 最近 7 天趨勢） */
  getStats() {
    return this.http.get(`${this.base}/stats`);
  }
  /** 同一次 tick 全部紀錄 */
  getByBatchId(batchId) {
    return this.http.get(`${this.base}/batches/${batchId}`);
  }
  /** 單筆詳情 */
  getById(id) {
    return this.http.get(`${this.base}/${id}`);
  }
  static \u0275fac = function AttendanceReminderLogService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _AttendanceReminderLogService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _AttendanceReminderLogService, factory: _AttendanceReminderLogService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AttendanceReminderLogService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/attendance-reminder-logs/models/attendance-reminder-log.model.ts
var REMINDER_TYPE_LABELS = {
  clockIn: "\u4E0A\u73ED\u63D0\u9192",
  clockOut: "\u4E0B\u73ED\u63D0\u9192",
  batchStart: "\u6279\u6B21\u555F\u52D5"
};
var STATUS_LABELS = {
  success: "\u6210\u529F",
  failure: "\u5931\u6557",
  batchStart: "\u6279\u6B21"
};
var TRIGGER_SOURCE_LABELS = {
  auto: "\u81EA\u52D5\u6392\u7A0B",
  manual: "\u624B\u52D5\u89F8\u767C"
};
var ERROR_CATEGORY_LABELS = {
  not_friend: "\u672A\u52A0\u597D\u53CB",
  token_invalid: "Token \u5931\u6548",
  rate_limited: "\u901F\u7387\u9650\u5236 (429)",
  network_error: "\u7DB2\u8DEF\u932F\u8AA4",
  unknown: "\u5176\u4ED6\u932F\u8AA4",
  system_error: "\u7CFB\u7D71\u4F8B\u5916"
};

export {
  AttendanceReminderLogService,
  REMINDER_TYPE_LABELS,
  STATUS_LABELS,
  TRIGGER_SOURCE_LABELS,
  ERROR_CATEGORY_LABELS
};
//# sourceMappingURL=chunk-7EPQTUWZ.js.map
