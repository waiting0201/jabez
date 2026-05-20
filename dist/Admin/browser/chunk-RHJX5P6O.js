import {
  toObservable,
  toSignal
} from "./chunk-Y7L4DWFX.js";
import {
  Component,
  DatePipe,
  HttpClient,
  HttpParams,
  Injectable,
  computed,
  environment,
  inject,
  setClassMetadata,
  signal,
  switchMap,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomListener,
  ɵɵdomProperty,
  ɵɵgetCurrentView,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate3
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/payment-reminder-logs/services/payment-reminder-log.service.ts
var PaymentReminderLogService = class _PaymentReminderLogService {
  http = inject(HttpClient);
  base = `${environment.apiUrl}/admin`;
  getPaged(query) {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== null && v !== void 0 && v !== "")
        params = params.set(k, String(v));
    }
    return this.http.get(`${this.base}/payment-reminder-logs`, { params });
  }
  /** Superadmin 手動觸發撥款提醒（除錯用）*/
  manualRun() {
    return this.http.post(`${this.base}/payment-reminder/run`, {});
  }
  static \u0275fac = function PaymentReminderLogService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentReminderLogService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PaymentReminderLogService, factory: _PaymentReminderLogService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentReminderLogService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/payment-reminder-logs/models/payment-reminder-log.model.ts
var STATUS_LABELS = {
  success: "\u5DF2\u9001\u9054",
  failure: "\u5931\u6557",
  batchStart: "\u6279\u6B21\u958B\u59CB",
  skipped_already_sent: "\u540C\u65E5\u5DF2\u63A8\u3001\u8DF3\u904E"
};
var STATUS_CLASSES = {
  success: "bg-success-subtle text-success",
  failure: "bg-danger-subtle text-danger",
  batchStart: "bg-secondary-subtle text-secondary",
  skipped_already_sent: "bg-warning-subtle text-warning-emphasis"
};

