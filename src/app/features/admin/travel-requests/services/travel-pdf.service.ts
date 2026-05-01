import { Injectable, inject, signal } from '@angular/core';
import { TravelRequest } from '../models/travel-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmtDate, fmt, buildDynamicSignBlocks } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class TravelPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印出差申請單 */
  async printTravelRequest(
    r: TravelRequest,
    submittedByName: string,
    approvalRecords: ApprovalRecord[] = [],
    flow?: ApprovalFlow,
    submittedBySignatureUrl?: string,
    reviewerSignatureUrls?: Map<string, string>,
    paidAt?: string,
    paidBySignatureUrl?: string,
  ) {
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
      doc.text('出 差 預 支 申 請 單', pw / 2, y, { align: 'center' });

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

      const startDate = r.startDate ? fmtDate(r.startDate) : '';
      const endDate = r.endDate ? fmtDate(r.endDate) : '';

      lv('申請人：', submittedByName, mx, y, true);
      lv('出差地點：', r.destination, pw / 2, y, true);

      y += 6;
      lv('出差期間：', `${startDate} ～ ${endDate}`, mx, y);
      lv('假日執行活動：', r.isHolidayTravel ? '是' : '否', pw - mx - 50, y);

      y += 6;
      if (r.projectCode || r.projectName) {
        lv('關聯專案：', `${r.projectCode ?? ''}${r.projectName ? ' - ' + r.projectName : ''}`, mx, y, true);
      }
      lv('金額合計：', `NT$ ${fmt(r.grandTotal)}`, pw - mx - 60, y, true);

      y += 6;
      lv('出差目的：', r.purpose || '', mx, y);

      // ── 費用明細表格 ──
      y += 8;
      const items = r.items || [];

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
          item.note || '',
        ]);
      }

      // 合計列
      bodyRows.push([
        { content: '合計', colSpan: 5, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.grandTotal), styles: { fontStyle: 'bold', halign: 'right' } },
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
          0: { cellWidth: cw * 0.09, halign: 'center' },  // 分類
          1: { cellWidth: cw * 0.05, halign: 'center' },  // 項次
          2: { cellWidth: cw * 0.28 },                     // 項目說明
          3: { cellWidth: cw * 0.11, halign: 'right' },    // 單價
          4: { cellWidth: cw * 0.10, halign: 'center' },   // 數量
          5: { cellWidth: cw * 0.12, halign: 'right' },    // 總價
          6: { cellWidth: cw * 0.25 },                     // 備註
        },
        head: [['分類', '項次', '項目說明', '單價', '數量/單位', '總價', '備註']],
        body: bodyRows,
      });

      // ── 預計撥款日 / 撥款日 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 8;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      lv('預計撥款日：', r.estimatedPaymentDate ? fmtDT(r.estimatedPaymentDate).split(' ')[0] : '—', mx, y, true);
      lv('撥  款  日：', r.paidAt ? fmtDT(r.paidAt).split(' ')[0] : '—', pw - mx - 55, y, true);

      // ── 簽名欄 ──
      y += 10;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      const submitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, '申請者', paidAt, paidBySignatureUrl);
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`出差預支申請單-${r.id}.pdf`);
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
    paidAt?: string,
    paidBySignatureUrl?: string,
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
      cashier: { paidBySignatureUrl, paidAt },
    });
  }
}
