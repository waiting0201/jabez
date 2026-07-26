import { Injectable, inject, signal } from '@angular/core';
import { TravelPaymentRequest } from '../models/travel-payment-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmtDate, fmt, buildDynamicSignBlocks, designatedStepOrdersOf } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class TravelPaymentPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印出差請款申請單 */
  async printTravelPaymentRequest(
    r: TravelPaymentRequest,
    submittedByName: string,
    approvalRecords: ApprovalRecord[] = [],
    flow?: ApprovalFlow,
    submittedBySignatureUrl?: string,
    reviewerSignatureUrls?: Map<string, string>,
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
      doc.text('出 差 請 款 申 請 單', pw / 2, y, { align: 'center' });

      // ── 單號（右上角）──
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textMuted);
      doc.text(`單號：${r.requestNo}`, pw - mx, y, { align: 'right' });
      doc.setTextColor(...CIS.textPrimary);

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
      lv('金額合計：', `NT$ ${fmt(r.grandTotal)}`, pw - mx - 60, y, true);

      y += 6;
      if (r.projectCode || r.projectName) {
        lv('關聯專案：', `${r.projectCode ?? ''}${r.projectName ? ' - ' + r.projectName : ''}`, mx, y, true);
      }

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
          item.invoiceNo || '',
          item.invoiceDate ? fmtDate(item.invoiceDate) : '',
        ]);
      }

      // 合計列
      bodyRows.push([
        { content: '合計', colSpan: 5, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.grandTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '', '', '',
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
          0: { cellWidth: cw * 0.08, halign: 'center' },  // 分類
          1: { cellWidth: cw * 0.05, halign: 'center' },  // 項次
          2: { cellWidth: cw * 0.22 },                     // 項目說明
          3: { cellWidth: cw * 0.10, halign: 'right' },    // 單價
          4: { cellWidth: cw * 0.09, halign: 'center' },   // 數量
          5: { cellWidth: cw * 0.10, halign: 'right' },    // 總價
          6: { cellWidth: cw * 0.16 },                     // 備註
          7: { cellWidth: cw * 0.10, halign: 'center' },   // 發票號碼
          8: { cellWidth: cw * 0.10, halign: 'center' },   // 發票日期
        },
        head: [['分類', '項次', '項目說明', '單價', '數量/單位', '總價', '備註', '發票號碼', '發票日期']],
        body: bodyRows,
      });

      // ── 撥款明細（分期撥款表，若無 installments 則 fallback 顯示單筆）──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 6;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      if (r.installments && r.installments.length > 0) {
        autoTable(doc, {
          startY: y,
          margin: {left: mx, right: mx, top: 20},
          theme: 'grid',
          styles: {font: F, fontSize: 9, textColor: [...CIS.textPrimary], lineColor: [...CIS.border], lineWidth: 0.3, cellPadding: {top: 3, bottom: 3, left: 4, right: 4}},
          headStyles: {font: F, fillColor: [...CIS.forest], textColor: 255, fontSize: 9.5, fontStyle: 'bold', halign: 'center', cellPadding: {top: 4, bottom: 4, left: 4, right: 4}},
          columnStyles: {
            0: {cellWidth: cw * 0.08, halign: 'center'},
            1: {cellWidth: cw * 0.18, halign: 'center'},
            2: {cellWidth: cw * 0.18, halign: 'center'},
            3: {cellWidth: cw * 0.18, halign: 'right'},
            4: {cellWidth: cw * 0.38},
          },
          head: [[{content: '撥款明細', colSpan: 5, styles: {halign: 'center'}}], ['期數', '預計撥款日', '實際撥款日', '金　額', '備　註']],
          body: r.installments.map(ins => [
            String(ins.installmentNo),
            ins.expectedDate ? fmtDT(ins.expectedDate).split(' ')[0] : '—',
            ins.paidAt ? fmtDT(ins.paidAt).split(' ')[0] : '尚未撥款',
            ins.amount.toLocaleString('zh-TW'),
            ins.note || '',
          ]),
        });
        y = (doc as any).lastAutoTable.finalY + 4;
      } else {
        lv('撥款資訊：', '尚未排定撥款', mx, y, true);
      }

      // ── 簽名欄 ──
      y += 10;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      const submitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      // 出納簽名取最後一期已撥款者（若有）
      const lastPaid = r.installments?.filter(i => i.paidAt).slice(-1)[0];
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, '申請者', lastPaid?.paidAt, lastPaid?.paidBySignatureUrl, designatedStepOrdersOf(r.designatedReviewers));
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`出差請款申請單-${r.requestNo}.pdf`);
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
    designatedStepOrders: number[] = [],
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      designatedStepOrders,
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
      cashier: { paidBySignatureUrl, paidAt },
    });
  }
}
