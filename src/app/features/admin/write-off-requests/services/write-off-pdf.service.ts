import { Injectable, inject, signal } from '@angular/core';
import { WriteOffRequest } from '../models/write-off-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmt, buildDynamicSignBlocks } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class WriteOffPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印預支沖銷申請表 */
  async printWriteOff(r: WriteOffRequest, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow, submittedBySignatureUrl?: string, refundedAt?: string, refundedBySignatureUrl?: string) {
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
      doc.text('經 費 沖 銷 申 請 表', pw / 2, y, { align: 'center' });

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
      lv('案號：', r.projectCode, pw - mx - 50, y, true);

      y += 6;
      lv('沖銷單號：', r.requestNo, mx, y);
      lv('關聯預支單號：', r.advanceRequestNo, pw / 2, y);

      y += 6;
      lv('案名：', r.projectCode, mx, y, true);

      y += 6;
      lv('活動名稱：', r.activityName, mx, y);
      y += 6;
      lv('活動期間：', r.activityPeriod, mx, y);

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
          fmt(item.cashAmount),
          item.checkAmount > 0 ? fmt(item.checkAmount) : '',
          item.invoiceNo || '',
          item.note || '',
        ]);
      }

      // 合計列
      bodyRows.push([
        { content: '沖 銷 現 金 數', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
        '',
      ]);
      bodyRows.push([
        { content: '月結支票金額', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        '',
        { content: r.checkTotal > 0 ? fmt(r.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);
      bodyRows.push([
        { content: '總計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.grandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);

      // 預支金額與沖銷結餘摘要
      const totalWrittenOff = r.advanceWrittenOffTotal + r.grandTotal;
      const refundedAmount = r.advanceRefundedAmount ?? 0;
      const balance = r.advanceGrandTotal - totalWrittenOff + refundedAmount;
      bodyRows.push([
        { content: '預支金額', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.advanceGrandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);
      if (r.advanceWrittenOffTotal > 0) {
        bodyRows.push([
          { content: '前次已沖銷', colSpan: 6, styles: { halign: 'right' } },
          { content: fmt(r.advanceWrittenOffTotal), colSpan: 2, styles: { halign: 'right' } },
          '',
          '',
        ]);
      }
      if (refundedAmount > 0) {
        bodyRows.push([
          { content: '實際退款', colSpan: 6, styles: { halign: 'right' } },
          { content: fmt(refundedAmount), colSpan: 2, styles: { halign: 'right' } },
          '',
          '',
        ]);
      }
      bodyRows.push([
        { content: '結餘', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(balance), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right', textColor: balance < 0 ? [...CIS.red] : [...CIS.textPrimary] } },
        '',
        '',
      ]);
      if (r.advanceIsClosed && (r.advanceGrandTotal - totalWrittenOff) < 0) {
        const fmtDate = (v?: string) => v ? new Date(v).toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Taipei' }) : '尚未設定';
        bodyRows.push([
          { content: '預計退款日', colSpan: 6, styles: { halign: 'right' } },
          { content: fmtDate(r.estimatedRefundDate), colSpan: 2, styles: { halign: 'right' } },
          '',
          '',
        ]);
        bodyRows.push([
          { content: '退款日', colSpan: 6, styles: { halign: 'right' } },
          { content: r.refundedAt ? fmtDate(r.refundedAt) : '尚未退款', colSpan: 2, styles: { halign: 'right' } },
          '',
          '',
        ]);
      }

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
          2: { cellWidth: cw * 0.19 },                     // 項目(說明)
          3: { cellWidth: cw * 0.09, halign: 'right' },    // 單價
          4: { cellWidth: cw * 0.07, halign: 'center' },   // 數量/單位
          5: { cellWidth: cw * 0.09, halign: 'right' },    // 總價
          6: { cellWidth: cw * 0.11, halign: 'right' },    // 現金(預支)
          7: { cellWidth: cw * 0.11, halign: 'right' },    // 支票(月結算)
          8: { cellWidth: cw * 0.11, halign: 'center' },   // 發票號碼
          9: { cellWidth: cw * 0.12 },                     // 備註
        },
        head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '現金\n(預支)', '支票\n(月結算)', '發票號碼', '備註']],
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
      doc.text('(報銷時需檢附活動日期、活動名稱及活動流程；', mx, y);
      y += 4;
      doc.text(' 若為觀摩活動，則需另附參加學員簽到表【或名單】﹔', mx, y);
      y += 4;
      doc.text('報銷時請附活動行程表及照片，並附上相關發票正本。 )', mx, y);

      y += 8;
      const submitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, '申請者', refundedAt, refundedBySignatureUrl);
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`預支沖銷申請表-${r.requestNo}.pdf`);
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
    refundedAt?: string,
    refundedBySignatureUrl?: string,
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
      cashier: { refundedAt, refundedBySignatureUrl },
    });
  }
}
