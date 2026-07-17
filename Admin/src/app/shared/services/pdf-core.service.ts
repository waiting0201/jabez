import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

/** 簽名欄資料 */
export interface SignBlock {
  label: string;
  signatureUrl?: string;
  date: string;
}

/** buildDynamicSignBlocks 用的最小 step 介面（與 ApprovalFlowStep 結構相容） */
export interface SignFlowStep {
  stepOrder: number;
  departmentName?: string;
  departmentCode?: string;
  jobTitleName?: string;
  jobTitleLevel?: number;
  useDirectSupervisor?: boolean;
  useApplicantDesignated?: boolean;
  note?: string;
}

/** buildDynamicSignBlocks 用的最小 flow 介面（與 ApprovalFlow 結構相容） */
export interface SignFlow {
  steps: SignFlowStep[];
}

/** buildDynamicSignBlocks 用的最小 record 介面（與 ApprovalRecord 結構相容） */
export interface SignRecord {
  stepOrder: number;
  reviewedAt?: Date | string | null;
  reviewerSignatureUrl?: string;
  reviewerJobTitle?: string;
  reviewerJobTitleLevel?: number;
}

/** buildDynamicSignBlocks 出納欄資訊（請款 / 預支 / 出差請款 PDF 才傳） */
export interface SignCashierInfo {
  paidBySignatureUrl?: string;
  paidAt?: string | Date | null;
  /** 退款處理人簽名（沖銷類用，優先於 paidBy） */
  refundedBySignatureUrl?: string;
  /** 退款日期（沖銷類用，優先於 paidAt） */
  refundedAt?: string | Date | null;
}

/** buildDynamicSignBlocks 主要參數 */
export interface BuildDynamicSignBlocksOptions {
  flow?: SignFlow;
  records: SignRecord[];
  submittedBySignatureUrl?: string;
  submitDate: string;
  applicantLabel: string;
  cashier?: SignCashierInfo;
}

/** 總監職稱層級（JobTitle.Level = 1，與後端簽核邏輯一致；勿依賴職稱名稱） */
const DIRECTOR_JOB_TITLE_LEVEL = 1;

/** 總監室部門代碼（與 auth.service.ts 撥款權限部門代碼一致） */
const OFFICE_OF_DIRECTOR_DEPT_CODE = 'Office of the Director';

/**
 * step 是否為總監步驟。
 * 優先以 JobTitle.Level / Department.Code 判定（改名不受影響）；
 * Level / Code 缺值時（舊資料）才 fallback 名稱比對。
 */
function isDirectorStep(step: SignFlowStep): boolean {
  const byJobTitle = step.jobTitleLevel != null
    ? step.jobTitleLevel === DIRECTOR_JOB_TITLE_LEVEL
    : !!step.jobTitleName?.includes('總監');
  const byDepartment = step.departmentCode
    ? step.departmentCode === OFFICE_OF_DIRECTOR_DEPT_CODE
    : !!step.departmentName?.includes('總監');
  return byJobTitle || byDepartment;
}

/** record 審核者是否為總監（優先以 Level 判定，缺值時 fallback 名稱比對） */
function isDirectorReviewer(r: SignRecord): boolean {
  return r.reviewerJobTitleLevel != null
    ? r.reviewerJobTitleLevel === DIRECTOR_JOB_TITLE_LEVEL
    : !!r.reviewerJobTitle?.includes('總監');
}

/** step → 簽名欄 label */
function resolveStepLabel(step: SignFlowStep): string {
  if (step.useDirectSupervisor) return '上層級';
  if (isDirectorStep(step)) return '總監核准';
  if (step.departmentName?.includes('財務')) return '財務部簽核';
  if (step.departmentName?.includes('會計')) return '會計';
  return step.note || step.departmentName || step.jobTitleName || `Step ${step.stepOrder}`;
}

