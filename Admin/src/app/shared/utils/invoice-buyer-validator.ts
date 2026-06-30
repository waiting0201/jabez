/**
 * 發票買方抬頭／統編驗證（單一真相）。
 *
 * 公司只有以下 5 組合法的「抬頭 ＋ 統編」組合，發票買受人必須為其一。
 * 上傳發票經 OCR 辨識買方抬頭與統編後，於上傳當下做即時驗證，
 * 不符者在該列明細下方顯示警告（非阻擋式，仍可送出）。
 *
 * 未來增減公司只需維護 VALID_INVOICE_BUYERS。
 */

export interface InvoiceBuyer {
  /** 抬頭（公司名稱） */
  title: string;
  /** 統一編號（8 碼數字） */
  taxId: string;
}

export const VALID_INVOICE_BUYERS: readonly InvoiceBuyer[] = [
  { title: '雅比斯國際創意策略股份有限公司', taxId: '28830371' },
  { title: '雅比斯國際創意策略股份有限公司壯圍營業所', taxId: '92663912' },
  { title: '疆界地域美學有限公司', taxId: '42837895' },
  { title: '疆界地域美學有限公司豐濱營業所', taxId: '60277862' },
  { title: '樂樂院子創新有限公司', taxId: '54968007' },
] as const;

export type InvoiceBuyerLevel = 'ok' | 'warn';

export interface InvoiceBuyerResult {
  level: InvoiceBuyerLevel;
  message?: string;
}

/** 統編：全形數字轉半形後只留數字 */
const normalizeTaxId = (value: string) =>
  (value ?? '')
    .replace(/[０-９]/g, (d) => String.fromCharCode(d.charCodeAt(0) - 0xfee0))
    .replace(/\D/g, '');

/** 抬頭：移除所有空白（含全形）以容忍手寫間距差異 */
const normalizeTitle = (value: string) => (value ?? '').replace(/\s+/g, '').trim();

/**
 * 抬頭相容性比對（容忍 OCR 對長中文公司名常見的缺字 / 截斷）。
 * 統編已唯一識別公司實體，此處只要抬頭「明顯屬於同一家」即視為相容：
 * 完全相等、互為子字串、或公司名前 3 個識別字相同（如「雅比斯」「疆界地」）。
 * 不同公司（前綴不同）才會判為不符。
 */
const titleCompatible = (entryTitle: string, ocrTitle: string): boolean => {
  const a = normalizeTitle(entryTitle);
  const b = normalizeTitle(ocrTitle);
  if (!b) return true; // 抬頭讀不到 → 以統編為準
  if (a === b || a.includes(b) || b.includes(a)) return true;
  return a.slice(0, 3) === b.slice(0, 3);
};

/**
 * 驗證發票買方抬頭與統編是否為公司合法的 4 組之一。統編為主要錨點（8 碼數字較可靠）。
 * - 抬頭與統編「需皆讀得到」才判斷；任一缺漏（含收銀機 / 二聯式 / 手寫發票讀不全）→ ok（不警告）
 * - 統編符合某組 + 抬頭相容 → ok
 * - 統編符合某組但抬頭明顯為他家公司 → warn（抬頭與統編不符）
 * - 統編不在 4 組內 → warn（統編不正確）
 */
export function validateInvoiceBuyer(buyerName: string, buyerTaxId: string): InvoiceBuyerResult {
  const taxId = normalizeTaxId(buyerTaxId);
  const title = normalizeTitle(buyerName);

  // 抬頭與統編需皆讀得到才判斷；任一缺漏 → 無從判斷，不跳警告
  if (!taxId || !title) {
    return { level: 'ok' };
  }

  const byTaxId = VALID_INVOICE_BUYERS.find((b) => b.taxId === taxId);
  if (byTaxId) {
    return titleCompatible(byTaxId.title, buyerName)
      ? { level: 'ok' }
      : { level: 'warn', message: '買方抬頭與統編不符，請確認。' };
  }

  return { level: 'warn', message: '買方統編不正確（非公司抬頭），請確認。' };
}
