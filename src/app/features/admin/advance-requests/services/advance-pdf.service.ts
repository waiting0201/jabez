import {Injectable, signal} from '@angular/core';
import {AdvanceRequest, WriteOffRecord} from '../models/advance-request.model';
import {ApprovalRecord, ApprovalFlow} from '../../approval-tasks/models/approval-task.model';

/** 簽名欄資料 */
interface SignBlock {
  label: string;
  name: string;
  date: string;
}

/** ArrayBuffer → base64 */
function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  const chunk = 8192;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
  }
  return btoa(binary);
}

/** CIS 色彩 */
const CIS = {
  forest:      [105, 159, 52]  as const,
  forestMid:   [74, 107, 58]   as const,
  textPrimary: [82, 83, 88]    as const,
  textMuted:   [163, 150, 133] as const,
  bgBase:      [245, 242, 237] as const,
  border:      [221, 214, 200] as const,
  red:         [160, 64, 64]   as const,
};

const fmt = (n: number) => n.toLocaleString('zh-TW');

@Injectable({providedIn: 'root'})
export class AdvancePdfService {
  pdfLoading = signal(false);

  private assetCache: Promise<{regular: string; bold: string}> | null = null;

  private loadFonts(): Promise<{regular: string; bold: string}> {
    if (!this.assetCache) {
      this.assetCache = Promise.all([
        fetch('/assets/fonts/NotoSansTC-Regular.ttf').then(r => r.arrayBuffer()),
        fetch('/assets/fonts/NotoSansTC-Bold.ttf').then(r => r.arrayBuffer()),
      ]).then(([regular, bold]) => ({
        regular: arrayBufferToBase64(regular),
        bold: arrayBufferToBase64(bold),
      }));
    }
    return this.assetCache;
  }

  /** 列印經費預支申請表 */
  async printAdvanceRequest(r: AdvanceRequest, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow) {
    this.pdfLoading.set(true);
    try {
      const [{default: jsPDF}, {default: autoTable}, fonts] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.loadFonts(),
      ]);

      const doc = new jsPDF('landscape', 'mm', 'a4');
      const F = 'NotoSansTC';
      doc.addFileToVFS('NotoSansTC-Regular.ttf', fonts.regular);
      doc.addFileToVFS('NotoSansTC-Bold.ttf', fonts.bold);
      doc.addFont('NotoSansTC-Regular.ttf', F, 'normal');
      doc.addFont('NotoSansTC-Bold.ttf', F, 'bold');

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
      doc.text('雅比斯國際創意策略股份有限公司', pw / 2, y, {align: 'center'});
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text('經 費 預 支 申 請 表', pw / 2, y, {align: 'center'});

