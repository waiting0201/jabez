import {
  CIS,
  FONT_FAMILY,
  PdfCoreService,
  fmt,
  fmtDT,
  fmtDate
} from "./chunk-NXTOFNYH.js";
import {
  Injectable,
  inject,
  setClassMetadata,
  signal,
  ɵɵdefineInjectable
} from "./chunk-GQPYF5UN.js";

// src/app/features/admin/travel-payment-requests/services/travel-payment-pdf.service.ts
var TravelPaymentPdfService = class _TravelPaymentPdfService {
  pdfLoading = signal(false, ...ngDevMode ? [{ debugName: "pdfLoading" }] : []);
  pdfCore = inject(PdfCoreService);
  /** 列印出差請款申請單 */
  async printTravelPaymentRequest(r, submittedByName, approvalRecords = [], flow, submittedBySignatureUrl, reviewerSignatureUrls, paidAt, paidBySignatureUrl) {
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
      doc.text("\u51FA \u5DEE \u8ACB \u6B3E \u7533 \u8ACB \u55AE", pw / 2, y, { align: "center" });
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
      lv("\u51FA\u5DEE\u5730\u9EDE\uFF1A", r.destination, pw / 2, y, true);
      y += 6;
      lv("\u51FA\u5DEE\u671F\u9593\uFF1A", `${startDate} \uFF5E ${endDate}`, mx, y);
      lv("\u91D1\u984D\u5408\u8A08\uFF1A", `NT$ ${fmt(r.grandTotal)}`, pw - mx - 60, y, true);
      y += 6;
      if (r.projectCode || r.projectName) {
        lv("\u95DC\u806F\u5C08\u6848\uFF1A", `${r.projectCode ?? ""}${r.projectName ? " - " + r.projectName : ""}`, mx, y, true);
      }
      y += 6;
      lv("\u51FA\u5DEE\u76EE\u7684\uFF1A", r.purpose || "", mx, y);
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
          item.note || "",
          item.invoiceNo || "",
          item.invoiceDate ? fmtDate(item.invoiceDate) : ""
        ]);
      }
      bodyRows.push([
        { content: "\u5408\u8A08", colSpan: 5, styles: { halign: "right", fontStyle: "bold" } },
        { content: fmt(r.grandTotal), styles: { fontStyle: "bold", halign: "right" } },
        "",
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
          2: { cellWidth: cw * 0.22 },
          // 項目說明
          3: { cellWidth: cw * 0.1, halign: "right" },
          // 單價
          4: { cellWidth: cw * 0.09, halign: "center" },
          // 數量
          5: { cellWidth: cw * 0.1, halign: "right" },
          // 總價
          6: { cellWidth: cw * 0.16 },
          // 備註
          7: { cellWidth: cw * 0.1, halign: "center" },
          // 發票號碼
          8: { cellWidth: cw * 0.1, halign: "center" }
          // 發票日期
        },
        head: [["\u5206\u985E", "\u9805\u6B21", "\u9805\u76EE\u8AAA\u660E", "\u55AE\u50F9", "\u6578\u91CF/\u55AE\u4F4D", "\u7E3D\u50F9", "\u5099\u8A3B", "\u767C\u7968\u865F\u78BC", "\u767C\u7968\u65E5\u671F"]],
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
      doc.save(`\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\u55AE-${r.id}.pdf`);
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
  static \u0275fac = function TravelPaymentPdfService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _TravelPaymentPdfService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _TravelPaymentPdfService, factory: _TravelPaymentPdfService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(TravelPaymentPdfService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  TravelPaymentPdfService
};
//# sourceMappingURL=chunk-ODOUDVYE.js.map