// src/app/features/admin/payment-reminder-logs/pages/payment-reminder-log-list/payment-reminder-log-list.ts
var _forTrack0 = ($index, $item) => $item.id;
function PaymentReminderLogList_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElement(0, "span", 18);
    \u0275\u0275text(1, " \u57F7\u884C\u4E2D\u2026 ");
  }
}
function PaymentReminderLogList_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u624B\u52D5\u89F8\u767C\u64A5\u6B3E\u63D0\u9192 ");
  }
}
function PaymentReminderLogList_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 8);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.runResult());
  }
}
function PaymentReminderLogList_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 9);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.runError());
  }
}
function PaymentReminderLogList_For_39_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "span", 21);
    \u0275\u0275text(1);
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const log_r2 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(log_r2.triggeredByUserName);
  }
}
function PaymentReminderLogList_For_39_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "tr")(1, "td", 7);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "date");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(4, "td", 19);
    \u0275\u0275text(5);
    \u0275\u0275pipe(6, "date");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(7, "td")(8, "span", 20);
    \u0275\u0275text(9);
    \u0275\u0275domElementEnd();
    \u0275\u0275conditionalCreate(10, PaymentReminderLogList_For_39_Conditional_10_Template, 2, 1, "span", 21);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(11, "td");
    \u0275\u0275text(12);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(13, "td", 15);
    \u0275\u0275text(14);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(15, "td")(16, "span");
    \u0275\u0275text(17);
    \u0275\u0275domElementEnd()();
    \u0275\u0275domElementStart(18, "td", 22);
    \u0275\u0275text(19);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(20, "td", 23);
    \u0275\u0275text(21);
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    const log_r2 = ctx.$implicit;
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(3, 11, log_r2.tickedAtTaipei, "yyyy-MM-dd HH:mm:ss"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(6, 14, log_r2.reminderDateTaipei, "yyyy-MM-dd"));
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(log_r2.triggerSource === "auto" ? "\u81EA\u52D5" : "\u624B\u52D5");
    \u0275\u0275advance();
    \u0275\u0275conditional(log_r2.triggeredByUserName ? 10 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(log_r2.financeUserName || log_r2.userNameSnapshot || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(log_r2.itemCount);
    \u0275\u0275advance(2);
    \u0275\u0275classMap("badge " + ctx_r0.statusClass[log_r2.status]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r0.statusLabel[log_r2.status] || log_r2.status);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(log_r2.errorMessage || "");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(log_r2.durationMs ?? "\u2014");
  }
}
function PaymentReminderLogList_Conditional_40_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "tr")(1, "td", 24);
    \u0275\u0275text(2, "\u5C1A\u7121\u7D00\u9304");
    \u0275\u0275domElementEnd()();
  }
}
function PaymentReminderLogList_Conditional_41_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "div", 17)(1, "div", 25);
    \u0275\u0275text(2);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(3, "div", 26)(4, "button", 27);
    \u0275\u0275domListener("click", function PaymentReminderLogList_Conditional_41_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r3);
      const ctx_r0 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r0.prev());
    });
    \u0275\u0275text(5, "\u4E0A\u4E00\u9801");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(6, "button", 27);
    \u0275\u0275domListener("click", function PaymentReminderLogList_Conditional_41_Template_button_click_6_listener() {
      \u0275\u0275restoreView(_r3);
      const ctx_r0 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r0.next());
    });
    \u0275\u0275text(7, "\u4E0B\u4E00\u9801");
    \u0275\u0275domElementEnd()()();
  }
  if (rf & 2) {
    const ctx_r0 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate3("\u5171 ", ctx_r0.totalCount(), " \u7B46\uFF0C\u7B2C ", ctx_r0.page(), " / ", ctx_r0.totalPages(), " \u9801");
    \u0275\u0275advance(2);
    \u0275\u0275domProperty("disabled", ctx_r0.page() <= 1);
    \u0275\u0275advance(2);
    \u0275\u0275domProperty("disabled", ctx_r0.page() >= ctx_r0.totalPages());
  }
}
var PaymentReminderLogList = class _PaymentReminderLogList {
  service = inject(PaymentReminderLogService);
  PAGE_SIZE = 30;
  page = signal(1, ...ngDevMode ? [{ debugName: "page" }] : []);
  refresh = signal(0, ...ngDevMode ? [{ debugName: "refresh" }] : []);
  statusLabel = STATUS_LABELS;
  statusClass = STATUS_CLASSES;
  result = toSignal(toObservable(computed(() => ({ page: this.page(), refresh: this.refresh() }))).pipe(switchMap(({ page }) => this.service.getPaged({ page, pageSize: this.PAGE_SIZE }))), { initialValue: { items: [], totalCount: 0, page: 1, pageSize: 30, totalPages: 1 } });
  logs = computed(() => this.result().items, ...ngDevMode ? [{ debugName: "logs" }] : []);
  totalCount = computed(() => this.result().totalCount, ...ngDevMode ? [{ debugName: "totalCount" }] : []);
  totalPages = computed(() => this.result().totalPages, ...ngDevMode ? [{ debugName: "totalPages" }] : []);
  goTo(p) {
    this.page.set(p);
  }
  prev() {
    if (this.page() > 1)
      this.page.update((p) => p - 1);
  }
  next() {
    if (this.page() < this.totalPages())
      this.page.update((p) => p + 1);
  }
  running = signal(false, ...ngDevMode ? [{ debugName: "running" }] : []);
  runResult = signal("", ...ngDevMode ? [{ debugName: "runResult" }] : []);
  runError = signal("", ...ngDevMode ? [{ debugName: "runError" }] : []);
  manualRun() {
    this.running.set(true);
    this.runResult.set("");
    this.runError.set("");
    this.service.manualRun().subscribe({
      next: (r) => {
        this.running.set(false);
        this.runResult.set(`\u5DF2\u57F7\u884C\uFF1A\u6488\u5230 ${r.upcomingItemCount} \u7B46\u5F85\u63D0\u9192\u3001\u63A8\u7D66 ${r.financeUserCount} \u4F4D\u8CA1\u52D9\u4EBA\u54E1\uFF08\u6210\u529F ${r.successCount}, \u8DF3\u904E ${r.skippedAlreadySent}, \u5931\u6557 ${r.failureCount}\uFF09`);
        this.refresh.update((v) => v + 1);
      },
      error: (err) => {
        this.running.set(false);
        this.runError.set(err.error?.message || "\u624B\u52D5\u89F8\u767C\u5931\u6557\u3002");
      }
    });
  }
  static \u0275fac = function PaymentReminderLogList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentReminderLogList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PaymentReminderLogList, selectors: [["app-payment-reminder-log-list"]], decls: 42, vars: 6, consts: [[1, "page-content"], [1, "page-header", "mb-6"], [1, "fw-600", "mb-1"], [1, "text-muted", "small", "mb-0"], [1, "card", "border-0", "shadow-sm", "mb-4"], [1, "card-body", "flex", "items-center", "gap-3", "flex-wrap"], ["type", "button", 1, "btn", "btn-primary", "inline-flex", "items-center", "gap-2", 3, "click", "disabled"], [1, "small", "text-muted"], [1, "card-footer", "bg-success-subtle", "text-success", "small"], [1, "card-footer", "bg-danger-subtle", "text-danger", "small"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "bg-[--bg-base]"], [1, "text-center"], [1, "text-right"], [1, "card-footer", "flex", "items-center", "justify-between", "flex-wrap", "gap-2"], [1, "inline-block", "w-4", "h-4", "border-2", "border-white/30", "border-t-white", "rounded-full", "animate-spin"], [1, "small"], [1, "badge", "bg-secondary-subtle", "text-secondary"], [1, "ms-1", "small", "text-muted"], [1, "small", "text-danger"], [1, "text-right", "small", "text-muted"], ["colspan", "8", 1, "text-center", "text-muted", "py-6"], [1, "text-muted", "small"], [1, "flex", "gap-1"], [1, "btn", "btn-sm", "btn-outline-secondary", 3, "click", "disabled"]], template: function PaymentReminderLogList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275domElementStart(0, "div", 0)(1, "div", 1)(2, "h1", 2);
      \u0275\u0275text(3, "\u64A5\u6B3E\u63D0\u9192\u7D00\u9304");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(4, "p", 3);
      \u0275\u0275text(5, "\u7CFB\u7D71\u6BCF\u65E5 09:00 (Taipei) \u81EA\u52D5\u57F7\u884C / \u63D0\u524D N \u5929\u63D0\u9192\uFF0C\u50C5 Superadmin \u53EF\u67E5\u770B\u3002");
      \u0275\u0275domElementEnd()();
      \u0275\u0275domElementStart(6, "div", 4)(7, "div", 5)(8, "button", 6);
      \u0275\u0275domListener("click", function PaymentReminderLogList_Template_button_click_8_listener() {
        return ctx.manualRun();
      });
      \u0275\u0275conditionalCreate(9, PaymentReminderLogList_Conditional_9_Template, 2, 0)(10, PaymentReminderLogList_Conditional_10_Template, 1, 0);
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(11, "div", 7);
      \u0275\u0275text(12, "\u7ACB\u5373\u4F9D\u73FE\u6709\u8A2D\u5B9A\u6488\u51FA\u5F85\u64A5 installments \u4E26\u63A8\u7D66\u8CA1\u52D9\u90E8\u5168\u54E1\uFF08\u540C\u65E5\u5DF2\u63A8\u6703\u81EA\u52D5\u8DF3\u904E\uFF09\u3002");
      \u0275\u0275domElementEnd()();
      \u0275\u0275conditionalCreate(13, PaymentReminderLogList_Conditional_13_Template, 2, 1, "div", 8);
      \u0275\u0275conditionalCreate(14, PaymentReminderLogList_Conditional_14_Template, 2, 1, "div", 9);
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(15, "div", 10)(16, "div", 11)(17, "div", 12)(18, "table", 13)(19, "thead", 14)(20, "tr")(21, "th");
      \u0275\u0275text(22, "\u89F8\u767C\u6642\u9593");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(23, "th");
      \u0275\u0275text(24, "\u63D0\u9192\u65E5\u671F");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(25, "th");
      \u0275\u0275text(26, "\u89F8\u767C\u65B9\u5F0F");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(27, "th");
      \u0275\u0275text(28, "\u8CA1\u52D9\u4EBA\u54E1");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(29, "th", 15);
      \u0275\u0275text(30, "\u7B46\u6578");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(31, "th");
      \u0275\u0275text(32, "\u72C0\u614B");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(33, "th");
      \u0275\u0275text(34, "\u932F\u8AA4\u8A0A\u606F");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(35, "th", 16);
      \u0275\u0275text(36, "\u8017\u6642(ms)");
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(37, "tbody");
      \u0275\u0275repeaterCreate(38, PaymentReminderLogList_For_39_Template, 22, 17, "tr", null, _forTrack0);
      \u0275\u0275conditionalCreate(40, PaymentReminderLogList_Conditional_40_Template, 3, 0, "tr");
      \u0275\u0275domElementEnd()()()();
      \u0275\u0275conditionalCreate(41, PaymentReminderLogList_Conditional_41_Template, 8, 5, "div", 17);
      \u0275\u0275domElementEnd()();
    }
    if (rf & 2) {
      \u0275\u0275advance(8);
      \u0275\u0275domProperty("disabled", ctx.running());
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.running() ? 9 : 10);
      \u0275\u0275advance(4);
      \u0275\u0275conditional(ctx.runResult() ? 13 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.runError() ? 14 : -1);
      \u0275\u0275advance(24);
      \u0275\u0275repeater(ctx.logs());
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.logs().length === 0 ? 40 : -1);
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.totalPages() > 1 ? 41 : -1);
    }
  }, dependencies: [DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentReminderLogList, [{
    type: Component,
    args: [{ selector: "app-payment-reminder-log-list", imports: [DatePipe], template: `<div class="page-content">
  <div class="page-header mb-6">
    <h1 class="fw-600 mb-1">\u64A5\u6B3E\u63D0\u9192\u7D00\u9304</h1>
    <p class="text-muted small mb-0">\u7CFB\u7D71\u6BCF\u65E5 09:00 (Taipei) \u81EA\u52D5\u57F7\u884C / \u63D0\u524D N \u5929\u63D0\u9192\uFF0C\u50C5 Superadmin \u53EF\u67E5\u770B\u3002</p>
  </div>

  <div class="card border-0 shadow-sm mb-4">
    <div class="card-body flex items-center gap-3 flex-wrap">
      <button type="button" class="btn btn-primary inline-flex items-center gap-2"
              [disabled]="running()"
              (click)="manualRun()">
        @if (running()) {
          <span class="inline-block w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
          \u57F7\u884C\u4E2D\u2026
        } @else {
          \u624B\u52D5\u89F8\u767C\u64A5\u6B3E\u63D0\u9192
        }
      </button>
      <div class="small text-muted">\u7ACB\u5373\u4F9D\u73FE\u6709\u8A2D\u5B9A\u6488\u51FA\u5F85\u64A5 installments \u4E26\u63A8\u7D66\u8CA1\u52D9\u90E8\u5168\u54E1\uFF08\u540C\u65E5\u5DF2\u63A8\u6703\u81EA\u52D5\u8DF3\u904E\uFF09\u3002</div>
    </div>
    @if (runResult()) {
      <div class="card-footer bg-success-subtle text-success small">{{ runResult() }}</div>
    }
    @if (runError()) {
      <div class="card-footer bg-danger-subtle text-danger small">{{ runError() }}</div>
    }
  </div>

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-sm mb-0">
          <thead class="bg-[--bg-base]">
            <tr>
              <th>\u89F8\u767C\u6642\u9593</th>
              <th>\u63D0\u9192\u65E5\u671F</th>
              <th>\u89F8\u767C\u65B9\u5F0F</th>
              <th>\u8CA1\u52D9\u4EBA\u54E1</th>
              <th class="text-center">\u7B46\u6578</th>
              <th>\u72C0\u614B</th>
              <th>\u932F\u8AA4\u8A0A\u606F</th>
              <th class="text-right">\u8017\u6642(ms)</th>
            </tr>
          </thead>
          <tbody>
            @for (log of logs(); track log.id) {
              <tr>
                <td class="small text-muted">{{ log.tickedAtTaipei | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td class="small">{{ log.reminderDateTaipei | date:'yyyy-MM-dd' }}</td>
                <td>
                  <span class="badge bg-secondary-subtle text-secondary">{{ log.triggerSource === 'auto' ? '\u81EA\u52D5' : '\u624B\u52D5' }}</span>
                  @if (log.triggeredByUserName) {
                    <span class="ms-1 small text-muted">{{ log.triggeredByUserName }}</span>
                  }
                </td>
                <td>{{ log.financeUserName || log.userNameSnapshot || '\u2014' }}</td>
                <td class="text-center">{{ log.itemCount }}</td>
                <td><span [class]="'badge ' + statusClass[log.status]">{{ statusLabel[log.status] || log.status }}</span></td>
                <td class="small text-danger">{{ log.errorMessage || '' }}</td>
                <td class="text-right small text-muted">{{ log.durationMs ?? '\u2014' }}</td>
              </tr>
            }
            @if (logs().length === 0) {
              <tr><td colspan="8" class="text-center text-muted py-6">\u5C1A\u7121\u7D00\u9304</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
    @if (totalPages() > 1) {
      <div class="card-footer flex items-center justify-between flex-wrap gap-2">
        <div class="text-muted small">\u5171 {{ totalCount() }} \u7B46\uFF0C\u7B2C {{ page() }} / {{ totalPages() }} \u9801</div>
        <div class="flex gap-1">
          <button class="btn btn-sm btn-outline-secondary" [disabled]="page() <= 1" (click)="prev()">\u4E0A\u4E00\u9801</button>
          <button class="btn btn-sm btn-outline-secondary" [disabled]="page() >= totalPages()" (click)="next()">\u4E0B\u4E00\u9801</button>
        </div>
      </div>
    }
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PaymentReminderLogList, { className: "PaymentReminderLogList", filePath: "src/app/features/admin/payment-reminder-logs/pages/payment-reminder-log-list/payment-reminder-log-list.ts", lineNumber: 18 });
})();
export {
  PaymentReminderLogList
};
//# sourceMappingURL=chunk-RHJX5P6O.js.map
