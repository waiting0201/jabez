import { Injectable, inject, signal } from '@angular/core';
import { TravelWriteOffRequest } from '../models/travel-write-off-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmtDate, fmt, buildDynamicSignBlocks, designatedStepOrdersOf } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class TravelWriteOffPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印出差沖銷申請表 */
  async printTravelWriteOff(r: TravelWriteOffRequest, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow, submittedBySignatureUrl?: string) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.pdfCore.loadFonts(),
      ]);

      const doc = new jsPDF('landscape', 'mm', 'a4');
      const F = FONT_FAMILY;
      this.pdfCore.registerFonts(doc, fonts);

      const pw = doc.internal.pageSize.getWidth();   // 297
      const ph = doc.internal.pageSize.getHeight();   // 210
      const mx = 14;
      const cw = pw - mx * 2;

      let y = 16;

      // ── 頂部裝飾線 ──
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      // ── 公司名稱 + 標題 ──
      y += 10;
      doc.setFont(F, 'bold');
      doc.setFontSize(11);
      doc.setTextColor(...CIS.textPrimary);
      doc.text('雅比斯國際創意策略股份有限公司', pw / 2, y, { align: 'center' });
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text('出 差 預 支 沖 銷 申 請 表', pw / 2, y, { align: 'center' });

      // ── 表頭資訊 ──
      y += 10;
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textPrimary);

      /** 畫標籤+值，值緊貼冒號後 */
      const lv = (label: string, value: string, x: number, yy: number, bold = false) => {
        doc.setFont(F, 'normal');
        doc.text(label, x, yy);
        const lw = doc.getTextWidth(label);
        if (bold) doc.setFont(F, 'bold');
        doc.text(value, x + lw, yy);
        doc.setFont(F, 'normal');
      };

      lv('申請人：', submittedByName, mx, y, true);
      lv('案號：', r.projectCode || '', pw - mx - 50, y, true);

      y += 6;
      lv('沖銷單號：', r.requestNo || '—', mx, y);
      lv('關聯出差單號：', r.travelRequestNo, pw / 2, y);

      y += 6;
      lv('目的地：', r.destination, mx, y, true);

      y += 6;
      const dateRange = `${fmtDate(r.startDate)} ~ ${fmtDate(r.endDate)}`;
      lv('出差期間：', dateRange, mx, y);

      y += 6;
      lv('出差目的：', r.purpose, mx, y);

      // ── 明細表格 ──
      y += 8;
      const items = r.items || [];

      // 建立表格資料：按分類分組
      const bodyRows: any[][] = [];
      let lastCategory = '';
      for (const item of items) {
        const cat = item.category === lastCategory ? '' : item.category;
        lastCategory = item.category;
        bodyRows.push([
          cat,
          item.seqNo.toString(),
          item.itemName,
          `${fmt(item.unitPrice)}元`,
          item.quantity,
          fmt(item.totalPrice),
          item.invoiceNo || '',
          item.note || '',
        ]);
      }

      // 合計列
      bodyRows.push([
        { content: '總計', colSpan: 5, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.grandTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);

      // 出差金額與沖銷結餘摘要
      const totalWrittenOff = r.travelWrittenOffTotal + r.grandTotal;
      const refundedAmount = r.travelRefundedAmount ?? 0;
      const balance = r.travelGrandTotal - totalWrittenOff + refundedAmount;
      bodyRows.push([
        { content: '出差金額', colSpan: 5, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.travelGrandTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);
      if (r.travelWrittenOffTotal > 0) {
        bodyRows.push([
          { content: '前次已沖銷', colSpan: 5, styles: { halign: 'right' } },
          { content: fmt(r.travelWrittenOffTotal), styles: { halign: 'right' } },
          '',
          '',
        ]);
      }
      if (refundedAmount > 0) {
        bodyRows.push([
          { content: '實際撥款', colSpan: 5, styles: { halign: 'right' } },
          { content: fmt(refundedAmount), styles: { halign: 'right' } },
          '',
          '',
        ]);
      }
      bodyRows.push([
        { content: '結餘', colSpan: 5, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(balance), styles: { fontStyle: 'bold', halign: 'right', textColor: balance < 0 ? [...CIS.red] : [...CIS.textPrimary] } },
        '',
        '',
      ]);

      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        theme: 'grid',
        styles: {
          font: F, fontSize: 8.5,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: { top: 2.5, bottom: 2.5, left: 3, right: 3 },
        },
        headStyles: {
          font: F, fillColor: [...CIS.forest], textColor: 255,
          fontSize: 9, fontStyle: 'bold', halign: 'center',
          cellPadding: { top: 3, bottom: 3, left: 3, right: 3 },
        },
        columnStyles: {
          0: { cellWidth: cw * 0.07, halign: 'center' },  // 分類
          1: { cellWidth: cw * 0.04, halign: 'center' },  // 項次
          2: { cellWidth: cw * 0.22 },                     // 項目(說明)
          3: { cellWidth: cw * 0.09, halign: 'right' },    // 單價
          4: { cellWidth: cw * 0.07, halign: 'center' },   // 數量/單位
          5: { cellWidth: cw * 0.12, halign: 'right' },    // 總價
          6: { cellWidth: cw * 0.16, halign: 'center' },   // 發票號碼
          7: { cellWidth: cw * 0.23 },                     // 備註
        },
        head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '發票號碼', '備註']],
        body: bodyRows,
      });

      // ── 簽名欄 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 10;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      // 注意事項
      doc.setFont(F, 'normal');
      doc.setFontSize(7.5);
      doc.setTextColor(...CIS.red);
      doc.text('(報銷時需檢附出差日期、目的地及出差目的；', mx, y);
      y += 4;
      doc.text(' 報銷時請附交通票據及住宿收據，並附上相關發票正本。 )', mx, y);

      y += 8;
      const submitDate = r.submittedAt ? fmtDT(r.submittedAt) : '';
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, '申請者', designatedStepOrdersOf(r.designatedReviewers));
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`出差預支沖銷申請表-${r.requestNo || r.id}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 根據 flow steps 動態建立簽名欄資料 */
  private _buildSignBlocks(
    flow: ApprovalFlow | undefined,
    records: ApprovalRecord[],
    submittedBySignatureUrl: string | undefined,
    submitDate: string,
    applicantLabel: string,
    designatedStepOrders: number[] = [],
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      designatedStepOrders,
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
    });
  }
}