/**
 * 依 flow.steps 動態建立 PDF 簽名欄。
 *
 * - 每個非 useApplicantDesignated step 各一格，依 stepOrder 反轉後排列
 * - 「總監核准」一律 hoist 到最左，不論 flow 中總監步驟的 stepOrder
 * - 指定簽核步驟（useApplicantDesignated）不獨立佔欄位
 * - 例外：若指定簽核紀錄裡有人職稱層級為總監（JobTitle.Level=1）：
 *   - flow 沒有總監步驟 → 加「總監核准」欄至最左
 *   - flow 已有總監步驟 → 額外加「總監（指定）」欄並列在「總監核准」右側（即使同人簽兩次，兩格皆顯示）
 * - 出納欄（如有）緊接在 step 欄位之後、申請者欄之前
 * - 申請者欄永遠在最右
 */
export function buildDynamicSignBlocks(opts: BuildDynamicSignBlocksOptions): SignBlock[] {
  const { flow, records, submittedBySignatureUrl, submitDate, applicantLabel, cashier } = opts;

  const steps = (flow?.steps ?? []).slice().sort((a, b) => a.stepOrder - b.stepOrder);

  // 1. 為每個非 useApplicantDesignated 步驟建一格
  const stepBlocks: SignBlock[] = [];
  for (const step of steps) {
    if (step.useApplicantDesignated) continue;
    const rec = records.find(r => r.stepOrder === step.stepOrder);
    stepBlocks.push({
      label: resolveStepLabel(step),
      signatureUrl: rec?.reviewerSignatureUrl,
      date: rec?.reviewedAt ? fmtDT(rec.reviewedAt as Date | string) : '',
    });
  }

  // 1.5 將「總監核准」block 移至陣列尾端 → reverse 後保證落在最左
  //     不論 flow 中總監步驟的 stepOrder 排序如何，PDF 第一欄一律為總監
  const directorBlocks = stepBlocks.filter(b => b.label === '總監核准');
  if (directorBlocks.length > 0) {
    for (const d of directorBlocks) {
      stepBlocks.splice(stepBlocks.indexOf(d), 1);
    }
    stepBlocks.push(...directorBlocks);
  }

  // 2. 處理「指定簽核中的總監」
  const designatedStep = steps.find(s => s.useApplicantDesignated);
  if (designatedStep) {
    const designatedDirectors = records.filter(r =>
      r.stepOrder === designatedStep.stepOrder && isDirectorReviewer(r)
    );
    const designatedDirector = designatedDirectors.at(-1);

    if (designatedDirector) {
      const existing = stepBlocks.find(b => b.label === '總監核准');

      if (!existing) {
        // flow 沒有總監步驟 → 插入新「總監核准」欄（放最後面，reverse 後在最左）
        stepBlocks.push({
          label: '總監核准',
          signatureUrl: designatedDirector.reviewerSignatureUrl,
          date: designatedDirector.reviewedAt
            ? fmtDT(designatedDirector.reviewedAt as Date | string) : '',
        });
      } else {
        // flow 已有總監步驟 → 加「總監（指定）」欄並列；插在「總監核准」之前
        // （reverse 後「總監核准」在最左、「總監（指定）」緊接在右側）
        const idx = stepBlocks.indexOf(existing);
        stepBlocks.splice(idx, 0, {
          label: '總監（指定）',
          signatureUrl: designatedDirector.reviewerSignatureUrl,
          date: designatedDirector.reviewedAt
            ? fmtDT(designatedDirector.reviewedAt as Date | string) : '',
        });
      }
    }
  }

  // 3. 反轉（最高權限步驟在左，最接近申請者的 step 在右）
  const ordered = stepBlocks.reverse();

  // 4. 出納欄
  if (cashier) {
    const cashierDate = cashier.refundedAt ?? cashier.paidAt;
    ordered.push({
      label: '出納',
      signatureUrl: cashier.refundedBySignatureUrl ?? cashier.paidBySignatureUrl,
      date: cashierDate ? fmtDT(cashierDate as Date | string) : '',
    });
  }

  // 5. 申請者欄
  ordered.push({
    label: applicantLabel,
    signatureUrl: submittedBySignatureUrl,
    date: submitDate,
  });

  return ordered;
}

