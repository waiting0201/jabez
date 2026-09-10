/**
 * 瀏覽器儲存的安全存取（單一真相）。
 *
 * 存在理由：`localStorage` / `sessionStorage` **不是永遠可用的**，而失敗方式是 throw 而非回傳 null：
 *   · iOS Safari「設定 → Safari → 阻擋所有 Cookie」開啟時，連讀取 `window.localStorage`
 *     這個 property 本身都會丟 `SecurityError: The operation is insecure.`
 *   · 舊版 iOS 的無痕視窗把配額設為 0，`setItem` 一律丟 `QuotaExceededError`
 *   · 企業 MDM / 家長控制 / 瀏覽器擴充套件亦可能整個關掉網站儲存
 *
 * 2026-09 實際發生過：一位員工用 iPhone Safari 開站看到**整頁純白**。原因是
 * `AuthService` 的 `_token` 是 field initializer（`localStorage.getItem(...)`），
 * 而首頁決策點 `app.routes.ts` 與 `authGuard` / `noAuthGuard` 三個入口都會 `inject(AuthService)`，
 * 於是第一次導航就必定踩到 → 建構失敗 → router 爆掉 → `<app-root>` 什麼都沒 render。
 * 連登入頁都白（`login.ts` 的「記住我」同樣是裸的 field initializer），使用者完全無從自救。
 *
 * 因此**全站禁止直接呼叫 `localStorage` / `sessionStorage`**，一律走這裡的 `safeLocal` / `safeSession`：
 *   · 讀取失敗 → 回 `null`（呼叫端本來就要處理「沒存過」的情況，不需另外改）
 *   · 寫入失敗 → 靜默略過，改寫進記憶體 fallback
 *
 * 記憶體 fallback 的用意：儲存被擋掉的使用者仍能在**單次瀏覽期間**正常登入與操作
 *（token 存在記憶體，refresh 也拿得到），只是重新整理後要重新登入 —— 比整站白畫面好得多。
 */

type StorageKind = 'local' | 'session';

/** 探測用的 key，寫完立刻刪掉；同時涵蓋「讀得到但寫不進去」的零配額情境 */
const PROBE_KEY = '__jabez_storage_probe__';

class SafeStorage {
  /** `undefined` = 尚未探測、`null` = 不可用（走記憶體） */
  private native: Storage | null | undefined = undefined;

  /** 原生儲存不可用時的替代品，生命週期僅限本次頁面載入 */
  private memory = new Map<string, string>();

  constructor(private readonly kind: StorageKind) {}

  getItem(key: string): string | null {
    const store = this.resolve();
    if (!store) return this.memory.get(key) ?? null;
    try {
      return store.getItem(key);
    } catch {
      return this.memory.get(key) ?? null;
    }
  }

  setItem(key: string, value: string): void {
    this.memory.set(key, value);
    const store = this.resolve();
    if (!store) return;
    try {
      store.setItem(key, value);
    } catch {
      // 配額用盡 / 中途被停用：記憶體已經寫過了，這裡靜默略過即可
    }
  }

  removeItem(key: string): void {
    this.memory.delete(key);
    const store = this.resolve();
    if (!store) return;
    try {
      store.removeItem(key);
    } catch {
      // 同 setItem：刪不掉也不該讓呼叫端爆掉
    }
  }

  /** 儲存目前是否真的可用（僅供診斷／提示用，一般流程不需要判斷） */
  get isPersistent(): boolean {
    return this.resolve() !== null;
  }

  /**
   * 解析原生儲存並快取結果。
   * 注意 `window.localStorage` 的 property getter 本身就可能 throw，故連取得參考都要包在 try 裡，
   * 且不可用 `typeof localStorage === 'undefined'` 判斷 —— `typeof` 會觸發 getter，一樣會爆。
   */
  private resolve(): Storage | null {
    if (this.native !== undefined) return this.native;
    try {
      const store = this.kind === 'local' ? window.localStorage : window.sessionStorage;
      store.setItem(PROBE_KEY, '1');
      store.removeItem(PROBE_KEY);
      this.native = store;
    } catch {
      this.native = null;
    }
    return this.native;
  }
}

/** `localStorage` 的安全替身（跨分頁、可持久） */
export const safeLocal = new SafeStorage('local');

/** `sessionStorage` 的安全替身（僅限本分頁） */
export const safeSession = new SafeStorage('session');
