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

/** 統編檢查碼權重（財政部統一編號邏輯乘數） */
const TAX_ID_WEIGHTS = [1, 2, 1, 2, 1, 2, 4, 1] as const;

/**
 * 台灣統一編號格式 + 檢查碼驗證（財政部 2023 新制）。
 * 逐位乘以權重後「拆位相加」求總和，總和須為 5 的倍數；
 * 第 7 碼為 7 時另允許總和 +1 為 5 的倍數。
 *
 * 用途：區分「OCR 把手寫數字讀錯」與「這張發票真的不是開給本公司」——
 * 前者多半連檢查碼都過不了，可給使用者更精準的提示。
 */
export function isValidTaxIdFormat(taxId: string): boolean {
  const id = normalizeTaxId(taxId);
  if (id.length !== 8) return false;

  const sum = [...id].reduce((acc, ch, i) => {
    const product = Number(ch) * TAX_ID_WEIGHTS[i];
    return acc + Math.floor(product / 10) + (product % 10);
  }, 0);

  return sum % 5 === 0 || (id[6] === '7' && (sum + 1) % 5 === 0);
}

/**
 * 兩組同長度統編是否「僅差 1 碼」（漢明距離 1）。
 * 手寫發票最常見的誤讀就是單一數字看錯（9→5、6→8），此判定用來認出這種情況。
 */
const differsByOneDigit = (a: string, b: string): boolean => {
  if (a.length !== b.length) return false;
  let diff = 0;
  for (let i = 0; i < a.length; i++) {
    if (a[i] !== b[i] && ++diff > 1) return false;
  }
  return diff === 1;
};

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
 * 驗證發票買方抬頭與統編是否為公司合法的 5 組之一。統編為主要錨點（8 碼數字較可靠）。
 * - 抬頭與統編「需皆讀得到」才判斷；任一缺漏（含收銀機 / 二聯式 / 手寫發票讀不全）→ ok（不警告）
 * - 買方統編與賣方統編相同 → OCR 抓錯欄位（多半是抄了「營業人蓋用統一發票專用章」），視為讀不到 → ok
 * - 統編符合某組 + 抬頭相容 → ok
 * - 統編符合某組但抬頭明顯為他家公司 → warn（抬頭與統編不符）
 * - 統編不在 5 組內、且格式 / 檢查碼不合，但與某組「僅差 1 碼」且抬頭相容 → ok（手寫誤讀該組）
 * - 統編不在 5 組內、且格式 / 檢查碼不合 → warn（辨識不完整，多半是手寫誤讀）
 * - 統編不在 5 組內、但格式合法 → warn（統編不正確）
 *
 * 所有警告訊息一律帶出「讀到的統編」，使用者才有辦法自行判斷是 OCR 抓錯欄位還是真的開錯抬頭。
 *
 * @param sellerTaxId 賣方統編（OCR 一併辨識的發票專用章統編），用於交叉比對排除抓錯欄位；可不帶。
 */
export function validateInvoiceBuyer(
  buyerName: string,
  buyerTaxId: string,
  sellerTaxId = '',
): InvoiceBuyerResult {
  const taxId = normalizeTaxId(buyerTaxId);
  const title = normalizeTitle(buyerName);

  // 抬頭與統編需皆讀得到才判斷；任一缺漏 → 無從判斷，不跳警告
  if (!taxId || !title) {
    return { level: 'ok' };
  }

  // 買方統編 == 賣方統編 → OCR 顯然抄到發票專用章（手寫發票的買受人統編潦草時最常發生），
  // 視同買方統編讀不到，不跳假警告
  if (taxId === normalizeTaxId(sellerTaxId)) {
    return { level: 'ok' };
  }

  const byTaxId = VALID_INVOICE_BUYERS.find((b) => b.taxId === taxId);
  if (byTaxId) {
    return titleCompatible(byTaxId.title, buyerName)
      ? { level: 'ok' }
      : { level: 'warn', message: `買方抬頭「${buyerName}」與統編 ${taxId} 不符，請確認。` };
  }

  if (!isValidTaxIdFormat(taxId)) {
    // 檢查碼不合 ＝ 這串數字幾乎不可能是真實存在的統編，多半是手寫誤讀。
    // 若與某組白名單「僅差 1 碼」且抬頭也相容，即認定為該組的誤讀，不跳假警告。
    // 限定在「檢查碼已不合法」的前提下才容錯，真實他家公司的統編（檢查碼必合法）不受影響。
    const misread = VALID_INVOICE_BUYERS.find(
      (b) => differsByOneDigit(b.taxId, taxId) && titleCompatible(b.title, buyerName),
    );
    if (misread) {
      return { level: 'ok' };
    }

    return {
      level: 'warn',
      message: `買方統編「${taxId}」辨識不完整（可能為手寫誤讀），請確認。`,
    };
  }

  return { level: 'warn', message: `買方統編「${taxId}」不在公司白名單，請確認。` };
}
