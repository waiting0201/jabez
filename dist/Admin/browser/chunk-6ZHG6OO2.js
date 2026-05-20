import {
  TravelPaymentPdfService
} from "./chunk-ZAYEBMQK.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS
} from "./chunk-BWAYLXX3.js";
import {
  TravelPaymentRequestService
} from "./chunk-MKTMSFDP.js";
import {
  FilePreviewModal
} from "./chunk-GWKNDEFV.js";
import "./chunk-346USOMS.js";
import {
  InstallmentsTable
} from "./chunk-KNQM2RNK.js";
import "./chunk-YGQK3CZP.js";
import {
  ApprovalTimeline
} from "./chunk-B4OWGIJG.js";
import {
  ApprovalTaskService
} from "./chunk-HXO5P7BO.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-DUW2WF5C.js";
import {
  DomSanitizer
} from "./chunk-JDEYLUO2.js";
import {
  Component,
  DatePipe,
  DecimalPipe,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵelement,
  ɵɵelementEnd,
  ɵɵelementStart,
  ɵɵgetCurrentView,
  ɵɵinterpolate,
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
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate2
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/travel-payment-requests/pages/travel-payment-detail/travel-payment-detail.ts
var _c0 = (a0) => ["/admin/travel-payment-requests", a0, "edit"];
var _forTrack0 = ($index, $item) => $item.id;
function TravelPaymentDetail_Conditional_1_Conditional_12_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 35);
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_12_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 14);
    \u0275\u0275element(1, "use", 36);
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 34);
    \u0275\u0275listener("click", function TravelPaymentDetail_Conditional_1_Conditional_12_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printRequest());
    });
    \u0275\u0275conditionalCreate(1, TravelPaymentDetail_Conditional_1_Conditional_12_Conditional_1_Template, 1, 0, "span", 35)(2, TravelPaymentDetail_Conditional_1_Conditional_12_Conditional_2_Template, 2, 0, ":svg:svg", 14);
    \u0275\u0275text(3, " \u5217\u5370 PDF ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275property("disabled", ctx_r1.pdfLoading());
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.pdfLoading() ? 1 : 2);
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_13_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 12);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 37);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u7DE8\u8F2F ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c0, r_r3.id));
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_14_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 35);
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_14_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 14);
    \u0275\u0275element(1, "use", 41);
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 38);
    \u0275\u0275listener("click", function TravelPaymentDetail_Conditional_1_Conditional_14_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.submitRequest());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 39);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(4, "button", 40);
    \u0275\u0275listener("click", function TravelPaymentDetail_Conditional_1_Conditional_14_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.deleteRequest());
    });
    \u0275\u0275conditionalCreate(5, TravelPaymentDetail_Conditional_1_Conditional_14_Conditional_5_Template, 1, 0, "span", 35)(6, TravelPaymentDetail_Conditional_1_Conditional_14_Conditional_6_Template, 2, 0, ":svg:svg", 14);
    \u0275\u0275text(7, " \u522A\u9664 ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(4);
    \u0275\u0275property("disabled", ctx_r1.deleting());
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.deleting() ? 5 : 6);
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_57_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 27)(1, "div", 25);
    \u0275\u0275text(2, "\u95DC\u806F\u5C08\u6848");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 42);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate2("", r_r3.projectCode, "", r_r3.projectName ? " - " + r_r3.projectName : "");
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_70_For_31_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 59);
    \u0275\u0275listener("click", function TravelPaymentDetail_Conditional_1_Conditional_70_For_31_Conditional_2_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r5);
      const item_r6 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.openPreview(item_r6.fileName || "\u767C\u7968", item_r6.fileUrl));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 60);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const item_r6 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("title", \u0275\u0275interpolate(item_r6.fileName));
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_70_For_31_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 52);
    \u0275\u0275conditionalCreate(2, TravelPaymentDetail_Conditional_1_Conditional_70_For_31_Conditional_2_Template, 3, 2, "button", 53);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 54);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 54);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 54);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 55);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 54);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 56);
    \u0275\u0275text(15);
    \u0275\u0275pipe(16, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "td", 57);
    \u0275\u0275text(18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "td", 58);
    \u0275\u0275text(20);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "td", 54);
    \u0275\u0275text(22);
    \u0275\u0275pipe(23, "date");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const item_r6 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275conditional(item_r6.fileUrl ? 2 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.category);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.seqNo);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.itemName);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 10, item_r6.unitPrice, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r6.quantity);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(16, 13, item_r6.totalPrice, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r6.note || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.invoiceNo || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r6.invoiceDate ? \u0275\u0275pipeBind2(23, 16, item_r6.invoiceDate, "yyyy-MM-dd") : "\u2014");
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_70_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18)(1, "div", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 20);
    \u0275\u0275element(3, "use", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u8CBB\u7528\u660E\u7D30 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 43)(6, "div", 44)(7, "table", 45)(8, "thead", 46)(9, "tr");
    \u0275\u0275element(10, "th", 47);
    \u0275\u0275elementStart(11, "th");
    \u0275\u0275text(12, "\u5206\u985E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "th");
    \u0275\u0275text(14, "\u9805\u6B21");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "th");
    \u0275\u0275text(16, "\u9805\u76EE\u8AAA\u660E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "th", 48);
    \u0275\u0275text(18, "\u55AE\u50F9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "th");
    \u0275\u0275text(20, "\u6578\u91CF/\u55AE\u4F4D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "th", 48);
    \u0275\u0275text(22, "\u7E3D\u50F9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "th");
    \u0275\u0275text(24, "\u5099\u8A3B");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "th");
    \u0275\u0275text(26, "\u767C\u7968\u865F\u78BC");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(27, "th");
    \u0275\u0275text(28, "\u767C\u7968\u65E5\u671F");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(29, "tbody");
    \u0275\u0275repeaterCreate(30, TravelPaymentDetail_Conditional_1_Conditional_70_For_31_Template, 24, 19, "tr", null, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(32, "tfoot")(33, "tr", 46)(34, "td", 49);
    \u0275\u0275text(35, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(36, "td", 50);
    \u0275\u0275text(37);
    \u0275\u0275pipe(38, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(39, "td", 51);
    \u0275\u0275elementEnd()()()()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(30);
    \u0275\u0275repeater(r_r3.items);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(38, 1, r_r3.grandTotal, "1.0-0"));
  }
}
function TravelPaymentDetail_Conditional_1_Conditional_72_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "app-approval-timeline", 33);
  }
  if (rf & 2) {
    const task_r7 = ctx;
    \u0275\u0275property("flow", task_r7.flow ?? null)("approvalRecords", task_r7.approvalRecords)("currentStepOrder", task_r7.currentStepOrder)("status", task_r7.status);
  }
}
function TravelPaymentDetail_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 3)(1, "div", 4)(2, "a", 5);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 6);
    \u0275\u0275element(4, "use", 7);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h4", 8);
    \u0275\u0275text(6, "\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB ");
    \u0275\u0275elementStart(7, "span", 9);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "span");
    \u0275\u0275text(10);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(11, "div", 10);
    \u0275\u0275conditionalCreate(12, TravelPaymentDetail_Conditional_1_Conditional_12_Template, 4, 2, "button", 11);
    \u0275\u0275conditionalCreate(13, TravelPaymentDetail_Conditional_1_Conditional_13_Template, 4, 3, "a", 12);
    \u0275\u0275conditionalCreate(14, TravelPaymentDetail_Conditional_1_Conditional_14_Template, 8, 2);
    \u0275\u0275elementStart(15, "a", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(16, "svg", 14);
    \u0275\u0275element(17, "use", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275text(18, " \u8FD4\u56DE\u5217\u8868 ");
    \u0275\u0275elementEnd()()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(19, "div", 16)(20, "div", 17)(21, "div", 18)(22, "div", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(23, "svg", 20);
    \u0275\u0275element(24, "use", 21);
    \u0275\u0275elementEnd();
    \u0275\u0275text(25, " \u51FA\u5DEE\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(26, "div", 22)(27, "div", 23)(28, "div", 24)(29, "div", 25);
    \u0275\u0275text(30, "\u51FA\u5DEE\u5730\u9EDE");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(31, "div", 26);
    \u0275\u0275text(32);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(33, "div", 27)(34, "div", 25);
    \u0275\u0275text(35, "\u7533\u8ACB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(36, "div", 28);
    \u0275\u0275text(37);
    \u0275\u0275pipe(38, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(39, "div", 27)(40, "div", 25);
    \u0275\u0275text(41, "\u7533\u8ACB\u4EBA");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(42, "div", 28);
    \u0275\u0275text(43);
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(44, "div", 23)(45, "div", 27)(46, "div", 25);
    \u0275\u0275text(47, "\u958B\u59CB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(48, "div", 28);
    \u0275\u0275text(49);
    \u0275\u0275pipe(50, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(51, "div", 27)(52, "div", 25);
    \u0275\u0275text(53, "\u7D50\u675F\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(54, "div", 28);
    \u0275\u0275text(55);
    \u0275\u0275pipe(56, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(57, TravelPaymentDetail_Conditional_1_Conditional_57_Template, 5, 2, "div", 27);
    \u0275\u0275elementStart(58, "div", 27)(59, "div", 25);
    \u0275\u0275text(60, "\u91D1\u984D\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(61, "div", 29);
    \u0275\u0275text(62);
    \u0275\u0275pipe(63, "number");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(64, "div", 30)(65, "div", 31)(66, "div", 25);
    \u0275\u0275text(67, "\u51FA\u5DEE\u76EE\u7684");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(68, "div", 28);
    \u0275\u0275text(69);
    \u0275\u0275elementEnd()()()()();
    \u0275\u0275conditionalCreate(70, TravelPaymentDetail_Conditional_1_Conditional_70_Template, 40, 4, "div", 18);
    \u0275\u0275element(71, "app-installments-table", 32);
    \u0275\u0275conditionalCreate(72, TravelPaymentDetail_Conditional_1_Conditional_72_Template, 1, 4, "app-approval-timeline", 33);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_20_0;
    const r_r3 = ctx;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(8);
    \u0275\u0275textInterpolate(r_r3.requestNo);
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + ctx_r1.statusClass[r_r3.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.statusLabel[r_r3.approvalStatus]);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.approvalStatus === "approved" ? 12 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" || r_r3.approvalStatus === "returned" ? 13 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" ? 14 : -1);
    \u0275\u0275advance(18);
    \u0275\u0275textInterpolate(r_r3.destination);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(38, 20, r_r3.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(r_r3.employeeName || "\u2014");
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(50, 23, r_r3.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(56, 26, r_r3.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.projectCode || r_r3.projectName ? 57 : -1);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(63, 29, r_r3.grandTotal, "1.0-0"));
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(r_r3.purpose);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.items && r_r3.items.length > 0 ? 70 : -1);
    \u0275\u0275advance();
    \u0275\u0275property("installmentsInput", r_r3.installments)("paymentStatus", r_r3.paymentStatus)("totalAmount", r_r3.grandTotal);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_20_0 = ctx_r1.approvalTask()) ? 72 : -1, tmp_20_0);
  }
}
function TravelPaymentDetail_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 1);
    \u0275\u0275text(1, "\u8F09\u5165\u4E2D\u2026");
    \u0275\u0275elementEnd();
  }
}
function TravelPaymentDetail_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r8 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "app-file-preview-modal", 61);
    \u0275\u0275listener("closed", function TravelPaymentDetail_Conditional_3_Template_app_file_preview_modal_closed_0_listener() {
      \u0275\u0275restoreView(_r8);
      const ctx_r1 = \u0275\u0275nextContext();
      return \u0275\u0275resetView(ctx_r1.closePreview());
    });
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275property("file", ctx_r1.previewFile);
  }
}
var TravelPaymentDetail = class _TravelPaymentDetail {
  service = inject(TravelPaymentRequestService);
  pdfService = inject(TravelPaymentPdfService);
  taskService = inject(ApprovalTaskService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  sanitizer = inject(DomSanitizer);
  request = signal(null, ...ngDevMode ? [{ debugName: "request" }] : []);
  approvalTask = signal(null, ...ngDevMode ? [{ debugName: "approvalTask" }] : []);
  deleting = signal(false, ...ngDevMode ? [{ debugName: "deleting" }] : []);
  /** 檔案預覽 modal */
  previewFile = null;
  openPreview(name, url) {
    if (!url)
      return;
    this.previewFile = { name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url) };
  }
  closePreview() {
    this.previewFile = null;
  }
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get("id");
    this.service.getById(id).subscribe((r) => {
      this.request.set(r);
    });
    this.taskService.getById(id, "travel_payment").subscribe({
      next: (t) => this.approvalTask.set(t),
      error: () => {
      }
      // draft 狀態可能尚無簽核記錄
    });
  }
  get pdfLoading() {
    return this.pdfService.pdfLoading;
  }
  printRequest() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) {
      this.pdfService.printTravelPaymentRequest(r, t?.submittedBy ?? "", t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
    }
  }
  submitRequest() {
    const r = this.request();
    if (!r)
      return;
    this.service.submit(r.id).subscribe((updated) => {
      this.request.set(updated);
    });
  }
  deleteRequest() {
    const r = this.request();
    if (!r || !confirm("\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\uFF1F"))
      return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(["/admin/travel-payment-requests"]),
      error: () => this.deleting.set(false)
    });
  }
  static \u0275fac = function TravelPaymentDetail_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelPaymentDetail)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _TravelPaymentDetail, selectors: [["app-travel-payment-detail"]], decls: 4, vars: 2, consts: [[1, "container-fluid", "py-3"], [1, "text-center", "py-6", "text-muted"], [3, "file"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2", "flex-wrap"], ["routerLink", "/admin/travel-payment-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "font-monospace", "text-muted"], [1, "flex", "flex-wrap", "gap-2"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "disabled"], [1, "btn", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "routerLink"], ["routerLink", "/admin/travel-payment-requests", 1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#list"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm", "mb-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#map-pin"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "text-muted", "small"], [1, "fw-600"], [1, "col-6", "col-md-3"], [1, "fw-500"], [1, "fw-600", "text-lg"], [1, "row", "g-3", "mb-0"], [1, "col-12"], [3, "installmentsInput", "paymentStatus", "totalAmount"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["role", "status", 1, "spinner-border", "spinner-border-sm"], ["href", "/assets/icons/sprite.svg#printer"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#send"], [1, "btn", "btn-outline-danger", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["href", "/assets/icons/sprite.svg#trash-2"], [1, "fw-500", "font-monospace"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "width", "40px"], [1, "text-right"], ["colspan", "6", 1, "text-right", "fw-500", "small"], [1, "text-right", "fw-600"], ["colspan", "3"], [1, "align-middle", "text-center"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "title"], [1, "small"], [1, "text-right", "small"], [1, "text-right", "small", "fw-500"], [1, "small", "text-muted"], [1, "small", "font-monospace"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "click", "title"], ["href", "/assets/icons/sprite.svg#file-text"], [3, "closed", "file"]], template: function TravelPaymentDetail_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, TravelPaymentDetail_Conditional_1_Template, 73, 32)(2, TravelPaymentDetail_Conditional_2_Template, 2, 0, "div", 1);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(3, TravelPaymentDetail_Conditional_3_Template, 1, 1, "app-file-preview-modal", 2);
    }
    if (rf & 2) {
      let tmp_0_0;
      \u0275\u0275advance();
      \u0275\u0275conditional((tmp_0_0 = ctx.request()) ? 1 : 2, tmp_0_0);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.previewFile ? 3 : -1);
    }
  }, dependencies: [RouterLink, ApprovalTimeline, FilePreviewModal, InstallmentsTable, DecimalPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelPaymentDetail, [{
    type: Component,
    args: [{ selector: "app-travel-payment-detail", imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable], template: `<div class="container-fluid py-3">
  @if (request(); as r) {
    <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
      <div class="flex items-center gap-2 flex-wrap">
        <a routerLink="/admin/travel-payment-requests" class="btn btn-sm btn-outline-secondary">
          <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
        </a>
        <h4 class="mb-0">\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB <span class="font-monospace text-muted">{{ r.requestNo }}</span></h4>
        <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>
      </div>
      <div class="flex flex-wrap gap-2">
        @if (r.approvalStatus === 'approved') {
          <button class="btn btn-outline-secondary inline-flex items-center gap-1"
                  (click)="printRequest()" [disabled]="pdfLoading()">
            @if (pdfLoading()) {
              <span class="spinner-border spinner-border-sm" role="status"></span>
            } @else {
              <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#printer"></use></svg>
            }
            \u5217\u5370 PDF
          </button>
        }
        @if (r.approvalStatus === 'draft' || r.approvalStatus === 'returned') {
          <a [routerLink]="['/admin/travel-payment-requests', r.id, 'edit']"
             class="btn btn-outline-primary inline-flex items-center gap-1">
            <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#edit"></use></svg>
            \u7DE8\u8F2F
          </a>
        }
        @if (r.approvalStatus === 'draft') {
          <button class="btn btn-primary inline-flex items-center gap-1" (click)="submitRequest()">
            <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#send"></use></svg>
            \u9001\u51FA\u7533\u8ACB
          </button>
          <button class="btn btn-outline-danger inline-flex items-center gap-1"
                  (click)="deleteRequest()" [disabled]="deleting()">
            @if (deleting()) {
              <span class="spinner-border spinner-border-sm" role="status"></span>
            } @else {
              <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#trash-2"></use></svg>
            }
            \u522A\u9664
          </button>
        }
        <a routerLink="/admin/travel-payment-requests" class="btn btn-outline-secondary inline-flex items-center gap-1">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#list"></use></svg>
          \u8FD4\u56DE\u5217\u8868
        </a>
      </div>
    </div>

    <div class="row g-4">
    <div class="col-12 col-xl-10">

    <!-- \u51FA\u5DEE\u8CC7\u8A0A -->
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#map-pin"></use>
        </svg>
        \u51FA\u5DEE\u8CC7\u8A0A
      </div>
      <div class="card-body">
        <div class="row g-3 mb-4">
          <div class="col-12 col-md-6">
            <div class="text-muted small">\u51FA\u5DEE\u5730\u9EDE</div>
            <div class="fw-600">{{ r.destination }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7533\u8ACB\u65E5\u671F</div>
            <div class="fw-500">{{ r.createdAt | date:'yyyy-MM-dd' }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7533\u8ACB\u4EBA</div>
            <div class="fw-500">{{ r.employeeName || '\u2014' }}</div>
          </div>
        </div>

        <div class="row g-3 mb-4">
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u958B\u59CB\u65E5\u671F</div>
            <div class="fw-500">{{ r.startDate | date:'yyyy-MM-dd' }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7D50\u675F\u65E5\u671F</div>
            <div class="fw-500">{{ r.endDate | date:'yyyy-MM-dd' }}</div>
          </div>
          @if (r.projectCode || r.projectName) {
            <div class="col-6 col-md-3">
              <div class="text-muted small">\u95DC\u806F\u5C08\u6848</div>
              <div class="fw-500 font-monospace">{{ r.projectCode }}{{ r.projectName ? ' - ' + r.projectName : '' }}</div>
            </div>
          }
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u91D1\u984D\u5408\u8A08</div>
            <div class="fw-600 text-lg">{{ r.grandTotal | number:'1.0-0' }}</div>
          </div>
        </div>

        <div class="row g-3 mb-0">
          <div class="col-12">
            <div class="text-muted small">\u51FA\u5DEE\u76EE\u7684</div>
            <div class="fw-500">{{ r.purpose }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- \u8CBB\u7528\u660E\u7D30 -->
    @if (r.items && r.items.length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#list"></use>
          </svg>
          \u8CBB\u7528\u660E\u7D30
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-sm mb-0">
              <thead class="table-light">
                <tr>
                  <th style="width:40px"></th>
                  <th>\u5206\u985E</th>
                  <th>\u9805\u6B21</th>
                  <th>\u9805\u76EE\u8AAA\u660E</th>
                  <th class="text-right">\u55AE\u50F9</th>
                  <th>\u6578\u91CF/\u55AE\u4F4D</th>
                  <th class="text-right">\u7E3D\u50F9</th>
                  <th>\u5099\u8A3B</th>
                  <th>\u767C\u7968\u865F\u78BC</th>
                  <th>\u767C\u7968\u65E5\u671F</th>
                </tr>
              </thead>
              <tbody>
                @for (item of r.items; track item.id) {
                  <tr>
                    <td class="align-middle text-center">
                      @if (item.fileUrl) {
                        <button type="button" class="btn btn-sm btn-ghost-secondary p-1"
                                (click)="openPreview(item.fileName || '\u767C\u7968', item.fileUrl!)"
                                title="{{ item.fileName }}">
                          <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
                        </button>
                      }
                    </td>
                    <td class="small">{{ item.category }}</td>
                    <td class="small">{{ item.seqNo }}</td>
                    <td class="small">{{ item.itemName }}</td>
                    <td class="text-right small">{{ item.unitPrice | number:'1.0-0' }}</td>
                    <td class="small">{{ item.quantity }}</td>
                    <td class="text-right small fw-500">{{ item.totalPrice | number:'1.0-0' }}</td>
                    <td class="small text-muted">{{ item.note || '\u2014' }}</td>
                    <td class="small font-monospace">{{ item.invoiceNo || '\u2014' }}</td>
                    <td class="small">{{ item.invoiceDate ? (item.invoiceDate | date:'yyyy-MM-dd') : '\u2014' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr class="table-light">
                  <td colspan="6" class="text-right fw-500 small">\u5408\u8A08</td>
                  <td class="text-right fw-600">{{ r.grandTotal | number:'1.0-0' }}</td>
                  <td colspan="3"></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>
    }

    <!-- \u64A5\u6B3E\u660E\u7D30\uFF08\u6838\u51C6\u5F8C\u624D\u6703\u6709\u8CC7\u6599\uFF1Bread-only\uFF09-->
    <app-installments-table
      [installmentsInput]="r.installments"
      [paymentStatus]="r.paymentStatus"
      [totalAmount]="r.grandTotal" />

    <!-- \u7C3D\u6838\u6D41\u7A0B\u6642\u9593\u8EF8 -->
    @if (approvalTask(); as task) {
      <app-approval-timeline
        [flow]="task.flow ?? null"
        [approvalRecords]="task.approvalRecords"
        [currentStepOrder]="task.currentStepOrder"
        [status]="task.status" />
    }

    </div>
    </div>

  } @else {
    <div class="text-center py-6 text-muted">\u8F09\u5165\u4E2D\u2026</div>
  }
</div>

@if (previewFile) {
  <app-file-preview-modal [file]="previewFile" (closed)="closePreview()" />
}
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(TravelPaymentDetail, { className: "TravelPaymentDetail", filePath: "src/app/features/admin/travel-payment-requests/pages/travel-payment-detail/travel-payment-detail.ts", lineNumber: 19 });
})();
export {
  TravelPaymentDetail
};
//# sourceMappingURL=chunk-6ZHG6OO2.js.map
