import {Component, input, output, signal, computed, HostListener} from '@angular/core';
import {SafeResourceUrl} from '@angular/platform-browser';

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
          @if (isImage()) {
            <div class="file-preview-image-wrap">
              <img [src]="file().url"
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
export class FilePreviewModal {
  file = input.required<PreviewFileData>();
  closed = output<void>();

  zoomLevel = signal(1);
  zoomPercent = computed(() => Math.round(this.zoomLevel() * 100));

  isImage = computed(() => /\.(jpe?g|png|gif|webp|bmp)$/i.test(this.file().name));
  isPdf = computed(() => /\.pdf$/i.test(this.file().name));

  typeIcon = computed(() => this.isImage() ? 'image' : this.isPdf() ? 'file-text' : 'file');
  typeLabel = computed(() => this.isImage() ? '圖片' : this.isPdf() ? 'PDF' : '檔案');
  typeBadgeClass = computed(() =>
    this.isImage() ? 'file-preview-badge-image' :
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
