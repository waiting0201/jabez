import {environment} from '@/environments/environment';

/**
 * 檔案預覽 / 對外請求用的 URL 白名單（縱深防禦，後端另有 FileUrl 白名單）。
 *
 * 威脅：申請明細的 fileUrl 為使用者可控字串，若為 `javascript:` / 外部網址，
 * 經 `bypassSecurityTrustResourceUrl` 放進 iframe 會在 SPA origin 執行，或把 Bearer 送往外部主機。
 *
 * 允許：① `blob:` 本地物件網址（且 origin 為本站）② http(s) 且主機為 environment.apiUrl 主機
 *       或 Azure Blob 儲存網域（*.blob.core.windows.net）；非 production 另放行本機 Azurite。
 */
const BLOB_HOST_RE = /\.blob\.core\.windows\.net$/i;
const DEV_HOSTS = new Set(['localhost', '127.0.0.1']);

function apiOrigin(): string | null {
  try {
    return new URL(environment.apiUrl).origin;
  } catch {
    return null;
  }
}

/** 是否為「本站 API」網址（只有這種請求可附加 Bearer）。相對路徑視為同源 API 不在此判斷。 */
export function isApiUrl(url: string): boolean {
  const origin = apiOrigin();
  if (!origin) return false;
  try {
    return new URL(url, window.location.href).origin === origin;
  } catch {
    return false;
  }
}

/** 是否為允許預覽（iframe / fetch 讀檔）的網址。 */
export function isSafePreviewUrl(url: string | null | undefined): boolean {
  if (!url) return false;
  const trimmed = url.trim();
  try {
    if (/^blob:/i.test(trimmed)) {
      // blob:https://host/uuid —— 必須是本站自己建立的物件網址
      return new URL(trimmed.slice(5)).origin === window.location.origin;
    }
    const u = new URL(trimmed);
    if (u.protocol !== 'https:' && u.protocol !== 'http:') return false;
    if (u.origin === apiOrigin()) return true;
    if (BLOB_HOST_RE.test(u.hostname)) return true;
    return !environment.production && DEV_HOSTS.has(u.hostname);
  } catch {
    return false; // 相對路徑 / 非法字串一律不放行
  }
}
