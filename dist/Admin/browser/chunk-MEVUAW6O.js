import {
  PaymentPdfService
} from "./chunk-N6WM3UHQ.js";
import {
  PaymentRequestService
} from "./chunk-V5VXKUPP.js";
import {
  FilePreviewModal
} from "./chunk-GWKNDEFV.js";
import "./chunk-346USOMS.js";
import {
  InstallmentsTable
} from "./chunk-64MF6XW3.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  PAYMENT_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS
} from "./chunk-JXHNN362.js";
import {
  ApprovalTaskService,
  ApprovalTimeline
} from "./chunk-IUP4A3MK.js";
import "./chunk-PKXTA7WQ.js";
import "./chunk-U6NS4RSC.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-DUW2WF5C.js";
import {
  DomSanitizer
} from "./chunk-JDEYLUO2.js";
import "./chunk-ZSGTQ3YJ.js";
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

// src/app/features/admin/payment-requests/pages/payment-detail/payment-detail.ts
var _c0 = (a0) => ["/admin/payment-requests", a0, "edit"];
var _forTrack0 = ($index, $item) => $item.id;
function PaymentDetail_Conditional_1_Conditional_14_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 34);
  }
}
function PaymentDetail_Conditional_1_Conditional_14_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 14);
    \u0275\u0275element(1, "use", 35);
    \u0275\u0275elementEnd();
  }
}
function PaymentDetail_Conditional_1_Conditional_14_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 33);
    \u0275\u0275listener("click", function PaymentDetail_Conditional_1_Conditional_14_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printRequest());
    });
    \u0275\u0275conditionalCreate(1, PaymentDetail_Conditional_1_Conditional_14_Conditional_1_Template, 1, 0, "span", 34)(2, PaymentDetail_Conditional_1_Conditional_14_Conditional_2_Template, 2, 0, ":svg:svg", 14);
    \u0275\u0275text(3, " \u5217\u5370\u8ACB\u6B3E\u55AE ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275property("disabled", ctx_r1.pdfLoading());
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.pdfLoading() ? 1 : 2);
  }
}
function PaymentDetail_Conditional_1_Conditional_15_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 12);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 36);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u7DE8\u8F2F ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c0, r_r3.id));
  }
}
function PaymentDetail_Conditional_1_Conditional_16_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 34);
  }
}
function PaymentDetail_Conditional_1_Conditional_16_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 14);
    \u0275\u0275element(1, "use", 40);
    \u0275\u0275elementEnd();
  }
}
function PaymentDetail_Conditional_1_Conditional_16_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 37);
    \u0275\u0275listener("click", function PaymentDetail_Conditional_1_Conditional_16_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.submitRequest());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 38);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(4, "button", 39);
    \u0275\u0275listener("click", function PaymentDetail_Conditional_1_Conditional_16_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.deleteRequest());
    });
    \u0275\u0275conditionalCreate(5, PaymentDetail_Conditional_1_Conditional_16_Conditional_5_Template, 1, 0, "span", 34)(6, PaymentDetail_Conditional_1_Conditional_16_Conditional_6_Template, 2, 0, ":svg:svg", 14);
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
function PaymentDetail_Conditional_1_Conditional_47_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 27)(1, "div", 25);
    \u0275\u0275text(2, "\u7533\u8ACB\u4EBA");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 28);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(ctx.submittedBy || "\u2014");
  }
}
function PaymentDetail_Conditional_1_Conditional_48_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 30)(1, "div", 25);
    \u0275\u0275text(2, "\u8AAA\u660E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 28);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(r_r3.reason);
  }
}
function PaymentDetail_Conditional_1_Conditional_49_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18)(1, "div", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 20);
    \u0275\u0275element(3, "use", 41);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u53D7\u6B3E\u5EE0\u5546 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 22)(6, "div", 23)(7, "div", 24)(8, "div", 25);
    \u0275\u0275text(9, "\u5EE0\u5546\u540D\u7A31");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "div", 28);
    \u0275\u0275text(11);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(12, "div", 24)(13, "div", 25);
    \u0275\u0275text(14, "\u7D71\u4E00\u7DE8\u865F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "div", 26);
    \u0275\u0275text(16);
    \u0275\u0275elementEnd()()()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(11);
    \u0275\u0275textInterpolate(r_r3.vendorName || "\u2014");
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(r_r3.vendorTaxId || "\u2014");
  }
}
function PaymentDetail_Conditional_1_Conditional_50_For_23_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r5 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 56);
    \u0275\u0275listener("click", function PaymentDetail_Conditional_1_Conditional_50_For_23_Conditional_2_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r5);
      const inv_r6 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.openPreview(inv_r6.fileName || "\u767C\u7968", inv_r6.fileUrl));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 14);
    \u0275\u0275element(2, "use", 57);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const inv_r6 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("title", \u0275\u0275interpolate(inv_r6.fileName));
  }
}
function PaymentDetail_Conditional_1_Conditional_50_For_23_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 50);
    \u0275\u0275conditionalCreate(2, PaymentDetail_Conditional_1_Conditional_50_For_23_Conditional_2_Template, 3, 2, "button", 51);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 52);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 53);
    \u0275\u0275text(6);
    \u0275\u0275pipe(7, "date");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(8, "td", 53);
    \u0275\u0275text(9);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(10, "td", 54);
    \u0275\u0275text(11);
    \u0275\u0275pipe(12, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "td", 55);
    \u0275\u0275text(14);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const inv_r6 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275conditional(inv_r6.fileUrl ? 2 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(inv_r6.invoiceNo || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(inv_r6.invoiceDate ? \u0275\u0275pipeBind2(7, 6, inv_r6.invoiceDate, "yyyy-MM-dd") : "\u2014");
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(inv_r6.itemName || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(12, 9, inv_r6.amount, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(inv_r6.note || "\u2014");
  }
}
function PaymentDetail_Conditional_1_Conditional_50_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 18)(1, "div", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 20);
    \u0275\u0275element(3, "use", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u767C\u7968\u660E\u7D30 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 42)(6, "div", 43)(7, "table", 44)(8, "thead", 45)(9, "tr");
    \u0275\u0275element(10, "th", 46);
    \u0275\u0275elementStart(11, "th");
    \u0275\u0275text(12, "\u767C\u7968\u865F\u78BC");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "th");
    \u0275\u0275text(14, "\u767C\u7968\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "th");
    \u0275\u0275text(16, "\u9805\u76EE");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "th", 47);
    \u0275\u0275text(18, "\u91D1\u984D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "th");
    \u0275\u0275text(20, "\u5099\u8A3B");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(21, "tbody");
    \u0275\u0275repeaterCreate(22, PaymentDetail_Conditional_1_Conditional_50_For_23_Template, 15, 12, "tr", null, _forTrack0);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(24, "tfoot")(25, "tr", 45)(26, "td", 48);
    \u0275\u0275text(27, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(28, "td", 49);
    \u0275\u0275text(29);
    \u0275\u0275pipe(30, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(31, "td");
    \u0275\u0275elementEnd()()()()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(22);
    \u0275\u0275repeater(r_r3.invoices);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(30, 1, r_r3.totalAmount, "1.0-0"));
  }
}
function PaymentDetail_Conditional_1_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "app-approval-timeline", 32);
  }
  if (rf & 2) {
    const task_r7 = ctx;
    \u0275\u0275property("flow", task_r7.flow ?? null)("approvalRecords", task_r7.approvalRecords)("currentStepOrder", task_r7.currentStepOrder)("status", task_r7.status);
  }
}
function PaymentDetail_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 3)(1, "div", 4)(2, "a", 5);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 6);
    \u0275\u0275element(4, "use", 7);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h4", 8);
    \u0275\u0275text(6, "\u8ACB\u6B3E\u7533\u8ACB ");
    \u0275\u0275elementStart(7, "span", 9);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "span");
    \u0275\u0275text(10);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(11, "span");
    \u0275\u0275text(12);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(13, "div", 10);
    \u0275\u0275conditionalCreate(14, PaymentDetail_Conditional_1_Conditional_14_Template, 4, 2, "button", 11);
    \u0275\u0275conditionalCreate(15, PaymentDetail_Conditional_1_Conditional_15_Template, 4, 3, "a", 12);
    \u0275\u0275conditionalCreate(16, PaymentDetail_Conditional_1_Conditional_16_Template, 8, 2);
    \u0275\u0275elementStart(17, "a", 13);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(18, "svg", 14);
    \u0275\u0275element(19, "use", 15);
    \u0275\u0275elementEnd();
    \u0275\u0275text(20, " \u8FD4\u56DE\u5217\u8868 ");
    \u0275\u0275elementEnd()()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(21, "div", 16)(22, "div", 17)(23, "div", 18)(24, "div", 19);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(25, "svg", 20);
    \u0275\u0275element(26, "use", 21);
    \u0275\u0275elementEnd();
    \u0275\u0275text(27, " \u8ACB\u6B3E\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(28, "div", 22)(29, "div", 23)(30, "div", 24)(31, "div", 25);
    \u0275\u0275text(32, "\u95DC\u806F\u5C08\u6848");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(33, "div", 26);
    \u0275\u0275text(34);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(35, "div", 27)(36, "div", 25);
    \u0275\u0275text(37, "\u7533\u8ACB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(38, "div", 28);
    \u0275\u0275text(39);
    \u0275\u0275pipe(40, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(41, "div", 27)(42, "div", 25);
    \u0275\u0275text(43, "\u7E3D\u91D1\u984D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(44, "div", 29);
    \u0275\u0275text(45);
    \u0275\u0275pipe(46, "number");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(47, PaymentDetail_Conditional_1_Conditional_47_Template, 5, 1, "div", 27);
    \u0275\u0275conditionalCreate(48, PaymentDetail_Conditional_1_Conditional_48_Template, 5, 1, "div", 30);
    \u0275\u0275elementEnd()()();
    \u0275\u0275conditionalCreate(49, PaymentDetail_Conditional_1_Conditional_49_Template, 17, 2, "div", 18);
    \u0275\u0275conditionalCreate(50, PaymentDetail_Conditional_1_Conditional_50_Template, 32, 4, "div", 18);
    \u0275\u0275element(51, "app-installments-table", 31);
    \u0275\u0275conditionalCreate(52, PaymentDetail_Conditional_1_Conditional_52_Template, 1, 4, "app-approval-timeline", 32);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_13_0;
    let tmp_20_0;
    const r_r3 = ctx;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(8);
    \u0275\u0275textInterpolate(r_r3.requestNo);
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + ctx_r1.typeClass[r_r3.type]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.typeLabel[r_r3.type]);
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + ctx_r1.statusClass[r_r3.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.statusLabel[r_r3.approvalStatus]);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.approvalStatus === "approved" && ctx_r1.approvalTask() ? 14 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" || r_r3.approvalStatus === "returned" ? 15 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" ? 16 : -1);
    \u0275\u0275advance(18);
    \u0275\u0275textInterpolate2("", r_r3.projectCode, "", r_r3.projectName ? " - " + r_r3.projectName : "");
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(40, 22, r_r3.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(46, 25, r_r3.totalAmount, "1.0-0"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional((tmp_13_0 = ctx_r1.approvalTask()) ? 47 : -1, tmp_13_0);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.reason ? 48 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.type === "vendor" ? 49 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.invoices && r_r3.invoices.length > 0 ? 50 : -1);
    \u0275\u0275advance();
    \u0275\u0275property("installmentsInput", r_r3.installments)("paymentStatus", r_r3.paymentStatus)("totalAmount", r_r3.totalAmount);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_20_0 = ctx_r1.approvalTask()) ? 52 : -1, tmp_20_0);
  }
}
function PaymentDetail_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 1);
    \u0275\u0275text(1, "\u8F09\u5165\u4E2D\u2026");
    \u0275\u0275elementEnd();
  }
}
function PaymentDetail_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r8 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "app-file-preview-modal", 58);
    \u0275\u0275listener("closed", function PaymentDetail_Conditional_3_Template_app_file_preview_modal_closed_0_listener() {
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
var PaymentDetail = class _PaymentDetail {
  service = inject(PaymentRequestService);
  pdfService = inject(PaymentPdfService);
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
  typeLabel = PAYMENT_TYPE_LABELS;
  typeClass = PAYMENT_TYPE_CLASSES;
  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get("id");
    this.service.getById(id).subscribe((r) => {
      this.request.set(r);
    });
    this.taskService.getById(id, "payment_request").subscribe({
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
    const t = this.approvalTask();
    if (t)
      this.pdfService.printPaymentRequest(t);
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
    if (!r || !confirm("\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u8ACB\u6B3E\u7533\u8ACB\uFF1F"))
      return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(["/admin/payment-requests"]),
      error: () => this.deleting.set(false)
    });
  }
  static \u0275fac = function PaymentDetail_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentDetail)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _PaymentDetail, selectors: [["app-payment-detail"]], decls: 4, vars: 2, consts: [[1, "container-fluid", "py-3"], [1, "text-center", "py-6", "text-muted"], [3, "file"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2", "flex-wrap"], ["routerLink", "/admin/payment-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "font-monospace", "text-muted"], [1, "flex", "flex-wrap", "gap-2"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "disabled"], [1, "btn", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "routerLink"], ["routerLink", "/admin/payment-requests", 1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#list"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm", "mb-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#dollar-sign"], [1, "card-body"], [1, "row", "g-3", "mb-0"], [1, "col-12", "col-md-6"], [1, "text-muted", "small"], [1, "fw-500", "font-monospace"], [1, "col-6", "col-md-3"], [1, "fw-500"], [1, "fw-600", "text-lg"], [1, "col-12"], [3, "installmentsInput", "paymentStatus", "totalAmount"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["role", "status", 1, "spinner-border", "spinner-border-sm"], ["href", "/assets/icons/sprite.svg#printer"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#send"], [1, "btn", "btn-outline-danger", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["href", "/assets/icons/sprite.svg#trash-2"], ["href", "/assets/icons/sprite.svg#briefcase"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "width", "40px"], [1, "text-right"], ["colspan", "4", 1, "text-right", "fw-500", "small"], [1, "text-right", "fw-600"], [1, "align-middle", "text-center"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "title"], [1, "small", "font-monospace"], [1, "small"], [1, "text-right", "small", "fw-500"], [1, "small", "text-muted"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "click", "title"], ["href", "/assets/icons/sprite.svg#file-text"], [3, "closed", "file"]], template: function PaymentDetail_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, PaymentDetail_Conditional_1_Template, 53, 28)(2, PaymentDetail_Conditional_2_Template, 2, 0, "div", 1);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(3, PaymentDetail_Conditional_3_Template, 1, 1, "app-file-preview-modal", 2);
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
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentDetail, [{
    type: Component,
    args: [{ selector: "app-payment-detail", imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable], template: `<div class="container-fluid py-3">
  @if (request(); as r) {
    <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
      <div class="flex items-center gap-2 flex-wrap">
        <a routerLink="/admin/payment-requests" class="btn btn-sm btn-outline-secondary">
          <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
        </a>
        <h4 class="mb-0">\u8ACB\u6B3E\u7533\u8ACB <span class="font-monospace text-muted">{{ r.requestNo }}</span></h4>
        <span [class]="'badge ' + typeClass[r.type]">{{ typeLabel[r.type] }}</span>
        <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>
      </div>
      <div class="flex flex-wrap gap-2">
        @if (r.approvalStatus === 'approved' && approvalTask()) {
          <button class="btn btn-outline-secondary inline-flex items-center gap-1"
                  (click)="printRequest()" [disabled]="pdfLoading()">
            @if (pdfLoading()) {
              <span class="spinner-border spinner-border-sm" role="status"></span>
            } @else {
              <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#printer"></use></svg>
            }
            \u5217\u5370\u8ACB\u6B3E\u55AE
          </button>
        }
        @if (r.approvalStatus === 'draft' || r.approvalStatus === 'returned') {
          <a [routerLink]="['/admin/payment-requests', r.id, 'edit']"
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
        <a routerLink="/admin/payment-requests" class="btn btn-outline-secondary inline-flex items-center gap-1">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#list"></use></svg>
          \u8FD4\u56DE\u5217\u8868
        </a>
      </div>
    </div>

    <div class="row g-4">
    <div class="col-12 col-xl-10">

    <!-- \u8ACB\u6B3E\u8CC7\u8A0A -->
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#dollar-sign"></use>
        </svg>
        \u8ACB\u6B3E\u8CC7\u8A0A
      </div>
      <div class="card-body">
        <div class="row g-3 mb-0">
          <div class="col-12 col-md-6">
            <div class="text-muted small">\u95DC\u806F\u5C08\u6848</div>
            <div class="fw-500 font-monospace">{{ r.projectCode }}{{ r.projectName ? ' - ' + r.projectName : '' }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7533\u8ACB\u65E5\u671F</div>
            <div class="fw-500">{{ r.createdAt | date:'yyyy-MM-dd' }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7E3D\u91D1\u984D</div>
            <div class="fw-600 text-lg">{{ r.totalAmount | number:'1.0-0' }}</div>
          </div>
          @if (approvalTask(); as t) {
            <div class="col-6 col-md-3">
              <div class="text-muted small">\u7533\u8ACB\u4EBA</div>
              <div class="fw-500">{{ t.submittedBy || '\u2014' }}</div>
            </div>
          }
          @if (r.reason) {
            <div class="col-12">
              <div class="text-muted small">\u8AAA\u660E</div>
              <div class="fw-500">{{ r.reason }}</div>
            </div>
          }
        </div>
      </div>
    </div>

    <!-- \u5EE0\u5546\u8CC7\u8A0A\uFF08type=vendor \u6642\u986F\u793A\uFF09 -->
    @if (r.type === 'vendor') {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#briefcase"></use>
          </svg>
          \u53D7\u6B3E\u5EE0\u5546
        </div>
        <div class="card-body">
          <div class="row g-3 mb-0">
            <div class="col-12 col-md-6">
              <div class="text-muted small">\u5EE0\u5546\u540D\u7A31</div>
              <div class="fw-500">{{ r.vendorName || '\u2014' }}</div>
            </div>
            <div class="col-12 col-md-6">
              <div class="text-muted small">\u7D71\u4E00\u7DE8\u865F</div>
              <div class="fw-500 font-monospace">{{ r.vendorTaxId || '\u2014' }}</div>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- \u767C\u7968\u660E\u7D30 -->
    @if (r.invoices && r.invoices.length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#list"></use>
          </svg>
          \u767C\u7968\u660E\u7D30
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-sm mb-0">
              <thead class="table-light">
                <tr>
                  <th style="width:40px"></th>
                  <th>\u767C\u7968\u865F\u78BC</th>
                  <th>\u767C\u7968\u65E5\u671F</th>
                  <th>\u9805\u76EE</th>
                  <th class="text-right">\u91D1\u984D</th>
                  <th>\u5099\u8A3B</th>
                </tr>
              </thead>
              <tbody>
                @for (inv of r.invoices; track inv.id) {
                  <tr>
                    <td class="align-middle text-center">
                      @if (inv.fileUrl) {
                        <button type="button" class="btn btn-sm btn-ghost-secondary p-1"
                                (click)="openPreview(inv.fileName || '\u767C\u7968', inv.fileUrl!)"
                                title="{{ inv.fileName }}">
                          <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
                        </button>
                      }
                    </td>
                    <td class="small font-monospace">{{ inv.invoiceNo || '\u2014' }}</td>
                    <td class="small">{{ inv.invoiceDate ? (inv.invoiceDate | date:'yyyy-MM-dd') : '\u2014' }}</td>
                    <td class="small">{{ inv.itemName || '\u2014' }}</td>
                    <td class="text-right small fw-500">{{ inv.amount | number:'1.0-0' }}</td>
                    <td class="small text-muted">{{ inv.note || '\u2014' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr class="table-light">
                  <td colspan="4" class="text-right fw-500 small">\u5408\u8A08</td>
                  <td class="text-right fw-600">{{ r.totalAmount | number:'1.0-0' }}</td>
                  <td></td>
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
      [totalAmount]="r.totalAmount" />

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
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(PaymentDetail, { className: "PaymentDetail", filePath: "src/app/features/admin/payment-requests/pages/payment-detail/payment-detail.ts", lineNumber: 19 });
})();
export {
  PaymentDetail
};
//# sourceMappingURL=chunk-MEVUAW6O.js.map
