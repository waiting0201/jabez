import { Injectable, signal } from '@angular/core';
import { AdvanceRequest, WriteOffRecord } from '../models/advance-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { environment } from '../../../../../environments/environment';

/** 簽名欄資料 */
interface SignBlock {
  label: string;
  signatureUrl?: string;
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

/**
 * 將簽名 URL 轉為可存取的端點：
 * - 相對路徑（如 files/signatures/xxx.png）→ 加上 apiUrl 前綴
 * - 完整 blob URL → 萃取檔名，轉為 API 代理路徑
 */
function resolveSignatureUrl(url: string): string {
  if (!url.startsWith('http')) {
    return `${environment.apiUrl}/${url}`;
  }
  const match = url.match(/\/signatures\/(.+)$/);
  if (match) {
    return `${environment.apiUrl}/files/signatures/${match[1]}`;
  }
  return url;
}

/** 格式化日期時間（保證日期與時間之間有空格） */
function fmtDT(val: string | Date): string {
  const d = new Date(val);
  const tz = 'Asia/Taipei';
  const date = d.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: tz });
  const time = d.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: tz });
  return `${date} ${time}`;
}

/** CIS 色彩 */
const CIS = {
  forest: [105, 159, 52] as const,
  forestMid: [74, 107, 58] as const,
  textPrimary: [82, 83, 88] as const,
  textMuted: [163, 150, 133] as const,
  bgBase: [245, 242, 237] as const,
  border: [221, 214, 200] as const,
  red: [160, 64, 64] as const,
};

const fmt = (n: number) => n.toLocaleString('zh-TW');

@Injectable({ providedIn: 'root' })
export class AdvancePdfService {
  pdfLoading = signal(false);

  private assetCache: Promise<{ regular: string; bold: string }> | null = null;