/** 繪製簽名欄的選項（各 PDF service 依版型微調） */
export interface DrawSignatureOptions {
  gap?: number;       // 欄間距（預設 3）
  labelSize?: number; // 標籤字體大小（預設 8.5）
  maxH?: number;      // 簽名圖片最大高度 mm（預設 10）
  padding?: number;   // 簽名圖片左右留白 mm（預設 3）
}

/** ArrayBuffer → base64 */
export function arrayBufferToBase64(buffer: ArrayBuffer): string {
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
export function resolveSignatureUrl(url: string): string {
  if (!url.startsWith('http')) {
    return `${environment.apiUrl}/${url}`;
  }
  const match = url.match(/\/signatures\/(.+)$/);
  if (match) {
    return `${environment.apiUrl}/files/signatures/${match[1]}`;
  }
  return url;
}

/**
 * 將私有容器（quotes 報價單 / request-attachments 整單附件）的原始 blob URL
 * 轉為後端 JWT 代理路徑，避免前端直接 fetch / iframe 私有 blob 遭 403 或 CORS。
 * blob name 含日期子路徑（yyyy/MM/{guid}{ext}），故保留斜線。
 * 非這兩個容器（或非完整 blob URL）則原樣回傳。
 */
export function resolveFileProxyUrl(url: string): string {
  if (!url) return url;
  const match = url.match(/\/(quotes|request-attachments)\/(.+)$/);
  if (match) {
    return `${environment.apiUrl}/files/${match[1]}/${match[2]}`;
  }
  return url;
}

/** 格式化日期時間（保證日期與時間之間有空格） */
export function fmtDT(val: string | Date): string {
  const d = new Date(val);
  const tz = 'Asia/Taipei';
  const date = d.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: tz });
  const time = d.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: tz });
  return `${date} ${time}`;
}

/** 格式化日期（僅日期） */
export function fmtDate(val: string | Date): string {
  const d = new Date(val);
  return d.toLocaleDateString('zh-TW', {
    year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Taipei'
  });
}

/** 數字千分位格式化 */
export const fmt = (n: number) => n.toLocaleString('zh-TW');

/** CIS 色彩設計語言（所有 PDF service 的 superset） */
export const CIS = {
  forest:      [105, 159, 52]  as const,
  forestMid:   [74, 107, 58]   as const,
  accent:      [140, 115, 85]  as const,
  textPrimary: [82, 83, 88]    as const,
  textMuted:   [163, 150, 133] as const,
  bgBase:      [245, 242, 237] as const,
  bgSurface:   [253, 250, 245] as const,
  border:      [221, 214, 200] as const,
  red:         [160, 64, 64]   as const,
};

/** 字體名稱常數 */
export const FONT_FAMILY = 'NotoSansTC';

/**
 * 將圖片透過 Canvas 縮放至指定尺寸，用於壓縮簽名圖片。
 * - 不放大（scale 最大為 1）
 * - 保留透明背景（PNG）
 */
async function optimizeSignatureImage(buf: ArrayBuffer, mime: string): Promise<string> {
  const maxW = 300;
  const maxH = 150;

  const blob = new Blob([buf], { type: mime });
  const url = URL.createObjectURL(blob);
  try {
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const el = new Image();
      el.onload = () => resolve(el);
      el.onerror = reject;
      el.src = url;
    });

    const scale = Math.min(1, maxW / img.width, maxH / img.height);
    const w = Math.round(img.width * scale);
    const h = Math.round(img.height * scale);

    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(img, 0, 0, w, h);

    return canvas.toDataURL('image/png');
  } finally {
    URL.revokeObjectURL(url);
  }
}

