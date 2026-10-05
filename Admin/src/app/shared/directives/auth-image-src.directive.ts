import {DestroyRef, Directive, ElementRef, effect, inject, input} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';

/**
 * 需要登入才能讀的圖片（例如簽名檔 `/files/signatures/...`）的 `<img>` 顯示方式。
 *
 * `<img src>` 無法帶 Authorization header，所以改成：以 HttpClient（經 authInterceptor 附 Bearer token）
 * 取回 blob → `URL.createObjectURL` → 寫進 `src`。用法：`<img [appAuthSrc]="url" alt="...">`（不要同時綁 `[src]`）。
 *
 * - `data:` / `blob:` URL（本機預覽）與非本站 API 的網址原樣寫入 `src`，**不送 token**
 *   （interceptor 對任何 URL 都附 token，絕不能讓 token 送去第三方網域）。
 * - 載入失敗或網址為空：清空 `src`（不顯示破圖）。
 * - 切換網址或元件銷毀時回收舊的 object URL。
 */
@Directive({
  selector: 'img[appAuthSrc]',
})
export class AuthImageSrcDirective {
  private http = inject(HttpClient);
  private el = inject<ElementRef<HTMLImageElement>>(ElementRef);

  readonly appAuthSrc = input<string | null | undefined>(null);

  private objectUrl: string | null = null;
  private seq = 0;

  constructor() {
    effect(() => this.load(this.appAuthSrc()));
    inject(DestroyRef).onDestroy(() => this.revoke());
  }

  private load(url: string | null | undefined): void {
    const mySeq = ++this.seq;
    this.revoke();
    const img = this.el.nativeElement;

    if (!url) {
      img.removeAttribute('src');
      return;
    }
    if (!url.startsWith(environment.apiUrl)) {
      img.src = url;
      return;
    }

    this.http.get(url, {responseType: 'blob'}).subscribe({
      next: blob => {
        if (mySeq !== this.seq) return;   // 已換成別張圖，丟棄過時的回應
        this.objectUrl = URL.createObjectURL(blob);
        img.src = this.objectUrl;
      },
      error: () => {
        if (mySeq === this.seq) img.removeAttribute('src');
      },
    });
  }

  private revoke(): void {
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = null;
    }
  }
}
