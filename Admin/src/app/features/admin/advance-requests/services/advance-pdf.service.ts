import { Injectable, inject, signal } from '@angular/core';
import { AdvanceRequest, APPROVAL_STATUS_LABELS, ApprovalStatus, roundLabel } from '../models/advance-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmt, buildDynamicSignBlocks } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class AdvancePdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印經費預支申請表 */
  async printAdvanceRequest(r: AdvanceRequest, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow, submittedBySignatureUrl?: string) {
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
      doc.text('經 費 預 支 申 請 表', pw / 2, y, { align: 'center' });

      // ── 表頭資訊 ──
      y += 10;
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textPrimary);

      const advDate = r.advanceDate ? new Date(r.advanceDate).toLocaleDateString('zh-TW') : '';

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
      // 預支日期：有追加批次時逐批列出「第N次追加：日期　金額」，並在最後標示預支總額
      const rounds = r.rounds ?? [];
      if (rounds.length > 1) {
        rounds.forEach((rd, idx) => {
          const d = rd.advanceDate ? new Date(rd.advanceDate).toLocaleDateString('zh-TW') : '';
          lv(idx === 0 ? '預支日期：' : '　　　　　',
             `${roundLabel(rd.roundNo)}　${d}　${fmt(rd.grandTotal)}元${rd.reason ? `（${rd.reason}）` : ''}`,
             mx, y);
          y += 5.5;
        });
        lv('預支總額：', `${fmt(r.grandTotal)}元`, mx, y, true);
      } else {
        lv('預支日期：', advDate, mx, y);
      }

      y += 6;
      const projectName = r.activityName || '';
      lv('案名：', projectName, mx, y, true);

      y += 6;
      lv('活動名稱：', r.activityName, mx, y);
      y += 6;
      lv('活動期間：', r.activityPeriod, mx, y);

      // ── 明細表格 ──
      y += 8;
      const items = r.items || [];

      // 建立表格資料：按批次 + 分類分組（同批次 / 同分類第二列起留白）
      const bodyRows: any[][] = [];
      let lastCategory = '';
      let lastRound = 0;
      for (const item of items) {
        const roundCell = item.roundNo === lastRound ? '' : roundLabel(item.roundNo);
        if (item.roundNo !== lastRound) lastCategory = '';   // 換批次時分類重新顯示
        lastRound = item.roundNo;
        const cat = item.category === lastCategory ? '' : item.category;
        lastCategory = item.category;
        bodyRows.push([
          roundCell,
          cat,
          item.seqNo.toString(),
          item.itemName,
          `${fmt(item.unitPrice)}元`,
          item.quantity,
          fmt(item.totalPrice),
          fmt(item.cashAmount),
          item.checkAmount > 0 ? fmt(item.checkAmount) : '',
          item.note || '',
        ]);
      }

      // 合計列（批次欄使合併寬度 +1）
      bodyRows.push([
        { content: '預 支 現 金 數', colSpan: 7, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);
      bodyRows.push([
        { content: '月結支票金額', colSpan: 7, styles: { halign: 'right', fontStyle: 'bold' } },
        '',
        { content: r.checkTotal > 0 ? fmt(r.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
        '',
      ]);
      bodyRows.push([
        { content: '總計', colSpan: 7, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.grandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
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
          0: { cellWidth: cw * 0.08, halign: 'center' },     // 批次
          1: { cellWidth: cw * 0.07, halign: 'center' },     // 分類
          2: { cellWidth: cw * 0.04, halign: 'center' },     // 項次
          3: { cellWidth: cw * 0.19 },                       // 項目(說明)
          4: { cellWidth: cw * 0.09, halign: 'right' },      // 單價
          5: { cellWidth: cw * 0.07, halign: 'center' },     // 數量/單位
          6: { cellWidth: cw * 0.09, halign: 'right' },      // 總價
          7: { cellWidth: cw * 0.11, halign: 'right' },      // 現金(預支)
          8: { cellWidth: cw * 0.11, halign: 'right' },      // 支票(月結算)
          9: { cellWidth: cw * 0.15 },                       // 備註
        },
        head: [['批次', '分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '現金\n(預支)', '支票\n(月結算)', '備註']],
        body: bodyRows,
      });

      // ── 沖銷紀錄表格（按次展開） ──
      y = (doc as any).lastAutoTable.finalY + 6;

      // ── 撥款明細（分期撥款表，若無 installments 則 fallback 顯示單筆）──
      y += 2;
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
        y = (doc as any).lastAutoTable.finalY + 6;
      } else {
        lv('撥款資訊：', '尚未排定撥款', mx, y, true);
        y += 6;
      }

      const woRecords = r.writeOffRecords || [];
      for (const wo of woRecords) {
        // 小標題：第 N 次沖銷
        if (y + 30 > ph - 15) { doc.addPage(); y = 20; }
        y += 6;
        doc.setFont(F, 'bold');
        doc.setFontSize(10);
        doc.setTextColor(...CIS.forestMid);
        const statusLabel = APPROVAL_STATUS_LABELS[wo.approvalStatus as ApprovalStatus] || wo.approvalStatus;
        doc.text(`第 ${wo.writeOffNo} 次沖銷 - ${wo.requestNo}（${statusLabel}）`, mx, y);
        y += 6;

        // 沖銷明細表格
        const woBodyRows: any[][] = [];
        let woLastCat = '';
        for (const item of wo.items) {
          const cat = item.category === woLastCat ? '' : item.category;
          woLastCat = item.category;
          woBodyRows.push([
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
        woBodyRows.push([
          { content: '現金小計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
          { content: fmt(wo.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
          '', '', '',
        ]);
        woBodyRows.push([
          { content: '支票小計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
          '',
          { content: wo.checkTotal > 0 ? fmt(wo.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
          '', '',
        ]);
        woBodyRows.push([
          { content: '總計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
          { content: fmt(wo.grandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
          '', '',
        ]);

        autoTable(doc, {
          startY: y,
          margin: { left: mx, right: mx },
          theme: 'grid',
          styles: {
            font: F, fontSize: 8,
            textColor: [...CIS.textPrimary],
            lineColor: [...CIS.border],
            lineWidth: 0.3,
            cellPadding: { top: 2, bottom: 2, left: 2.5, right: 2.5 },
          },
          headStyles: {
            font: F, fillColor: [...CIS.forestMid], textColor: 255,
            fontSize: 8, fontStyle: 'bold', halign: 'center',
            cellPadding: { top: 2.5, bottom: 2.5, left: 2.5, right: 2.5 },
          },
          columnStyles: {
            0: { cellWidth: cw * 0.07, halign: 'center' },  // 分類
            1: { cellWidth: cw * 0.04, halign: 'center' },  // 項次
            2: { cellWidth: cw * 0.19 },                     // 項目(說明)
            3: { cellWidth: cw * 0.09, halign: 'right' },    // 單價
            4: { cellWidth: cw * 0.07, halign: 'center' },   // 數量/單位
            5: { cellWidth: cw * 0.09, halign: 'right' },    // 總價
            6: { cellWidth: cw * 0.11, halign: 'right' },    // 現金
            7: { cellWidth: cw * 0.11, halign: 'right' },    // 支票
            8: { cellWidth: cw * 0.11, halign: 'center' },   // 發票號碼
            9: { cellWidth: cw * 0.12 },                     // 備註
          },
          head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '現金', '支票', '發票號碼', '備註']],
          body: woBodyRows,
        });

        y = (doc as any).lastAutoTable.finalY + 4;
      }

      // ── 沖銷金額摘要 ──
      if (woRecords.length > 0) {
        const writtenOff = woRecords
          .filter(w => w.approvalStatus !== 'rejected')
          .reduce((sum, w) => sum + w.grandTotal, 0);
        const remaining = r.grandTotal - writtenOff;

        if (y + 20 > ph - 15) { doc.addPage(); y = 20; }
        y += 4;
        doc.setFont(F, 'bold');
        doc.setFontSize(9.5);
        doc.setTextColor(...CIS.textPrimary);

        const summaryX = pw - mx;
        doc.text(`預支總金額：${fmt(r.grandTotal)} 元`, summaryX, y, { align: 'right' });
        y += 6;
        doc.text(`已沖銷金額：${fmt(writtenOff)} 元`, summaryX, y, { align: 'right' });
        y += 6;
        const [cr, cg, cb] = remaining > 0 ? CIS.red : CIS.forest;
        doc.setTextColor(cr, cg, cb);
        doc.text(`待沖銷金額：${fmt(remaining)} 元`, summaryX, y, { align: 'right' });
        doc.setTextColor(...CIS.textPrimary);
      }

      // ── 簽名欄 ──
      y += 6;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      // 注意事項
      doc.setFont(F, 'normal');
      doc.setFontSize(7.5);
      doc.setTextColor(...CIS.red);
      doc.text('(申請預支時請檢附活動企劃書, 報銷時需檢附活動日期、活動名稱及活動流程；', mx, y);
      y += 4;
      doc.text(' 若為觀摩活動，則需另附參加學員簽到表【或名單】﹔', mx, y);
      y += 4;
      doc.text('報銷時請附活動行程表及照片。 )', mx, y);

      y += 8;
      const advSubmitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      // 出納簽名取最後一期已撥款者（若有）
      const lastPaid = r.installments?.filter(i => i.paidAt).slice(-1)[0];
      // 追加後兩輪簽核紀錄併存，必須指定批次，否則簽名欄會印出前一輪的簽章
      const advSignBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, advSubmitDate, '申請者', lastPaid?.paidBySignatureUrl, lastPaid?.paidAt, r.currentRoundNo ?? 1);
      const advSigMap = await this.pdfCore.loadSignatureImages(advSignBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, advSignBlocks, advSigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`經費預支申請表-${r.requestNo}.pdf`);
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
    paidBySignatureUrl?: string,
    paidAt?: string,
    roundNo = 1,
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
      cashier: { paidBySignatureUrl, paidAt },
      roundNo,
    });
  }
}
