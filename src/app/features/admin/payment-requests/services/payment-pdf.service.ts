import { Injectable, signal } from '@angular/core';
import { ApprovalTask, ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
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

/** CIS 色彩設計語言 */
const CIS = {
  forest:      [105, 159, 52]  as const,
  forestMid:   [74, 107, 58]   as const,
  accent:      [140, 115, 85]  as const,
  textPrimary: [82, 83, 88]    as const,
  textMuted:   [163, 150, 133] as const,
  bgBase:      [245, 242, 237] as const,
  bgSurface:   [253, 250, 245] as const,
  border:      [221, 214, 200] as const,
};

@Injectable({ providedIn: 'root' })
export class PaymentPdfService {
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

  /** 列印請款單 PDF */
  async printPaymentRequest(task: ApprovalTask) {
    if (!task.paymentDetail || task.status !== 'approved') return;

    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.loadFonts(),
      ]);

      const doc = new jsPDF('portrait', 'mm', 'a4');
      const F = 'NotoSansTC';

      doc.addFileToVFS('NotoSansTC-Regular.ttf', fonts.regular);
      doc.addFileToVFS('NotoSansTC-Bold.ttf', fonts.bold);
      doc.addFont('NotoSansTC-Regular.ttf', F, 'normal');
      doc.addFont('NotoSansTC-Bold.ttf', F, 'bold');

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
      lv('受款人：', task.submittedBy, mx, y, true);
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
      const sigImageMap = await this._loadSignatureImages(signBlocks);

      // 如果簽名欄會超出頁面，換頁
      if (y + 40 > ph - 20) {
        doc.addPage();
        y = 30;
      }

      this._drawSignatureBlock(doc, F, mx, pw, cw, y, signBlocks, sigImageMap);

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

  /** 根據簽核流程和記錄建立簽名欄資料（固定順序：總監→財務→會計→出納→指定審核→請款人） */
  private _buildSignBlocks(task: ApprovalTask, submitDate: string): SignBlock[] {
    const blocks: SignBlock[] = [];
    const records = task.approvalRecords || [];

    // 根據 flow steps 建立 label → stepOrder 映射（不依賴 hardcoded stepOrder）
    const labelToStep: Record<string, number> = {};
    let step1Label = '指定審核'; // step1 的預設 label
    if (task.flow) {
      for (const step of task.flow.steps) {
        let label: string;
        if (step.useApplicantDesignated) {
          label = '指定審核';
        } else if (step.useDirectSupervisor) {
          label = '上層級';
        } else if (step.jobTitleName?.includes('總監')) {
          label = '總監';
        } else if (step.departmentName?.includes('財務')) {
          label = '財務';
        } else if (step.departmentName?.includes('會計')) {
          label = '會計';
        } else {
          label = step.departmentName || step.jobTitleName || `Step ${step.stepOrder}`;
        }
        labelToStep[label] = step.stepOrder;
        if (step.stepOrder === 1) step1Label = label;
      }
    }

    /** 根據 label 找到對應的 approval record 並建立 block */
    const addStepBlock = (label: string) => {
      const so = labelToStep[label];
      const rec = so != null ? records.find(r => r.stepOrder === so) : undefined;
      blocks.push({
        label,
        signatureUrl: rec?.reviewerSignatureUrl,
        date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : '',
      });
    };

    // 固定順序：總監 → 財務 → 會計
    addStepBlock('總監');
    addStepBlock('財務');
    addStepBlock('會計');

    // 出納（撥款者簽名 + 撥款日期）
    blocks.push({
      label: '出納',
      signatureUrl: task.paymentDetail?.paidBySignatureUrl,
      date: task.paymentDetail?.paidAt ? fmtDT(task.paymentDetail.paidAt) : '',
    });

    // 指定審核（step1，label 可能是「指定審核」「部門主管」「上層級」）
    addStepBlock(step1Label);

    // 請款人（最右邊）
    blocks.push({
      label: '請款人',
      signatureUrl: task.submittedBySignatureUrl,
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
    const blockCount = blocks.length;
    const gap = 4;
    const blockW = (cw - gap * (blockCount - 1)) / blockCount;

    // 繪製上方分隔線
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.3);
    doc.line(mx, y, pw - mx, y);

    y += 6;
    for (let i = 0; i < blockCount; i++) {
      const bx = mx + i * (blockW + gap);
      const block = blocks[i];

      // 標籤
      doc.setFont(F, 'bold');
      doc.setFontSize(9);
      doc.setTextColor(...CIS.textPrimary);
      doc.text(block.label, bx + blockW / 2, y, {align: 'center'});

      // 簽名線
      const lineY = y + 16;
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.2);
      doc.line(bx + 2, lineY, bx + blockW - 2, lineY);

      // 簽名檔圖片（簽名線上方，等比例縮放）
      if (block.signatureUrl && sigImageMap.has(block.signatureUrl)) {
        const sigData = sigImageMap.get(block.signatureUrl)!;
        const maxW = blockW - 8;  // 左右各留 4mm
        const maxH = 12;          // 最大高度 12mm
        try {
          const imgProps = doc.getImageProperties(sigData);
          const ratio = Math.min(maxW / imgProps.width, maxH / imgProps.height);
          const imgW = imgProps.width * ratio;
          const imgH = imgProps.height * ratio;
          const imgX = bx + (blockW - imgW) / 2;
          const imgY = lineY - imgH - 1;  // 簽名線上方 1mm
          doc.addImage(sigData, imgX, imgY, imgW, imgH);
        } catch { /* 圖片格式有誤則跳過 */ }
      }

      // 日期時間（簽名線下方）
      if (block.date) {
        doc.setFont(F, 'normal');
        doc.setFontSize(6.5);
        doc.setTextColor(...CIS.textMuted);
        doc.text(block.date, bx + blockW / 2, lineY + 5, {align: 'center'});
      }
    }
  }
}
