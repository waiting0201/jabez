import {
  AttendanceReminderLogService,
  ERROR_CATEGORY_LABELS,
  REMINDER_TYPE_LABELS,
  STATUS_LABELS,
  TRIGGER_SOURCE_LABELS
} from "./chunk-7EPQTUWZ.js";
import {
  ToastrService
} from "./chunk-X3EGCDLG.js";
import {
  DefaultValueAccessor,
  FormsModule,
  NgControlStatus,
  NgModel,
  NgSelectOption,
  SelectControlValueAccessor,
  ɵNgSelectMultipleOption
} from "./chunk-TUAOQ2AP.js";
import {
  RouterLink
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  CommonModule,
  Component,
  HttpClient,
  computed,
  environment,
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
  ɵɵgetCurrentView,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵproperty,
  ɵɵpureFunction1,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵstyleProp,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate3
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/attendance-reminder-logs/pages/attendance-reminder-log-list/attendance-reminder-log-list.ts
var _c0 = (a0) => ["/admin/attendance-reminder-logs/batches", a0];
var _forTrack0 = ($index, $item) => $item.id;
var _forTrack1 = ($index, $item) => $item.day;
function AttendanceReminderLogList_Conditional_18_Conditional_25_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 51);
    \u0275\u0275text(1, "\u5C1A\u7121\u8CC7\u6599");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogList_Conditional_18_Conditional_26_For_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 53);
    \u0275\u0275element(1, "div", 54)(2, "div", 55);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const d_r1 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(3);
    \u0275\u0275property("title", d_r1.day + " \u6210\u529F " + d_r1.pushed + " / \u5931\u6557 " + d_r1.failed);
    \u0275\u0275advance();
    \u0275\u0275styleProp("height", d_r1.pushed * 40 / ctx_r1.maxBar(), "px");
    \u0275\u0275advance();
    \u0275\u0275styleProp("height", d_r1.failed * 40 / ctx_r1.maxBar(), "px");
  }
}
function AttendanceReminderLogList_Conditional_18_Conditional_26_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 52);
    \u0275\u0275repeaterCreate(1, AttendanceReminderLogList_Conditional_18_Conditional_26_For_2_Template, 3, 5, "div", 53, _forTrack1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const s_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275repeater(s_r3.last7Days);
  }
}
function AttendanceReminderLogList_Conditional_18_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 11)(1, "div", 12)(2, "div", 44)(3, "div", 45);
    \u0275\u0275text(4, "\u4ECA\u65E5\u5DF2\u63A8\u64AD");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "div", 46);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(7, "div", 12)(8, "div", 44)(9, "div", 45);
    \u0275\u0275text(10, "\u4ECA\u65E5\u5931\u6557");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "div", 47);
    \u0275\u0275text(12);
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(13, "div", 12)(14, "div", 44)(15, "div", 45);
    \u0275\u0275text(16, "\u4ECA\u65E5\u6279\u6B21 tick \u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "div", 48);
    \u0275\u0275text(18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "div", 49);
    \u0275\u0275text(20, "\uFF1D\u6392\u7A0B\u88AB\u89F8\u767C\u6B21\u6578\uFF08\u5373\u4F7F 0 \u5C0D\u8C61\uFF09");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(21, "div", 12)(22, "div", 44)(23, "div", 50);
    \u0275\u0275text(24, "\u6700\u8FD1 7 \u5929\u8DA8\u52E2");
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(25, AttendanceReminderLogList_Conditional_18_Conditional_25_Template, 2, 0, "div", 51)(26, AttendanceReminderLogList_Conditional_18_Conditional_26_Template, 3, 0, "div", 52);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const s_r3 = ctx;
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(s_r3.todayPushed);
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(s_r3.todayFailed);
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(s_r3.todayBatchTicks);
    \u0275\u0275advance(7);
    \u0275\u0275conditional(s_r3.last7Days.length === 0 ? 25 : 26);
  }
}
function AttendanceReminderLogList_For_113_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 59);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const row_r4 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u2014 \u6279\u6B21\u555F\u52D5 (", row_r4.userNameSnapshot, ")");
  }
}
function AttendanceReminderLogList_For_113_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0);
  }
  if (rf & 2) {
    const row_r4 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275textInterpolate1(" ", row_r4.userName ?? row_r4.userNameSnapshot ?? "\u2014", " ");
  }
}
function AttendanceReminderLogList_For_113_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 62);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const row_r4 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("title", row_r4.triggeredByName ? "\u7531 " + row_r4.triggeredByName + " \u89F8\u767C" : "");
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.triggerSourceLabels[row_r4.triggerSource], " ");
  }
}
function AttendanceReminderLogList_For_113_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 59);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const row_r4 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.triggerSourceLabels[row_r4.triggerSource]);
  }
}
function AttendanceReminderLogList_For_113_Case_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 63);
    \u0275\u0275text(1, "\u6210\u529F");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogList_For_113_Case_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 64);
    \u0275\u0275text(1, "\u5931\u6557");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogList_For_113_Case_17_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 65);
    \u0275\u0275text(1, "\u6279\u6B21");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogList_For_113_Conditional_19_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 67);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const row_r4 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("title", row_r4.errorMessage ?? "");
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.errorCategoryLabels[row_r4.errorCategory], " ");
  }
}
function AttendanceReminderLogList_For_113_Conditional_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 59);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function AttendanceReminderLogList_For_113_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 56);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 38)(4, "span", 57);
    \u0275\u0275text(5);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(6, "td", 58);
    \u0275\u0275conditionalCreate(7, AttendanceReminderLogList_For_113_Conditional_7_Template, 2, 1, "span", 59)(8, AttendanceReminderLogList_For_113_Conditional_8_Template, 1, 1);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 60);
    \u0275\u0275text(10);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "td", 61);
    \u0275\u0275conditionalCreate(12, AttendanceReminderLogList_For_113_Conditional_12_Template, 2, 2, "span", 62)(13, AttendanceReminderLogList_For_113_Conditional_13_Template, 2, 1, "span", 59);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 38);
    \u0275\u0275conditionalCreate(15, AttendanceReminderLogList_For_113_Case_15_Template, 2, 0, "span", 63)(16, AttendanceReminderLogList_For_113_Case_16_Template, 2, 0, "span", 64)(17, AttendanceReminderLogList_For_113_Case_17_Template, 2, 0, "span", 65);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(18, "td", 66);
    \u0275\u0275conditionalCreate(19, AttendanceReminderLogList_For_113_Conditional_19_Template, 2, 2, "span", 67)(20, AttendanceReminderLogList_For_113_Conditional_20_Template, 2, 0, "span", 59);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "td", 68);
    \u0275\u0275text(22);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "td", 69);
    \u0275\u0275text(24);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "td", 38)(26, "a", 70);
    \u0275\u0275text(27);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    let tmp_15_0;
    const row_r4 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(ctx_r1.formatTaipei(row_r4.tickedAtTaipei));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" ", ctx_r1.reminderTypeLabels[row_r4.reminderType], " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(row_r4.reminderType === "batchStart" ? 7 : 8);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(row_r4.targetTimeTaipei);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(row_r4.triggerSource === "manual" ? 12 : 13);
    \u0275\u0275advance(3);
    \u0275\u0275conditional((tmp_15_0 = row_r4.status) === "success" ? 15 : tmp_15_0 === "failure" ? 16 : tmp_15_0 === "batchStart" ? 17 : -1);
    \u0275\u0275advance(4);
    \u0275\u0275conditional(row_r4.errorCategory ? 19 : 20);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(row_r4.httpStatusCode ?? "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", row_r4.durationMs !== null && row_r4.durationMs !== void 0 ? row_r4.durationMs + "ms" : "\u2014", " ");
    \u0275\u0275advance(2);
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(12, _c0, row_r4.batchId))("title", row_r4.batchId);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.shortBatchId(row_r4.batchId), " ");
  }
}
function AttendanceReminderLogList_ForEmpty_114_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u8F09\u5165\u4E2D... ");
  }
}
function AttendanceReminderLogList_ForEmpty_114_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275text(0, " \u5C1A\u7121\u8CC7\u6599 ");
  }
}
function AttendanceReminderLogList_ForEmpty_114_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 71);
    \u0275\u0275conditionalCreate(2, AttendanceReminderLogList_ForEmpty_114_Conditional_2_Template, 1, 0)(3, AttendanceReminderLogList_ForEmpty_114_Conditional_3_Template, 1, 0);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275conditional(ctx_r1.loading() ? 2 : 3);
  }
}
function AttendanceReminderLogList_Conditional_115_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 43)(1, "span", 51);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 72)(4, "button", 7);
    \u0275\u0275listener("click", function AttendanceReminderLogList_Conditional_115_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.goToPage(ctx_r1.currentPage() - 1));
    });
    \u0275\u0275text(5, "\u4E0A\u4E00\u9801");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "button", 7);
    \u0275\u0275listener("click", function AttendanceReminderLogList_Conditional_115_Template_button_click_6_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.goToPage(ctx_r1.currentPage() + 1));
    });
    \u0275\u0275text(7, "\u4E0B\u4E00\u9801");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate3("\u5171 ", ctx_r1.totalCount(), " \u7B46\uFF0C\u7B2C ", ctx_r1.currentPage(), " / ", ctx_r1.totalPages(), " \u9801");
    \u0275\u0275advance(2);
    \u0275\u0275property("disabled", ctx_r1.currentPage() <= 1);
    \u0275\u0275advance(2);
    \u0275\u0275property("disabled", ctx_r1.currentPage() >= ctx_r1.totalPages());
  }
}
var AttendanceReminderLogList = class _AttendanceReminderLogList {
  service = inject(AttendanceReminderLogService);
  http = inject(HttpClient);
  toastr = inject(ToastrService);
  /** 篩選條件 */
  fromDate = signal("", ...ngDevMode ? [{ debugName: "fromDate" }] : []);
  toDate = signal("", ...ngDevMode ? [{ debugName: "toDate" }] : []);
  reminderType = signal("", ...ngDevMode ? [{ debugName: "reminderType" }] : []);
  status = signal("", ...ngDevMode ? [{ debugName: "status" }] : []);
  errorCategory = signal("", ...ngDevMode ? [{ debugName: "errorCategory" }] : []);
  triggerSource = signal("", ...ngDevMode ? [{ debugName: "triggerSource" }] : []);
  /** 紀錄與統計 */
  records = signal([], ...ngDevMode ? [{ debugName: "records" }] : []);
  stats = signal(null, ...ngDevMode ? [{ debugName: "stats" }] : []);
  loading = signal(false, ...ngDevMode ? [{ debugName: "loading" }] : []);
  triggering = signal(false, ...ngDevMode ? [{ debugName: "triggering" }] : []);
  /** 分頁 */
  currentPage = signal(1, ...ngDevMode ? [{ debugName: "currentPage" }] : []);
  totalCount = signal(0, ...ngDevMode ? [{ debugName: "totalCount" }] : []);
  totalPages = signal(1, ...ngDevMode ? [{ debugName: "totalPages" }] : []);
  pageSize = 20;
  /** Label 對照表（給 template 用） */
  reminderTypeLabels = REMINDER_TYPE_LABELS;
  statusLabels = STATUS_LABELS;
  errorCategoryLabels = ERROR_CATEGORY_LABELS;
  triggerSourceLabels = TRIGGER_SOURCE_LABELS;
  /** 7 天趨勢最大值（畫長條圖用） */
  maxBar = computed(() => {
    const days = this.stats()?.last7Days ?? [];
    return Math.max(1, ...days.map((d) => d.pushed + d.failed));
  }, ...ngDevMode ? [{ debugName: "maxBar" }] : []);
  ngOnInit() {
    const today = /* @__PURE__ */ new Date();
    const fromDefault = new Date(today);
    fromDefault.setDate(today.getDate() - 6);
    this.fromDate.set(this.toIsoDate(fromDefault));
    this.toDate.set(this.toIsoDate(today));
    this.loadStats();
    this.search();
  }
  search() {
    this.currentPage.set(1);
    this.fetchData();
  }
  resetFilters() {
    this.reminderType.set("");
    this.status.set("");
    this.errorCategory.set("");
    this.triggerSource.set("");
    this.search();
  }
  goToPage(page) {
    this.currentPage.set(page);
    this.fetchData();
  }
  /** 手動觸發推播（Superadmin 限定） */
  triggerNow(type) {
    this.triggering.set(true);
    this.http.post(`${environment.apiUrl}/admin/attendance-reminder/run`, null, { params: { type } }).subscribe({
      next: (res) => {
        this.toastr.success(`${type === "clockIn" ? "\u4E0A\u73ED" : "\u4E0B\u73ED"}\u63D0\u9192\u5DF2\u89F8\u767C\uFF1A\u5C0D\u8C61 ${res.recipientCount} \u4EBA\uFF0C\u6210\u529F ${res.pushedCount}\uFF0C\u5931\u6557 ${res.failureCount}`, "\u63A8\u64AD\u5B8C\u6210");
        this.triggering.set(false);
        this.loadStats();
        this.search();
      },
      error: (err) => {
        this.toastr.error(err?.error?.message ?? "\u89F8\u767C\u5931\u6557", "\u932F\u8AA4");
        this.triggering.set(false);
      }
    });
  }
  fetchData() {
    this.loading.set(true);
    this.service.getPaged({
      from: this.fromDate() || void 0,
      to: this.toDate() || void 0,
      reminderType: this.reminderType() || void 0,
      status: this.status() || void 0,
      errorCategory: this.errorCategory() || void 0,
      triggerSource: this.triggerSource() || void 0,
      page: this.currentPage(),
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.records.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.totalPages.set(Math.max(1, res.totalPages ?? 1));
        this.loading.set(false);
      },
      error: (err) => {
        this.records.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        this.toastr.error(err?.error?.message ?? "\u8F09\u5165\u5931\u6557", "\u932F\u8AA4");
      }
    });
  }
  loadStats() {
    this.service.getStats().subscribe({
      next: (s) => this.stats.set(s),
      error: () => this.stats.set(null)
    });
  }
  toIsoDate(d) {
    const yr = d.getFullYear();
    const mo = String(d.getMonth() + 1).padStart(2, "0");
    const da = String(d.getDate()).padStart(2, "0");
    return `${yr}-${mo}-${da}`;
  }
  /** 格式化台北時間 → 字串 (MM/dd HH:mm:ss) */
  formatTaipei(iso) {
    if (!iso)
      return "\u2014";
    const d = new Date(iso);
    return `${String(d.getMonth() + 1).padStart(2, "0")}/${String(d.getDate()).padStart(2, "0")} ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}:${String(d.getSeconds()).padStart(2, "0")}`;
  }
  /** BatchId 短碼（前 8 字） */
  shortBatchId(id) {
    return id ? id.substring(0, 8) : "";
  }
  static \u0275fac = function AttendanceReminderLogList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _AttendanceReminderLogList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _AttendanceReminderLogList, selectors: [["app-attendance-reminder-log-list"]], decls: 116, vars: 11, consts: [[1, "container-fluid", "py-3"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#bell"], [1, "mb-0"], [1, "flex", "flex-wrap", "gap-2"], [1, "btn", "btn-sm", "btn-outline", 3, "click", "disabled"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#play"], [1, "text-muted", "small", "mb-4"], [1, "grid", "grid-cols-1", "sm:grid-cols-2", "lg:grid-cols-4", "gap-3", "mb-4"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "grid", "grid-cols-1", "sm:grid-cols-2", "lg:grid-cols-4", "gap-2", "px-4", "py-3", "border-b", "items-end"], [1, "text-xs", "text-muted", "mb-1", "block"], ["type", "date", 1, "form-control", "w-full", 3, "ngModelChange", "ngModel"], [1, "form-control", "w-full", 3, "ngModelChange", "ngModel"], ["value", ""], ["value", "clockIn"], ["value", "clockOut"], ["value", "batchStart"], ["value", "success"], ["value", "failure"], ["value", "not_friend"], ["value", "token_invalid"], ["value", "rate_limited"], ["value", "network_error"], ["value", "unknown"], ["value", "system_error"], ["value", "auto"], ["value", "manual"], [1, "flex", "gap-2", "sm:col-span-2", "lg:col-span-2"], [1, "btn", "btn-primary", "flex-1", 3, "click"], [1, "btn", "btn-outline", "flex-1", 3, "click"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-center"], [1, "text-center", "hidden", "md:table-cell"], [1, "hidden", "lg:table-cell"], [1, "text-center", "hidden", "xl:table-cell"], [1, "text-right", "hidden", "xl:table-cell"], [1, "flex", "items-center", "justify-between", "px-4", "py-3", "border-t"], [1, "card-body"], [1, "text-muted", "small", "mb-1"], [1, "text-success", 2, "font-size", "1.75rem", "font-weight", "700"], [1, "text-danger", 2, "font-size", "1.75rem", "font-weight", "700"], [2, "font-size", "1.75rem", "font-weight", "700"], [1, "text-muted", "small", "mt-1"], [1, "text-muted", "small", "mb-2"], [1, "text-muted", "small"], [1, "flex", "items-end", "gap-1", 2, "height", "48px"], [1, "flex", "flex-col", "items-center", 2, "flex", "1", "min-width", "12px", 3, "title"], [2, "width", "100%", "background", "var(--forest)", "border-radius", "2px 2px 0 0"], [2, "width", "100%", "background", "var(--red)", "border-radius", "0 0 2px 2px"], [1, "font-monospace", "small"], [1, "badge", "bg-[--bg-base]", "text-[--text-secondary]", "small"], [1, "fw-500"], [1, "text-muted"], [1, "text-center", "hidden", "md:table-cell", "font-monospace", "small"], [1, "text-center", "hidden", "md:table-cell", "small"], [1, "badge", "bg-warning-subtle", "text-warning-emphasis", 3, "title"], [1, "badge", "bg-success-subtle", "text-success"], [1, "badge", "bg-danger-subtle", "text-danger"], [1, "badge", "bg-secondary-subtle", "text-secondary"], [1, "hidden", "lg:table-cell", "small"], [1, "text-danger", 3, "title"], [1, "text-center", "hidden", "xl:table-cell", "text-muted", "small"], [1, "text-right", "hidden", "xl:table-cell", "text-muted", "small"], [1, "font-monospace", "small", "text-primary", 3, "routerLink", "title"], ["colspan", "10", 1, "text-center", "text-muted", "py-4"], [1, "flex", "gap-1"]], template: function AttendanceReminderLogList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u6253\u5361\u63D0\u9192\u7D00\u9304");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 6)(8, "button", 7);
      \u0275\u0275listener("click", function AttendanceReminderLogList_Template_button_click_8_listener() {
        return ctx.triggerNow("clockIn");
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(9, "svg", 8);
      \u0275\u0275element(10, "use", 9);
      \u0275\u0275elementEnd();
      \u0275\u0275text(11, " \u624B\u52D5\u89F8\u767C\u4E0A\u73ED\u63D0\u9192 ");
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(12, "button", 7);
      \u0275\u0275listener("click", function AttendanceReminderLogList_Template_button_click_12_listener() {
        return ctx.triggerNow("clockOut");
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(13, "svg", 8);
      \u0275\u0275element(14, "use", 9);
      \u0275\u0275elementEnd();
      \u0275\u0275text(15, " \u624B\u52D5\u89F8\u767C\u4E0B\u73ED\u63D0\u9192 ");
      \u0275\u0275elementEnd()()();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(16, "p", 10);
      \u0275\u0275text(17, " \u81EA\u52D5\u6392\u7A0B\u65BC\u53F0\u5317\u6642\u5340 7-9\u300116-18 \u6642\u6BB5\u6BCF\u5206\u9418\u6AA2\u67E5\uFF1B\u547D\u4E2D\u4E0A\u4E0B\u73ED\u524D 2 \u5206\u9418\u89F8\u767C\u3002\u9031\u672B\u8207\u5DF2\u6253\u5361 / \u8ACB\u5047\u6DB5\u84CB\u76EE\u6A19\u6642\u523B\u8005\u81EA\u52D5\u6392\u9664\u3002 ");
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(18, AttendanceReminderLogList_Conditional_18_Template, 27, 4, "div", 11);
      \u0275\u0275elementStart(19, "div", 12)(20, "div", 13)(21, "div", 14)(22, "div")(23, "label", 15);
      \u0275\u0275text(24, "\u8D77\u65E5");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(25, "input", 16);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_input_ngModelChange_25_listener($event) {
        return ctx.fromDate.set($event);
      });
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(26, "div")(27, "label", 15);
      \u0275\u0275text(28, "\u8FC4\u65E5");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(29, "input", 16);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_input_ngModelChange_29_listener($event) {
        return ctx.toDate.set($event);
      });
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(30, "div")(31, "label", 15);
      \u0275\u0275text(32, "\u63D0\u9192\u985E\u578B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(33, "select", 17);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_select_ngModelChange_33_listener($event) {
        return ctx.reminderType.set($event);
      });
      \u0275\u0275elementStart(34, "option", 18);
      \u0275\u0275text(35, "\u5168\u90E8");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(36, "option", 19);
      \u0275\u0275text(37, "\u4E0A\u73ED\u63D0\u9192");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(38, "option", 20);
      \u0275\u0275text(39, "\u4E0B\u73ED\u63D0\u9192");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(40, "option", 21);
      \u0275\u0275text(41, "\u6279\u6B21\u555F\u52D5");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(42, "div")(43, "label", 15);
      \u0275\u0275text(44, "\u7D50\u679C");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(45, "select", 17);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_select_ngModelChange_45_listener($event) {
        return ctx.status.set($event);
      });
      \u0275\u0275elementStart(46, "option", 18);
      \u0275\u0275text(47, "\u5168\u90E8");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(48, "option", 22);
      \u0275\u0275text(49, "\u6210\u529F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(50, "option", 23);
      \u0275\u0275text(51, "\u5931\u6557");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(52, "option", 21);
      \u0275\u0275text(53, "\u6279\u6B21\u555F\u52D5");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(54, "div")(55, "label", 15);
      \u0275\u0275text(56, "\u5931\u6557\u539F\u56E0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(57, "select", 17);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_select_ngModelChange_57_listener($event) {
        return ctx.errorCategory.set($event);
      });
      \u0275\u0275elementStart(58, "option", 18);
      \u0275\u0275text(59, "\u5168\u90E8");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(60, "option", 24);
      \u0275\u0275text(61, "\u672A\u52A0\u597D\u53CB");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(62, "option", 25);
      \u0275\u0275text(63, "Token \u5931\u6548");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(64, "option", 26);
      \u0275\u0275text(65, "\u901F\u7387\u9650\u5236 (429)");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(66, "option", 27);
      \u0275\u0275text(67, "\u7DB2\u8DEF\u932F\u8AA4");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(68, "option", 28);
      \u0275\u0275text(69, "\u5176\u4ED6\u932F\u8AA4");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(70, "option", 29);
      \u0275\u0275text(71, "\u7CFB\u7D71\u4F8B\u5916");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(72, "div")(73, "label", 15);
      \u0275\u0275text(74, "\u89F8\u767C\u4F86\u6E90");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(75, "select", 17);
      \u0275\u0275listener("ngModelChange", function AttendanceReminderLogList_Template_select_ngModelChange_75_listener($event) {
        return ctx.triggerSource.set($event);
      });
      \u0275\u0275elementStart(76, "option", 18);
      \u0275\u0275text(77, "\u5168\u90E8");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(78, "option", 30);
      \u0275\u0275text(79, "\u81EA\u52D5\u6392\u7A0B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(80, "option", 31);
      \u0275\u0275text(81, "\u624B\u52D5\u89F8\u767C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(82, "div", 32)(83, "button", 33);
      \u0275\u0275listener("click", function AttendanceReminderLogList_Template_button_click_83_listener() {
        return ctx.search();
      });
      \u0275\u0275text(84, "\u7BE9\u9078");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(85, "button", 34);
      \u0275\u0275listener("click", function AttendanceReminderLogList_Template_button_click_85_listener() {
        return ctx.resetFilters();
      });
      \u0275\u0275text(86, "\u91CD\u8A2D");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(87, "div", 35)(88, "table", 36)(89, "thead", 37)(90, "tr")(91, "th");
      \u0275\u0275text(92, "\u6642\u9593");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(93, "th", 38);
      \u0275\u0275text(94, "\u985E\u578B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(95, "th");
      \u0275\u0275text(96, "\u5C0D\u8C61");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(97, "th", 39);
      \u0275\u0275text(98, "\u76EE\u6A19\u6642\u523B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(99, "th", 39);
      \u0275\u0275text(100, "\u89F8\u767C");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(101, "th", 38);
      \u0275\u0275text(102, "\u7D50\u679C");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(103, "th", 40);
      \u0275\u0275text(104, "\u5931\u6557\u539F\u56E0");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(105, "th", 41);
      \u0275\u0275text(106, "HTTP");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(107, "th", 42);
      \u0275\u0275text(108, "\u8017\u6642");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(109, "th", 38);
      \u0275\u0275text(110, "\u6279\u6B21");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(111, "tbody");
      \u0275\u0275repeaterCreate(112, AttendanceReminderLogList_For_113_Template, 28, 14, "tr", null, _forTrack0, false, AttendanceReminderLogList_ForEmpty_114_Template, 4, 1, "tr");
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(115, AttendanceReminderLogList_Conditional_115_Template, 8, 5, "div", 43);
      \u0275\u0275elementEnd()()();
    }
    if (rf & 2) {
      let tmp_2_0;
      \u0275\u0275advance(8);
      \u0275\u0275property("disabled", ctx.triggering());
      \u0275\u0275advance(4);
      \u0275\u0275property("disabled", ctx.triggering());
      \u0275\u0275advance(6);
      \u0275\u0275conditional((tmp_2_0 = ctx.stats()) ? 18 : -1, tmp_2_0);
      \u0275\u0275advance(7);
      \u0275\u0275property("ngModel", ctx.fromDate());
      \u0275\u0275advance(4);
      \u0275\u0275property("ngModel", ctx.toDate());
      \u0275\u0275advance(4);
      \u0275\u0275property("ngModel", ctx.reminderType());
      \u0275\u0275advance(12);
      \u0275\u0275property("ngModel", ctx.status());
      \u0275\u0275advance(12);
      \u0275\u0275property("ngModel", ctx.errorCategory());
      \u0275\u0275advance(18);
      \u0275\u0275property("ngModel", ctx.triggerSource());
      \u0275\u0275advance(37);
      \u0275\u0275repeater(ctx.records());
      \u0275\u0275advance(3);
      \u0275\u0275conditional(ctx.totalPages() > 1 ? 115 : -1);
    }
  }, dependencies: [CommonModule, FormsModule, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, SelectControlValueAccessor, NgControlStatus, NgModel, RouterLink], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(AttendanceReminderLogList, [{
    type: Component,
    args: [{ selector: "app-attendance-reminder-log-list", imports: [CommonModule, FormsModule, RouterLink], template: `<div class="container-fluid py-3">

  <!-- Header -->
  <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
    <div class="flex items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#bell"></use>
      </svg>
      <h4 class="mb-0">\u6253\u5361\u63D0\u9192\u7D00\u9304</h4>
    </div>
    <div class="flex flex-wrap gap-2">
      <button class="btn btn-sm btn-outline" [disabled]="triggering()" (click)="triggerNow('clockIn')">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#play"></use></svg>
        \u624B\u52D5\u89F8\u767C\u4E0A\u73ED\u63D0\u9192
      </button>
      <button class="btn btn-sm btn-outline" [disabled]="triggering()" (click)="triggerNow('clockOut')">
        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#play"></use></svg>
        \u624B\u52D5\u89F8\u767C\u4E0B\u73ED\u63D0\u9192
      </button>
    </div>
  </div>

  <p class="text-muted small mb-4">
    \u81EA\u52D5\u6392\u7A0B\u65BC\u53F0\u5317\u6642\u5340 7-9\u300116-18 \u6642\u6BB5\u6BCF\u5206\u9418\u6AA2\u67E5\uFF1B\u547D\u4E2D\u4E0A\u4E0B\u73ED\u524D 2 \u5206\u9418\u89F8\u767C\u3002\u9031\u672B\u8207\u5DF2\u6253\u5361 / \u8ACB\u5047\u6DB5\u84CB\u76EE\u6A19\u6642\u523B\u8005\u81EA\u52D5\u6392\u9664\u3002
  </p>

  <!-- Stats Cards -->
  @if (stats(); as s) {
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="text-muted small mb-1">\u4ECA\u65E5\u5DF2\u63A8\u64AD</div>
          <div class="text-success" style="font-size: 1.75rem; font-weight: 700">{{ s.todayPushed }}</div>
        </div>
      </div>
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="text-muted small mb-1">\u4ECA\u65E5\u5931\u6557</div>
          <div class="text-danger" style="font-size: 1.75rem; font-weight: 700">{{ s.todayFailed }}</div>
        </div>
      </div>
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="text-muted small mb-1">\u4ECA\u65E5\u6279\u6B21 tick \u6578</div>
          <div style="font-size: 1.75rem; font-weight: 700">{{ s.todayBatchTicks }}</div>
          <div class="text-muted small mt-1">\uFF1D\u6392\u7A0B\u88AB\u89F8\u767C\u6B21\u6578\uFF08\u5373\u4F7F 0 \u5C0D\u8C61\uFF09</div>
        </div>
      </div>
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <div class="text-muted small mb-2">\u6700\u8FD1 7 \u5929\u8DA8\u52E2</div>
          @if (s.last7Days.length === 0) {
            <div class="text-muted small">\u5C1A\u7121\u8CC7\u6599</div>
          } @else {
            <div class="flex items-end gap-1" style="height: 48px">
              @for (d of s.last7Days; track d.day) {
                <div class="flex flex-col items-center" style="flex: 1; min-width: 12px"
                     [title]="d.day + ' \u6210\u529F ' + d.pushed + ' / \u5931\u6557 ' + d.failed">
                  <div style="width: 100%; background: var(--forest); border-radius: 2px 2px 0 0"
                       [style.height.px]="(d.pushed * 40) / maxBar()"></div>
                  <div style="width: 100%; background: var(--red); border-radius: 0 0 2px 2px"
                       [style.height.px]="(d.failed * 40) / maxBar()"></div>
                </div>
              }
            </div>
          }
        </div>
      </div>
    </div>
  }

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">

      <!-- Filters -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2 px-4 py-3 border-b items-end">
        <div>
          <label class="text-xs text-muted mb-1 block">\u8D77\u65E5</label>
          <input type="date" class="form-control w-full" [ngModel]="fromDate()" (ngModelChange)="fromDate.set($event)" />
        </div>
        <div>
          <label class="text-xs text-muted mb-1 block">\u8FC4\u65E5</label>
          <input type="date" class="form-control w-full" [ngModel]="toDate()" (ngModelChange)="toDate.set($event)" />
        </div>
        <div>
          <label class="text-xs text-muted mb-1 block">\u63D0\u9192\u985E\u578B</label>
          <select class="form-control w-full" [ngModel]="reminderType()" (ngModelChange)="reminderType.set($event)">
            <option value="">\u5168\u90E8</option>
            <option value="clockIn">\u4E0A\u73ED\u63D0\u9192</option>
            <option value="clockOut">\u4E0B\u73ED\u63D0\u9192</option>
            <option value="batchStart">\u6279\u6B21\u555F\u52D5</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-muted mb-1 block">\u7D50\u679C</label>
          <select class="form-control w-full" [ngModel]="status()" (ngModelChange)="status.set($event)">
            <option value="">\u5168\u90E8</option>
            <option value="success">\u6210\u529F</option>
            <option value="failure">\u5931\u6557</option>
            <option value="batchStart">\u6279\u6B21\u555F\u52D5</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-muted mb-1 block">\u5931\u6557\u539F\u56E0</label>
          <select class="form-control w-full" [ngModel]="errorCategory()" (ngModelChange)="errorCategory.set($event)">
            <option value="">\u5168\u90E8</option>
            <option value="not_friend">\u672A\u52A0\u597D\u53CB</option>
            <option value="token_invalid">Token \u5931\u6548</option>
            <option value="rate_limited">\u901F\u7387\u9650\u5236 (429)</option>
            <option value="network_error">\u7DB2\u8DEF\u932F\u8AA4</option>
            <option value="unknown">\u5176\u4ED6\u932F\u8AA4</option>
            <option value="system_error">\u7CFB\u7D71\u4F8B\u5916</option>
          </select>
        </div>
        <div>
          <label class="text-xs text-muted mb-1 block">\u89F8\u767C\u4F86\u6E90</label>
          <select class="form-control w-full" [ngModel]="triggerSource()" (ngModelChange)="triggerSource.set($event)">
            <option value="">\u5168\u90E8</option>
            <option value="auto">\u81EA\u52D5\u6392\u7A0B</option>
            <option value="manual">\u624B\u52D5\u89F8\u767C</option>
          </select>
        </div>
        <div class="flex gap-2 sm:col-span-2 lg:col-span-2">
          <button class="btn btn-primary flex-1" (click)="search()">\u7BE9\u9078</button>
          <button class="btn btn-outline flex-1" (click)="resetFilters()">\u91CD\u8A2D</button>
        </div>
      </div>

      <!-- Table -->
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u6642\u9593</th>
              <th class="text-center">\u985E\u578B</th>
              <th>\u5C0D\u8C61</th>
              <th class="text-center hidden md:table-cell">\u76EE\u6A19\u6642\u523B</th>
              <th class="text-center hidden md:table-cell">\u89F8\u767C</th>
              <th class="text-center">\u7D50\u679C</th>
              <th class="hidden lg:table-cell">\u5931\u6557\u539F\u56E0</th>
              <th class="text-center hidden xl:table-cell">HTTP</th>
              <th class="text-right hidden xl:table-cell">\u8017\u6642</th>
              <th class="text-center">\u6279\u6B21</th>
            </tr>
          </thead>
          <tbody>
            @for (row of records(); track row.id) {
              <tr>
                <td class="font-monospace small">{{ formatTaipei(row.tickedAtTaipei) }}</td>
                <td class="text-center">
                  <span class="badge bg-[--bg-base] text-[--text-secondary] small">
                    {{ reminderTypeLabels[row.reminderType] }}
                  </span>
                </td>
                <td class="fw-500">
                  @if (row.reminderType === 'batchStart') {
                    <span class="text-muted">\u2014 \u6279\u6B21\u555F\u52D5 ({{ row.userNameSnapshot }})</span>
                  } @else {
                    {{ row.userName ?? row.userNameSnapshot ?? '\u2014' }}
                  }
                </td>
                <td class="text-center hidden md:table-cell font-monospace small">{{ row.targetTimeTaipei }}</td>
                <td class="text-center hidden md:table-cell small">
                  @if (row.triggerSource === 'manual') {
                    <span class="badge bg-warning-subtle text-warning-emphasis"
                          [title]="row.triggeredByName ? '\u7531 ' + row.triggeredByName + ' \u89F8\u767C' : ''">
                      {{ triggerSourceLabels[row.triggerSource] }}
                    </span>
                  } @else {
                    <span class="text-muted">{{ triggerSourceLabels[row.triggerSource] }}</span>
                  }
                </td>
                <td class="text-center">
                  @switch (row.status) {
                    @case ('success') {
                      <span class="badge bg-success-subtle text-success">\u6210\u529F</span>
                    }
                    @case ('failure') {
                      <span class="badge bg-danger-subtle text-danger">\u5931\u6557</span>
                    }
                    @case ('batchStart') {
                      <span class="badge bg-secondary-subtle text-secondary">\u6279\u6B21</span>
                    }
                  }
                </td>
                <td class="hidden lg:table-cell small">
                  @if (row.errorCategory) {
                    <span class="text-danger" [title]="row.errorMessage ?? ''">
                      {{ errorCategoryLabels[row.errorCategory] }}
                    </span>
                  } @else {
                    <span class="text-muted">\u2014</span>
                  }
                </td>
                <td class="text-center hidden xl:table-cell text-muted small">{{ row.httpStatusCode ?? '\u2014' }}</td>
                <td class="text-right hidden xl:table-cell text-muted small">
                  {{ row.durationMs !== null && row.durationMs !== undefined ? row.durationMs + 'ms' : '\u2014' }}
                </td>
                <td class="text-center">
                  <a [routerLink]="['/admin/attendance-reminder-logs/batches', row.batchId]"
                     class="font-monospace small text-primary" [title]="row.batchId">
                    {{ shortBatchId(row.batchId) }}
                  </a>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="10" class="text-center text-muted py-4">
                  @if (loading()) { \u8F09\u5165\u4E2D... } @else { \u5C1A\u7121\u8CC7\u6599 }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      @if (totalPages() > 1) {
        <div class="flex items-center justify-between px-4 py-3 border-t">
          <span class="text-muted small">\u5171 {{ totalCount() }} \u7B46\uFF0C\u7B2C {{ currentPage() }} / {{ totalPages() }} \u9801</span>
          <div class="flex gap-1">
            <button class="btn btn-sm btn-outline" [disabled]="currentPage() <= 1" (click)="goToPage(currentPage() - 1)">\u4E0A\u4E00\u9801</button>
            <button class="btn btn-sm btn-outline" [disabled]="currentPage() >= totalPages()" (click)="goToPage(currentPage() + 1)">\u4E0B\u4E00\u9801</button>
          </div>
        </div>
      }

    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(AttendanceReminderLogList, { className: "AttendanceReminderLogList", filePath: "src/app/features/admin/attendance-reminder-logs/pages/attendance-reminder-log-list/attendance-reminder-log-list.ts", lineNumber: 27 });
})();
export {
  AttendanceReminderLogList
};
//# sourceMappingURL=chunk-ZO5RGUCJ.js.map
