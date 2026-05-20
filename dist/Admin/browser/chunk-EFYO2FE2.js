import {
  AttendanceReminderLogService,
  ERROR_CATEGORY_LABELS,
  REMINDER_TYPE_LABELS,
  TRIGGER_SOURCE_LABELS
} from "./chunk-7EPQTUWZ.js";
import {
  ActivatedRoute,
  RouterLink
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  CommonModule,
  Component,
  computed,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵproperty,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/attendance-reminder-logs/pages/attendance-reminder-log-detail/attendance-reminder-log-detail.ts
var _forTrack0 = ($index, $item) => $item.id;
function AttendanceReminderLogDetail_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8);
    \u0275\u0275text(1, "\u8F09\u5165\u4E2D...");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 9);
    \u0275\u0275text(1, "\u627E\u4E0D\u5230\u6B64\u6279\u6B21\u7684\u7D00\u9304\u3002");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_0_Conditional_22_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 23);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const bs_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\uFF08", bs_r1.triggeredByName, "\uFF09");
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 10)(1, "div", 12);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 13);
    \u0275\u0275element(3, "use", 17);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u6279\u6B21\u6458\u8981 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 18)(6, "div", 19)(7, "div")(8, "div", 20);
    \u0275\u0275text(9, "\u89F8\u767C\u6642\u9593");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "div", 21);
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div")(13, "div", 20);
    \u0275\u0275text(14, "\u76EE\u6A19\u6642\u523B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "div", 21);
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(17, "div")(18, "div", 20);
    \u0275\u0275text(19, "\u89F8\u767C\u4F86\u6E90");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(20, "div", 22);
    \u0275\u0275text(21);
    \u0275\u0275conditionalCreate(22, AttendanceReminderLogDetail_Conditional_12_Conditional_0_Conditional_22_Template, 2, 1, "span", 23);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(23, "div")(24, "div", 20);
    \u0275\u0275text(25, "\u63A8\u64AD\u7D50\u679C");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(26, "div", 22)(27, "span", 24);
    \u0275\u0275text(28);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(29, "span", 25);
    \u0275\u0275text(30, "/");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(31, "span", 26);
    \u0275\u0275text(32);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(33, "span", 25);
    \u0275\u0275text(34, "/");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(35, "span");
    \u0275\u0275text(36);
    \u0275\u0275elementEnd()()()()()();
  }
  if (rf & 2) {
    const bs_r1 = ctx;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(11);
    \u0275\u0275textInterpolate(ctx_r1.formatTaipei(bs_r1.tickedAtTaipei));
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(bs_r1.targetTimeTaipei);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate1(" ", ctx_r1.triggerSourceLabels[bs_r1.triggerSource], " ");
    \u0275\u0275advance();
    \u0275\u0275conditional(bs_r1.triggeredByName ? 22 : -1);
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate1("\u6210\u529F ", ctx_r1.pushedCount());
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1("\u5931\u6557 ", ctx_r1.failedCount());
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1("\u5171 ", ctx_r1.pushes().length, " \u4EBA");
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 8);
    \u0275\u0275text(1, "\u6B64\u6279\u6B21\u7121\u63A8\u64AD\u5C0D\u8C61\uFF08recipientCount = 0\uFF09\u3002");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 33);
    \u0275\u0275text(1, "\u6210\u529F");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 34);
    \u0275\u0275text(1, "\u5931\u6557");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_9_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 40);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const row_r3 = \u0275\u0275nextContext(2).$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(row_r3.errorMessage);
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 39);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(2, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_9_Conditional_2_Template, 2, 1, "div", 40);
  }
  if (rf & 2) {
    const row_r3 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(3);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.errorCategoryLabels[row_r3.errorCategory]);
    \u0275\u0275advance();
    \u0275\u0275conditional(row_r3.errorMessage ? 2 : -1);
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 36);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 22);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 32);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 30);
    \u0275\u0275conditionalCreate(6, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_6_Template, 2, 0, "span", 33)(7, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_7_Template, 2, 0, "span", 34);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td", 35);
    \u0275\u0275conditionalCreate(9, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_9_Template, 3, 2)(10, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Conditional_10_Template, 2, 0, "span", 36);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td", 37);
    \u0275\u0275text(12);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 38);
    \u0275\u0275text(14);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const row_r3 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(row_r3.userName ?? row_r3.userNameSnapshot ?? "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", row_r3.lineUserIdSnapshot ? row_r3.lineUserIdSnapshot.substring(0, 12) + "\u2026" : "\u2014", " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(row_r3.status === "success" ? 6 : 7);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(row_r3.errorCategory ? 9 : 10);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(row_r3.httpStatusCode ?? "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", row_r3.durationMs !== null && row_r3.durationMs !== void 0 ? row_r3.durationMs + "ms" : "\u2014", " ");
  }
}
function AttendanceReminderLogDetail_Conditional_12_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 16)(1, "table", 27)(2, "thead", 28)(3, "tr")(4, "th");
    \u0275\u0275text(5, "\u5C0D\u8C61");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "th", 29);
    \u0275\u0275text(7, "LINE userId");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "th", 30);
    \u0275\u0275text(9, "\u7D50\u679C");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "th");
    \u0275\u0275text(11, "\u5931\u6557\u539F\u56E0");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "th", 30);
    \u0275\u0275text(13, "HTTP");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "th", 31);
    \u0275\u0275text(15, "\u8017\u6642");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(16, "tbody");
    \u0275\u0275repeaterCreate(17, AttendanceReminderLogDetail_Conditional_12_Conditional_8_For_18_Template, 15, 6, "tr", null, _forTrack0);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(17);
    \u0275\u0275repeater(ctx_r1.pushes());
  }
}
function AttendanceReminderLogDetail_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, AttendanceReminderLogDetail_Conditional_12_Conditional_0_Template, 37, 7, "div", 10);
    \u0275\u0275elementStart(1, "div", 11)(2, "div", 12);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 13);
    \u0275\u0275element(4, "use", 14);
    \u0275\u0275elementEnd();
    \u0275\u0275text(5);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(6, "div", 15);
    \u0275\u0275conditionalCreate(7, AttendanceReminderLogDetail_Conditional_12_Conditional_7_Template, 2, 0, "div", 8)(8, AttendanceReminderLogDetail_Conditional_12_Conditional_8_Template, 19, 0, "div", 16);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_1_0;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275conditional((tmp_1_0 = ctx_r1.batchStart()) ? 0 : -1, tmp_1_0);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate1(" \u63A8\u64AD\u5C0D\u8C61\u660E\u7D30\uFF08", ctx_r1.pushes().length, " \u4EBA\uFF09 ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.pushes().length === 0 ? 7 : 8);
  }
}
var AttendanceReminderLogDetail = class _AttendanceReminderLogDetail {
  route = inject(ActivatedRoute);
  service = inject(AttendanceReminderLogService);
  rows = signal([], ...ngDevMode ? [{ debugName: "rows" }] : []);
  loading = signal(false, ...ngDevMode ? [{ debugName: "loading" }] : []);
  batchId = signal("", ...ngDevMode ? [{ debugName: "batchId" }] : []);
  /** 批次啟動紀錄（第一筆，可能不存在） */
  batchStart = computed(() => this.rows().find((r) => r.reminderType === "batchStart"), ...ngDevMode ? [{ debugName: "batchStart" }] : []);
  /** 推播紀錄（排除 batchStart） */
  pushes = computed(() => this.rows().filter((r) => r.reminderType !== "batchStart"), ...ngDevMode ? [{ debugName: "pushes" }] : []);
  /** 統計 */
  pushedCount = computed(() => this.pushes().filter((r) => r.status === "success").length, ...ngDevMode ? [{ debugName: "pushedCount" }] : []);
  failedCount = computed(() => this.pushes().filter((r) => r.status === "failure").length, ...ngDevMode ? [{ debugName: "failedCount" }] : []);
  reminderTypeLabels = REMINDER_TYPE_LABELS;
  errorCategoryLabels = ERROR_CATEGORY_LABELS;
  triggerSourceLabels = TRIGGER_SOURCE_LABELS;
  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("batchId") ?? "";
    this.batchId.set(id);
    if (!id)
      return;
    this.loading.set(true);
    this.service.getByBatchId(id).subscribe({
      next: (rows) => {
        this.rows.set(rows ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
      }
    });
  }
  formatTaipei(iso) {
    if (!iso)
      return "\u2014";
    const d = new Date(iso);
    return `${d.getFullYear()}/${String(d.getMonth() + 1).padStart(2, "0")}/${String(d.getDate()).padStart(2, "0")} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}:${String(d.getSeconds()).padStart(2, "0")}`;
  }
  static \u0275fac = function AttendanceReminderLogDetail_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _AttendanceReminderLogDetail)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _AttendanceReminderLogDetail, selectors: [["app-attendance-reminder-log-detail"]], decls: 13, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2", "flex-wrap"], ["routerLink", "/admin/attendance-reminder-logs", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "badge", "bg-secondary-subtle", "text-secondary", "font-monospace", "small", 3, "title"], [1, "text-center", "text-muted", "py-4"], [1, "alert", "alert-warning"], [1, "card", "border-0", "shadow-sm", "mb-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#users"], [1, "card-body", "p-0"], [1, "table-responsive"], ["href", "/assets/icons/sprite.svg#info"], [1, "card-body"], [1, "grid", "grid-cols-1", "sm:grid-cols-2", "lg:grid-cols-4", "gap-4"], [1, "text-muted", "small", "mb-1"], [1, "fw-500", "font-monospace"], [1, "fw-500"], [1, "text-muted", "small"], [1, "text-success"], [1, "text-muted", "mx-1"], [1, "text-danger"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "font-monospace"], [1, "text-center"], [1, "text-right"], [1, "font-monospace", "small", "text-muted"], [1, "badge", "bg-success-subtle", "text-success"], [1, "badge", "bg-danger-subtle", "text-danger"], [1, "small"], [1, "text-muted"], [1, "text-center", "text-muted", "small"], [1, "text-right", "text-muted", "small"], [1, "text-danger", "fw-500"], [1, "text-muted", 2, "word-break", "break-all"]], template: function AttendanceReminderLogDetail_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2)(3, "a", 3);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(4, "svg", 4);
      \u0275\u0275element(5, "use", 5);
      \u0275\u0275elementEnd()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(6, "h4", 6);
      \u0275\u0275text(7, "\u6253\u5361\u63D0\u9192\u6279\u6B21\u8A73\u60C5");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(8, "span", 7);
      \u0275\u0275text(9);
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(10, AttendanceReminderLogDetail_Conditional_10_Template, 2, 0, "div", 8)(11, AttendanceReminderLogDetail_Conditional_11_Template, 2, 0, "div", 9)(12, AttendanceReminderLogDetail_Conditional_12_Template, 9, 3);
      \u0275\u0275elementEnd();
    }
    if (rf & 2) {
      \u0275\u0275advance(8);
      \u0275\u0275property("title", ctx.batchId());
      \u0275\u0275advance();
      \u0275\u0275textInterpolate1(" ", ctx.batchId().substring(0, 8), " ");
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.loading() ? 10 : ctx.rows().length === 0 ? 11 : 12);
    }
  }, dependencies: [CommonModule, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AttendanceReminderLogDetail, [{
    type: Component,
    args: [{ selector: "app-attendance-reminder-log-detail", imports: [CommonModule, RouterLink], template: `<div class="container-fluid py-3">

  <!-- Header -->
  <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
    <div class="flex items-center gap-2 flex-wrap">
      <a routerLink="/admin/attendance-reminder-logs" class="btn btn-sm btn-outline-secondary">
        <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
      </a>
      <h4 class="mb-0">\u6253\u5361\u63D0\u9192\u6279\u6B21\u8A73\u60C5</h4>
      <span class="badge bg-secondary-subtle text-secondary font-monospace small" [title]="batchId()">
        {{ batchId().substring(0, 8) }}
      </span>
    </div>
  </div>

  @if (loading()) {
    <div class="text-center text-muted py-4">\u8F09\u5165\u4E2D...</div>
  } @else if (rows().length === 0) {
    <div class="alert alert-warning">\u627E\u4E0D\u5230\u6B64\u6279\u6B21\u7684\u7D00\u9304\u3002</div>
  } @else {

    <!-- \u6279\u6B21\u6458\u8981 -->
    @if (batchStart(); as bs) {
      <div class="card border-0 shadow-sm mb-4">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#info"></use></svg>
          \u6279\u6B21\u6458\u8981
        </div>
        <div class="card-body">
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div>
              <div class="text-muted small mb-1">\u89F8\u767C\u6642\u9593</div>
              <div class="fw-500 font-monospace">{{ formatTaipei(bs.tickedAtTaipei) }}</div>
            </div>
            <div>
              <div class="text-muted small mb-1">\u76EE\u6A19\u6642\u523B</div>
              <div class="fw-500 font-monospace">{{ bs.targetTimeTaipei }}</div>
            </div>
            <div>
              <div class="text-muted small mb-1">\u89F8\u767C\u4F86\u6E90</div>
              <div class="fw-500">
                {{ triggerSourceLabels[bs.triggerSource] }}
                @if (bs.triggeredByName) {
                  <span class="text-muted small">\uFF08{{ bs.triggeredByName }}\uFF09</span>
                }
              </div>
            </div>
            <div>
              <div class="text-muted small mb-1">\u63A8\u64AD\u7D50\u679C</div>
              <div class="fw-500">
                <span class="text-success">\u6210\u529F {{ pushedCount() }}</span>
                <span class="text-muted mx-1">/</span>
                <span class="text-danger">\u5931\u6557 {{ failedCount() }}</span>
                <span class="text-muted mx-1">/</span>
                <span>\u5171 {{ pushes().length }} \u4EBA</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- \u63A8\u64AD\u660E\u7D30 -->
    <div class="card border-0 shadow-sm">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#users"></use></svg>
        \u63A8\u64AD\u5C0D\u8C61\u660E\u7D30\uFF08{{ pushes().length }} \u4EBA\uFF09
      </div>
      <div class="card-body p-0">
        @if (pushes().length === 0) {
          <div class="text-center text-muted py-4">\u6B64\u6279\u6B21\u7121\u63A8\u64AD\u5C0D\u8C61\uFF08recipientCount = 0\uFF09\u3002</div>
        } @else {
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead class="table-light">
                <tr>
                  <th>\u5C0D\u8C61</th>
                  <th class="font-monospace">LINE userId</th>
                  <th class="text-center">\u7D50\u679C</th>
                  <th>\u5931\u6557\u539F\u56E0</th>
                  <th class="text-center">HTTP</th>
                  <th class="text-right">\u8017\u6642</th>
                </tr>
              </thead>
              <tbody>
                @for (row of pushes(); track row.id) {
                  <tr>
                    <td class="fw-500">{{ row.userName ?? row.userNameSnapshot ?? '\u2014' }}</td>
                    <td class="font-monospace small text-muted">
                      {{ row.lineUserIdSnapshot ? (row.lineUserIdSnapshot.substring(0, 12) + '\u2026') : '\u2014' }}
                    </td>
                    <td class="text-center">
                      @if (row.status === 'success') {
                        <span class="badge bg-success-subtle text-success">\u6210\u529F</span>
                      } @else {
                        <span class="badge bg-danger-subtle text-danger">\u5931\u6557</span>
                      }
                    </td>
                    <td class="small">
                      @if (row.errorCategory) {
                        <span class="text-danger fw-500">{{ errorCategoryLabels[row.errorCategory] }}</span>
                        @if (row.errorMessage) {
                          <div class="text-muted" style="word-break: break-all">{{ row.errorMessage }}</div>
                        }
                      } @else {
                        <span class="text-muted">\u2014</span>
                      }
                    </td>
                    <td class="text-center text-muted small">{{ row.httpStatusCode ?? '\u2014' }}</td>
                    <td class="text-right text-muted small">
                      {{ row.durationMs !== null && row.durationMs !== undefined ? row.durationMs + 'ms' : '\u2014' }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    </div>
  }

</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(AttendanceReminderLogDetail, { className: "AttendanceReminderLogDetail", filePath: "src/app/features/admin/attendance-reminder-logs/pages/attendance-reminder-log-detail/attendance-reminder-log-detail.ts", lineNumber: 17 });
})();
export {
  AttendanceReminderLogDetail
};
//# sourceMappingURL=chunk-EFYO2FE2.js.map
