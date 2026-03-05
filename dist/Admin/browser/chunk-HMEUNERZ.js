import {
  AttendanceService,
  OvertimeRequestService
} from "./chunk-BFT3JBVI.js";
import {
  AuthService,
  ChangeDetectionStrategy,
  Component,
  DatePipe,
  DecimalPipe,
  computed,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdomElement,
  ɵɵdomElementEnd,
  ɵɵdomElementStart,
  ɵɵdomListener,
  ɵɵdomProperty,
  ɵɵgetCurrentView,
  ɵɵnamespaceHTML,
  ɵɵnamespaceSVG,
  ɵɵnextContext,
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2,
  ɵɵtextInterpolate3
} from "./chunk-7JUZFUWV.js";
import "./chunk-7WUTQBRG.js";

// src/app/features/dashboard/pages/dashboard/dashboard.ts
var _forTrack0 = ($index, $item) => $item.id;
function Dashboard_Conditional_87_For_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "option", 47);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "date");
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const req_r3 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275domProperty("value", req_r3.id)("selected", req_r3.id === ctx_r1.selectedOvertimeId());
    \u0275\u0275advance();
    \u0275\u0275textInterpolate3(" ", \u0275\u0275pipeBind2(2, 5, req_r3.overtimeDate, "MM/dd"), " \u2014 ", (req_r3.projectCodes == null ? null : req_r3.projectCodes.join(", ")) || "\u7121\u5C08\u6848", " (", req_r3.estimatedHours, "h) ");
  }
}
function Dashboard_Conditional_87_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275domElementStart(0, "div", 38)(1, "label", 45);
    \u0275\u0275text(2, "\u9078\u64C7\u52A0\u73ED\u7533\u8ACB\u55AE");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(3, "select", 46);
    \u0275\u0275domListener("change", function Dashboard_Conditional_87_Template_select_change_3_listener($event) {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.onOvertimeSelect($event));
    });
    \u0275\u0275repeaterCreate(4, Dashboard_Conditional_87_For_5_Template, 3, 8, "option", 47, _forTrack0);
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(6, "div", 48)(7, "button", 49);
    \u0275\u0275domListener("click", function Dashboard_Conditional_87_Template_button_click_7_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.confirmOvertimeStart());
    });
    \u0275\u0275text(8, "\u78BA\u8A8D\u6253\u5361");
    \u0275\u0275domElementEnd();
    \u0275\u0275domElementStart(9, "button", 50);
    \u0275\u0275domListener("click", function Dashboard_Conditional_87_Template_button_click_9_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.cancelOvertimeSelector());
    });
    \u0275\u0275text(10, "\u53D6\u6D88");
    \u0275\u0275domElementEnd()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275repeater(ctx_r1.approvedRequests());
  }
}
function Dashboard_Case_95_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "p", 41);
    \u0275\u0275text(1, "\u6253\u5361\u6642\u81EA\u52D5\u53D6\u5F97 GPS \u5EA7\u6A19\u3002");
    \u0275\u0275domElementEnd();
  }
}
function Dashboard_Case_96_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 42);
    \u0275\u0275domElement(1, "div", 51);
    \u0275\u0275domElementStart(2, "span", 52);
    \u0275\u0275text(3, "\u5B9A\u4F4D\u4E2D\u2026");
    \u0275\u0275domElementEnd()();
  }
}
function Dashboard_Case_97_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "p", 56);
    \u0275\u0275text(1);
    \u0275\u0275pipe(2, "number");
    \u0275\u0275pipe(3, "number");
    \u0275\u0275domElementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate2(" ", \u0275\u0275pipeBind2(2, 2, ctx_r1.gpsCoords().lat, "1.6-6"), ", ", \u0275\u0275pipeBind2(3, 5, ctx_r1.gpsCoords().lng, "1.6-6"), " ");
  }
}
function Dashboard_Case_97_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 53);
    \u0275\u0275namespaceSVG();
    \u0275\u0275domElementStart(1, "svg", 54);
    \u0275\u0275domElement(2, "use", 30);
    \u0275\u0275domElementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275domElementStart(3, "span", 55);
    \u0275\u0275text(4, "\u5B9A\u4F4D\u6210\u529F");
    \u0275\u0275domElementEnd()();
    \u0275\u0275conditionalCreate(5, Dashboard_Case_97_Conditional_5_Template, 4, 8, "p", 56);
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(5);
    \u0275\u0275conditional(ctx_r1.gpsCoords() ? 5 : -1);
  }
}
function Dashboard_Case_98_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 43);
    \u0275\u0275namespaceSVG();
    \u0275\u0275domElementStart(1, "svg", 54);
    \u0275\u0275domElement(2, "use", 57);
    \u0275\u0275domElementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275domElementStart(3, "span", 55);
    \u0275\u0275text(4, "\u7121\u6CD5\u53D6\u5F97\u5B9A\u4F4D\uFF08\u6253\u5361\u4ECD\u6709\u6548\uFF09");
    \u0275\u0275domElementEnd()();
  }
}
function Dashboard_Conditional_99_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275domElementStart(0, "div", 44)(1, "div", 58);
    \u0275\u0275text(2);
    \u0275\u0275domElementEnd()();
  }
  if (rf & 2) {
    const t_r4 = ctx;
    \u0275\u0275advance();
    \u0275\u0275classMap(t_r4.type === "success" ? "bg-success text-white" : t_r4.type === "warning" ? "bg-warning text-dark" : "bg-danger text-white");
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1(" ", t_r4.message, " ");
  }
}
var DAY_NAMES = ["\u65E5", "\u4E00", "\u4E8C", "\u4E09", "\u56DB", "\u4E94", "\u516D"];
var Dashboard = class _Dashboard {
  auth = inject(AuthService);
  attendanceService = inject(AttendanceService);
  overtimeService = inject(OvertimeRequestService);
  timerId = null;
  /** Real-time clock signal updated every second */
  now = signal(/* @__PURE__ */ new Date(), ...ngDevMode ? [{ debugName: "now" }] : []);
  /** Today's attendance record */
  todayRecord = signal(null, ...ngDevMode ? [{ debugName: "todayRecord" }] : []);
  /** Approved overtime requests available for today */
  approvedRequests = signal([], ...ngDevMode ? [{ debugName: "approvedRequests" }] : []);
  /** Selected overtime request ID for overtime start */
  selectedOvertimeId = signal(null, ...ngDevMode ? [{ debugName: "selectedOvertimeId" }] : []);
  /** Whether the overtime request selector is visible (shown on "加班開始" click) */
  showOvertimeSelector = signal(false, ...ngDevMode ? [{ debugName: "showOvertimeSelector" }] : []);
  /** GPS state */
  gpsStatus = signal("idle", ...ngDevMode ? [{ debugName: "gpsStatus" }] : []);
  gpsCoords = signal(null, ...ngDevMode ? [{ debugName: "gpsCoords" }] : []);
  /** Loading state for clock actions */
  loading = signal(false, ...ngDevMode ? [{ debugName: "loading" }] : []);
  /** Toast message */
  toast = signal(null, ...ngDevMode ? [{ debugName: "toast" }] : []);
  /** User display name */
  userName = computed(() => this.auth.currentUser()?.name ?? "\u4F7F\u7528\u8005", ...ngDevMode ? [{ debugName: "userName" }] : []);
  /** Formatted date: yyyy/MM/dd 星期X */
  dateDisplay = computed(() => {
    const d = this.now();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const dd = String(d.getDate()).padStart(2, "0");
    return `${yyyy}/${mm}/${dd} \u661F\u671F${DAY_NAMES[d.getDay()]}`;
  }, ...ngDevMode ? [{ debugName: "dateDisplay" }] : []);
  /** Formatted time: HH:mm:ss */
  timeDisplay = computed(() => {
    const d = this.now();
    return [d.getHours(), d.getMinutes(), d.getSeconds()].map((n) => String(n).padStart(2, "0")).join(":");
  }, ...ngDevMode ? [{ debugName: "timeDisplay" }] : []);
  /** Button enable states */
  canClockIn = computed(() => {
    const r = this.todayRecord();
    return !r?.clockInTime && !this.loading();
  }, ...ngDevMode ? [{ debugName: "canClockIn" }] : []);
  canClockOut = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockInTime && !r?.clockOutTime && !this.loading();
  }, ...ngDevMode ? [{ debugName: "canClockOut" }] : []);
  canOvertimeStart = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockOutTime && !r?.overtimeStartTime && this.approvedRequests().length > 0 && !this.loading();
  }, ...ngDevMode ? [{ debugName: "canOvertimeStart" }] : []);
  canOvertimeEnd = computed(() => {
    const r = this.todayRecord();
    return !!r?.overtimeStartTime && !r?.overtimeEndTime && !this.loading();
  }, ...ngDevMode ? [{ debugName: "canOvertimeEnd" }] : []);
  ngOnInit() {
    this.timerId = setInterval(() => this.now.set(/* @__PURE__ */ new Date()), 1e3);
    this.attendanceService.getToday().subscribe((r) => {
      if (r)
        this.todayRecord.set(r);
    });
    this.overtimeService.getApprovedForToday().subscribe((list) => {
      this.approvedRequests.set(list);
      if (list.length > 0)
        this.selectedOvertimeId.set(list[0].id);
    });
  }
  ngOnDestroy() {
    if (this.timerId)
      clearInterval(this.timerId);
  }
  onOvertimeSelect(event) {
    const val = event.target.value;
    this.selectedOvertimeId.set(val ? +val : null);
  }
  formatTime(isoString) {
    if (!isoString)
      return "--:--";
    const match = isoString.match(/T(\d{2}):(\d{2})/);
    if (!match)
      return "--:--";
    return `${match[1]}:${match[2]}`;
  }
  /** 點擊加班開始 → 先顯示選擇器 */
  onOvertimeStartClick() {
    if (this.approvedRequests().length === 0)
      return;
    if (!this.selectedOvertimeId()) {
      this.selectedOvertimeId.set(this.approvedRequests()[0].id);
    }
    this.showOvertimeSelector.set(true);
  }
  /** 選擇器中確認 → 執行打卡 */
  confirmOvertimeStart() {
    this.showOvertimeSelector.set(false);
    this.performAction("overtime-start");
  }
  cancelOvertimeSelector() {
    this.showOvertimeSelector.set(false);
  }
  /** Perform a clock action: get GPS → call service → update state */
  performAction(type) {
    if (this.loading())
      return;
    this.loading.set(true);
    this.gpsStatus.set("locating");
    this._getGps().then((coords) => {
      this.gpsCoords.set(coords);
      this.gpsStatus.set(coords ? "success" : "failed");
      const body = {
        latitude: coords?.lat,
        longitude: coords?.lng,
        overtimeRequestId: type === "overtime-start" ? this.selectedOvertimeId() ?? void 0 : void 0
      };
      let obs$;
      switch (type) {
        case "clock-in":
          obs$ = this.attendanceService.clockIn(body);
          break;
        case "clock-out":
          obs$ = this.attendanceService.clockOut(body);
          break;
        case "overtime-start":
          obs$ = this.attendanceService.overtimeStart(body);
          break;
        case "overtime-end":
          obs$ = this.attendanceService.overtimeEnd(body);
          break;
      }
      obs$.subscribe({
        next: (record) => {
          this.todayRecord.set(record);
          this.loading.set(false);
          const labels = {
            "clock-in": "\u4E0A\u73ED\u6253\u5361",
            "clock-out": "\u4E0B\u73ED\u6253\u5361",
            "overtime-start": "\u52A0\u73ED\u958B\u59CB",
            "overtime-end": "\u52A0\u73ED\u7D50\u675F"
          };
          this.showToast(`${labels[type]}\u6210\u529F\uFF01`, coords ? "success" : "warning");
        },
        error: () => {
          this.loading.set(false);
          this.showToast("\u6253\u5361\u5931\u6557\uFF0C\u8ACB\u7A0D\u5F8C\u91CD\u8A66", "error");
        }
      });
    });
  }
  showToast(message, type) {
    this.toast.set({ message, type });
    setTimeout(() => this.toast.set(null), 3e3);
  }
  _getGps() {
    return new Promise((resolve) => {
      if (!navigator.geolocation) {
        resolve(null);
        return;
      }
      navigator.geolocation.getCurrentPosition((pos) => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude }), () => resolve(null), { enableHighAccuracy: true, timeout: 8e3, maximumAge: 0 });
    });
  }
  static \u0275fac = function Dashboard_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _Dashboard)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _Dashboard, selectors: [["app-dashboard"]], decls: 100, vars: 14, consts: [[1, "container-fluid", "py-3"], [1, "flex", "items-center", "gap-3", "mb-6"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#clock"], [1, "mb-0"], [1, "text-muted", "mb-0", "small"], [1, "row", "g-4"], [1, "col-12", "col-lg-8"], [1, "card", "border-0", "shadow-sm", "mb-4"], [1, "card-body", "text-center", "py-5"], [1, "text-muted", "mb-2", "fw-500"], [1, "mb-0", "font-monospace", 2, "font-size", "3.5rem", "font-weight", "700", "letter-spacing", "0.05em", "color", "var(--text-primary)"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#activity"], [1, "card-body"], [1, "row", "g-3", "text-center"], [1, "col-6", "col-md-3"], [1, "rounded-lg", "py-3", 2, "background", "var(--accent-dim)"], [1, "sa-icon", "mb-1", 2, "stroke", "var(--green)"], ["href", "/assets/icons/sprite.svg#log-in"], [1, "text-muted", "small", "mb-1"], [1, "fw-600", "font-monospace", "mb-0"], [1, "sa-icon", "mb-1", 2, "stroke", "var(--yellow)"], ["href", "/assets/icons/sprite.svg#log-out"], [1, "sa-icon", "mb-1", 2, "stroke", "var(--purple)"], ["href", "/assets/icons/sprite.svg#zap"], [1, "sa-icon", "mb-1", 2, "stroke", "var(--red)"], ["href", "/assets/icons/sprite.svg#zap-off"], [1, "card", "border-0", "shadow-sm"], ["href", "/assets/icons/sprite.svg#check-circle"], [1, "row", "g-3"], [1, "btn", "btn-primary", "w-100", "py-3", "flex", "flex-col", "items-center", "gap-2", 3, "click", "disabled"], [1, "sa-icon", "sa-icon-2x", 2, "stroke", "currentColor"], [1, "fw-600"], [1, "btn", "btn-warning", "w-100", "py-3", "flex", "flex-col", "items-center", "gap-2", 3, "click", "disabled"], [1, "btn", "btn-outline-primary", "w-100", "py-3", "flex", "flex-col", "items-center", "gap-2", 3, "click", "disabled"], [1, "btn", "btn-outline-secondary", "w-100", "py-3", "flex", "flex-col", "items-center", "gap-2", 3, "click", "disabled"], [1, "mt-4", "pt-3", "border-t"], [1, "col-12", "col-lg-4"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "text-muted", "small", "mb-0"], [1, "flex", "items-center", "gap-2", "text-muted"], [1, "flex", "items-center", "gap-2", 2, "color", "var(--yellow)"], [1, "position-fixed", "bottom-0", "end-0", "p-4", 2, "z-index", "1080"], [1, "form-label", "fw-600"], [1, "form-select", "mb-3", 3, "change"], [3, "value", "selected"], [1, "flex", "gap-2"], [1, "btn", "btn-primary", "btn-sm", 3, "click"], [1, "btn", "btn-outline-secondary", "btn-sm", 3, "click"], [1, "spinner-border", "spinner-border-sm"], [1, "small"], [1, "flex", "items-center", "gap-2", "mb-2", 2, "color", "var(--green)"], [1, "sa-icon", 2, "stroke", "currentColor"], [1, "small", "fw-500"], [1, "font-monospace", "small", "text-muted", "mb-0"], ["href", "/assets/icons/sprite.svg#alert-triangle"], [1, "px-4", "py-3", "rounded-lg", "shadow-lg", "fw-500"]], template: function Dashboard_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275domElementStart(0, "div", 0)(1, "div", 1);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(2, "svg", 2);
      \u0275\u0275domElement(3, "use", 3);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(4, "div")(5, "h4", 4);
      \u0275\u0275text(6, "\u6253\u5361\u7CFB\u7D71");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(7, "p", 5);
      \u0275\u0275text(8);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(9, "div", 6)(10, "div", 7)(11, "div", 8)(12, "div", 9)(13, "p", 10);
      \u0275\u0275text(14);
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(15, "p", 11);
      \u0275\u0275text(16);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(17, "div", 8)(18, "div", 12);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(19, "svg", 13);
      \u0275\u0275domElement(20, "use", 14);
      \u0275\u0275domElementEnd();
      \u0275\u0275text(21, " \u4ECA\u65E5\u6253\u5361\u7D00\u9304 ");
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(22, "div", 15)(23, "div", 16)(24, "div", 17)(25, "div", 18);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(26, "svg", 19);
      \u0275\u0275domElement(27, "use", 20);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(28, "p", 21);
      \u0275\u0275text(29, "\u4E0A\u73ED");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(30, "p", 22);
      \u0275\u0275text(31);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(32, "div", 17)(33, "div", 18);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(34, "svg", 23);
      \u0275\u0275domElement(35, "use", 24);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(36, "p", 21);
      \u0275\u0275text(37, "\u4E0B\u73ED");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(38, "p", 22);
      \u0275\u0275text(39);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(40, "div", 17)(41, "div", 18);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(42, "svg", 25);
      \u0275\u0275domElement(43, "use", 26);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(44, "p", 21);
      \u0275\u0275text(45, "\u52A0\u73ED\u958B\u59CB");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(46, "p", 22);
      \u0275\u0275text(47);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(48, "div", 17)(49, "div", 18);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(50, "svg", 27);
      \u0275\u0275domElement(51, "use", 28);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(52, "p", 21);
      \u0275\u0275text(53, "\u52A0\u73ED\u7D50\u675F");
      \u0275\u0275domElementEnd();
      \u0275\u0275domElementStart(54, "p", 22);
      \u0275\u0275text(55);
      \u0275\u0275domElementEnd()()()()()();
      \u0275\u0275domElementStart(56, "div", 29)(57, "div", 12);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(58, "svg", 13);
      \u0275\u0275domElement(59, "use", 30);
      \u0275\u0275domElementEnd();
      \u0275\u0275text(60, " \u6253\u5361\u64CD\u4F5C ");
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(61, "div", 15)(62, "div", 31)(63, "div", 17)(64, "button", 32);
      \u0275\u0275domListener("click", function Dashboard_Template_button_click_64_listener() {
        return ctx.performAction("clock-in");
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(65, "svg", 33);
      \u0275\u0275domElement(66, "use", 20);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(67, "span", 34);
      \u0275\u0275text(68, "\u4E0A\u73ED\u6253\u5361");
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(69, "div", 17)(70, "button", 35);
      \u0275\u0275domListener("click", function Dashboard_Template_button_click_70_listener() {
        return ctx.performAction("clock-out");
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(71, "svg", 33);
      \u0275\u0275domElement(72, "use", 24);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(73, "span", 34);
      \u0275\u0275text(74, "\u4E0B\u73ED\u6253\u5361");
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(75, "div", 17)(76, "button", 36);
      \u0275\u0275domListener("click", function Dashboard_Template_button_click_76_listener() {
        return ctx.onOvertimeStartClick();
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(77, "svg", 33);
      \u0275\u0275domElement(78, "use", 26);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(79, "span", 34);
      \u0275\u0275text(80, "\u52A0\u73ED\u958B\u59CB");
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(81, "div", 17)(82, "button", 37);
      \u0275\u0275domListener("click", function Dashboard_Template_button_click_82_listener() {
        return ctx.performAction("overtime-end");
      });
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(83, "svg", 33);
      \u0275\u0275domElement(84, "use", 28);
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(85, "span", 34);
      \u0275\u0275text(86, "\u52A0\u73ED\u7D50\u675F");
      \u0275\u0275domElementEnd()()()();
      \u0275\u0275conditionalCreate(87, Dashboard_Conditional_87_Template, 11, 0, "div", 38);
      \u0275\u0275domElementEnd()()();
      \u0275\u0275domElementStart(88, "div", 39)(89, "div", 29)(90, "div", 12);
      \u0275\u0275namespaceSVG();
      \u0275\u0275domElementStart(91, "svg", 13);
      \u0275\u0275domElement(92, "use", 40);
      \u0275\u0275domElementEnd();
      \u0275\u0275text(93, " \u5B9A\u4F4D\u8CC7\u8A0A ");
      \u0275\u0275domElementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275domElementStart(94, "div", 15);
      \u0275\u0275conditionalCreate(95, Dashboard_Case_95_Template, 2, 0, "p", 41)(96, Dashboard_Case_96_Template, 4, 0, "div", 42)(97, Dashboard_Case_97_Template, 6, 1)(98, Dashboard_Case_98_Template, 5, 0, "div", 43);
      \u0275\u0275domElementEnd()()()();
      \u0275\u0275conditionalCreate(99, Dashboard_Conditional_99_Template, 3, 3, "div", 44);
      \u0275\u0275domElementEnd();
    }
    if (rf & 2) {
      let tmp_3_0;
      let tmp_4_0;
      let tmp_5_0;
      let tmp_6_0;
      let tmp_12_0;
      let tmp_13_0;
      \u0275\u0275advance(8);
      \u0275\u0275textInterpolate1("", ctx.userName(), "\uFF0C\u6B61\u8FCE\u56DE\u4F86");
      \u0275\u0275advance(6);
      \u0275\u0275textInterpolate(ctx.dateDisplay());
      \u0275\u0275advance(2);
      \u0275\u0275textInterpolate1(" ", ctx.timeDisplay(), " ");
      \u0275\u0275advance(15);
      \u0275\u0275textInterpolate(ctx.formatTime((tmp_3_0 = ctx.todayRecord()) == null ? null : tmp_3_0.clockInTime));
      \u0275\u0275advance(8);
      \u0275\u0275textInterpolate(ctx.formatTime((tmp_4_0 = ctx.todayRecord()) == null ? null : tmp_4_0.clockOutTime));
      \u0275\u0275advance(8);
      \u0275\u0275textInterpolate(ctx.formatTime((tmp_5_0 = ctx.todayRecord()) == null ? null : tmp_5_0.overtimeStartTime));
      \u0275\u0275advance(8);
      \u0275\u0275textInterpolate(ctx.formatTime((tmp_6_0 = ctx.todayRecord()) == null ? null : tmp_6_0.overtimeEndTime));
      \u0275\u0275advance(9);
      \u0275\u0275domProperty("disabled", !ctx.canClockIn());
      \u0275\u0275advance(6);
      \u0275\u0275domProperty("disabled", !ctx.canClockOut());
      \u0275\u0275advance(6);
      \u0275\u0275domProperty("disabled", !ctx.canOvertimeStart());
      \u0275\u0275advance(6);
      \u0275\u0275domProperty("disabled", !ctx.canOvertimeEnd());
      \u0275\u0275advance(5);
      \u0275\u0275conditional(ctx.showOvertimeSelector() ? 87 : -1);
      \u0275\u0275advance(8);
      \u0275\u0275conditional((tmp_12_0 = ctx.gpsStatus()) === "idle" ? 95 : tmp_12_0 === "locating" ? 96 : tmp_12_0 === "success" ? 97 : tmp_12_0 === "failed" ? 98 : -1);
      \u0275\u0275advance(4);
      \u0275\u0275conditional((tmp_13_0 = ctx.toast()) ? 99 : -1, tmp_13_0);
    }
  }, dependencies: [DatePipe, DecimalPipe], encapsulation: 2, changeDetection: 0 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(Dashboard, [{
    type: Component,
    args: [{ selector: "app-dashboard", imports: [DatePipe, DecimalPipe], changeDetection: ChangeDetectionStrategy.OnPush, template: `<div class="container-fluid py-3">\r
\r
  <!-- Header -->\r
  <div class="flex items-center gap-3 mb-6">\r
    <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">\r
      <use href="/assets/icons/sprite.svg#clock"></use>\r
    </svg>\r
    <div>\r
      <h4 class="mb-0">\u6253\u5361\u7CFB\u7D71</h4>\r
      <p class="text-muted mb-0 small">{{ userName() }}\uFF0C\u6B61\u8FCE\u56DE\u4F86</p>\r
    </div>\r
  </div>\r
\r
  <div class="row g-4">\r
    <div class="col-12 col-lg-8">\r
\r
      <!-- Real-time Clock Card -->\r
      <div class="card border-0 shadow-sm mb-4">\r
        <div class="card-body text-center py-5">\r
          <p class="text-muted mb-2 fw-500">{{ dateDisplay() }}</p>\r
          <p class="mb-0 font-monospace" style="font-size: 3.5rem; font-weight: 700; letter-spacing: 0.05em; color: var(--text-primary)">\r
            {{ timeDisplay() }}\r
          </p>\r
        </div>\r
      </div>\r
\r
      <!-- Today Status Card -->\r
      <div class="card border-0 shadow-sm mb-4">\r
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">\r
          <svg class="sa-icon text-primary" style="stroke: currentColor">\r
            <use href="/assets/icons/sprite.svg#activity"></use>\r
          </svg>\r
          \u4ECA\u65E5\u6253\u5361\u7D00\u9304\r
        </div>\r
        <div class="card-body">\r
          <div class="row g-3 text-center">\r
            <div class="col-6 col-md-3">\r
              <div class="rounded-lg py-3" style="background: var(--accent-dim)">\r
                <svg class="sa-icon mb-1" style="stroke: var(--green)"><use href="/assets/icons/sprite.svg#log-in"></use></svg>\r
                <p class="text-muted small mb-1">\u4E0A\u73ED</p>\r
                <p class="fw-600 font-monospace mb-0">{{ formatTime(todayRecord()?.clockInTime) }}</p>\r
              </div>\r
            </div>\r
            <div class="col-6 col-md-3">\r
              <div class="rounded-lg py-3" style="background: var(--accent-dim)">\r
                <svg class="sa-icon mb-1" style="stroke: var(--yellow)"><use href="/assets/icons/sprite.svg#log-out"></use></svg>\r
                <p class="text-muted small mb-1">\u4E0B\u73ED</p>\r
                <p class="fw-600 font-monospace mb-0">{{ formatTime(todayRecord()?.clockOutTime) }}</p>\r
              </div>\r
            </div>\r
            <div class="col-6 col-md-3">\r
              <div class="rounded-lg py-3" style="background: var(--accent-dim)">\r
                <svg class="sa-icon mb-1" style="stroke: var(--purple)"><use href="/assets/icons/sprite.svg#zap"></use></svg>\r
                <p class="text-muted small mb-1">\u52A0\u73ED\u958B\u59CB</p>\r
                <p class="fw-600 font-monospace mb-0">{{ formatTime(todayRecord()?.overtimeStartTime) }}</p>\r
              </div>\r
            </div>\r
            <div class="col-6 col-md-3">\r
              <div class="rounded-lg py-3" style="background: var(--accent-dim)">\r
                <svg class="sa-icon mb-1" style="stroke: var(--red)"><use href="/assets/icons/sprite.svg#zap-off"></use></svg>\r
                <p class="text-muted small mb-1">\u52A0\u73ED\u7D50\u675F</p>\r
                <p class="fw-600 font-monospace mb-0">{{ formatTime(todayRecord()?.overtimeEndTime) }}</p>\r
              </div>\r
            </div>\r
          </div>\r
        </div>\r
      </div>\r
\r
      <!-- Clock Action Buttons -->\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">\r
          <svg class="sa-icon text-primary" style="stroke: currentColor">\r
            <use href="/assets/icons/sprite.svg#check-circle"></use>\r
          </svg>\r
          \u6253\u5361\u64CD\u4F5C\r
        </div>\r
        <div class="card-body">\r
          <div class="row g-3">\r
            <!-- Clock In -->\r
            <div class="col-6 col-md-3">\r
              <button class="btn btn-primary w-100 py-3 flex flex-col items-center gap-2"\r
                      [disabled]="!canClockIn()"\r
                      (click)="performAction('clock-in')">\r
                <svg class="sa-icon sa-icon-2x" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#log-in"></use></svg>\r
                <span class="fw-600">\u4E0A\u73ED\u6253\u5361</span>\r
              </button>\r
            </div>\r
            <!-- Clock Out -->\r
            <div class="col-6 col-md-3">\r
              <button class="btn btn-warning w-100 py-3 flex flex-col items-center gap-2"\r
                      [disabled]="!canClockOut()"\r
                      (click)="performAction('clock-out')">\r
                <svg class="sa-icon sa-icon-2x" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#log-out"></use></svg>\r
                <span class="fw-600">\u4E0B\u73ED\u6253\u5361</span>\r
              </button>\r
            </div>\r
            <!-- Overtime Start (click \u2192 show selector) -->\r
            <div class="col-6 col-md-3">\r
              <button class="btn btn-outline-primary w-100 py-3 flex flex-col items-center gap-2"\r
                      [disabled]="!canOvertimeStart()"\r
                      (click)="onOvertimeStartClick()">\r
                <svg class="sa-icon sa-icon-2x" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#zap"></use></svg>\r
                <span class="fw-600">\u52A0\u73ED\u958B\u59CB</span>\r
              </button>\r
            </div>\r
            <!-- Overtime End -->\r
            <div class="col-6 col-md-3">\r
              <button class="btn btn-outline-secondary w-100 py-3 flex flex-col items-center gap-2"\r
                      [disabled]="!canOvertimeEnd()"\r
                      (click)="performAction('overtime-end')">\r
                <svg class="sa-icon sa-icon-2x" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#zap-off"></use></svg>\r
                <span class="fw-600">\u52A0\u73ED\u7D50\u675F</span>\r
              </button>\r
            </div>\r
          </div>\r
\r
          <!-- Overtime Request Selector (shown after clicking \u52A0\u73ED\u958B\u59CB) -->\r
          @if (showOvertimeSelector()) {\r
            <div class="mt-4 pt-3 border-t">\r
              <label class="form-label fw-600">\u9078\u64C7\u52A0\u73ED\u7533\u8ACB\u55AE</label>\r
              <select class="form-select mb-3" (change)="onOvertimeSelect($event)">\r
                @for (req of approvedRequests(); track req.id) {\r
                  <option [value]="req.id" [selected]="req.id === selectedOvertimeId()">\r
                    {{ req.overtimeDate | date:'MM/dd' }} \u2014 {{ req.projectCodes?.join(', ') || '\u7121\u5C08\u6848' }} ({{ req.estimatedHours }}h)\r
                  </option>\r
                }\r
              </select>\r
              <div class="flex gap-2">\r
                <button class="btn btn-primary btn-sm" (click)="confirmOvertimeStart()">\u78BA\u8A8D\u6253\u5361</button>\r
                <button class="btn btn-outline-secondary btn-sm" (click)="cancelOvertimeSelector()">\u53D6\u6D88</button>\r
              </div>\r
            </div>\r
          }\r
        </div>\r
      </div>\r
    </div>\r
\r
    <!-- Right sidebar: GPS status -->\r
    <div class="col-12 col-lg-4">\r
      <div class="card border-0 shadow-sm">\r
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">\r
          <svg class="sa-icon text-primary" style="stroke: currentColor">\r
            <use href="/assets/icons/sprite.svg#map-pin"></use>\r
          </svg>\r
          \u5B9A\u4F4D\u8CC7\u8A0A\r
        </div>\r
        <div class="card-body">\r
          @switch (gpsStatus()) {\r
            @case ('idle') {\r
              <p class="text-muted small mb-0">\u6253\u5361\u6642\u81EA\u52D5\u53D6\u5F97 GPS \u5EA7\u6A19\u3002</p>\r
            }\r
            @case ('locating') {\r
              <div class="flex items-center gap-2 text-muted">\r
                <div class="spinner-border spinner-border-sm"></div>\r
                <span class="small">\u5B9A\u4F4D\u4E2D\u2026</span>\r
              </div>\r
            }\r
            @case ('success') {\r
              <div class="flex items-center gap-2 mb-2" style="color: var(--green)">\r
                <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#check-circle"></use></svg>\r
                <span class="small fw-500">\u5B9A\u4F4D\u6210\u529F</span>\r
              </div>\r
              @if (gpsCoords()) {\r
                <p class="font-monospace small text-muted mb-0">\r
                  {{ gpsCoords()!.lat | number:'1.6-6' }}, {{ gpsCoords()!.lng | number:'1.6-6' }}\r
                </p>\r
              }\r
            }\r
            @case ('failed') {\r
              <div class="flex items-center gap-2" style="color: var(--yellow)">\r
                <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>\r
                <span class="small fw-500">\u7121\u6CD5\u53D6\u5F97\u5B9A\u4F4D\uFF08\u6253\u5361\u4ECD\u6709\u6548\uFF09</span>\r
              </div>\r
            }\r
          }\r
        </div>\r
      </div>\r
    </div>\r
  </div>\r
\r
  <!-- Toast notification -->\r
  @if (toast(); as t) {\r
    <div class="position-fixed bottom-0 end-0 p-4" style="z-index: 1080">\r
      <div class="px-4 py-3 rounded-lg shadow-lg fw-500"\r
           [class]="t.type === 'success' ? 'bg-success text-white' : t.type === 'warning' ? 'bg-warning text-dark' : 'bg-danger text-white'">\r
        {{ t.message }}\r
      </div>\r
    </div>\r
  }\r
\r
</div>\r
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(Dashboard, { className: "Dashboard", filePath: "src/app/features/dashboard/pages/dashboard/dashboard.ts", lineNumber: 17 });
})();

// src/app/features/dashboard/dashboard.routes.ts
var DASHBOARD_ROUTES = [
  {
    path: "",
    component: Dashboard,
    data: { title: "Dashboard" }
  }
];
export {
  DASHBOARD_ROUTES
};
//# sourceMappingURL=chunk-HMEUNERZ.js.map
