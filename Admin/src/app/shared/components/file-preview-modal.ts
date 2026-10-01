import {Component, input, output, signal, computed, effect, inject, HostListener, OnDestroy} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {SafeResourceUrl} from '@angular/platform-browser';
import {firstValueFrom} from 'rxjs';
import {environment} from '@/environments/environment';
import {convertHeicToJpeg, isHeicName} from '../utils/heic';

/**
 * 明細憑證四個容器的原始 blob 網址 → API 代理網址（帶 JWT，避開 Storage CORS）。
 * 只供 HEIC 預覽取 bytes 用；一般圖片 / PDF 仍直接用原始網址顯示。
 */
const ITEM_FILE_CONTAINER_RE = /\/(invoices|advance-files|write-off-invoices|travel-write-off-invoices)\/(.+)$/;

export interface PreviewFileData {
  name: string;
  url: string;
  safeUrl?: SafeResourceUrl;
}

@Component({
  selector: 'app-file-preview-modal',
  template: `
    <!-- Backdrop -->
    <div class="file-preview-backdrop" (click)="closed.emit()">
      <div class="file-preview-container" (click)="$event.stopPropagation()">

        <!-- Toolbar -->
        <div class="file-preview-toolbar">
          <div class="flex items-center gap-3 min-w-0">
            <span class="file-preview-type-badge" [class]="typeBadgeClass()">
              <svg style="width:14px;height:14px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                <use [attr.href]="'/assets/icons/sprite.svg#' + typeIcon()"></use>
              </svg>
              {{ typeLabel() }}
            </span>
            <span class="file-preview-filename">{{ file().name }}</span>
          </div>
          <div class="flex items-center gap-1">
            @if (isImage()) {
              <button class="file-preview-btn" (click)="zoomOut()" title="縮小 (-)">
                <svg style="width:16px;height:16px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                  <use href="/assets/icons/sprite.svg#zoom-out"></use>
                </svg>
              </button>
              <span class="file-preview-zoom-label">
                {{ zoomPercent() }}%
              </span>
              <button class="file-preview-btn" (click)="zoomIn()" title="放大 (+)">
                <svg style="width:16px;height:16px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                  <use href="/assets/icons/sprite.svg#zoom-in"></use>
                </svg>
              </button>
              <div class="file-preview-divider"></div>
            }
            <a class="file-preview-btn" [href]="file().url" [download]="file().name" title="下載檔案">
              <svg style="width:16px;height:16px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                <use href="/assets/icons/sprite.svg#download"></use>
              </svg>
            </a>
            <div class="file-preview-divider"></div>
            <button class="file-preview-btn file-preview-btn-close" (click)="closed.emit()" title="關閉 (Esc)">
              <svg style="width:18px;height:18px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                <use href="/assets/icons/sprite.svg#x"></use>
              </svg>
            </button>
          </div>
        </div>

        <!-- Viewer -->
        <div class="file-preview-viewer">
          @if (isHeic() && !heicUrl()) {
            <div class="file-preview-fallback">
              <div class="file-preview-fallback-card">
                @if (heicFailed()) {
                  <p class="file-preview-fallback-text">此 HEIC 圖片無法在瀏覽器中轉換預覽，請下載後檢視</p>
                  <a [href]="file().url" [download]="file().name"
                     class="btn btn-sm btn-primary inline-flex items-center gap-2">
                    <svg style="width:14px;height:14px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                      <use href="/assets/icons/sprite.svg#download"></use>
                    </svg>
                    下載檔案
                  </a>
                } @else {
                  <span class="inline-block w-6 h-6 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
                  <p class="file-preview-fallback-text">HEIC 圖片轉換中…</p>
                }
              </div>
            </div>
          } @else if (isImage()) {
            <div class="file-preview-image-wrap">
              <img [src]="displayUrl()"
                   [alt]="file().name"
                   class="file-preview-image"
                   [style.transform]="'scale(' + zoomLevel() + ')'"
                   draggable="false">
            </div>
          } @else if (isPdf()) {
            <iframe [src]="file().safeUrl" class="file-preview-pdf"></iframe>
          } @else {
            <div class="file-preview-fallback">
              <div class="file-preview-fallback-card">
                <svg style="width:48px;height:48px;stroke:currentColor;fill:none;stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round;opacity:0.4">
                  <use href="/assets/icons/sprite.svg#file"></use>
                </svg>
                <p class="file-preview-fallback-text">此檔案類型無法預覽</p>
                <a [href]="file().url" [download]="file().name"
                   class="btn btn-sm btn-primary inline-flex items-center gap-2">
                  <svg style="width:14px;height:14px;stroke:currentColor;fill:none;stroke-width:2;stroke-linecap:round;stroke-linejoin:round">
                    <use href="/assets/icons/sprite.svg#download"></use>
                  </svg>
                  下載檔案
                </a>
              </div>
            </div>
          }
        </div>

      </div>
    </div>
  `,
})
export class FilePreviewModal implements OnDestroy {
  private http = inject(HttpClient);