  private loadFonts(): Promise<{ regular: string; bold: string }> {
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
  async printAdvanceRequest(r: AdvanceRequest, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow, submittedBySignatureUrl?: string) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
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
      lv('預支日期：', advDate, mx, y);

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
        { content: '預 支 現 金 數', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(r.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '',
        '',
      ]);
      bodyRows.push([
        { content: '月結支票金額', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        '',
        { content: r.checkTotal > 0 ? fmt(r.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
        '',
      ]);
      bodyRows.push([
        { content: '總計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
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
          0: { cellWidth: cw * 0.08, halign: 'center' },  // 分類
          1: { cellWidth: cw * 0.04, halign: 'center' },  // 項次
          2: { cellWidth: cw * 0.22 },                      // 項目(說明)
          3: { cellWidth: cw * 0.10, halign: 'right' },     // 單價
          4: { cellWidth: cw * 0.08, halign: 'center' },    // 數量/單位
          5: { cellWidth: cw * 0.10, halign: 'right' },     // 總價
          6: { cellWidth: cw * 0.12, halign: 'right' },     // 現金(預支)
          7: { cellWidth: cw * 0.12, halign: 'right' },     // 支票(月結算)
          8: { cellWidth: cw * 0.14 },                      // 備註
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
      const advSubmitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      const advSignBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, advSubmitDate, '申請者');
      const advSigMap = await this._loadSignatureImages(advSignBlocks);
      this._drawSignatureBlock(doc, F, mx, pw, cw, y, advSignBlocks, advSigMap);

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
  async printWriteOff(r: AdvanceRequest, wo: WriteOffRecord, submittedByName: string, approvalRecords: ApprovalRecord[] = [], flow?: ApprovalFlow, submittedBySignatureUrl?: string) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
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
      doc.text('雅比斯國際創意策略股份有限公司', pw / 2, y, { align: 'center' });
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text('預 支 經 費 現 金 沖 銷 表', pw / 2, y, { align: 'center' });

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
      lv('預支日期：', advDate, mx, y);
      y += 6;
      lv('活動名稱：', r.activityName, mx, y);
      y += 6;
      lv('活動期間：', r.activityPeriod, mx, y);

      // ── 摘要區：預支現金數 / 前幾次沖銷 / 本次沖銷 / 待沖銷金額 ──
      y += 8;

      // 累計前幾次沖銷（writeOffNo < 當前）
      const prevTotal = (r.writeOffs || [])
        .filter(w => w.writeOffNo < wo.writeOffNo)
        .reduce((s, w) => s + w.grandTotal, 0);
      // 含本次的累計
      const accTotal = prevTotal + wo.grandTotal;
      const balance = r.grandTotal - accTotal;

      doc.setFont(F, 'bold');
      doc.setFontSize(10);
      const summaryX = pw / 2 - 30;
      const valueX = pw / 2 + 30;
      doc.text('預支現金數', summaryX, y, { align: 'right' });
      doc.text(fmt(r.grandTotal), valueX, y, { align: 'right' });

      // 有前幾次沖銷時才顯示
      if (prevTotal > 0) {
        y += 6;
        doc.text(`前${wo.writeOffNo - 1}次累計沖銷`, summaryX, y, { align: 'right' });
        doc.text(fmt(prevTotal), valueX, y, { align: 'right' });
      }

      y += 6;
      doc.text(`第${wo.writeOffNo}次沖銷金額`, summaryX, y, { align: 'right' });
      doc.text(fmt(wo.grandTotal), valueX, y, { align: 'right' });
      y += 6;
      doc.setTextColor(...CIS.red);
      doc.text('預支待沖銷金額', summaryX, y, { align: 'right' });
      doc.text(balance < 0 ? `-$${fmt(Math.abs(balance))}` : `$${fmt(balance)}`, valueX, y, { align: 'right' });
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
        { content: '預 支 現 金 數', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        '', '', '',
      ]);
      bodyRows.push([
        { content: '實支現金數', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(wo.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
        '', '',
      ]);
      bodyRows.push([
        { content: '月結支票金額', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        '',
        { content: wo.checkTotal > 0 ? fmt(wo.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
        '',
      ]);
      bodyRows.push([
        { content: '實際總支出', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: fmt(wo.grandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
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
          0: { cellWidth: cw * 0.08, halign: 'center' },
          1: { cellWidth: cw * 0.04, halign: 'center' },
          2: { cellWidth: cw * 0.22 },
          3: { cellWidth: cw * 0.10, halign: 'right' },
          4: { cellWidth: cw * 0.08, halign: 'center' },
          5: { cellWidth: cw * 0.10, halign: 'right' },
          6: { cellWidth: cw * 0.12, halign: 'right' },
          7: { cellWidth: cw * 0.12, halign: 'right' },
          8: { cellWidth: cw * 0.14 },
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
      const woSubmitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      const woSignBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, woSubmitDate, '申請者');
      const woSigMap = await this._loadSignatureImages(woSignBlocks);
      this._drawSignatureBlock(doc, F, mx, pw, cw, y, woSignBlocks, woSigMap);

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
    submittedBySignatureUrl: string | undefined,
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
        signatureUrl: rec?.reviewerSignatureUrl,
        date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : '',
      });
    }

    // 申請者（最右邊）
    blocks.push({
      label: applicantLabel,
      signatureUrl: submittedBySignatureUrl,
      date: submitDate,
    });

    return blocks;
  }

  /** 預載所有簽名欄圖片，回傳 URL → base64 data URI 的 Map */
  private async _loadSignatureImages(blocks: SignBlock[]): Promise<Map<string, string>> {
    const urls = blocks.map(b => b.signatureUrl).filter((u): u is string => !!u);
    const map = new Map<string, string>();
    await Promise.all(urls.map(async url => {
      try {
        const fetchUrl = resolveSignatureUrl(url);
        const resp = await fetch(fetchUrl);
        const buf = await resp.arrayBuffer();
        const mime = resp.headers.get('content-type') || 'image/png';
        // Map key 仍使用原始 url，與 blocks 資料保持一致
        map.set(url, `data:${mime};base64,${arrayBufferToBase64(buf)}`);
      } catch { /* 載入失敗則跳過 */ }
    }));
    return map;
  }

  /** 繪製簽名欄（含簽名圖片和日期） */
  private _drawSignatureBlock(
    doc: any, F: string, mx: number, pw: number, cw: number, y: number,
    blocks: SignBlock[], sigImageMap: Map<string, string>
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
      doc.text(block.label, bx + blockW / 2, y, { align: 'center' });

      // 簽名線
      const lineY = y + 14;
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.2);
      doc.line(bx + 2, lineY, bx + blockW - 2, lineY);

      // 簽名檔圖片（簽名線上方，等比例縮放）
      if (block.signatureUrl && sigImageMap.has(block.signatureUrl)) {
        const sigData = sigImageMap.get(block.signatureUrl)!;
        const maxW = blockW - 6;  // 左右各留 3mm
        const maxH = 10;          // 最大高度 10mm
        try {
          const imgProps = doc.getImageProperties(sigData);
          const ratio = Math.min(maxW / imgProps.width, maxH / imgProps.height);
          const imgW = imgProps.width * ratio;
          const imgH = imgProps.height * ratio;
          const imgX = bx + (blockW - imgW) / 2;
          const imgY = lineY - imgH - 1;
          doc.addImage(sigData, imgX, imgY, imgW, imgH);
        } catch { /* 圖片格式有誤則跳過 */ }
      }

      // 日期時間（簽名線下方）
      if (block.date) {
        doc.setFont(F, 'normal');
        doc.setFontSize(6.5);
        doc.setTextColor(...CIS.textMuted);
        doc.text(block.date, bx + blockW / 2, lineY + 5, { align: 'center' });
      }
    }
  }
}
