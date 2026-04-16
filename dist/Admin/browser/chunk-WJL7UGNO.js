import {
  CIS,
  FONT_FAMILY,
  PdfCoreService,
  fmt,
  fmtDT,
  fmtDate
} from "./chunk-74PAQPZY.js";
import {
  ApprovalTimeline,
  FilePreviewModal
} from "./chunk-AQCL77US.js";
import {
  ApprovalTaskService
} from "./chunk-E72ZCXMI.js";
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  HolidayTravelRequestService
} from "./chunk-KT3QJDIT.js";
import {
  ActivatedRoute,
  Router,
  RouterLink
} from "./chunk-UAVMLPEF.js";
import {
  DomSanitizer
} from "./chunk-K2EJQVOR.js";
import {
  Component,
  DatePipe,
  DecimalPipe,
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
  ɵɵtextInterpolate1,
  ɵɵtextInterpolate2
} from "./chunk-FX7BMVKQ.js";
import "./chunk-KWSTWQNB.js";

// src/app/features/admin/holiday-travel-requests/services/holiday-travel-pdf.service.ts
var HolidayTravelPdfService = class _HolidayTravelPdfService {
  pdfLoading = signal(false, ...ngDevMode ? [{ debugName: "pdfLoading" }] : []);
  pdfCore = inject(PdfCoreService);
  /** 列印假日執行活動申請單 */
  async printHolidayTravelRequest(r, submittedByName, approvalRecords = [], flow, submittedBySignatureUrl, reviewerSignatureUrls, paidAt, paidBySignatureUrl) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import("./chunk-CNNENCI2.js"),
        import("./chunk-S72SRWYK.js"),
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
      doc.text("\u5047 \u65E5 \u51FA \u5DEE \u7533 \u8ACB \u55AE", pw / 2, y, { align: "center" });
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
      lv("\u91D1\u984D\u5408\u8A08\uFF1A", `NT$ ${fmt(r.grandTotal)}`, pw - mx - 60, y, true);
      y += 6;
      lv("\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9\uFF1A", r.purpose || "", mx, y);
      if (r.participants && r.participants.length > 0) {
        y += 6;
        const names = r.participants.sort((a, b) => a.sortOrder - b.sortOrder).map((p) => p.userName || p.userId).join("\u3001");
        lv("\u53C3\u8207\u57F7\u884C\u4EBA\u54E1\uFF1A", names, mx, y);
      }
      y += 8;
      const items = r.items || [];
      const bodyRows = [];
      let lastCategory = "";
      for (const item of items) {
        const cat = item.category === lastCategory ? "" : item.category;
        lastCategory = item.category;
        bodyRows.push([
          cat,
          item.seqNo.toString(),
          item.itemName,
          `${fmt(item.unitPrice)}\u5143`,
          item.quantity,
          fmt(item.totalPrice),
          item.invoiceNo || "",
          item.note || ""
        ]);
      }
      bodyRows.push([
        { content: "\u5408\u8A08", colSpan: 5, styles: { halign: "right", fontStyle: "bold" } },
        { content: fmt(r.grandTotal), styles: { fontStyle: "bold", halign: "right" } },
        "",
        ""
      ]);
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        theme: "grid",
        styles: {
          font: F,
          fontSize: 8.5,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: { top: 2.5, bottom: 2.5, left: 3, right: 3 }
        },
        headStyles: {
          font: F,
          fillColor: [...CIS.forest],
          textColor: 255,
          fontSize: 9,
          fontStyle: "bold",
          halign: "center",
          cellPadding: { top: 3, bottom: 3, left: 3, right: 3 }
        },
        columnStyles: {
          0: { cellWidth: cw * 0.08, halign: "center" },
          // 分類
          1: { cellWidth: cw * 0.05, halign: "center" },
          // 項次
          2: { cellWidth: cw * 0.24 },
          // 項目說明
          3: { cellWidth: cw * 0.1, halign: "right" },
          // 單價
          4: { cellWidth: cw * 0.09, halign: "center" },
          // 數量
          5: { cellWidth: cw * 0.1, halign: "right" },
          // 總價
          6: { cellWidth: cw * 0.14, halign: "center" },
          // 發票號碼
          7: { cellWidth: cw * 0.2 }
          // 備註
        },
        head: [["\u5206\u985E", "\u9805\u6B21", "\u9805\u76EE\u8AAA\u660E", "\u55AE\u50F9", "\u6578\u91CF/\u55AE\u4F4D", "\u7E3D\u50F9", "\u767C\u7968\u865F\u78BC", "\u5099\u8A3B"]],
        body: bodyRows
      });
      const tableEndY = doc.lastAutoTable.finalY;
      y = tableEndY + 8;
      doc.setFont(F, "normal");
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      lv("\u9810\u8A08\u64A5\u6B3E\u65E5\uFF1A", r.estimatedPaymentDate ? fmtDT(r.estimatedPaymentDate).split(" ")[0] : "\u2014", mx, y, true);
      lv("\u64A5  \u6B3E  \u65E5\uFF1A", r.paidAt ? fmtDT(r.paidAt).split(" ")[0] : "\u2014", pw - mx - 55, y, true);
      y += 10;
      if (y + 35 > ph - 15) {
        doc.addPage();
        y = 20;
      }
      const submitDate = r.createdAt ? fmtDT(r.createdAt) : "";
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, "\u7533\u8ACB\u8005", paidAt, paidBySignatureUrl);
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
  /** 根據簽核流程和記錄建立簽名欄資料 */
  _buildSignBlocks(flow, records, submittedBySignatureUrl, submitDate, applicantLabel, paidAt, paidBySignatureUrl) {
    const blocks = [];
    const stepLabels = {};
    if (flow) {
      for (const step of flow.steps) {
        if (step.jobTitleName?.includes("\u7E3D\u76E3") || step.departmentName?.includes("\u7E3D\u76E3")) {
          stepLabels[step.stepOrder] = "\u7E3D\u76E3\u6838\u51C6";
        } else if (step.departmentName?.includes("\u8CA1\u52D9")) {
          stepLabels[step.stepOrder] = "\u8CA1\u52D9\u90E8\u7C3D\u6838";
        } else if (step.departmentName?.includes("\u6703\u8A08")) {
          stepLabels[step.stepOrder] = "\u6703\u8A08";
        } else if (step.stepOrder === 1) {
          stepLabels[step.stepOrder] = "\u90E8\u9580\u4E3B\u7BA1";
        } else {
          stepLabels[step.stepOrder] = step.note || step.jobTitleName || `Step ${step.stepOrder}`;
        }
      }
    }
    const labelRecordMap = /* @__PURE__ */ new Map();
    for (const rec of records) {
      const label = stepLabels[rec.stepOrder];
      if (label)
        labelRecordMap.set(label, rec);
    }
    const fixedLabels = ["\u7E3D\u76E3\u6838\u51C6", "\u8CA1\u52D9\u90E8\u7C3D\u6838", "\u6703\u8A08", "\u51FA\u7D0D", "\u90E8\u9580\u4E3B\u7BA1"];
    for (const label of fixedLabels) {
      if (label === "\u51FA\u7D0D") {
        blocks.push({
          label,
          signatureUrl: paidBySignatureUrl,
          date: paidAt ? fmtDT(paidAt) : ""
        });
      } else {
        const rec = labelRecordMap.get(label);
        blocks.push({
          label,
          signatureUrl: rec?.reviewerSignatureUrl,
          date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : ""
        });
      }
    }
    blocks.push({
      label: applicantLabel,
      signatureUrl: submittedBySignatureUrl,
      date: submitDate
    });
    return blocks;
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
var _forTrack1 = ($index, $item) => $item.id;
function HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "span", 31);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 32);
    \u0275\u0275element(1, "use", 33);
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_10_Template(rf, ctx) {
  if (rf & 1) {
    const _r1 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 30);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_10_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r1);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.printHolidayTravel());
    });
    \u0275\u0275conditionalCreate(1, HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_1_Template, 1, 0, "span", 31)(2, HolidayTravelDetail_Conditional_1_Conditional_10_Conditional_2_Template, 2, 0, ":svg:svg", 32);
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
    \u0275\u0275elementStart(0, "a", 11);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 32);
    \u0275\u0275element(2, "use", 34);
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
    \u0275\u0275element(0, "span", 31);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_6_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(0, "svg", 32);
    \u0275\u0275element(1, "use", 38);
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_12_Template(rf, ctx) {
  if (rf & 1) {
    const _r4 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 35);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_12_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.submitRequest());
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 32);
    \u0275\u0275element(2, "use", 36);
    \u0275\u0275elementEnd();
    \u0275\u0275text(3, " \u9001\u51FA\u7533\u8ACB ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(4, "button", 37);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_12_Template_button_click_4_listener() {
      \u0275\u0275restoreView(_r4);
      const ctx_r1 = \u0275\u0275nextContext(2);
      return \u0275\u0275resetView(ctx_r1.deleteRequest());
    });
    \u0275\u0275conditionalCreate(5, HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_5_Template, 1, 0, "span", 31)(6, HolidayTravelDetail_Conditional_1_Conditional_12_Conditional_6_Template, 2, 0, ":svg:svg", 32);
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
    \u0275\u0275elementStart(0, "span", 24);
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
    \u0275\u0275elementStart(0, "span", 21);
    \u0275\u0275text(1, "\u2014");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_52_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23)(1, "div", 21);
    \u0275\u0275text(2, "\u95DC\u806F\u5C08\u6848");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 39);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate2("", r_r3.projectCode, "", r_r3.projectName ? " - " + r_r3.projectName : "");
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_65_For_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "li", 42);
    \u0275\u0275text(1);
    \u0275\u0275elementEnd();
  }
  if (rf & 2) {
    const p_r5 = ctx.$implicit;
    \u0275\u0275advance();
    \u0275\u0275textInterpolate(p_r5.userName || p_r5.userId);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_65_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 14)(1, "div", 15);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 16);
    \u0275\u0275element(3, "use", 40);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u53C3\u8207\u57F7\u884C\u4EBA\u54E1 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 18)(6, "ol", 41);
    \u0275\u0275repeaterCreate(7, HolidayTravelDetail_Conditional_1_Conditional_65_For_8_Template, 2, 1, "li", 42, _forTrack0);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275repeater(r_r3.participants);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_66_For_29_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    const _r6 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "button", 60);
    \u0275\u0275listener("click", function HolidayTravelDetail_Conditional_1_Conditional_66_For_29_Conditional_2_Template_button_click_0_listener() {
      \u0275\u0275restoreView(_r6);
      const item_r7 = \u0275\u0275nextContext().$implicit;
      const ctx_r1 = \u0275\u0275nextContext(3);
      return \u0275\u0275resetView(ctx_r1.openPreview(item_r7.fileName || "\u767C\u7968", item_r7.fileUrl));
    });
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(1, "svg", 32);
    \u0275\u0275element(2, "use", 61);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const item_r7 = \u0275\u0275nextContext().$implicit;
    \u0275\u0275property("title", \u0275\u0275interpolate(item_r7.fileName || "\u767C\u7968"));
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_66_For_29_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "tr")(1, "td", 53);
    \u0275\u0275conditionalCreate(2, HolidayTravelDetail_Conditional_1_Conditional_66_For_29_Conditional_2_Template, 3, 2, "button", 54);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "td", 55);
    \u0275\u0275text(4);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(5, "td", 55);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "td", 55);
    \u0275\u0275text(8);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(9, "td", 56);
    \u0275\u0275text(10);
    \u0275\u0275pipe(11, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(12, "td", 55);
    \u0275\u0275text(13);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(14, "td", 57);
    \u0275\u0275text(15);
    \u0275\u0275pipe(16, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "td", 58);
    \u0275\u0275text(18);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "td", 59);
    \u0275\u0275text(20);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const item_r7 = ctx.$implicit;
    \u0275\u0275advance(2);
    \u0275\u0275conditional(item_r7.fileUrl ? 2 : -1);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r7.category || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r7.seqNo);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r7.itemName);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(11, 9, item_r7.unitPrice, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r7.quantity);
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(16, 12, item_r7.totalPrice, "1.0-0"));
    \u0275\u0275advance(3);
    \u0275\u0275textInterpolate(item_r7.invoiceNo || "\u2014");
    \u0275\u0275advance(2);
    \u0275\u0275textInterpolate(item_r7.note || "\u2014");
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_66_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 14)(1, "div", 15);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 16);
    \u0275\u0275element(3, "use", 43);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u8CBB\u7528\u660E\u7D30 ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 44)(6, "div", 45)(7, "table", 46)(8, "thead", 47)(9, "tr");
    \u0275\u0275element(10, "th", 48);
    \u0275\u0275elementStart(11, "th");
    \u0275\u0275text(12, "\u5206\u985E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(13, "th");
    \u0275\u0275text(14, "\u9805\u6B21");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(15, "th");
    \u0275\u0275text(16, "\u9805\u76EE\u8AAA\u660E");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(17, "th", 49);
    \u0275\u0275text(18, "\u55AE\u50F9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(19, "th");
    \u0275\u0275text(20, "\u6578\u91CF/\u55AE\u4F4D");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(21, "th", 49);
    \u0275\u0275text(22, "\u7E3D\u50F9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(23, "th");
    \u0275\u0275text(24, "\u767C\u7968\u865F\u78BC");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "th");
    \u0275\u0275text(26, "\u5099\u8A3B");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(27, "tbody");
    \u0275\u0275repeaterCreate(28, HolidayTravelDetail_Conditional_1_Conditional_66_For_29_Template, 21, 15, "tr", null, _forTrack1);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(30, "tfoot")(31, "tr", 47)(32, "td", 50);
    \u0275\u0275text(33, "\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(34, "td", 51);
    \u0275\u0275text(35);
    \u0275\u0275pipe(36, "number");
    \u0275\u0275elementEnd();
    \u0275\u0275element(37, "td", 52);
    \u0275\u0275elementEnd()()()()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(28);
    \u0275\u0275repeater(r_r3.items);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(36, 1, r_r3.grandTotal, "1.0-0"));
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_7_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23)(1, "div", 21);
    \u0275\u0275text(2, "\u9810\u8A08\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 25);
    \u0275\u0275text(4);
    \u0275\u0275pipe(5, "date");
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(4);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(5, 1, r_r3.estimatedPaymentDate, "yyyy-MM-dd"));
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_8_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23)(1, "div", 21);
    \u0275\u0275text(2, "\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "div", 65);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(4, "svg", 66);
    \u0275\u0275element(5, "use", 67);
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(6, "span", 68);
    \u0275\u0275text(7);
    \u0275\u0275pipe(8, "date");
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext(2);
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(8, 1, r_r3.paidAt, "yyyy-MM-dd"));
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_9_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 23)(1, "div", 21);
    \u0275\u0275text(2, "\u64A5\u6B3E\u65E5");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(3, "span", 21);
    \u0275\u0275text(4, "\u5C1A\u672A\u64A5\u6B3E");
    \u0275\u0275elementEnd()();
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_67_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 14)(1, "div", 15);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(2, "svg", 62);
    \u0275\u0275element(3, "use", 63);
    \u0275\u0275elementEnd();
    \u0275\u0275text(4, " \u64A5\u6B3E\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "div", 18)(6, "div", 64);
    \u0275\u0275conditionalCreate(7, HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_7_Template, 6, 4, "div", 23);
    \u0275\u0275conditionalCreate(8, HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_8_Template, 9, 4, "div", 23)(9, HolidayTravelDetail_Conditional_1_Conditional_67_Conditional_9_Template, 5, 0, "div", 23);
    \u0275\u0275elementEnd()()();
  }
  if (rf & 2) {
    const r_r3 = \u0275\u0275nextContext();
    \u0275\u0275advance(7);
    \u0275\u0275conditional(r_r3.estimatedPaymentDate ? 7 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.paidAt ? 8 : r_r3.estimatedPaymentDate ? 9 : -1);
  }
}
function HolidayTravelDetail_Conditional_1_Conditional_68_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275element(0, "app-approval-timeline", 29);
  }
  if (rf & 2) {
    const task_r8 = ctx;
    \u0275\u0275property("flow", task_r8.flow ?? null)("approvalRecords", task_r8.approvalRecords)("currentStepOrder", task_r8.currentStepOrder)("status", task_r8.status);
  }
}
function HolidayTravelDetail_Conditional_1_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 3)(1, "div", 4)(2, "a", 5);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(3, "svg", 6);
    \u0275\u0275element(4, "use", 7);
    \u0275\u0275elementEnd()();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(5, "h4", 8);
    \u0275\u0275text(6);
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(7, "span");
    \u0275\u0275text(8);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(9, "div", 9);
    \u0275\u0275conditionalCreate(10, HolidayTravelDetail_Conditional_1_Conditional_10_Template, 4, 2, "button", 10);
    \u0275\u0275conditionalCreate(11, HolidayTravelDetail_Conditional_1_Conditional_11_Template, 4, 3, "a", 11);
    \u0275\u0275conditionalCreate(12, HolidayTravelDetail_Conditional_1_Conditional_12_Template, 8, 2);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(13, "div", 12)(14, "div", 13)(15, "div", 14)(16, "div", 15);
    \u0275\u0275namespaceSVG();
    \u0275\u0275elementStart(17, "svg", 16);
    \u0275\u0275element(18, "use", 17);
    \u0275\u0275elementEnd();
    \u0275\u0275text(19, " \u6D3B\u52D5\u57FA\u672C\u8CC7\u8A0A ");
    \u0275\u0275elementEnd();
    \u0275\u0275namespaceHTML();
    \u0275\u0275elementStart(20, "div", 18)(21, "div", 19)(22, "div", 20)(23, "div", 21);
    \u0275\u0275text(24, "\u57F7\u884C\u6D3B\u52D5\u5730\u9EDE");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(25, "div", 22);
    \u0275\u0275text(26);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(27, "div", 23)(28, "div", 21);
    \u0275\u0275text(29, "\u5047\u65E5\u5929\u6578");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(30, "div");
    \u0275\u0275conditionalCreate(31, HolidayTravelDetail_Conditional_1_Conditional_31_Template, 2, 1, "span", 24)(32, HolidayTravelDetail_Conditional_1_Conditional_32_Template, 2, 0, "span", 21);
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(33, "div", 23)(34, "div", 21);
    \u0275\u0275text(35, "\u7533\u8ACB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(36, "div", 25);
    \u0275\u0275text(37);
    \u0275\u0275pipe(38, "date");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(39, "div", 19)(40, "div", 23)(41, "div", 21);
    \u0275\u0275text(42, "\u958B\u59CB\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(43, "div", 25);
    \u0275\u0275text(44);
    \u0275\u0275pipe(45, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275elementStart(46, "div", 23)(47, "div", 21);
    \u0275\u0275text(48, "\u7D50\u675F\u65E5\u671F");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(49, "div", 25);
    \u0275\u0275text(50);
    \u0275\u0275pipe(51, "date");
    \u0275\u0275elementEnd()();
    \u0275\u0275conditionalCreate(52, HolidayTravelDetail_Conditional_1_Conditional_52_Template, 5, 2, "div", 23);
    \u0275\u0275elementStart(53, "div", 23)(54, "div", 21);
    \u0275\u0275text(55, "\u91D1\u984D\u5408\u8A08");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(56, "div", 26);
    \u0275\u0275text(57);
    \u0275\u0275pipe(58, "number");
    \u0275\u0275elementEnd()()();
    \u0275\u0275elementStart(59, "div", 27)(60, "div", 28)(61, "div", 21);
    \u0275\u0275text(62, "\u6D3B\u52D5\u4E3B\u65E8\u53CA\u5167\u5BB9");
    \u0275\u0275elementEnd();
    \u0275\u0275elementStart(63, "div", 25);
    \u0275\u0275text(64);
    \u0275\u0275elementEnd()()()()();
    \u0275\u0275conditionalCreate(65, HolidayTravelDetail_Conditional_1_Conditional_65_Template, 9, 0, "div", 14);
    \u0275\u0275conditionalCreate(66, HolidayTravelDetail_Conditional_1_Conditional_66_Template, 38, 4, "div", 14);
    \u0275\u0275conditionalCreate(67, HolidayTravelDetail_Conditional_1_Conditional_67_Template, 10, 2, "div", 14);
    \u0275\u0275conditionalCreate(68, HolidayTravelDetail_Conditional_1_Conditional_68_Template, 1, 4, "app-approval-timeline", 29);
    \u0275\u0275elementEnd()();
  }
  if (rf & 2) {
    let tmp_19_0;
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
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(38, 19, r_r3.createdAt, "yyyy-MM-dd"));
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(45, 22, r_r3.startDate, "yyyy-MM-dd"));
    \u0275\u0275advance(6);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(51, 25, r_r3.endDate, "yyyy-MM-dd"));
    \u0275\u0275advance(2);
    \u0275\u0275conditional(r_r3.projectCode || r_r3.projectName ? 52 : -1);
    \u0275\u0275advance(5);
    \u0275\u0275textInterpolate(\u0275\u0275pipeBind2(58, 28, r_r3.grandTotal, "1.0-0"));
    \u0275\u0275advance(7);
    \u0275\u0275textInterpolate(r_r3.purpose);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.participants && r_r3.participants.length > 0 ? 65 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.items && r_r3.items.length > 0 ? 66 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional(r_r3.estimatedPaymentDate || r_r3.paidAt ? 67 : -1);
    \u0275\u0275advance();
    \u0275\u0275conditional((tmp_19_0 = ctx_r1.approvalTask()) ? 68 : -1, tmp_19_0);
  }
}
function HolidayTravelDetail_Conditional_2_Template(rf, ctx) {
  if (rf & 1) {
    \u0275\u0275elementStart(0, "div", 1);
    \u0275\u0275text(1, "\u8F09\u5165\u4E2D\u2026");
    \u0275\u0275elementEnd();
  }
}
function HolidayTravelDetail_Conditional_3_Template(rf, ctx) {
  if (rf & 1) {
    const _r9 = \u0275\u0275getCurrentView();
    \u0275\u0275elementStart(0, "app-file-preview-modal", 69);
    \u0275\u0275listener("closed", function HolidayTravelDetail_Conditional_3_Template_app_file_preview_modal_closed_0_listener() {
      \u0275\u0275restoreView(_r9);
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
var HolidayTravelDetail = class _HolidayTravelDetail {
  service = inject(HolidayTravelRequestService);
  pdfService = inject(HolidayTravelPdfService);
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
      this.pdfService.printHolidayTravelRequest(r, t?.submittedBy ?? "", t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl, void 0, r.paidAt, t?.travelDetail?.paidBySignatureUrl);
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
  static \u0275cmp = /* @__PURE__ */ \u0275\u0275defineComponent({ type: _HolidayTravelDetail, selectors: [["app-holiday-travel-detail"]], decls: 4, vars: 2, consts: [[1, "container-fluid", "py-3"], [1, "text-center", "py-6", "text-muted"], [3, "file"], [1, "flex", "flex-wrap", "items-center", "justify-between", "gap-2", "mb-6"], [1, "flex", "items-center", "gap-2", "flex-wrap"], ["routerLink", "/admin/holiday-travel-requests", 1, "btn", "btn-sm", "btn-outline-secondary"], [1, "sa-icon"], ["href", "/assets/icons/sprite.svg#arrow-left"], [1, "mb-0"], [1, "flex", "flex-wrap", "gap-2"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "disabled"], [1, "btn", "btn-outline-primary", "inline-flex", "items-center", "gap-1", 3, "routerLink"], [1, "row", "g-4"], [1, "col-12", "col-xl-10"], [1, "card", "border-0", "shadow-sm", "mb-6"], [1, "card-header", "bg-transparent", "border-bottom", "flex", "items-center", "gap-2", "fw-600"], [1, "sa-icon", "text-primary", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#sun"], [1, "card-body"], [1, "row", "g-3", "mb-4"], [1, "col-12", "col-md-6"], [1, "text-muted", "small"], [1, "fw-600"], [1, "col-6", "col-md-3"], [1, "fw-600", 2, "color", "var(--forest)"], [1, "fw-500"], [1, "fw-600", "text-lg"], [1, "row", "g-3", "mb-0"], [1, "col-12"], [3, "flow", "approvalRecords", "currentStepOrder", "status"], [1, "btn", "btn-outline-secondary", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["role", "status", 1, "spinner-border", "spinner-border-sm"], [1, "sa-icon", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#printer"], ["href", "/assets/icons/sprite.svg#edit"], [1, "btn", "btn-primary", "inline-flex", "items-center", "gap-1", 3, "click"], ["href", "/assets/icons/sprite.svg#send"], [1, "btn", "btn-outline-danger", "inline-flex", "items-center", "gap-1", 3, "click", "disabled"], ["href", "/assets/icons/sprite.svg#trash-2"], [1, "fw-500", "font-monospace"], ["href", "/assets/icons/sprite.svg#users"], [1, "mb-0", "ps-4"], [1, "small", "fw-500"], ["href", "/assets/icons/sprite.svg#list"], [1, "card-body", "p-0"], [1, "table-responsive"], [1, "table", "table-sm", "mb-0"], [1, "table-light"], [2, "width", "40px"], [1, "text-right"], ["colspan", "6", 1, "text-right", "fw-500", "small"], [1, "text-right", "fw-600"], ["colspan", "2"], [1, "align-middle", "text-center"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "title"], [1, "small"], [1, "text-right", "small"], [1, "text-right", "small", "fw-500"], [1, "small", "font-monospace"], [1, "small", "text-muted"], ["type", "button", 1, "btn", "btn-sm", "btn-ghost-secondary", "p-1", 3, "click", "title"], ["href", "/assets/icons/sprite.svg#file-text"], [1, "sa-icon", "text-muted", 2, "stroke", "currentColor"], ["href", "/assets/icons/sprite.svg#credit-card"], [1, "row", "g-3"], [1, "flex", "items-center", "gap-2"], [1, "sa-icon", 2, "color", "var(--green)", "stroke", "currentColor", "width", "16px", "height", "16px"], ["href", "/assets/icons/sprite.svg#check-circle"], [1, "fw-500", 2, "color", "var(--green)"], [3, "closed", "file"]], template: function HolidayTravelDetail_Template(rf, ctx) {
    if (rf & 1) {
      \u0275\u0275elementStart(0, "div", 0);
      \u0275\u0275conditionalCreate(1, HolidayTravelDetail_Conditional_1_Template, 69, 31)(2, HolidayTravelDetail_Conditional_2_Template, 2, 0, "div", 1);
      \u0275\u0275elementEnd();
      \u0275\u0275conditionalCreate(3, HolidayTravelDetail_Conditional_3_Template, 1, 1, "app-file-preview-modal", 2);
    }
    if (rf & 2) {
      let tmp_0_0;
      \u0275\u0275advance();
      \u0275\u0275conditional((tmp_0_0 = ctx.request()) ? 1 : 2, tmp_0_0);
      \u0275\u0275advance(2);
      \u0275\u0275conditional(ctx.previewFile ? 3 : -1);
    }
  }, dependencies: [RouterLink, ApprovalTimeline, FilePreviewModal, DecimalPipe, DatePipe], encapsulation: 2 });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(HolidayTravelDetail, [{
    type: Component,
    args: [{ selector: "app-holiday-travel-detail", imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal], template: `<div class="container-fluid py-3">
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
          <div class="col-6 col-md-3">
            <div class="text-muted small">\u91D1\u984D\u5408\u8A08</div>
            <div class="fw-600 text-lg">{{ r.grandTotal | number:'1.0-0' }}</div>
          </div>
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
                  <th>\u767C\u7968\u865F\u78BC</th>
                  <th>\u5099\u8A3B</th>
                </tr>
              </thead>
              <tbody>
                @for (item of r.items; track item.id) {
                  <tr>
                    <td class="align-middle text-center">
                      @if (item.fileUrl) {
                        <button type="button" class="btn btn-sm btn-ghost-secondary p-1"
                                (click)="openPreview(item.fileName || '\u767C\u7968', item.fileUrl)"
                                title="{{ item.fileName || '\u767C\u7968' }}">
                          <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
                        </button>
                      }
                    </td>
                    <td class="small">{{ item.category || '\u2014' }}</td>
                    <td class="small">{{ item.seqNo }}</td>
                    <td class="small">{{ item.itemName }}</td>
                    <td class="text-right small">{{ item.unitPrice | number:'1.0-0' }}</td>
                    <td class="small">{{ item.quantity }}</td>
                    <td class="text-right small fw-500">{{ item.totalPrice | number:'1.0-0' }}</td>
                    <td class="small font-monospace">{{ item.invoiceNo || '\u2014' }}</td>
                    <td class="small text-muted">{{ item.note || '\u2014' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr class="table-light">
                  <td colspan="6" class="text-right fw-500 small">\u5408\u8A08</td>
                  <td class="text-right fw-600">{{ r.grandTotal | number:'1.0-0' }}</td>
                  <td colspan="2"></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>
    }

    <!-- \u64A5\u6B3E\u8CC7\u8A0A\uFF08\u5DF2\u6838\u51C6\u5F8C\u986F\u793A\uFF09 -->
    @if (r.estimatedPaymentDate || r.paidAt) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-muted" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#credit-card"></use>
          </svg>
          \u64A5\u6B3E\u8CC7\u8A0A
        </div>
        <div class="card-body">
          <div class="row g-3">
            @if (r.estimatedPaymentDate) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">\u9810\u8A08\u64A5\u6B3E\u65E5</div>
                <div class="fw-500">{{ r.estimatedPaymentDate | date:'yyyy-MM-dd' }}</div>
              </div>
            }
            @if (r.paidAt) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">\u64A5\u6B3E\u65E5</div>
                <div class="flex items-center gap-2">
                  <svg class="sa-icon" style="color: var(--green); stroke: currentColor; width: 16px; height: 16px">
                    <use href="/assets/icons/sprite.svg#check-circle"></use>
                  </svg>
                  <span class="fw-500" style="color: var(--green)">{{ r.paidAt | date:'yyyy-MM-dd' }}</span>
                </div>
              </div>
            } @else if (r.estimatedPaymentDate) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">\u64A5\u6B3E\u65E5</div>
                <span class="text-muted small">\u5C1A\u672A\u64A5\u6B3E</span>
              </div>
            }
          </div>
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

@if (previewFile) {
  <app-file-preview-modal [file]="previewFile" (closed)="closePreview()" />
}
` }]
  }], null, null);
})();
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && \u0275setClassDebugInfo(HolidayTravelDetail, { className: "HolidayTravelDetail", filePath: "src/app/features/admin/holiday-travel-requests/pages/holiday-travel-detail/holiday-travel-detail.ts", lineNumber: 18 });
})();
export {
  HolidayTravelDetail
};
//# sourceMappingURL=chunk-WJL7UGNO.js.map