@Injectable({ providedIn: 'root' })
export class PdfCoreService {

  private fontCache: Promise<{ regular: string; bold: string }> | null = null;

  /** 載入字體（singleton cache，全應用只載入一次） */
  loadFonts(): Promise<{ regular: string; bold: string }> {
    if (!this.fontCache) {
      this.fontCache = Promise.all([
        fetch('/assets/fonts/NotoSansTC-Regular.subset.ttf').then(r => r.arrayBuffer()),
        fetch('/assets/fonts/NotoSansTC-Bold.subset.ttf').then(r => r.arrayBuffer()),
      ]).then(([regular, bold]) => ({
        regular: arrayBufferToBase64(regular),
        bold: arrayBufferToBase64(bold),
      }));
    }
    return this.fontCache;
  }

  /** 註冊字體到 jsPDF 文件 */
  registerFonts(doc: any, fonts: { regular: string; bold: string }): void {
    doc.addFileToVFS('NotoSansTC-Regular.ttf', fonts.regular);
    doc.addFileToVFS('NotoSansTC-Bold.ttf', fonts.bold);
    doc.addFont('NotoSansTC-Regular.ttf', FONT_FAMILY, 'normal');
    doc.addFont('NotoSansTC-Bold.ttf', FONT_FAMILY, 'bold');
  }

  /** 預載所有簽名欄圖片（含壓縮），回傳 URL → base64 data URI 的 Map */
  async loadSignatureImages(blocks: SignBlock[]): Promise<Map<string, string>> {
    const urls = blocks.map(b => b.signatureUrl).filter((u): u is string => !!u);
    const unique = [...new Set(urls)];
    const map = new Map<string, string>();
    await Promise.all(unique.map(async url => {
      try {
        const fetchUrl = resolveSignatureUrl(url);
        const resp = await fetch(fetchUrl);
        const buf = await resp.arrayBuffer();
        const mime = resp.headers.get('content-type') || 'image/png';
        const dataUri = await optimizeSignatureImage(buf, mime);
        map.set(url, dataUri);
      } catch { /* 載入失敗則跳過 */ }
    }));
    return map;
  }

  /** 繪製簽名欄（含簽名圖片和日期），各 PDF service 可透過 opts 微調尺寸 */
  drawSignatureBlock(
    doc: any, mx: number, pw: number, cw: number, y: number,
    blocks: SignBlock[], sigImageMap: Map<string, string>,
    opts?: DrawSignatureOptions,
  ): void {
    const F = FONT_FAMILY;
    const gap = opts?.gap ?? 3;
    const labelSize = opts?.labelSize ?? 8.5;
    const maxH = opts?.maxH ?? 10;
    const padding = opts?.padding ?? 3;

    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.3);
    doc.line(mx, y, pw - mx, y);

    y += (gap === 4 ? 6 : 5);
    const blockW = (cw - gap * (blocks.length - 1)) / blocks.length;

    for (let i = 0; i < blocks.length; i++) {
      const bx = mx + i * (blockW + gap);
      const block = blocks[i];

      // 標籤
      doc.setFont(F, 'bold');
      doc.setFontSize(labelSize);
      doc.setTextColor(...CIS.textPrimary);
      doc.text(block.label, bx + blockW / 2, y, { align: 'center' });

      // 簽名線
      const lineY = y + (gap === 4 ? 16 : 14);
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.2);
      doc.line(bx + 2, lineY, bx + blockW - 2, lineY);

      // 簽名檔圖片（簽名線上方，等比例縮放）
      if (block.signatureUrl && sigImageMap.has(block.signatureUrl)) {
        const sigData = sigImageMap.get(block.signatureUrl)!;
        const imgMaxW = blockW - padding * 2;
        try {
          const imgProps = doc.getImageProperties(sigData);
          const ratio = Math.min(imgMaxW / imgProps.width, maxH / imgProps.height);
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