  file = input.required<PreviewFileData>();
  closed = output<void>();

  zoomLevel = signal(1);
  zoomPercent = computed(() => Math.round(this.zoomLevel() * 100));

  /**
   * HEIC / HEIF：除 Safari 17+ 外瀏覽器都無法在 <img> 顯示，故開啟時先抓 bytes 在前端轉成 JPEG object URL。
   * 主要救的是「上傳時轉檔失敗、HEIC 原檔已存進去」的歷史資料（2026-10 PR-20261001-001）。
   */
  isHeic = computed(() => isHeicName(this.file().name));
  heicUrl = signal<string | null>(null);
  heicFailed = signal(false);

  isImage = computed(() => /\.(jpe?g|png|gif|webp|bmp)$/i.test(this.file().name) || this.heicUrl() !== null);
  displayUrl = computed(() => this.heicUrl() ?? this.file().url);

  constructor() {
    effect(() => {
      const f = this.file();
      this._revokeHeic();
      this.heicFailed.set(false);
      if (isHeicName(f.name)) void this._convertHeic(f.url);
    });
  }

  ngOnDestroy() { this._revokeHeic(); }

  private async _convertHeic(url: string) {
    try {
      const blob = await this._fetchBytes(url);
      const jpeg = await convertHeicToJpeg(blob);
      if (this.file().url !== url) return; // 轉換期間已切換到別的檔案
      this.heicUrl.set(URL.createObjectURL(jpeg));
    } catch {
      if (this.file().url === url) this.heicFailed.set(true);
    }
  }

  /** blob: 網址（FilePreviewLoader / 本機剛選的檔）直接 fetch；明細憑證走 API 代理；其餘嘗試直接 fetch */
  private async _fetchBytes(url: string): Promise<Blob> {
    if (url.startsWith('blob:')) return (await fetch(url)).blob();
    const m = url.match(ITEM_FILE_CONTAINER_RE);
    if (m) {
      const path = m[2].split('?')[0];
      return firstValueFrom(this.http.get(`${environment.apiUrl}/files/${m[1]}/${path}`, {responseType: 'blob'}));
    }
    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.blob();
  }

  private _revokeHeic() {
    const u = this.heicUrl();
    if (u) URL.revokeObjectURL(u);
    this.heicUrl.set(null);
  }
  isPdf = computed(() => /\.pdf$/i.test(this.file().name));

  typeIcon = computed(() => this.isImage() || this.isHeic() ? 'image' : this.isPdf() ? 'file-text' : 'file');
  typeLabel = computed(() => this.isImage() || this.isHeic() ? '圖片' : this.isPdf() ? 'PDF' : '檔案');
  typeBadgeClass = computed(() =>
    this.isImage() || this.isHeic() ? 'file-preview-badge-image' :
    this.isPdf() ? 'file-preview-badge-pdf' :
    'file-preview-badge-file'
  );

  zoomIn() { this.zoomLevel.update(z => Math.min(z + 0.25, 3)); }
  zoomOut() { this.zoomLevel.update(z => Math.max(z - 0.25, 0.25)); }

  @HostListener('document:keydown.escape')
  onEscape() { this.closed.emit(); }

  @HostListener('document:keydown.+')
  onPlus() { if (this.isImage()) this.zoomIn(); }

  @HostListener('document:keydown.-')
  onMinus() { if (this.isImage()) this.zoomOut(); }
}
