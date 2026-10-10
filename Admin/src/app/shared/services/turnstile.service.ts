import {Injectable} from '@angular/core';
import {environment} from '@/environments/environment';

/** Cloudflare Turnstile 全域物件（只宣告本服務用到的部分） */
interface TurnstileApi {
  render(container: HTMLElement, options: Record<string, unknown>): string;
  remove(widgetId: string): void;
}

declare global {
  interface Window { turnstile?: TurnstileApi; }
}

const SCRIPT_URL = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

/** 取 token 的總時限：Cloudflare 起疑時會浮出勾選框，要留時間讓使用者點 */
const TOKEN_TIMEOUT_MS = 15000;

/**
 * Cloudflare Turnstile 人機驗證（防機器人打卡第四道關卡，2026-10）。
 *
 * - script **第一次呼叫時才動態載入**，不拖慢其他頁面。
 * - 每次 `getToken` 都 render 一個新 widget、拿到 token 後立即 remove —— token 只能驗證一次，
 *   重用舊 widget 的 token 會被後端判為 failed。
 * - `appearance: 'interaction-only'`：平常完全看不到，Cloudflare 起疑時才在 container 浮出勾選框。
 * - **載入失敗 / 逾時 / sitekey 未設定一律回 null、不在前端擋**：放不放行由後端 `Turnstile:Mode` 決定
 *   （log 模式照常打卡；enforce 時後端回明確訊息）。前端擋下只會讓 Cloudflare 故障變成全公司打不了卡。
 * - CSP 須放行 `https://challenges.cloudflare.com`（script-src / frame-src / connect-src），漏了 widget 會被靜默擋下。
 */
@Injectable({providedIn: 'root'})
export class TurnstileService {
  private loading: Promise<TurnstileApi | null> | null = null;

  /** 在 container 內取得一枚綁定 action 的 token；失敗回 null */
  async getToken(container: HTMLElement | undefined, action: string): Promise<string | null> {
    if (!environment.turnstileSiteKey || !container) return null;

    const api = await this.loadScript();
    if (!api) return null;

    return new Promise<string | null>(resolve => {
      let widgetId: string | null = null;
      let settled = false;

      const finish = (token: string | null) => {
        if (settled) return;
        settled = true;
        clearTimeout(timer);
        if (widgetId) {
          try { api.remove(widgetId); } catch { /* widget 已不存在 */ }
        }
        resolve(token);
      };

      const timer = setTimeout(() => finish(null), TOKEN_TIMEOUT_MS);

      try {
        widgetId = api.render(container, {
          sitekey: environment.turnstileSiteKey,
          action,
          appearance: 'interaction-only',
          language: 'zh-tw',
          callback: (token: string) => finish(token),
          'error-callback': () => { finish(null); return true; },
          'expired-callback': () => finish(null),
          'timeout-callback': () => finish(null),
        });
      } catch {
        finish(null);
      }
    });
  }

  private loadScript(): Promise<TurnstileApi | null> {
    if (window.turnstile) return Promise.resolve(window.turnstile);
    if (this.loading) return this.loading;

    this.loading = new Promise<TurnstileApi | null>(resolve => {
      const script = document.createElement('script');
      script.src = SCRIPT_URL;
      script.async = true;
      script.onload = () => resolve(window.turnstile ?? null);
      script.onerror = () => {
        this.loading = null;   // 允許下次重試（例如網路暫時中斷）
        script.remove();
        resolve(null);
      };
      document.head.appendChild(script);
    });
    return this.loading;
  }
}
