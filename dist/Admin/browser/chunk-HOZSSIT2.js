import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS
} from "./chunk-LBNMAGIZ.js";
import {
  TravelPaymentRequestService
} from "./chunk-JB2CXS62.js";
import {
  toObservable,
  toSignal
} from "./chunk-Y7L4DWFX.js";
import {
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
  PAYMENT_INSTALLMENT_STATUS_LABELS
} from "./chunk-KP52QFLC.js";
import {
  HasPermissionDirective
} from "./chunk-M7DGJIC4.js";
import {
  RouterLink
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  AuthService
} from "./chunk-ZSGTQ3YJ.js";
import {
  Component,
  DatePipe,
  DecimalPipe,
  computed,
  inject,
  setClassMetadata,
  signal,
  switchMap,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassMap,
  ɵɵclassProp,
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
  ɵɵpipe,
  ɵɵpipeBind2,
  ɵɵproperty,
  ɵɵpureFunction1,
  ɵɵrepeater,
  ɵɵrepeaterCreate,
  ɵɵrepeaterTrackByIdentity,
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtemplate,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate2,
  ɵɵtextInterpolate3
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/travel-payment-requests/pages/travel-payment-list/travel-payment-list.ts
var _c0 = (a0) => [a0, "edit"];
var _c1 = (a0) => [a0];
var _forTrack0 = ($index, $item) => $item.id;
function TravelPaymentList_a_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 16);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 17);
    \u0275\u0275element(2, "use", 18);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u65B0\u589E\u7533\u8ACB ");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentList_For_32_Conditional_20_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span");
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ist_r1 = ctx;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275classMap("badge " + ctx_r1.installmentStatusClass[ist_r1]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.installmentStatusLabel[ist_r1]);
  }
}
function TravelPaymentList_For_32_Conditional_23_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r3 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 32);
    \u0275\u0275listener("click", function TravelPaymentList_For_32_Conditional_23_Conditional_3_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r3);
      const r_r4 = \u0275\u0275nextContext(2).$implicit;
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.delete(r_r4));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 17);
    \u0275\u0275element(2, "use", 33);
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentList_For_32_Conditional_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 29);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 17);
    \u0275\u0275element(2, "use", 30);
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(3, TravelPaymentList_For_32_Conditional_23_Conditional_3_Template, 3, 0, "button", 31);
  }
  if (rf & 2) {
    const r_r4 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(2, _c0, r_r4.id));
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.canDelete() && r_r4.approvalStatus === "draft" ? 3 : -1);
  }
}
function TravelPaymentList_For_32_Conditional_24_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 28);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 17);
    \u0275\u0275element(2, "use", 34);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r4 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c1, r_r4.id));
  }
}
function TravelPaymentList_For_32_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 19);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 20);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "td", 20);
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 21);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 22);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 23);
    \u0275\u0275text(15);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(16, "td")(17, "div", 24)(18, "span");
    \u0275\u0275text(19);
    \u0275\u0275elementEnd();
    \u0275\u0275conditionalCreate(20, TravelPaymentList_For_32_Conditional_20_Template, 2, 3, "span", 25);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(21, "td", 26)(22, "div", 27);
    \u0275\u0275conditionalCreate(23, TravelPaymentList_For_32_Conditional_23_Template, 4, 4)(24, TravelPaymentList_For_32_Conditional_24_Template, 3, 3, "a", 28);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    let tmp_18_0;
    const r_r4 = ctx.$implicit;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(r_r4.destination);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(5, 11, r_r4.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(8, 14, r_r4.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 17, r_r4.grandTotal, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(r_r4.purpose);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(r_r4.projectCode || "\u2014");
    \u0275\u0275advance(3);
    \u0275\u0275classMap("badge " + ctx_r1.statusClass[r_r4.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.statusLabel[r_r4.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_18_0 = ctx_r1.installmentStatusOf(r_r4)) ? 20 : -1, tmp_18_0);
    \u0275\u0275advance(3);
    \u0275\u0275conditional(ctx_r1.canWrite() && (r_r4.approvalStatus === "draft" || r_r4.approvalStatus === "returned") ? 23 : 24);
  }
}
function TravelPaymentList_ForEmpty_33_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 35);
    \u0275\u0275text(2, "\u5C1A\u7121\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\u3002");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentList_Conditional_34_For_15_Conditional_0_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 43)(1, "span", 45);
    \u0275\u0275text(2, "\u2026");
    \u0275\u0275elementEnd()();
  }
}
function TravelPaymentList_Conditional_34_For_15_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "li", 41)(1, "button", 42);
    \u0275\u0275listener("click", function TravelPaymentList_Conditional_34_For_15_Conditional_1_Template_button_click_1_listener() {
      \u0275\u0275restoreView(_r6);
      const p_r7 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.goTo(p_r7));
    });
    \u0275\u0275text(2);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const p_r7 = \u0275\u0275nextContext().$implicit;
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275classProp("active", p_r7 === ctx_r1.page());
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(p_r7);
  }
}
function TravelPaymentList_Conditional_34_For_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275conditionalCreate(0, TravelPaymentList_Conditional_34_For_15_Conditional_0_Template, 3, 0, "li", 43)(1, TravelPaymentList_Conditional_34_For_15_Conditional_1_Template, 3, 3, "li", 44);
  }
  if (rf & 2) {
    const p_r7 = ctx.$implicit;
    \u0275\u0275conditional(p_r7 === -1 ? 0 : 1);
  }
}
function TravelPaymentList_Conditional_34_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "div", 15)(1, "span", 36);
    \u0275\u0275text(2);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 37)(4, "button", 38);
    \u0275\u0275listener("click", function TravelPaymentList_Conditional_34_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.prev());
    });
    \u0275\u0275text(5, "\u2039");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(6, "span", 39);
    \u0275\u0275text(7);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "button", 38);
    \u0275\u0275listener("click", function TravelPaymentList_Conditional_34_Template_button_click_8_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.next());
    });
    \u0275\u0275text(9, "\u203A");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(10, "ul", 40)(11, "li", 41)(12, "button", 42);
    \u0275\u0275listener("click", function TravelPaymentList_Conditional_34_Template_button_click_12_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.prev());
    });
    \u0275\u0275text(13, "\u2039");
    \u0275\u0275elementEnd()();
    \u0275\u0275repeaterCreate(14, TravelPaymentList_Conditional_34_For_15_Template, 2, 1, null, null, \u0275\u0275repeaterTrackByIdentity);
    \u0275\u0275elementStart(16, "li", 41)(17, "button", 42);
    \u0275\u0275listener("click", function TravelPaymentList_Conditional_34_Template_button_click_17_listener() {
      \u0275\u0275restoreView(_r5);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.next());
    });
    \u0275\u0275text(18, "\u203A");
    \u0275\u0275elementEnd()()()();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate3("\u5171 ", ctx_r1.totalCount(), " \u7B46\uFF0C\u7B2C ", ctx_r1.page(), " / ", ctx_r1.totalPages(), " \u9801");
    \u0275\u0275advance(2);
    \u0275\u0275classProp("disabled", ctx_r1.page() === 1);
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate2("", ctx_r1.page(), " / ", ctx_r1.totalPages());
    \u0275\u0275advance();
    \u0275\u0275classProp("disabled", ctx_r1.page() === ctx_r1.totalPages());
    \u0275\u0275advance(3);
    \u0275\u0275classProp("disabled", ctx_r1.page() === 1);
    \u0275\u0275advance(3);
    \u0275\u0275repeater(ctx_r1.pageNumbers());
    \u0275\u0275advance(2);
    \u0275\u0275classProp("disabled", ctx_r1.page() === ctx_r1.totalPages());
  }
}
var TravelPaymentList = class _TravelPaymentList {
  service = inject(TravelPaymentRequestService);
  auth = inject(AuthService);
  canWrite() {
    return this.auth.hasPermission("travel-payment-requests:write");
  }
  canDelete() {
    return this.auth.hasPermission("travel-payment-requests:delete");
  }
  PAGE_SIZE = 20;
  page = signal(1, ...ngDevMode ? [{ debugName: "page" }] : []);
  refresh = signal(0, ...ngDevMode ? [{ debugName: "refresh" }] : []);
  result = toSignal(toObservable(computed(() => ({ page: this.page(), refresh: this.refresh() }))).pipe(switchMap(({ page }) => this.service.getPaged(page, this.PAGE_SIZE))), { initialValue: { items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1 } });
  pagedRequests = computed(() => this.result().items, ...ngDevMode ? [{ debugName: "pagedRequests" }] : []);
  totalCount = computed(() => this.result().totalCount, ...ngDevMode ? [{ debugName: "totalCount" }] : []);
  totalPages = computed(() => this.result().totalPages, ...ngDevMode ? [{ debugName: "totalPages" }] : []);
  pageNumbers = computed(() => buildPageNumbers(this.page(), this.totalPages()), ...ngDevMode ? [{ debugName: "pageNumbers" }] : []);
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
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  installmentStatusLabel = PAYMENT_INSTALLMENT_STATUS_LABELS;
  installmentStatusClass = PAYMENT_INSTALLMENT_STATUS_CLASSES;
  /** 分期撥款三態 badge（核准後才顯示）*/
  installmentStatusOf(r) {
    if (r.approvalStatus !== "approved")
      return null;
    return r.paymentStatus ?? null;
  }
  delete(r) {
    if (confirm(`\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\u55CE\uFF1F`)) {
      this.service.delete(r.id).subscribe(() => this.refresh.update((v) => v + 1));
    }
  }
  static \u0275fac = function TravelPaymentList_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelPaymentList)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _TravelPaymentList, selectors: [["app-travel-payment-list"]], decls: 35, vars: 3, consts: [[1, "container-fluid", "py-3"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2"], [1, "sa-icon", "sa-icon-2x", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "mb-0"], ["routerLink", "new", "class", "btn btn-primary inline-flex items-center gap-1", 4, "appHasPermission"], [1, "card", "border-0", "shadow-sm"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-hover", "mb-0"], [1, "table-light"], [1, "text-right"], [1, "hidden", "md:table-cell"], [1, "hidden", "lg:table-cell"], [1, "flex", "flex-col", "gap-2", "sm:flex-row", "sm:items-center", "sm:justify-between", "px-4", "py-3", "border-t"], ["routerLink", "new", 1, "btn", "btn-primary", "inline-flex", "items-center", "gap-1"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#plus"], [1, "fw-500"], [1, "text-muted", "small"], [1, "text-right", "fw-500"], [1, "text-muted", "small", "hidden", "md:table-cell"], [1, "font-monospace", "small", "hidden", "lg:table-cell"], [1, "flex", "flex-wrap", "items-center", "gap-1"], [3, "class"], [1, "text-right", 2, "white-space", "nowrap"], [1, "flex", "justify-end", "gap-1"], ["title", "\u6AA2\u8996", 1, "btn", "btn-sm", "btn-ghost-secondary", "inline-flex", "items-center", 3, "routerLink"], ["title", "\u7DE8\u8F2F", 1, "btn", "btn-sm", "btn-ghost-primary", "inline-flex", "items-center", 3, "routerLink"], ["href", "/assets/icons/sprite.svg#edit"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "inline-flex", "items-center"], ["title", "\u522A\u9664", 1, "btn", "btn-sm", "btn-ghost-danger", "inline-flex", "items-center", 3, "click"], ["href", "/assets/icons/sprite.svg#trash"], ["href", "/assets/icons/sprite.svg#eye"], ["colspan", "8", 1, "text-center", "text-muted", "py-4"], [1, "text-muted", "small", "text-center", "sm:text-left"], [1, "flex", "sm:hidden", "items-center", "gap-1"], [1, "page-link", "rounded", 3, "click"], [1, "px-2", "text-sm"], [1, "hidden", "sm:flex", "pagination", "mb-0"], [1, "page-item"], [1, "page-link", 3, "click"], [1, "page-item", "disabled"], [1, "page-item", 3, "active"], [1, "page-link"]], template: function TravelPaymentList_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0)(1, "div", 1)(2, "div", 2);
      \u0275\u0275namespaceSVG();
      \u0275\u0275elementStart(3, "svg", 3);
      \u0275\u0275element(4, "use", 4);
      \u0275\u0275elementEnd();
      \u0275\u0275namespaceHTML();
      \u0275\u0275elementStart(5, "h4", 5);
      \u0275\u0275text(6, "\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB");
      \u0275\u0275elementEnd()();
      \u0275\u0275template(7, TravelPaymentList_a_7_Template, 4, 0, "a", 6);
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(8, "div", 7)(9, "div", 8)(10, "div", 9)(11, "table", 10)(12, "thead", 11)(13, "tr")(14, "th");
      \u0275\u0275text(15, "\u51FA\u5DEE\u5730\u9EDE");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(16, "th");
      \u0275\u0275text(17, "\u958B\u59CB\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(18, "th");
      \u0275\u0275text(19, "\u7D50\u675F\u65E5\u671F");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(20, "th", 12);
      \u0275\u0275text(21, "\u91D1\u984D");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(22, "th", 13);
      \u0275\u0275text(23, "\u76EE\u7684");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(24, "th", 14);
      \u0275\u0275text(25, "\u5C08\u6848");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(26, "th");
      \u0275\u0275text(27, "\u72C0\u614B");
      \u0275\u0275elementEnd();
      \u0275\u0275elementStart(28, "th", 12);
      \u0275\u0275text(29, "\u64CD\u4F5C");
      \u0275\u0275elementEnd()()();
      \u0275\u0275elementStart(30, "tbody");
      \u0275\u0275repeaterCreate(31, TravelPaymentList_For_32_Template, 25, 20, "tr", null, _forTrack0, false, TravelPaymentList_ForEmpty_33_Template, 3, 0, "tr");
      \u0275\u0275elementEnd()()();
      \u0275\u0275conditionalCreate(34, TravelPaymentList_Conditional_34_Template, 19, 13, "div", 15);
      \u0275\u0275elementEnd()()();
    }
    if (rf & 2) {
      \u0275\u0275advance(7);
      \u0275\u0275property("appHasPermission", "travel-payment-requests:write");
      \u0275\u0275advance(24);
      \u0275\u0275repeater(ctx.pagedRequests());
      \u0275\u0275advance(3);
      \u0275\u0275conditional(ctx.totalPages() > 1 ? 34 : -1);
    }
  }, dependencies: [RouterLink, HasPermissionDirective, DatePipe, DecimalPipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelPaymentList, [{
    type: Component,
    args: [{ selector: "app-travel-payment-list", imports: [RouterLink, DatePipe, DecimalPipe, HasPermissionDirective], template: `<div class="container-fluid py-3">
  <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
    <div class="flex items-center gap-2">
      <svg class="sa-icon sa-icon-2x text-primary" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#map-pin"></use>
      </svg>
      <h4 class="mb-0">\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB</h4>
    </div>
    <a *appHasPermission="'travel-payment-requests:write'" routerLink="new" class="btn btn-primary inline-flex items-center gap-1">
      <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
      \u65B0\u589E\u7533\u8ACB
    </a>
  </div>

  <div class="card border-0 shadow-sm">
    <div class="card-body p-0">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead class="table-light">
            <tr>
              <th>\u51FA\u5DEE\u5730\u9EDE</th>
              <th>\u958B\u59CB\u65E5\u671F</th>
              <th>\u7D50\u675F\u65E5\u671F</th>
              <th class="text-right">\u91D1\u984D</th>
              <th class="hidden md:table-cell">\u76EE\u7684</th>
              <th class="hidden lg:table-cell">\u5C08\u6848</th>
              <th>\u72C0\u614B</th>
              <th class="text-right">\u64CD\u4F5C</th>
            </tr>
          </thead>
          <tbody>
            @for (r of pagedRequests(); track r.id) {
              <tr>
                <td class="fw-500">{{ r.destination }}</td>
                <td class="text-muted small">{{ r.startDate | date:'yyyy-MM-dd' }}</td>
                <td class="text-muted small">{{ r.endDate | date:'yyyy-MM-dd' }}</td>
                <td class="text-right fw-500">{{ r.grandTotal | number:'1.0-0' }}</td>
                <td class="text-muted small hidden md:table-cell">{{ r.purpose }}</td>
                <td class="font-monospace small hidden lg:table-cell">{{ r.projectCode || '\u2014' }}</td>
                <td>
                  <div class="flex flex-wrap items-center gap-1">
                    <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>
                    @if (installmentStatusOf(r); as ist) {
                      <span [class]="'badge ' + installmentStatusClass[ist]">{{ installmentStatusLabel[ist] }}</span>
                    }
                  </div>
                </td>
                <td class="text-right" style="white-space: nowrap">
                  <div class="flex justify-end gap-1">
                    @if (canWrite() && (r.approvalStatus === 'draft' || r.approvalStatus === 'returned')) {
                      <a [routerLink]="[r.id, 'edit']" class="btn btn-sm btn-ghost-primary inline-flex items-center" title="\u7DE8\u8F2F">
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
                      </a>
                      @if (canDelete() && r.approvalStatus === 'draft') {
                        <button class="btn btn-sm btn-ghost-danger inline-flex items-center" (click)="delete(r)" title="\u522A\u9664">
                          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash"></use></svg>
                        </button>
                      }
                    } @else {
                      <a [routerLink]="[r.id]" class="btn btn-sm btn-ghost-secondary inline-flex items-center" title="\u6AA2\u8996">
                        <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#eye"></use></svg>
                      </a>
                    }
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="8" class="text-center text-muted py-4">\u5C1A\u7121\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\u3002</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (totalPages() > 1) {
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between px-4 py-3 border-t">
          <span class="text-muted small text-center sm:text-left">\u5171 {{ totalCount() }} \u7B46\uFF0C\u7B2C {{ page() }} / {{ totalPages() }} \u9801</span>
          <div class="flex sm:hidden items-center gap-1">
            <button class="page-link rounded" [class.disabled]="page() === 1" (click)="prev()">\u2039</button>
            <span class="px-2 text-sm">{{ page() }} / {{ totalPages() }}</span>
            <button class="page-link rounded" [class.disabled]="page() === totalPages()" (click)="next()">\u203A</button>
          </div>
          <ul class="hidden sm:flex pagination mb-0">
            <li class="page-item" [class.disabled]="page() === 1">
              <button class="page-link" (click)="prev()">\u2039</button>
            </li>
            @for (p of pageNumbers(); track p) {
              @if (p === -1) {
                <li class="page-item disabled"><span class="page-link">\u2026</span></li>
              } @else {
                <li class="page-item" [class.active]="p === page()">
                  <button class="page-link" (click)="goTo(p)">{{ p }}</button>
                </li>
              }
            }
            <li class="page-item" [class.disabled]="page() === totalPages()">
              <button class="page-link" (click)="next()">\u203A</button>
            </li>
          </ul>
        </div>
      }
    </div>
  </div>
</div>
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(TravelPaymentList, { className: "TravelPaymentList", filePath: "src/app/features/admin/travel-payment-requests/pages/travel-payment-list/travel-payment-list.ts", lineNumber: 25 });
})();
function buildPageNumbers(current, total) {
  if (total <= 9)
    return Array.from({ length: total }, (_, i) => i + 1);
  const pages = [];
  let prev = 0;
  for (let i = 1; i <= total; i++) {
    if (i === 1 || i === total || i >= current - 2 && i <= current + 2) {
      if (prev && i - prev > 1)
        pages.push(-1);
      pages.push(i);
      prev = i;
    }
  }
  return pages;
}
export {
  TravelPaymentList
};
//# sourceMappingURL=chunk-HOZSSIT2.js.map