      // ── 表頭資訊 ──
      y += 10;
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textPrimary);

      const advDate = r.advanceDate ? new Date(r.advanceDate).toLocaleDateString('zh-TW') : '';

      doc.text(`申 請 人：`, mx, y);
      doc.setFont(F, 'bold'); doc.text(submittedByName, mx + 25, y); doc.setFont(F, 'normal');
      doc.text(`案　　號：${r.projectCode}`, pw - mx - 50, y);

      y += 6;
      doc.text(`預支日期：${advDate}`, mx, y);

      y += 6;
      doc.text(`案　　名：`, mx, y);
      // 案名可能較長，截斷
      const projectName = r.activityName || '';
      doc.setFont(F, 'bold'); doc.text(projectName, mx + 25, y); doc.setFont(F, 'normal');

      y += 6;
      doc.text(`活動名稱：${r.activityName}`, mx, y);
      y += 6;
      doc.text(`活動期間：${r.activityPeriod}`, mx, y);

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
          item.note || '',
        ]);
      }

      // 合計列
      bodyRows.push([
        {content: '預 支 現 金 數', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        {content: fmt(r.cashTotal), styles: {fontStyle: 'bold', halign: 'right'}},
        '',
        '',
      ]);
      bodyRows.push([
        {content: '月結支票金額', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        '',
        {content: r.checkTotal > 0 ? fmt(r.checkTotal) : '', styles: {fontStyle: 'bold', halign: 'right'}},
        '',
      ]);
      bodyRows.push([
        {content: '總計', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        {content: fmt(r.grandTotal), colSpan: 2, styles: {fontStyle: 'bold', halign: 'right'}},
        '',
      ]);

      autoTable(doc, {
        startY: y,
        margin: {left: mx, right: mx},
        theme: 'grid',
        styles: {
          font: F, fontSize: 8.5,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: {top: 2.5, bottom: 2.5, left: 3, right: 3},
        },
        headStyles: {
          font: F, fillColor: [...CIS.forest], textColor: 255,
          fontSize: 9, fontStyle: 'bold', halign: 'center',
          cellPadding: {top: 3, bottom: 3, left: 3, right: 3},
        },
        columnStyles: {
          0: {cellWidth: cw * 0.08, halign: 'center'},  // 分類
          1: {cellWidth: cw * 0.04, halign: 'center'},  // 項次
          2: {cellWidth: cw * 0.22},                      // 項目(說明)
          3: {cellWidth: cw * 0.10, halign: 'right'},     // 單價
          4: {cellWidth: cw * 0.08, halign: 'center'},    // 數量/單位
          5: {cellWidth: cw * 0.10, halign: 'right'},     // 總價
          6: {cellWidth: cw * 0.12, halign: 'right'},     // 現金(預支)
          7: {cellWidth: cw * 0.12, halign: 'right'},     // 支票(月結算)
          8: {cellWidth: cw * 0.14},                      // 備註
        },
        head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '現金\n(預支)', '支票\n(月結算)', '備註']],
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
      doc.text('(申請預支時請檢附活動企劃書, 報銷時需檢附活動日期、活動名稱及活動流程；', mx, y);
      y += 4;
      doc.text(' 若為觀摩活動，則需另附參加學員簽到表【或名單】﹔', mx, y);
      y += 4;
      doc.text('報銷時請附活動行程表及照片。 )', mx, y);

      y += 8;
      const advSubmitDate = r.createdAt ? new Date(r.createdAt).toLocaleDateString('zh-TW', {year: 'numeric', month: '2-digit', day: '2-digit'}) : '';
      const advSignBlocks = this._buildSignBlocks(flow, approvalRecords, submittedByName, advSubmitDate, '申請者');
      this._drawSignatureBlock(doc, F, mx, pw, cw, y, advSignBlocks);

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

  /** 列印預支經費現金沖銷表 */
  async printWriteOff(r: AdvanceRequest, wo: WriteOffRecord, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow) {
    this.pdfLoading.set(true);
    try {
      const [{default: jsPDF}, {default: autoTable}, fonts] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.loadFonts(),
      ]);

      const doc = new jsPDF('landscape', 'mm', 'a4');
      const F = 'NotoSansTC';
      doc.addFileToVFS('NotoSansTC-Regular.ttf', fonts.regular);
      doc.addFileToVFS('NotoSansTC-Bold.ttf', fonts.bold);
      doc.addFont('NotoSansTC-Regular.ttf', F, 'normal');
      doc.addFont('NotoSansTC-Bold.ttf', F, 'bold');

      const pw = doc.internal.pageSize.getWidth();
      const ph = doc.internal.pageSize.getHeight();
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
      doc.text('雅比斯國際創意策略股份有限公司', pw / 2, y, {align: 'center'});
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text('預 支 經 費 現 金 沖 銷 表', pw / 2, y, {align: 'center'});

      // ── 表頭資訊 ──
      y += 10;
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textPrimary);

      const advDate = r.advanceDate ? new Date(r.advanceDate).toLocaleDateString('zh-TW') : '';

      doc.text(`申 請 人：`, mx, y);
      doc.setFont(F, 'bold'); doc.text(submittedByName, mx + 25, y); doc.setFont(F, 'normal');
      doc.text(`案　　號：${r.projectCode}`, pw - mx - 50, y);

      y += 6;
      doc.text(`預支日期：${advDate}`, mx, y);
      y += 6;
      doc.text(`活動名稱：${r.activityName}`, mx, y);
      y += 6;
      doc.text(`活動期間：${r.activityPeriod}`, mx, y);

      // ── 摘要區：預支現金數 / 沖銷金額 / 繳回金額 ──
      y += 8;
      const balance = r.grandTotal - wo.grandTotal;

      doc.setFont(F, 'bold');
      doc.setFontSize(10);
      const summaryX = pw / 2 - 30;
      const valueX = pw / 2 + 30;
      doc.text('預支現金數', summaryX, y, {align: 'right'});
      doc.text(fmt(r.grandTotal), valueX, y, {align: 'right'});
      y += 6;
      doc.text(`第${wo.writeOffNo}次沖銷金額`, summaryX, y, {align: 'right'});
      doc.text(fmt(wo.grandTotal), valueX, y, {align: 'right'});
      y += 6;
      doc.setTextColor(...CIS.red);
      doc.text('繳回(應付)金額', summaryX, y, {align: 'right'});
      doc.text(`$${fmt(balance)}`, valueX, y, {align: 'right'});
      doc.setTextColor(...CIS.textPrimary);

      // ── 沖銷明細表格 ──
      y += 8;
      const items = wo.items || [];
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
          item.note || '',
        ]);
      }

      // 合計列
      bodyRows.push([
        {content: '預 支 現 金 數', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        '', '', '',
      ]);
      bodyRows.push([
        {content: '實支現金數', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        {content: fmt(wo.cashTotal), styles: {fontStyle: 'bold', halign: 'right'}},
        '', '',
      ]);
      bodyRows.push([
        {content: '月結支票金額', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        '',
        {content: wo.checkTotal > 0 ? fmt(wo.checkTotal) : '', styles: {fontStyle: 'bold', halign: 'right'}},
        '',
      ]);
      bodyRows.push([
        {content: '實際總支出', colSpan: 6, styles: {halign: 'right', fontStyle: 'bold'}},
        {content: fmt(wo.grandTotal), colSpan: 2, styles: {fontStyle: 'bold', halign: 'right'}},
        '',
      ]);

      autoTable(doc, {
        startY: y,
        margin: {left: mx, right: mx},
        theme: 'grid',
        styles: {
          font: F, fontSize: 8.5,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: {top: 2.5, bottom: 2.5, left: 3, right: 3},
        },
        headStyles: {
          font: F, fillColor: [...CIS.forest], textColor: 255,
          fontSize: 9, fontStyle: 'bold', halign: 'center',
          cellPadding: {top: 3, bottom: 3, left: 3, right: 3},
        },
        columnStyles: {
          0: {cellWidth: cw * 0.08, halign: 'center'},
          1: {cellWidth: cw * 0.04, halign: 'center'},
          2: {cellWidth: cw * 0.22},
          3: {cellWidth: cw * 0.10, halign: 'right'},
          4: {cellWidth: cw * 0.08, halign: 'center'},
          5: {cellWidth: cw * 0.10, halign: 'right'},
          6: {cellWidth: cw * 0.12, halign: 'right'},
          7: {cellWidth: cw * 0.12, halign: 'right'},
          8: {cellWidth: cw * 0.14},
        },
        head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '實際\n現金花費', '支票\n實際金額', '備註']],
        body: bodyRows,
      });

      // ── 注意事項 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 8;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      doc.setFont(F, 'normal');
      doc.setFontSize(7.5);
      doc.setTextColor(...CIS.red);
      doc.text('報銷時需檢附活動日期、活動名稱及活動流程；若為觀摩活動，則需另附參加學員簽到表【或名單】﹔報銷', mx, y);
      y += 4;
      doc.text('時請附活動行程表及照片。 )', mx, y);

      // ── 簽名欄 ──
      y += 8;
      const woSubmitDate = r.createdAt ? new Date(r.createdAt).toLocaleDateString('zh-TW', {year: 'numeric', month: '2-digit', day: '2-digit'}) : '';
      const woSignBlocks = this._buildSignBlocks(flow, approvalRecords, submittedByName, woSubmitDate, '申請者');
      this._drawSignatureBlock(doc, F, mx, pw, cw, y, woSignBlocks);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`預支沖銷表-${r.requestNo}-第${wo.writeOffNo}次.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 根據簽核流程和記錄建立簽名欄資料 */
  private _buildSignBlocks(
    flow: ApprovalFlow | undefined,
    records: ApprovalRecord[],
    submittedByName: string,
    submitDate: string,
    applicantLabel: string,
  ): SignBlock[] {
    const blocks: SignBlock[] = [];

    // 固定簽名欄標籤（參考文件格式）
    const fixedLabels = ['總監核准', '財務部簽核', '會計', '出納', '專案主管'];

    // 從流程步驟建立 stepOrder → label 對照
    const stepLabels: Record<number, string> = {};
    if (flow) {
      for (const step of flow.steps) {
        if (step.jobTitleName?.includes('總監') || step.departmentName?.includes('總監')) {
          stepLabels[step.stepOrder] = '總監核准';
        } else if (step.departmentName?.includes('財務')) {
          stepLabels[step.stepOrder] = '財務部簽核';
        } else if (step.departmentName?.includes('會計')) {
          stepLabels[step.stepOrder] = '會計';
        } else if (step.stepOrder === 1) {
          stepLabels[step.stepOrder] = '專案主管';
        } else {
          stepLabels[step.stepOrder] = step.note || step.jobTitleName || `Step ${step.stepOrder}`;
        }
      }
    }

    // 建立 label → record 對照
    const labelRecordMap = new Map<string, ApprovalRecord>();
    for (const rec of records) {
      const label = stepLabels[rec.stepOrder];
      if (label) labelRecordMap.set(label, rec);
    }

    // 按固定標籤順序輸出
    for (const label of fixedLabels) {
      const rec = labelRecordMap.get(label);
      blocks.push({
        label,
        name: rec?.reviewedBy || '',
        date: rec?.reviewedAt
          ? new Date(rec.reviewedAt).toLocaleDateString('zh-TW', {year: 'numeric', month: '2-digit', day: '2-digit'})
          : '',
      });
    }

    // 申請者（最右邊）
    blocks.push({
      label: applicantLabel,
      name: submittedByName,
      date: submitDate,
    });

    return blocks;
  }

  /** 繪製簽名欄（含名字和日期） */
  private _drawSignatureBlock(
    doc: any, F: string, mx: number, pw: number, cw: number, y: number, blocks: SignBlock[]
  ) {
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.3);
    doc.line(mx, y, pw - mx, y);

    y += 5;
    const gap = 3;
    const blockW = (cw - gap * (blocks.length - 1)) / blocks.length;

    for (let i = 0; i < blocks.length; i++) {
      const bx = mx + i * (blockW + gap);
      const block = blocks[i];

      // 標籤
      doc.setFont(F, 'bold');
      doc.setFontSize(8.5);
      doc.setTextColor(...CIS.textPrimary);
      doc.text(block.label, bx + blockW / 2, y, {align: 'center'});

      // 簽名線
      const lineY = y + 14;
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.2);
      doc.line(bx + 2, lineY, bx + blockW - 2, lineY);

      // 簽核者名字（簽名線上方）
      if (block.name) {
        doc.setFont(F, 'normal');
        doc.setFontSize(9);
        doc.setTextColor(...CIS.textPrimary);
        doc.text(block.name, bx + blockW / 2, lineY - 3, {align: 'center'});
      }

      // 日期（簽名線下方）
      if (block.date) {
        doc.setFont(F, 'normal');
        doc.setFontSize(7.5);
        doc.setTextColor(...CIS.textMuted);
        doc.text(block.date, bx + blockW / 2, lineY + 5, {align: 'center'});
      }
    }
  }
}
