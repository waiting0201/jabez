import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {ToastrService} from 'ngx-toastr';
import {firstValueFrom} from 'rxjs';
import {PreviewFileData} from '../components/file-preview-modal';
import {resolveFileProxyUrl} from './pdf-core.service';

/**
 * 私有容器（quotes 報價單 / request-attachments 整單附件）檔案預覽載入器。
 *
 * 這些檔案存於私有 Blob，前端不能直接把原始 blob URL 丟進 iframe / img（會 403 / CORS），
 * 且登入即可的代理路由也無法靠 iframe 帶 JWT。故統一改為：
 *   resolveFileProxyUrl → HttpClient（auth.interceptor 自動帶 JWT）取 blob → URL.createObjectURL。
 * 呼叫端關閉預覽時務必呼叫 {@link revoke} 釋放 object URL。
 */
@Injectable({providedIn: 'root'})
export class FilePreviewLoader {
  private http      = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);
  private toastr    = inject(ToastrService);

  /** 載入檔案為 object URL 並回傳預覽資料；失敗回傳 null 並提示。 */
  async load(rawUrl: string, name: string): Promise<PreviewFileData | null> {
    if (!rawUrl) return null;
    try {
      const url = resolveFileProxyUrl(rawUrl);
      const blob = await firstValueFrom(this.http.get(url, {responseType: 'blob'}));
      const objectUrl = URL.createObjectURL(blob);
      return {name, url: objectUrl, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(objectUrl)};
    } catch {
      this.toastr.error('無法載入檔案，請稍後再試。', '載入失敗');
      return null;
    }
  }

  /** 釋放先前 load 產生的 object URL。 */
  revoke(file: PreviewFileData | null) {
    if (file?.url?.startsWith('blob:')) URL.revokeObjectURL(file.url);
  }
}
