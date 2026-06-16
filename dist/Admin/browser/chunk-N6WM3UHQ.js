import {
  CIS,
  FONT_FAMILY,
  PdfCoreService,
  buildDynamicSignBlocks,
  fmtDT
} from "./chunk-346USOMS.js";
import {
  Injectable,
  inject,
  setClassMetadata,
  signal,
  ɵɵdefineInjectable
} from "./chunk-IFQ7CN6S.js";

// src/app/features/admin/payment-requests/services/payment-pdf.service.ts
var PaymentPdfService = class _PaymentPdfService {
  pdfLoading = signal(false, ...ngDevMode ? [{ debugName: "pdfLoading" }] : []);
  pdfCore = inject(PdfCoreService);
  /** 列印請款單 PDF */
  async printPaymentRequest(task) {
    if (!task.paymentDetail || task.status !== "approved")
      return;
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import("./chunk-4QY6N5TU.js"),
        import("./chunk-JZJ3IRCJ.js"),
        this.pdfCore.loadFonts()
      ]);
      const doc = new jsPDF("portrait", "mm", "a4");
      const F = FONT_FAMILY;
      this.pdfCore.registerFonts(doc, fonts);
      const pw = doc.internal.pageSize.getWidth();
      const ph = doc.internal.pageSize.getHeight();
      const mx = 18;
      const cw = pw - mx * 2;
      const d = task.paymentDetail;
      const fmt = (n) => n.toLocaleString("zh-TW");
      let y = 22;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);
      y += 12;
      const titleMap = { vendor: "\u5EE0 \u5546 \u8ACB \u6B3E \u55AE", travel: "\u54E1 \u5DE5 \u5DEE \u65C5 \u8ACB \u6B3E \u55AE" };
      const pdfTitle = titleMap[d.paymentType] || "\u8ACB \u6B3E \u55AE";
      doc.setFont(F, "bold");
      doc.setFontSize(20);
      doc.setTextColor(...CIS.forest);
      doc.text(pdfTitle, pw / 2, y, { align: "center" });
      doc.setFont(F, "normal");
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textMuted);
      doc.text(`\u55AE\u865F\uFF1A${d.requestNo}`, pw - mx, y, { align: "right" });
      doc.setTextColor(...CIS.textPrimary);
      y += 12;
      doc.setFont(F, "normal");
      doc.setFontSize(10);
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
      const submitDate = fmtDT(task.submittedAt);
      const payerLabel = d.paymentType === "vendor" ? "\u8ACB\u6B3E\u4EBA\uFF1A" : "\u53D7\u6B3E\u4EBA\uFF1A";
      lv(payerLabel, task.submittedBy, mx, y, true);
      lv("\u7533\u8ACB\u65E5\u671F\uFF1A", submitDate, pw - mx - 55, y, true);
      y += 8;
      const invoices = d.invoices || [];
      const bodyRows = invoices.map((inv) => [
        d.projectCode,
        inv.invoiceNo || "\u2014",
        inv.itemName || "\u2014",
        fmt(inv.amount),
        inv.note || ""
      ]);
      bodyRows.push([
        { content: "\u5408\u3000\u8A08", colSpan: 3, styles: { halign: "center", fontStyle: "bold" } },
        { content: fmt(d.totalAmount), styles: { fontStyle: "bold" } },
        ""
      ]);
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx, top: 20 },
        theme: "grid",
        showHead: "everyPage",
        styles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: { top: 3, bottom: 3, left: 4, right: 4 }
        },
        headStyles: {
          font: F,
          fillColor: [...CIS.forest],
          textColor: 255,
          fontSize: 9.5,
          fontStyle: "bold",
          halign: "center",
          cellPadding: { top: 4, bottom: 4, left: 4, right: 4 }
        },
        bodyStyles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary]
        },
        columnStyles: {
          0: { cellWidth: cw * 0.14, halign: "center" },
          // 案號
          1: { cellWidth: cw * 0.16, halign: "center" },
          // 發票號碼
          2: { cellWidth: cw * 0.3 },
          // 項目
          3: { cellWidth: cw * 0.16, halign: "right" },
          // 金額
          4: { cellWidth: cw * 0.24 }
          // 備註
        },
        head: [["\u6848 \u865F", "\u767C\u7968\u865F\u78BC", "\u9805\u3000\u3000\u76EE", "\u91D1\u3000\u984D", "\u5099\u3000\u8A3B"]],
        body: bodyRows
      });
      if (d.paymentType === "vendor") {
        const dash = (v) => v?.trim() || "\u2014";
        const vendorTableY = doc.lastAutoTable.finalY + 6;
        autoTable(doc, {
          startY: vendorTableY,
          margin: { left: mx, right: mx, top: 20 },
          theme: "grid",
          styles: {
            font: F,
            fontSize: 9,
            textColor: [...CIS.textPrimary],
            lineColor: [...CIS.border],
            lineWidth: 0.3,
            cellPadding: { top: 3, bottom: 3, left: 4, right: 4 }
          },
          headStyles: {
            font: F,
            fillColor: [...CIS.forest],
            textColor: 255,
            fontSize: 9.5,
            fontStyle: "bold",
            halign: "center",
            cellPadding: { top: 4, bottom: 4, left: 4, right: 4 }
          },
          columnStyles: {
            0: { cellWidth: cw * 0.14, fontStyle: "bold", halign: "right", fillColor: [248, 250, 247] },
            1: { cellWidth: cw * 0.36 },
            2: { cellWidth: cw * 0.14, fontStyle: "bold", halign: "right", fillColor: [248, 250, 247] },
            3: { cellWidth: cw * 0.36 }
          },
          head: [[{ content: "\u53D7\u6B3E\u4EBA\u8CC7\u8A0A", colSpan: 4, styles: { halign: "center" } }]],
          body: [
            ["\u5EE0\u5546\u540D\u7A31", dash(d.vendorName), "\u7D71\u3000\u3000\u7DE8", dash(d.vendorTaxId)],
            ["\u806F\u3000\u7D61\u3000\u4EBA", dash(d.vendorContactPerson), "\u806F\u7D61\u96FB\u8A71", dash(d.vendorPhone)],
            ["\u5E33\u6236\u8CC7\u6599", { content: dash(d.vendorBankAccount), colSpan: 3 }],
            ["\u516C\u53F8\u5730\u5740", { content: dash(d.vendorAddress), colSpan: 3 }]
          ]
        });
      }
      const tableEndY = doc.lastAutoTable.finalY;
      y = tableEndY + 6;
      doc.setFont(F, "normal");
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      if (d.installments && d.installments.length > 0) {
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
          body: d.installments.map((ins) => [
            String(ins.installmentNo),
            ins.expectedDate ? fmtDT(ins.expectedDate).split(" ")[0] : "\u2014",
            ins.paidAt ? fmtDT(ins.paidAt).split(" ")[0] : "\u5C1A\u672A\u64A5\u6B3E",
            fmt(ins.amount),
            ins.note || ""
          ])
        });
        y = doc.lastAutoTable.finalY + 6;
      } else {
        lv("\u64A5\u6B3E\u8CC7\u8A0A\uFF1A", "\u5C1A\u672A\u6392\u5B9A\u64A5\u6B3E", mx, y, true);
        y += 6;
      }
      y += 6;
      const signBlocks = this._buildSignBlocks(task, submitDate);
      const sigImageMap = await this.pdfCore.loadSignatureImages(signBlocks);
      if (y + 40 > ph - 20) {
        doc.addPage();
        y = 30;
      }
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigImageMap, { gap: 4, labelSize: 9, maxH: 12, padding: 4 });
      const bottomY = y + 34;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, bottomY, pw - mx, bottomY);
      doc.setLineWidth(0.8);
      doc.line(mx, bottomY + 1.5, pw - mx, bottomY + 1.5);
      doc.save(`\u8ACB\u6B3E\u55AE-${d.requestNo}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }
  /** 根據 flow steps 動態建立簽名欄資料 */
  _buildSignBlocks(task, submitDate) {
    return buildDynamicSignBlocks({
      flow: task.flow,
      records: task.approvalRecords || [],
      submittedBySignatureUrl: task.submittedBySignatureUrl,
      submitDate,
      applicantLabel: "\u8ACB\u6B3E\u4EBA",
      cashier: (() => {
        const lastPaid = task.paymentDetail?.installments?.filter((i) => i.paidAt).slice(-1)[0];
        return {
          paidBySignatureUrl: lastPaid?.paidBySignatureUrl,
          paidAt: lastPaid?.paidAt
        };
      })()
    });
  }
  static \u0275fac = function PaymentPdfService_Factory(__ngFactoryType__) {
    return new (__ngFactoryType__ || _PaymentPdfService)();
  };
  static \u0275prov = /* @__PURE__ */ \u0275\u0275defineInjectable({ token: _PaymentPdfService, factory: _PaymentPdfService.\u0275fac, providedIn: "root" });
};
(() => {
  (typeof ngDevMode === "undefined" || ngDevMode) && setClassMetadata(PaymentPdfService, [{
    type: Injectable,
    args: [{ providedIn: "root" }]
  }], null, null);
})();

export {
  PaymentPdfService
};
//# sourceMappingURL=chunk-N6WM3UHQ.js.map
