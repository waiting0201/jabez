/**
 * HEIC / HEIF（iPhone 預設拍照格式）處理的**單一入口**。
 *
 * 為什麼要收斂：除 Safari 17+ 以外的瀏覽器都無法在 <img> 顯示 HEIC，故上傳前一律轉 JPEG。
 * 原本六支申請表單各自 copy 一份 `_convertHeicIfNeeded`，三個毛病一起出事（2026-10 PR-20261001-001）：
 *   ① 只看副檔名 —— 檔名不是 .heic（或被改名）的 HEIC 直接漏網；
 *   ② 用的 heic2any 0.0.4 年久失修，新款 iPhone 的 HEIC 常解不開；
 *   ③ 轉失敗時 `return file` 靜默保留原檔 → 存進去的是瀏覽器顯示不了的 HEIC，預覽只剩「無法預覽」。
 * 本檔：三種判定（副檔名 / MIME / ftyp magic bytes，與後端 FileSignatureValidator 同一份 brand 清單）、
 * 以 heic-to（較新的 libheif）為主、heic2any 為備援，**兩者都失敗就丟例外**，由呼叫端拒收並提示。
 *
 * 兩個轉檔套件都很大（heic-to 約 3 MB），一律 dynamic import —— 沒碰到 HEIC 的人不會下載。
 */

/** ISO-BMFF `ftyp` box 的 HEIF 系 brand（同 Api/Common/FileSignatureValidator.cs） */
const HEIC_BRANDS = new Set(['heic', 'heix', 'hevc', 'heim', 'heis', 'hevm', 'hevs', 'heiq', 'mif1', 'msf1']);

/** 由檔名判斷（供只有 URL / 檔名、拿不到 bytes 的場合，例如預覽 modal 決定要不要轉檔） */
export function isHeicName(name: string | null | undefined): boolean {
  return /\.(heic|heif)$/i.test(name ?? '');
}

/** 由 bytes 判斷：offset 4–7 為 'ftyp'、8–11 為 HEIF 系 brand */
export async function isHeicBlob(blob: Blob): Promise<boolean> {
  if (blob.type === 'image/heic' || blob.type === 'image/heif') return true;
  try {
    const head = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
    if (head.length < 12) return false;
    const ascii = (from: number, to: number) => String.fromCharCode(...head.slice(from, to));
    return ascii(4, 8) === 'ftyp' && HEIC_BRANDS.has(ascii(8, 12));
  } catch {
    return false;
  }
}

/** 檔案是否為 HEIC：副檔名 / MIME / magic bytes 任一命中 */
export async function isHeicFile(file: File): Promise<boolean> {
  return isHeicName(file.name) || isHeicBlob(file);
}

/**
 * HEIC → JPEG Blob。heic-to 為主、heic2any 為備援；兩者都失敗時丟例外（**不得**退回原檔）。
 */
export async function convertHeicToJpeg(blob: Blob, quality = 0.85): Promise<Blob> {
  try {
    const {heicTo} = await import('heic-to');
    return await heicTo({blob, type: 'image/jpeg', quality});
  } catch (primaryError) {
    try {
      const heic2any = (await import('heic2any')).default;
      const out = await heic2any({blob, toType: 'image/jpeg', quality});
      return Array.isArray(out) ? out[0] : out;
    } catch {
      throw primaryError;
    }
  }
}

/**
 * 上傳前處理：是 HEIC 就轉成 `<原檔名>.jpg` 的 JPEG File，否則原樣回傳。
 * 轉換失敗丟例外 —— 呼叫端須拒收該檔並提示使用者，不可把 HEIC 原檔送出。
 */
export async function convertHeicIfNeeded(file: File, quality = 0.85): Promise<File> {
  if (!(await isHeicFile(file))) return file;
  const jpeg = await convertHeicToJpeg(file, quality);
  const baseName = file.name.replace(/\.[^.]+$/, '') || 'image';
  return new File([jpeg], `${baseName}.jpg`, {type: 'image/jpeg'});
}

/** 轉檔失敗時給使用者的統一訊息 */
export function heicFailedMessage(fileName: string): string {
  return `「${fileName}」為 HEIC 格式且無法轉換，請在手機「設定 › 相機 › 格式」改選「最相容」，或截圖 / 轉成 JPG 後再上傳。`;
}
