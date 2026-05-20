import {
  CIS,
  FONT_FAMILY,
  PdfCoreService,
  buildDynamicSignBlocks,
  fmt,
  fmtDT,
  fmtDate
} from "./chunk-346USOMS.js";
import {
  Injectable,
  inject,
  setClassMetadata,
  signal,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/travel-payment-requests/services/travel-payment-pdf.service.ts
var TravelPaymentPdfService = class _TravelPaymentPdfService {
  pdfLoading = signal(false, ...ngDevMode ? [{ debugName: "pdfLoading" }] : []);
  pdfCore = inject(PdfCoreService);
  /** 列印出差請款申請單 */
  async printTravelPaymentRequest(r, submittedByName, approvalRecords = [], flow, submittedBySignatureUrl, reviewerSignatureUrls) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import("./chunk-4QY6N5TU.js"),
        import("./chunk-JZJ3IRCJ.js"),
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
      doc.setFont(F, "normal");
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textMuted);
      doc.text(`\u55AE\u865F\uFF1A${r.requestNo}`, pw - mx, y, { align: "right" });
      doc.setTextColor(...CIS.textPrimary);
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
      y = tableEndY + 6;
      doc.setFont(F, "normal");
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      if (r.installments && r.installments.length > 0) {
        autoTable(doc, {
          startY: y,
          margin: { left: mx, right: mx, top: 20 },
          theme: "grid",
          styles: { font: F, fontSize: 9, textColor: [...CIS.textPrimary], lineColor: [...CIS.border], lineWidth: 0.3, cellPadding: { top: 3, bottom: 3, left: 4, right: 4 } },
          headStyles: { font: F, fillColor: [...CIS.forest], textColor: 255, fontSize: 9.5, fontStyle: "bold", halign: "center", cellPadding: { top: 4, bottom: 4, left: 4, right: 4 } },
          columnStyles: {
            0: { cellWidth: cw * 0.08, halign: "center" },
            1: { cellWidth: cw * 0.18, halign: "center" },
            2: { cellWidth: cw * 0.18, halign: "center" },
            3: { cellWidth: cw * 0.18, halign: "right" },
            4: { cellWidth: cw * 0.38 }
          },
          head: [[{ content: "\u64A5\u6B3E\u660E\u7D30", colSpan: 5, styles: { halign: "center" } }], ["\u671F\u6578", "\u9810\u8A08\u64A5\u6B3E\u65E5", "\u5BE6\u969B\u64A5\u6B3E\u65E5", "\u91D1\u3000\u984D", "\u5099\u3000\u8A3B"]],
          body: r.installments.map((ins) => [
            String(ins.installmentNo),
            ins.expectedDate ? fmtDT(ins.expectedDate).split(" ")[0] : "\u2014",
            ins.paidAt ? fmtDT(ins.paidAt).split(" ")[0] : "\u5C1A\u672A\u64A5\u6B3E",
            ins.amount.toLocaleString("zh-TW"),
            ins.note || ""
          ])
        });
        y = doc.lastAutoTable.finalY + 4;
      } else {
        lv("\u64A5\u6B3E\u8CC7\u8A0A\uFF1A", "\u5C1A\u672A\u6392\u5B9A\u64A5\u6B3E", mx, y, true);
      }
      y += 10;
      if (y + 35 > ph - 15) {
        doc.addPage();
        y = 20;
      }
      const submitDate = r.createdAt ? fmtDT(r.createdAt) : "";
      const lastPaid = r.installments?.filter((i) => i.paidAt).slice(-1)[0];
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, "\u7533\u8ACB\u8005", lastPaid?.paidAt, lastPaid?.paidBySignatureUrl);
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);
      doc.save(`\u51FA\u5DEE\u8ACB\u6B3E\u7533\u8ACB\u55AE-${r.requestNo}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }
  /** 根據 flow steps 動態建立簽名欄資料 */
  _buildSignBlocks(flow, records, submittedBySignatureUrl, submitDate, applicantLabel, paidAt, paidBySignatureUrl) {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
      cashier: { paidBySignatureUrl, paidAt }
    });
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
//# sourceMappingURL=chunk-ZAYEBMQK.js.map
