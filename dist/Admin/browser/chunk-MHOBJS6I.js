import {
  ToastrService
} from "./chunk-X3EGCDLG.js";
import {
  CheckboxControlValueAccessor,
  DefaultValueAccessor,
  FormsModule,
  NgControlStatus,
  NgModel,
  NgSelectOption,
  SelectControlValueAccessor,
  ɵNgSelectMultipleOption
} from "./chunk-TUAOQ2AP.js";
import "./chunk-JDEYLUO2.js";
import {
  Component,
  DatePipe,
  HttpClient,
  Injectable,
  computed,
  environment,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassProp,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵinterpolate1,
  ɵɵlistener,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIdentity,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/calendar-days/services/calendar-day.service.ts
var CalendarDayService = class _CalendarDayService {
  http = inject(HttpClient);
  base = `${environment.apiUrl}/calendar-days`;
  /** 查詢指定年份所有日曆資料 */
  getByYear(year) {
    return this.http.get(this.base, { params: { year } });
  }
  /** 從政府 API 匯入指定年份行事曆 */
  importYear(year) {
    return this.http.post(`${this.base}/import`, null, { params: { year } });
  }
  /** 手動新增單筆日曆資料 */
  create(data) {
    return this.http.post(this.base, data);
  }
  /** 更新單筆日曆資料 */
  update(id, changes) {
    return this.http.put(`${this.base}/${id}`, changes);
  }
  /** 刪除單筆日曆資料 */
  delete(id) {
    return this.http.delete(`${this.base}/${id}`);
  }
  static \u0275fac = function CalendarDayService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _CalendarDayService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _CalendarDayService, factory: _CalendarDayService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(CalendarDayService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/calendar-days/pages/calendar-day-list/calendar-day-list.ts
var _forTrack0 = ($index, $item) => $item.id;
function CalendarDayList_For_10_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "option", 7);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const y_r1 = ctx.$implicit;
    \u0275\u0275property("value", y_r1);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("", y_r1, " \u5E74");
  }
}
function CalendarDayList_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 19);
    \u0275\u0275elementStart(1, "span");
    \u0275\u0275text(2, "\u532F\u5165\u4E2D...");
    \u0275\u0275elementEnd();
  }
}
function CalendarDayList_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 20);
    \u0275\u0275element(1, "use", 21);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(2, "span");
    \u0275\u0275text(3);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1("\u532F\u5165 ", ctx_r1.selectedYear(), " \u5E74");
  }
}
function CalendarDayList_Conditional_21_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 14);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("\u5171 ", ctx_r1.days().length, " \u7B46");
  }
}
function CalendarDayList_Conditional_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 16);
    \u0275\u0275element(1, "div", 22);
    \u0275\u0275text(2, " \u8F09\u5165\u4E2D... ");
    \u0275\u0275elementEnd();
  }
}
function CalendarDayList_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 17);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 23);
    \u0275\u0275element(2, "use", 4);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(3, "p", 24);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "button", 25);
    \u0275\u0275listener("click", function CalendarDayList_Conditional_24_Template_button_click_5_listener() {
      \u0275\u0275restoreView(_r3);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.importYear());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(6, "svg", 20);
    \u0275\u0275element(7, "use", 21);
    \u0275\u0275elementEnd();
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate1(" \u5C1A\u672A\u532F\u5165 ", ctx_r1.selectedYear(), " \u5E74\u884C\u4E8B\u66C6\u8CC7\u6599\uFF0C\u8ACB\u9EDE\u9078\u532F\u5165\u6309\u9215\u3002 ");
    \u0275\u0275advance();
    \u0275\u0275property("disabled", ctx_r1.importing());
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate1(" \u532F\u5165 ", ctx_r1.selectedYear(), " \u5E74 ");
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 36);
    \u0275\u0275text(1, " \u653E\u5047 ");
    \u0275\u0275elementEnd();
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 37);
    \u0275\u0275text(1, "\u4E0A\u73ED");
    \u0275\u0275elementEnd();
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span");
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const day_r5 = \u0275\u0275nextContext(2).$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(day_r5.description);
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 38);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr")(1, "td", 34);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 35)(5, "span");
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(7, "td", 35);
    \u0275\u0275conditionalCreate(8, CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_8_Template, 2, 0, "span", 36)(9, CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_9_Template, 2, 0, "span", 37);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td");
    \u0275\u0275conditionalCreate(11, CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_11_Template, 2, 1, "span")(12, CalendarDayList_Conditional_25_For_16_Conditional_0_Conditional_12_Template, 2, 0, "span", 38);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 39)(14, "div", 40)(15, "button", 41);
    \u0275\u0275listener("click", function CalendarDayList_Conditional_25_For_16_Conditional_0_Template_button_click_15_listener() {
      \u0275\u0275restoreView(_r4);
      const day_r5 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.startEdit(day_r5));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(16, "svg", 20);
    \u0275\u0275element(17, "use", 42);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(18, "button", 43);
    \u0275\u0275listener("click", function CalendarDayList_Conditional_25_For_16_Conditional_0_Template_button_click_18_listener() {
      \u0275\u0275restoreView(_r4);
      const day_r5 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.delete(day_r5));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(19, "svg", 20);
    \u0275\u0275element(20, "use", 44);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const day_r5 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275classProp("table-success", day_r5.isHoliday)("text-muted", !day_r5.isHoliday && !ctx_r1.isWeekend(day_r5.date));
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(3, 12, day_r5.date, "yyyy/MM/dd"), " ");
    \u0275\u0275advance(3);
    \u0275\u0275classProp("text-danger", ctx_r1.isWeekend(day_r5.date))("fw-600", ctx_r1.isWeekend(day_r5.date));
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.getWeekdayLabel(day_r5.date), " ");
    \u0275\u0275advance(2);
    \u0275\u0275conditional(day_r5.isHoliday ? 8 : 9);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(day_r5.description ? 11 : 12);
  }
}
function CalendarDayList_Conditional_25_For_16_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "tr", 33)(1, "td", 34);
    \u0275\u0275text(2);
    \u0275\u0275pipe(3, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(4, "td", 35)(5, "span");
    \u0275\u0275text(6);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(7, "td", 35)(8, "div", 45)(9, "input", 46);
    \u0275\u0275listener("ngModelChange", function CalendarDayList_Conditional_25_For_16_Conditional_1_Template_input_ngModelChange_9_listener($event) {
      \u0275\u0275restoreView(_r6);
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.editingIsHoliday.set($event));
    });
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(10, "td")(11, "input", 47);
    \u0275\u0275listener("ngModelChange", function CalendarDayList_Conditional_25_For_16_Conditional_1_Template_input_ngModelChange_11_listener($event) {
      \u0275\u0275restoreView(_r6);
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.editingDescription.set($event));
    });
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "td", 39)(13, "div", 40)(14, "button", 48);
    \u0275\u0275listener("click", function CalendarDayList_Conditional_25_For_16_Conditional_1_Template_button_click_14_listener() {
      \u0275\u0275restoreView(_r6);
      const day_r5 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.saveEdit(day_r5));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(15, "svg", 20);
    \u0275\u0275element(16, "use", 49);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(17, "button", 50);
    \u0275\u0275listener("click", function CalendarDayList_Conditional_25_For_16_Conditional_1_Template_button_click_17_listener() {
      \u0275\u0275restoreView(_r6);
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.cancelEdit());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(18, "svg", 20);
    \u0275\u0275element(19, "use", 51);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const day_r5 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate1(" ", \u0275\u0275pipeBind2(3, 10, day_r5.date, "yyyy/MM/dd"), " ");
    \u0275\u0275advance(3);
    \u0275\u0275classProp("text-danger", ctx_r1.isWeekend(day_r5.date))("fw-600", ctx_r1.isWeekend(day_r5.date));
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", ctx_r1.getWeekdayLabel(day_r5.date), " ");
    \u0275\u0275advance(3);
    \u0275\u0275property("id", \u0275\u0275interpolate1("holiday-", day_r5.id))("ngModel", ctx_r1.editingIsHoliday());
    \u0275\u0275advance(2);
    \u0275\u0275property("ngModel", ctx_r1.editingDescription());
  }
}
function CalendarDayList_Conditional_25_For_16_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, CalendarDayList_Conditional_25_For_16_Conditional_0_Template, 21, 15, "tr", 32)(1, CalendarDayList_Conditional_25_For_16_Conditional_1_Template, 20, 13, "tr", 33);
  }
  if (rf & 2) {
    const day_r5 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275conditional(ctx_r1.editingId() !== day_r5.id ? 0 : 1);
  }
}
function CalendarDayList_Conditional_25_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18)(1, "table", 26)(2, "thead", 27)(3, "tr")(4, "th", 28);
    \u0275\u0275text(5, "\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "th", 29);
    \u0275\u0275text(7, "\u661F\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "th", 30);
    \u0275\u0275text(9, "\u72C0\u614B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "th");
    \u0275\u0275text(11, "\u8AAA\u660E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "th", 31);
    \u0275\u0275text(13, "\u64CD\u4F5C");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(14, "tbody");
    \u0275\u0275repeaterCreate(15, CalendarDayList_Conditional_25_For_16_Template, 2, 1, null, null, _forTrack0);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(15);
    \u0275\u0275repeater(ctx_r1.days());
  }
}
var WEEKDAY_LABELS = ["\u65E5", "\u4E00", "\u4E8C", "\u4E09", "\u56DB", "\u4E94", "\u516D"];
var CalendarDayList = class _CalendarDayList {
  svc = inject(CalendarDayService);
  toastr = inject(ToastrService);
  /** 目前選擇的年份，預設今年 */
  selectedYear = signal((/* @__PURE__ */ new Date()).getFullYear(), ...ngDevMode ? [{ debugName: "selectedYear" }] : []);
  /** 選單可用的年份範圍（前後 3 年） */
  yearOptions = computed(() => {
    const current = (/* @__PURE__ */ new Date()).getFullYear();
    return Array.from({ length: 7 }, (_, i) => current - 3 + i);
  }, ...ngDevMode ? [{ debugName: "yearOptions" }] : []);
  /** 所有日曆資料（來自 API） */
  days = signal([], ...ngDevMode ? [{ debugName: "days" }] : []);
  /** 載入中狀態 */
  loading = signal(false, ...ngDevMode ? [{ debugName: "loading" }] : []);
  /** 匯入中狀態 */
  importing = signal(false, ...ngDevMode ? [{ debugName: "importing" }] : []);
  /** 行內編輯中的記錄 id（同時只允許一筆） */
  editingId = signal(null, ...ngDevMode ? [{ debugName: "editingId" }] : []);
  /** 行內編輯暫存值 */
  editingIsHoliday = signal(false, ...ngDevMode ? [{ debugName: "editingIsHoliday" }] : []);
  editingDescription = signal("", ...ngDevMode ? [{ debugName: "editingDescription" }] : []);
  ngOnInit() {
    this.loadData();
  }
  /** 切換年份時重新載入 */
  onYearChange(year) {
    this.selectedYear.set(Number(year));
    this.cancelEdit();
    this.loadData();
  }
  /** 取得星期中文標籤 */
  getWeekdayLabel(dateStr) {
    const d = new Date(dateStr);
    return WEEKDAY_LABELS[d.getDay()];
  }
  /** 是否為週末（六日） */
  isWeekend(dateStr) {
    const day = new Date(dateStr).getDay();
    return day === 0 || day === 6;
  }
  /** 載入該年行事曆資料 */
  loadData() {
    this.loading.set(true);
    this.svc.getByYear(this.selectedYear()).subscribe({
      next: (data) => {
        this.days.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.toastr.error("\u8F09\u5165\u884C\u4E8B\u66C6\u8CC7\u6599\u5931\u6557");
        this.loading.set(false);
      }
    });
  }
  /** 從政府 API 匯入當年行事曆 */
  importYear() {
    const year = this.selectedYear();
    if (!confirm(`\u78BA\u5B9A\u8981\u532F\u5165 ${year} \u5E74\u884C\u4E8B\u66C6\u8CC7\u6599\u55CE\uFF1F
\u82E5\u5DF2\u6709\u8CC7\u6599\u5C07\u6703\u8986\u84CB\u3002`))
      return;
    this.importing.set(true);
    this.svc.importYear(year).subscribe({
      next: (data) => {
        this.days.set(data);
        this.toastr.success(`\u5DF2\u532F\u5165 ${year} \u5E74\u884C\u4E8B\u66C6\uFF0C\u5171 ${data.length} \u7B46`);
        this.importing.set(false);
      },
      error: () => {
        this.toastr.error("\u532F\u5165\u5931\u6557\uFF0C\u8ACB\u78BA\u8A8D\u7DB2\u8DEF\u9023\u7DDA\u6216\u7A0D\u5F8C\u518D\u8A66");
        this.importing.set(false);
      }
    });
  }
  /** 進入行內編輯模式 */
  startEdit(day) {
    this.editingId.set(day.id);
    this.editingIsHoliday.set(day.isHoliday);
    this.editingDescription.set(day.description ?? "");
  }
  /** 取消編輯 */
  cancelEdit() {
    this.editingId.set(null);
  }
  /** 儲存行內編輯 */
  saveEdit(day) {
    this.svc.update(day.id, {
      isHoliday: this.editingIsHoliday(),
      description: this.editingDescription()
    }).subscribe({
      next: (updated) => {
        this.days.update((list) => list.map((d) => d.id === updated.id ? updated : d));
        this.toastr.success("\u66F4\u65B0\u6210\u529F");
        this.editingId.set(null);
      },
      error: () => this.toastr.error("\u66F4\u65B0\u5931\u6557")
    });
  }
  /** 刪除單筆記錄 */
  delete(day) {
    const label = new Date(day.date).toLocaleDateString("zh-TW");
    if (!confirm(`\u78BA\u5B9A\u8981\u522A\u9664 ${label} \u7684\u884C\u4E8B\u66C6\u8CC7\u6599\u55CE\uFF1F`))
      return;
    this.svc.delete(day.id).subscribe({
      next: () => {
        this.days.update((list) => list.filter((d) => d.id !== day.id));
        this.toastr.success("\u522A\u9664\u6210\u529F");
      },
      error: () => this.toastr.error("\u522A\u9664\u5931\u6557")
    });
  }
  static \u0275fac = function CalendarDayList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _CalendarDayList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _CalendarDayList, selectors: [["app-calendar-day-list"]], decls: 26, vars: 6, consts: [[1, "container-fluid", "py-3"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#calendar"], [1, "mb-0"], [1, "form-select", "form-select-sm", 2, "width", "120px", 3, "ngModelChange", "ngModel"], [3, "value"], [1, "btn", "btn-outline-primary", "btn-sm", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], [1, "badge", "bg-secondary", "ms-auto", "fw-400"], [1, "card-body", "p-0"], [1, "text-center", "text-muted", "py-5"], [1, "text-center", "py-5"], [1, "table-responsive"], ["role", "status", "aria-hidden", "true", 1, "spinner-border", "spinner-border-sm"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#download"], ["role", "status", "aria-hidden", "true", 1, "spinner-border", "spinner-border-sm", "me-2"], [1, "sa-icon", "sa-icon-2x", "text-muted", "mb-3", 2, "stroke", "currentColor", "display", "block", "margin", "0 auto 0.75rem"], [1, "text-muted", "mb-3"], [1, "btn", "btn-primary", "btn-sm", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [2, "min-width", "110px"], [1, "text-center", 2, "width", "60px"], [1, "text-center", 2, "width", "100px"], [1, "text-right", 2, "width", "120px"], [3, "table-success", "text-muted"], [1, "table-warning"], [1, "fw-500"], [1, "text-center"], [1, "badge", 2, "background-color", "var(--forest)", "color", "#fff"], [1, "text-muted", "small"], [1, "text-muted"], [1, "text-right"], [1, "flex", "justify-end", "gap-1"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], [1, "form-check", "form-switch", "d-flex", "justify-content-center", "mb-0"], ["type", "checkbox", "role", "switch", 1, "form-check-input", 3, "ngModelChange", "id", "ngModel"], ["type", "text", "placeholder", "\u570B\u5B9A\u5047\u65E5\u540D\u7A31\u6216\u5099\u8A3B", 1, "form-control", "form-control-sm", 3, "ngModelChange", "ngModel"], ["title", "\u5132\u5B58", 1, "btn", "btn-sm", "btn-success", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#check"], ["title", "\u53D6\u6D88", 1, "btn", "btn-sm", "btn-outline-secondary", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#x"]], template: function CalendarDayList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u884C\u4E8B\u66C6\u7BA1\u7406");
      \u0275\u0275elementEnd()();
      \u0275\u0275elementStart(7, "div", 2)(8, "select", 6);
      \u0275\u0275listener("ngModelChange", function CalendarDayList_Template_select_ngModelChange_8_listener($event) {
        return ctx.onYearChange($event);
      });
      \u0275\u0275repeaterCreate(9, CalendarDayList_For_10_Template, 2, 2, "option", 7, \u0275\u0275repeaterTrackByIdentity);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(11, "button", 8);
      \u0275\u0275listener("click", function CalendarDayList_Template_button_click_11_listener() {
        return ctx.importYear();
      });
      \u0275\u0275conditionalCreate(12, CalendarDayList_Conditional_12_Template, 3, 0)(13, CalendarDayList_Conditional_13_Template, 4, 1);
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(14, "div", 9)(15, "div", 10)(16, "div", 11)(17, "div", 12);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(18, "svg", 13);
      \u0275\u0275element(19, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275text(20);
      \u0275\u0275conditionalCreate(21, CalendarDayList_Conditional_21_Template, 2, 1, "span", 14);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(22, "div", 15);
      \u0275\u0275conditionalCreate(23, CalendarDayList_Conditional_23_Template, 3, 0, "div", 16)(24, CalendarDayList_Conditional_24_Template, 9, 3, "div", 17)(25, CalendarDayList_Conditional_25_Template, 17, 0, "div", 18);
      \u0275\u0275elementEnd()()()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(8);
      \u0275\u0275property("ngModel", ctx.selectedYear());
      \u0275\u0275advance();
      \u0275\u0275repeater(ctx.yearOptions());
      \u0275\u0275advance(2);
      \u0275\u0275property("disabled", ctx.importing());
      \u0275\u0275advance();
      \u0275\u0275conditional(ctx.importing() ? 12 : 13);
      \u0275\u0275advance(8);
      \u0275\u0275textInterpolate1(" ", ctx.selectedYear(), " \u5E74\u884C\u4E8B\u66C6 ");
      \u0275\u0275advance();
      \u0275\u0275conditional(!ctx.loading() && ctx.days().length > 0 ? 21 : -1);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.loading() ? 23 : ctx.days().length === 0 ? 24 : 25);
    }
  }, dependencies: [FormsModule, NgSelectOption, \u0275NgSelectMultipleOption, DefaultValueAccessor, CheckboxControlValueAccessor, SelectControlValueAccessor, NgControlStatus, NgModel, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(CalendarDayList, [{
    type: Component,
    args: [{ selector: "app-calendar-day-list", imports: [FormsModule, DatePipe], template: `<div class="container-fluid py-3">

  <!-- \u9801\u982D -->
  <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
    <div class="flex items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#calendar"></use>
      </svg>
      <h4 class="mb-0">\u884C\u4E8B\u66C6\u7BA1\u7406</h4>
    </div>

    <!-- \u5DE5\u5177\u5217\uFF1A\u5E74\u4EFD\u9078\u64C7 + \u532F\u5165\u6309\u9215 -->
    <div class="flex items-center gap-2">
      <select class="form-select form-select-sm"
              style="width: 120px"
              [ngModel]="selectedYear()"
              (ngModelChange)="onYearChange($event)">
        @for (y of yearOptions(); track y) {
          <option [value]="y">{{ y }} \u5E74</option>
        }
      </select>

      <button class="btn btn-outline-primary btn-sm inline-flex items-center gap-1"
              [disabled]="importing()"
              (click)="importYear()">
        @if (importing()) {
          <span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
          <span>\u532F\u5165\u4E2D...</span>
        } @else {
          <svg class="sa-icon" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#download"></use>
          </svg>
          <span>\u532F\u5165 {{ selectedYear() }} \u5E74</span>
        }
      </button>
    </div>
  </div>

  <!-- \u4E3B\u5167\u5BB9\u5340 -->
  <div class="row g-4">
    <div class="col-12 col-xl-10">

      <div class="card border-0 shadow-sm">
        <!-- \u5361\u7247\u6A19\u982D -->
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#calendar"></use>
          </svg>
          {{ selectedYear() }} \u5E74\u884C\u4E8B\u66C6
          @if (!loading() && days().length > 0) {
            <span class="badge bg-secondary ms-auto fw-400">\u5171 {{ days().length }} \u7B46</span>
          }
        </div>

        <div class="card-body p-0">

          <!-- \u8F09\u5165\u4E2D -->
          @if (loading()) {
            <div class="text-center text-muted py-5">
              <div class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></div>
              \u8F09\u5165\u4E2D...
            </div>
          }

          <!-- \u7121\u8CC7\u6599\u63D0\u793A -->
          @else if (days().length === 0) {
            <div class="text-center py-5">
              <svg class="sa-icon sa-icon-2x text-muted mb-3" style="stroke: currentColor; display: block; margin: 0 auto 0.75rem">
                <use href="/assets/icons/sprite.svg#calendar"></use>
              </svg>
              <p class="text-muted mb-3">
                \u5C1A\u672A\u532F\u5165 {{ selectedYear() }} \u5E74\u884C\u4E8B\u66C6\u8CC7\u6599\uFF0C\u8ACB\u9EDE\u9078\u532F\u5165\u6309\u9215\u3002
              </p>
              <button class="btn btn-primary btn-sm inline-flex items-center gap-1"
                      [disabled]="importing()"
                      (click)="importYear()">
                <svg class="sa-icon" style="stroke: currentColor">
                  <use href="/assets/icons/sprite.svg#download"></use>
                </svg>
                \u532F\u5165 {{ selectedYear() }} \u5E74
              </button>
            </div>
          }

          <!-- \u8CC7\u6599\u8868\u683C -->
          @else {
            <div class="table-responsive">
              <table class="table table-hover mb-0">
                <thead class="table-light">
                  <tr>
                    <th style="min-width: 110px">\u65E5\u671F</th>
                    <th style="width: 60px" class="text-center">\u661F\u671F</th>
                    <th style="width: 100px" class="text-center">\u72C0\u614B</th>
                    <th>\u8AAA\u660E</th>
                    <th style="width: 120px" class="text-right">\u64CD\u4F5C</th>
                  </tr>
                </thead>
                <tbody>
                  @for (day of days(); track day.id) {
                    <!-- \u4E00\u822C\u986F\u793A\u5217 -->
                    @if (editingId() !== day.id) {
                      <tr [class.table-success]="day.isHoliday" [class.text-muted]="!day.isHoliday && !isWeekend(day.date)">
                        <td class="fw-500">
                          {{ day.date | date:'yyyy/MM/dd' }}
                        </td>
                        <td class="text-center">
                          <span [class.text-danger]="isWeekend(day.date)"
                                [class.fw-600]="isWeekend(day.date)">
                            {{ getWeekdayLabel(day.date) }}
                          </span>
                        </td>
                        <td class="text-center">
                          @if (day.isHoliday) {
                            <span class="badge"
                                  style="background-color: var(--forest); color: #fff;">
                              \u653E\u5047
                            </span>
                          } @else {
                            <span class="text-muted small">\u4E0A\u73ED</span>
                          }
                        </td>
                        <td>
                          @if (day.description) {
                            <span>{{ day.description }}</span>
                          } @else {
                            <span class="text-muted">\u2014</span>
                          }
                        </td>
                        <td class="text-right">
                          <div class="flex justify-end gap-1">
                            <button class="btn btn-sm btn-ghost-primary inline-flex items-center"
                                    title="\u7DE8\u8F2F"
                                    (click)="startEdit(day)">
                              <svg class="sa-icon" style="stroke: currentColor">
                                <use href="/assets/icons/sprite.svg#edit"></use>
                              </svg>
                            </button>
                            <button class="btn btn-sm btn-ghost-danger inline-flex items-center"
                                    title="\u522A\u9664"
                                    (click)="delete(day)">
                              <svg class="sa-icon" style="stroke: currentColor">
                                <use href="/assets/icons/sprite.svg#trash"></use>
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    }

                    <!-- \u884C\u5167\u7DE8\u8F2F\u5217 -->
                    @else {
                      <tr class="table-warning">
                        <td class="fw-500">
                          {{ day.date | date:'yyyy/MM/dd' }}
                        </td>
                        <td class="text-center">
                          <span [class.text-danger]="isWeekend(day.date)"
                                [class.fw-600]="isWeekend(day.date)">
                            {{ getWeekdayLabel(day.date) }}
                          </span>
                        </td>
                        <td class="text-center">
                          <!-- \u653E\u5047\u72C0\u614B\u5207\u63DB -->
                          <div class="form-check form-switch d-flex justify-content-center mb-0">
                            <input class="form-check-input"
                                   type="checkbox"
                                   role="switch"
                                   id="holiday-{{ day.id }}"
                                   [ngModel]="editingIsHoliday()"
                                   (ngModelChange)="editingIsHoliday.set($event)">
                          </div>
                        </td>
                        <td>
                          <!-- \u8AAA\u660E\u6587\u5B57\u7DE8\u8F2F -->
                          <input type="text"
                                 class="form-control form-control-sm"
                                 placeholder="\u570B\u5B9A\u5047\u65E5\u540D\u7A31\u6216\u5099\u8A3B"
                                 [ngModel]="editingDescription()"
                                 (ngModelChange)="editingDescription.set($event)">
                        </td>
                        <td class="text-right">
                          <div class="flex justify-end gap-1">
                            <button class="btn btn-sm btn-success inline-flex items-center"
                                    title="\u5132\u5B58"
                                    (click)="saveEdit(day)">
                              <svg class="sa-icon" style="stroke: currentColor">
                                <use href="/assets/icons/sprite.svg#check"></use>
                              </svg>
                            </button>
                            <button class="btn btn-sm btn-outline-secondary inline-flex items-center"
                                    title="\u53D6\u6D88"
                                    (click)="cancelEdit()">
                              <svg class="sa-icon" style="stroke: currentColor">
                                <use href="/assets/icons/sprite.svg#x"></use>
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>
          }

        </div>
      </div>

    </div>
  </div>

</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(CalendarDayList, { className: "CalendarDayList", filePath: "src/app/features/admin/calendar-days/pages/calendar-day-list/calendar-day-list.ts", lineNumber: 16 });
})();
export {
  CalendarDayList
};
//# sourceMappingURL=chunk-MHOBJS6I.js.map
