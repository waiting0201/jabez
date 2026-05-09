import {
  CIS,
  FONT_FAMILY,
  PdfCoreService,
  buildDynamicSignBlocks,
  fmtDT,
  fmtDate
} from "./chunk-II2TI2JG.js";
import {
  ApprovalTimeline
} from "./chunk-B4OWGIJG.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  HolidayTravelRequestService
} from "./chunk-HTJRHBRI.js";
import {
  ApprovalTaskService
} from "./chunk-TZRFZK6Q.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-DUW2WF5C.js";
import "./chunk-JDEYLUO2.js";
import {
  Component,
  DatePipe,
  Injectable,
  inject,
  setClassMetadata,
  signal,
  ɵsetClassDebugInfo,
  ɵɵadvance,
  ɵɵclassMap,
  ɵɵconditional,
  ɵɵconditionalCreate,
  ɵɵdefineComponent,
  ɵɵdefineInjectable,
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
  ɵɵresetView,
  ɵɵrestoreView,
  ɵɵtext,
  ɵɵtextInterpolate,
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2
} from "./chunk-IFQ7CN6S.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/holiday-travel-requests/services/holiday-travel-pdf.service.ts
var HolidayTravelPdfService = class _HolidayTravelPdfService {
  pdfLoading = signal(false, ...ngDevMode ? [{ debugName: "pdfLoading" }] : []);
  pdfCore = inject(PdfCoreService);
  /** 列印假日執行活動申請單 */
  async printHolidayTravelRequest(r, submittedByName, approvalRecords = [], flow, submittedBySignatureUrl) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, fonts] = await Promise.all([
        import("./chunk-4QY6N5TU.js"),
        this.pdfCore.loadFonts()
      ]);
      const doc = new jsPDF("landscape", "mm", "a4");
      const F = FONT_FAMILY;
      this.pdfCore.registerFonts(doc, fonts);
      const pw = doc.internal.pageSize.getWidth();
      const ph = doc.internal.pageSize.getHeight();
      const mx = 14;
      const cw = pw - mx * 2;
      let y = 16;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);
      y += 10;
      doc.setFont(F, "bold");
      doc.setFontSize(11);
      doc.setTextColor(...CIS.textPrimary);
      doc.text("\u96C5\u6BD4\u65AF\u570B\u969B\u5275\u610F\u7B56\u7565\u80A1\u4EFD\u6709\u9650\u516C\u53F8", pw / 2, y, { align: "center" });
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text("\u5047 \u65E5 \u57F7 \u884C \u6D3B \u52D5 \u7533 \u8ACB \u55AE", pw / 2, y, { align: "center" });
      y += 10;
      doc.setFont(F, "normal");
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textPrimary);
      const lv = (label, value, x, yy, bold = false) => {
        doc.setFont(F, "normal");
        doc.text(label, x, yy);
        const lw = doc.getTextWidth(label);
        if (bold)
          doc.setFont(F, "bold");
        doc.text(value, x + lw, yy);
        doc.setFont(F, "normal");
      };
      const startDate = r.startDate ? fmtDate(r.startDate) : "";
      const endDate = r.endDate ? fmtDate(r.endDate) : "";
      lv("\u7533\u8ACB\u4EBA\uFF1A", submittedByName, mx, y, true);
      lv("\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE\uFF1A", r.destination, pw / 2, y, true);
      y += 6;
      lv("\u6D3B\u52D5\u671F\u9593\uFF1A", `${startDate} \uFF5E ${endDate}`, mx, y);
      lv("\u5047\u65E5\u5929\u6578\uFF1A", r.holidayDays != null ? `${r.holidayDays} \u5929` : "\u2014", pw - mx - 50, y);
      y += 6;
      if (r.projectCode || r.projectName) {
        lv("\u95DC\u806F\u5C08\u6848\uFF1A", `${r.projectCode ?? ""}${r.projectName ? " - " + r.projectName : ""}`, mx, y, true);
      }
      y += 6;
      lv("\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9\uFF1A", r.purpose || "", mx, y);
      if (r.participants && r.participants.length > 0) {
        y += 6;
        const names = r.participants.sort((a, b) => a.sortOrder - b.sortOrder).map((p) => p.userName || p.userId).join("\u3001");
        lv("\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\uFF1A", names, mx, y);
      }
      y += 16;
      if (y + 35 > ph - 15) {
        doc.addPage();
        y = 20;
      }
      const submitDate = r.createdAt ? fmtDT(r.createdAt) : "";
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, "\u7533\u8ACB\u8005");
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);
      doc.save(`\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB\u55AE-${r.id}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }
  /** 根據 flow steps 動態建立簽名欄資料 */
  _buildSignBlocks(flow, records, submittedBySignatureUrl, submitDate, applicantLabel) {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel
    });
  }
  static \u0275fac = function HolidayTravelPdfService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _HolidayTravelPdfService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _HolidayTravelPdfService, factory: _HolidayTravelPdfService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelPdfService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

// src/app/features/admin/holiday-travel-requests/pages/holiday-travel-detail/holiday-travel-detail.ts
var _c0 = (a0) => ["/admin/holiday-travel-requests", a0, "edit"];
var _forTrack0 = ($index, $item) => $item.userId;
function HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 29);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 30);
    \u0275\u0275element(1, "use", 31);
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 28);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_10_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printHolidayTravel());
    });
    \u0275\u0275conditionalCreate(1, HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_1_Template, 1, 0, "span", 29)(2, HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_2_Template, 2, 0, ":svg:svg", 30);
    \u0275\u0275text(3, " \u5217\u5370\u7533\u8ACB\u55AE ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const ctx_r1 = \u0275\u0275nextContext(2);
    \u0275\u0275property("disabled", ctx_r1.pdfLoading());
    \u0275\u0275advance();
    \u0275\u0275conditional(ctx_r1.pdfLoading() ? 1 : 2);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_11_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "a", 10);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 30);
    \u0275\u0275element(2, "use", 32);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u7DE8\u8F2F ");
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275property("routerLink", \u0275\u0275pureFunction1(1, _c0, r_r3.id));
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_5_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 29);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 30);
    \u0275\u0275element(1, "use", 36);
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 33);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_12_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.submitRequest());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 30);
    \u0275\u0275element(2, "use", 34);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(4, "button", 35);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_12_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.deleteRequest());
    });
    \u0275\u0275conditionalCreate(5, HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_5_Template, 1, 0, "span", 29)(6, HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_6_Template, 2, 0, ":svg:svg", 30);
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
function HolidayTravelDetail_Conditional_1_Conditional_31_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 23);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance();
    \u0275\u0275textInterpolate1("", r_r3.holidayDays, " \u5929");
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_32_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "span", 20);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 22)(1, "div", 20);
    \u0275\u0275text(2, "\u95DC\u806F\u5C08\u6848");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 37);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate2("", r_r3.projectCode, "", r_r3.projectName ? " - " + r_r3.projectName : "");
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_59_For_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 40);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const p_r5 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(p_r5.userName || p_r5.userId);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_59_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 13)(1, "div", 14);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 15);
    \u0275\u0275element(3, "use", 38);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 17)(6, "ol", 39);
    \u0275\u0275repeaterCreate(7, HolidayTravelDetail_Conditional_1_Conditional_59_For_8_Template, 2, 1, "li", 40, _forTrack0);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275repeater(r_r3.participants);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_60_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "app-approval-timeline", 27);
  }
  if (rf & 2) {
    const task_r6 = ctx;
    \u0275\u0275property("flow", task_r6.flow ?? null)("approvalRecords", task_r6.approvalRecords)("currentStepOrder", task_r6.currentStepOrder)("status", task_r6.status);
  }
}
function HolidayTravelDetail_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 2)(1, "div", 3)(2, "a", 4);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 5);
    \u0275\u0275element(4, "use", 6);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h4", 7);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "span");
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "div", 8);
    \u0275\u0275conditionalCreate(10, HolidayTravelDetail_Conditional_1_Conditional_10_Template, 4, 2, "button", 9);
    \u0275\u0275conditionalCreate(11, HolidayTravelDetail_Conditional_1_Conditional_11_Template, 4, 3, "a", 10);
    \u0275\u0275conditionalCreate(12, HolidayTravelDetail_Conditional_1_Conditional_12_Template, 8, 2);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(13, "div", 11)(14, "div", 12)(15, "div", 13)(16, "div", 14);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 15);
    \u0275\u0275element(18, "use", 16);
    \u0275\u0275elementEnd();
    \u0275\u0275text(19, " \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(20, "div", 17)(21, "div", 18)(22, "div", 19)(23, "div", 20);
    \u0275\u0275text(24, "\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "div", 21);
    \u0275\u0275text(26);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(27, "div", 22)(28, "div", 20);
    \u0275\u0275text(29, "\u5047\u65E5\u5929\u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(30, "div");
    \u0275\u0275conditionalCreate(31, HolidayTravelDetail_Conditional_1_Conditional_31_Template, 2, 1, "span", 23)(32, HolidayTravelDetail_Conditional_1_Conditional_32_Template, 2, 0, "span", 20);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(33, "div", 22)(34, "div", 20);
    \u0275\u0275text(35, "\u7533\u8ACB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(36, "div", 24);
    \u0275\u0275text(37);
    \u0275\u0275pipe(38, "date");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(39, "div", 18)(40, "div", 22)(41, "div", 20);
    \u0275\u0275text(42, "\u958B\u59CB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(43, "div", 24);
    \u0275\u0275text(44);
    \u0275\u0275pipe(45, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(46, "div", 22)(47, "div", 20);
    \u0275\u0275text(48, "\u7D50\u675F\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(49, "div", 24);
    \u0275\u0275text(50);
    \u0275\u0275pipe(51, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(52, HolidayTravelDetail_Conditional_1_Conditional_52_Template, 5, 2, "div", 22);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(53, "div", 25)(54, "div", 26)(55, "div", 20);
    \u0275\u0275text(56, "\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(57, "div", 24);
    \u0275\u0275text(58);
    \u0275\u0275elementEnd()()()()();
    \u0275\u0275conditionalCreate(59, HolidayTravelDetail_Conditional_1_Conditional_59_Template, 9, 0, "div", 13);
    \u0275\u0275conditionalCreate(60, HolidayTravelDetail_Conditional_1_Conditional_60_Template, 1, 4, "app-approval-timeline", 27);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_16_0;
    const r_r3 = ctx;
    const ctx_r1 = \u0275\u0275nextContext();
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate1("\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB #", r_r3.id);
    \u0275\u0275advance();
    \u0275\u0275classMap("badge " + ctx_r1.statusClass[r_r3.approvalStatus]);
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(ctx_r1.statusLabel[r_r3.approvalStatus]);
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.approvalStatus === "approved" ? 10 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" || r_r3.approvalStatus === "returned" ? 11 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.approvalStatus === "draft" ? 12 : -1);
    \u0275\u0275advance(14);
    \u0275\u0275textInterpolate(r_r3.destination);
    \u0275\u0275advance(5);
    \u0275\u0275conditional(r_r3.holidayDays != null ? 31 : 32);
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(38, 16, r_r3.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(45, 19, r_r3.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(51, 22, r_r3.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.projectCode || r_r3.projectName ? 52 : -1);
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(r_r3.purpose);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.participants && r_r3.participants.length > 0 ? 59 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_16_0 = ctx_r1.approvalTask()) ? 60 : -1, tmp_16_0);
  }
}
function HolidayTravelDetail_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 1);
    \u0275\u0275text(1, "\u8F09\u5165\u4E2D\u2026");
    \u0275\u0275elementEnd();
  }
}
var HolidayTravelDetail = class _HolidayTravelDetail {
  service = inject(HolidayTravelRequestService);
  pdfService = inject(HolidayTravelPdfService);
  taskService = inject(ApprovalTaskService);
  route = inject(ActivatedRoute);
  router = inject(Router);
  request = signal(null, ...ngDevMode ? [{ debugName: "request" }] : []);
  approvalTask = signal(null, ...ngDevMode ? [{ debugName: "approvalTask" }] : []);
  deleting = signal(false, ...ngDevMode ? [{ debugName: "deleting" }] : []);
  statusLabel = APPROVAL_STATUS_LABELS;
  statusClass = APPROVAL_STATUS_CLASSES;
  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get("id");
    this.service.getById(id).subscribe((r) => {
      this.request.set(r);
    });
    this.taskService.getById(id, "holiday_travel").subscribe({
      next: (t) => this.approvalTask.set(t),
      error: () => {
      }
      // draft 狀態可能尚無簽核記錄
    });
  }
  get pdfLoading() {
    return this.pdfService.pdfLoading;
  }
  printHolidayTravel() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) {
      this.pdfService.printHolidayTravelRequest(r, t?.submittedBy ?? "", t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
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
    if (!r || !confirm("\u78BA\u5B9A\u8981\u522A\u9664\u6B64\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB\uFF1F"))
      return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(["/admin/holiday-travel-requests"]),
      error: () => this.deleting.set(false)
    });
  }
  static \u0275fac = function HolidayTravelDetail_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _HolidayTravelDetail)();
  };
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _HolidayTravelDetail, selectors: [["app-holiday-travel-detail"]], decls: 3, vars: 1, consts: [[1, "container-fluid", "py-3"], [1, "text-center", "py-6", "text-muted"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2", "flex-wrap"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "flex", "flex-wrap", "gap-2"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "disabled"], [1, "btn", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "routerLink"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm", "mb-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#sun"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "text-muted", "small"], [1, "fw-600"], [1, "col-6", "col-md-3"], [1, "fw-600", 2, "color", "var(--forest)"], [1, "fw-500"], [1, "row", "g-3", "mb-0"], [1, "col-12"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["role", "status", 1, "spinner-border", "spinner-border-sm"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#printer"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#send"], [1, "btn", "btn-outline-danger", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["href", "/assets/icons/sprite.svg#trash-2"], [1, "fw-500", "font-monospace"], ["href", "/assets/icons/sprite.svg#users"], [1, "mb-0", "ps-4"], [1, "small", "fw-500"]], template: function HolidayTravelDetail_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, HolidayTravelDetail_Conditional_1_Template, 61, 25)(2, HolidayTravelDetail_Conditional_2_Template, 2, 0, "div", 1);
      \u0275\u0275elementEnd();
    }
    if (rf & 2) {
      let tmp_0_0;
      \u0275\u0275advance();
      \u0275\u0275conditional((tmp_0_0 = ctx.request()) ? 1 : 2, tmp_0_0);
    }
  }, dependencies: [RouterLink, ApprovalTimeline, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelDetail, [{
    type: Component,
    args: [{ selector: "app-holiday-travel-detail", imports: [RouterLink, DatePipe, ApprovalTimeline], template: `<div class="container-fluid py-3">
  @if (request(); as r) {
    <div class="flex flex-wrap items-center justify-between gap-2 mb-6">
      <div class="flex items-center gap-2 flex-wrap">
        <a routerLink="/admin/holiday-travel-requests" class="btn btn-sm btn-outline-secondary">
          <svg class="sa-icon"><use href="/assets/icons/sprite.svg#arrow-left"></use></svg>
        </a>
        <h4 class="mb-0">\u5047\u65E5\u57F7\u884C\u6D3B\u52D5\u7533\u8ACB #{{ r.id }}</h4>
        <span [class]="'badge ' + statusClass[r.approvalStatus]">{{ statusLabel[r.approvalStatus] }}</span>
      </div>
      <div class="flex flex-wrap gap-2">
        @if (r.approvalStatus === 'approved') {
          <button class="btn btn-outline-secondary inline-flex items-center gap-1"
                  (click)="printHolidayTravel()" [disabled]="pdfLoading()">
            @if (pdfLoading()) {
              <span class="spinner-border spinner-border-sm" role="status"></span>
            } @else {
              <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#printer"></use></svg>
            }
            \u5217\u5370\u7533\u8ACB\u55AE
          </button>
        }
        @if (r.approvalStatus === 'draft' || r.approvalStatus === 'returned') {
          <a [routerLink]="['/admin/holiday-travel-requests', r.id, 'edit']"
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
      </div>
    </div>

    <div class="row g-4">
    <div class="col-12 col-xl-10">

    <!-- \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A -->
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#sun"></use>
        </svg>
        \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A
      </div>
      <div class="card-body">
        <div class="row g-3 mb-4">
          <div class="col-12 col-md-6">
            <div class="text-muted small">\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE</div>
            <div class="fw-600">{{ r.destination }}</div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u5047\u65E5\u5929\u6578</div>
            <div>
              @if (r.holidayDays != null) {
                <span class="fw-600" style="color: var(--forest)">{{ r.holidayDays }} \u5929</span>
              } @else {
                <span class="text-muted small">\u2014</span>
              }
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u7533\u8ACB\u65E5\u671F</div>
            <div class="fw-500">{{ r.createdAt | date:'yyyy-MM-dd' }}</div>
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
        </div>

        <div class="row g-3 mb-0">
          <div class="col-12">
            <div class="text-muted small">\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9</div>
            <div class="fw-500">{{ r.purpose }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 -->
    @if (r.participants && r.participants.length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#users"></use>
          </svg>
          \u53C3\u8207\u57F7\u884C\u4EBA\u54E1
        </div>
        <div class="card-body">
          <ol class="mb-0 ps-4">
            @for (p of r.participants; track p.userId) {
              <li class="small fw-500">{{ p.userName || p.userId }}</li>
            }
          </ol>
        </div>
      </div>
    }

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
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(HolidayTravelDetail, { className: "HolidayTravelDetail", filePath: "src/app/features/admin/holiday-travel-requests/pages/holiday-travel-detail/holiday-travel-detail.ts", lineNumber: 16 });
})();
export {
  HolidayTravelDetail
};
//# sourceMappingURL=chunk-2VNX4F4H.js.map
