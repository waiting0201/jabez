import { Injectable, signal } from '@angular/core';
import { TravelRequest } from '../models/travel-request.model';
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

/** 格式化日期（僅日期） */
function fmtDate(val: string | Date): string {
  const d = new Date(val);
  return d.toLocaleDateString('zh-TW', {
    year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Taipei'
  });
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
export class TravelPdfService {
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
      doc.text('出 差 申 請 單', pw / 2, y, { align: 'center' });

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
      lv('假日出差：', r.isHolidayTravel ? '是' : '否', pw - mx - 50, y);

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
      const sigMap = await this._loadSignatureImages(signBlocks);
      this._drawSignatureBlock(doc, F, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`出差申請單-${r.id}.pdf`);
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
    paidAt?: string,
    paidBySignatureUrl?: string,
  ): SignBlock[] {
    const blocks: SignBlock[] = [];

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
          stepLabels[step.stepOrder] = '部門主管';
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

    // 固定簽名欄標籤順序
    const fixedLabels = ['總監核准', '財務部簽核', '會計', '出納', '部門主管'];

    for (const label of fixedLabels) {
      if (label === '出納') {
        // 出納欄位顯示撥款者簽名 + 撥款日期
        blocks.push({
          label,
          signatureUrl: paidBySignatureUrl,
          date: paidAt ? fmtDT(paidAt) : '',
        });
      } else {
        const rec = labelRecordMap.get(label);
        blocks.push({
          label,
          signatureUrl: rec?.reviewerSignatureUrl,
          date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : '',
        });
      }
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
        const maxW = blockW - 6;
        const maxH = 10;
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
