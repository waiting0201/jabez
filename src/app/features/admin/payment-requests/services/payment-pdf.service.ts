import { Injectable, inject, signal } from '@angular/core';
import { ApprovalTask } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, buildDynamicSignBlocks } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class PaymentPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印請款單 PDF */
  async printPaymentRequest(task: ApprovalTask) {
    if (!task.paymentDetail || task.status !== 'approved') return;

    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.pdfCore.loadFonts(),
      ]);

      const doc = new jsPDF('portrait', 'mm', 'a4');
      const F = FONT_FAMILY;

      this.pdfCore.registerFonts(doc, fonts);

      const pw = doc.internal.pageSize.getWidth();   // 210
      const ph = doc.internal.pageSize.getHeight();   // 297
      const mx = 18;                                   // 左右邊距
      const cw = pw - mx * 2;                         // 內容寬度
      const d = task.paymentDetail!;
      const fmt = (n: number) => n.toLocaleString('zh-TW');

      let y = 22;

      // ── 頂部裝飾線（森林綠雙線）──
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      // ── 標題：依請款類型顯示 ──
      y += 12;
      const titleMap: Record<string, string> = {vendor: '廠 商 請 款 單', travel: '員 工 差 旅 請 款 單'};
      const pdfTitle = titleMap[d.paymentType] || '請 款 單';
      doc.setFont(F, 'bold');
      doc.setFontSize(20);
      doc.setTextColor(...CIS.forest);
      doc.text(pdfTitle, pw / 2, y, {align: 'center'});

      // ── 受款人 / 申請日期 ──
      y += 12;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
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

      const submitDate = fmtDT(task.submittedAt);
      const payerLabel = d.paymentType === 'vendor' ? '請款人：' : '受款人：';
      lv(payerLabel, task.submittedBy, mx, y, true);
      lv('申請日期：', submitDate, pw - mx - 55, y, true);

      // ── 明細表格 ──
      y += 8;
      const invoices = d.invoices || [];
      const bodyRows = invoices.map(inv => [
        d.projectCode,
        inv.invoiceNo || '—',
        inv.itemName || '—',
        fmt(inv.amount),
        inv.note || '',
      ]);
      // 合計列
      bodyRows.push([
        {content: '合　計', colSpan: 3, styles: {halign: 'center', fontStyle: 'bold'}} as any,
        {content: fmt(d.totalAmount), styles: {fontStyle: 'bold'}} as any,
        '',
      ]);

      autoTable(doc, {
        startY: y,
        margin: {left: mx, right: mx, top: 20},
        theme: 'grid',
        showHead: 'everyPage',
        styles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: {top: 3, bottom: 3, left: 4, right: 4},
        },
        headStyles: {
          font: F,
          fillColor: [...CIS.forest],
          textColor: 255,
          fontSize: 9.5,
          fontStyle: 'bold',
          halign: 'center',
          cellPadding: {top: 4, bottom: 4, left: 4, right: 4},
        },
        bodyStyles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary],
        },
        columnStyles: {
          0: {cellWidth: cw * 0.14, halign: 'center'},  // 案號
          1: {cellWidth: cw * 0.16, halign: 'center'},  // 發票號碼
          2: {cellWidth: cw * 0.30},                     // 項目
          3: {cellWidth: cw * 0.16, halign: 'right'},    // 金額
          4: {cellWidth: cw * 0.24},                     // 備註
        },
        head: [['案 號', '發票號碼', '項　　目', '金　額', '備　註']],
        body: bodyRows,
      });

      // ── 受款人資訊（僅廠商請款顯示）──
      if (d.paymentType === 'vendor') {
        const dash = (v?: string | null) => (v?.trim() || '—');
        const vendorTableY = (doc as any).lastAutoTable.finalY + 6;
        autoTable(doc, {
          startY: vendorTableY,
          margin: {left: mx, right: mx, top: 20},
          theme: 'grid',
          styles: {
            font: F,
            fontSize: 9,
            textColor: [...CIS.textPrimary],
            lineColor: [...CIS.border],
            lineWidth: 0.3,
            cellPadding: {top: 3, bottom: 3, left: 4, right: 4},
          },
          headStyles: {
            font: F,
            fillColor: [...CIS.forest],
            textColor: 255,
            fontSize: 9.5,
            fontStyle: 'bold',
            halign: 'center',
            cellPadding: {top: 4, bottom: 4, left: 4, right: 4},
          },
          columnStyles: {
            0: {cellWidth: cw * 0.14, fontStyle: 'bold', halign: 'right', fillColor: [248, 250, 247]},
            1: {cellWidth: cw * 0.36},
            2: {cellWidth: cw * 0.14, fontStyle: 'bold', halign: 'right', fillColor: [248, 250, 247]},
            3: {cellWidth: cw * 0.36},
          },
          head: [[{content: '受款人資訊', colSpan: 4, styles: {halign: 'center'}}]],
          body: [
            ['廠商名稱', dash(d.vendorName), '統　　編', dash(d.vendorTaxId)],
            ['聯　絡　人', dash(d.vendorContactPerson), '聯絡電話', dash(d.vendorPhone)],
            ['帳戶資料', {content: dash(d.vendorBankAccount), colSpan: 3} as any],
            ['公司地址', {content: dash(d.vendorAddress), colSpan: 3} as any],
          ],
        });
      }

      // ── 預計撥款日 / 撥款日 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 8;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      lv('預計撥款日：', d.estimatedPaymentDate ? fmtDT(d.estimatedPaymentDate).split(' ')[0] : '—', mx, y, true);
      lv('撥  款  日：', d.paidAt ? fmtDT(d.paidAt).split(' ')[0] : '—', pw - mx - 55, y, true);

      // ── 簽名欄 ──
      y += 12;

      const signBlocks = this._buildSignBlocks(task, submitDate);
      const sigImageMap = await this.pdfCore.loadSignatureImages(signBlocks);

      // 如果簽名欄會超出頁面，換頁
      if (y + 40 > ph - 20) {
        doc.addPage();
        y = 30;
      }

      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigImageMap, { gap: 4, labelSize: 9, maxH: 12, padding: 4 });

      // ── 底部裝飾線（簽名欄下方）──
      const bottomY = y + 34;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, bottomY, pw - mx, bottomY);
      doc.setLineWidth(0.8);
      doc.line(mx, bottomY + 1.5, pw - mx, bottomY + 1.5);

      doc.save(`請款單-${d.projectCode}-${task.id}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 根據 flow steps 動態建立簽名欄資料 */
  private _buildSignBlocks(task: ApprovalTask, submitDate: string): SignBlock[] {
    return buildDynamicSignBlocks({
      flow: task.flow,
      records: task.approvalRecords || [],
      submittedBySignatureUrl: task.submittedBySignatureUrl,
      submitDate,
      applicantLabel: '請款人',
      cashier: {
        paidBySignatureUrl: task.paymentDetail?.paidBySignatureUrl,
        paidAt: task.paymentDetail?.paidAt,
      },
    });
  }
}
