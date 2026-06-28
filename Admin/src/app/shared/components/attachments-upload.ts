import {ChangeDetectionStrategy, Component, inject, input, signal} from '@angular/core';
import {DomSanitizer} from '@angular/platform-browser';
import {ImageCompressionService} from '../services/image-compression.service';
import {FilePreviewModal, PreviewFileData} from './file-preview-modal';
import {AttachmentItem} from '../../features/admin/approval-tasks/models/approval-task.model';

/** 單筆附件的內部狀態：既有檔（含 fileUrl）或新上傳檔（含 file） */
interface AttachmentEntry {
  uid: number;
  fileName: string;
  fileUrl?: string;    // 既有檔案的後端 URL（新上傳為 undefined）
  previewUrl: string;  // 預覽用 URL（既有=fileUrl；新檔=object URL）
  file?: File;         // 新上傳的壓縮後 File
}

/**
 * 整單批次附件上傳（照片 / PDF）共用元件。
 * 自管內部新增 / 既有 / 刪除狀態；圖片以 ImageCompressionService 壓縮後上傳。
 * 父表單透過 viewChild 取得實例後，呼叫 getNewFiles() / getMeta() 組 FormData。
 */
@Component({
  selector: 'app-attachments-upload',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FilePreviewModal],
  template: `
    <div class="card border-0 shadow-sm mt-6">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon text-primary" style="stroke: currentColor">
          <use href="/assets/icons/sprite.svg#paperclip"></use>
        </svg>
        附件
      </div>
      <div class="card-body">
        @if (!disabled()) {
          <label class="flex flex-col items-center justify-center rounded-3 py-4 px-4 mb-4 text-center"
                 style="cursor:pointer; border: 2px dashed var(--bs-border-color);">
            <svg class="sa-icon sa-icon-2x text-muted mb-2" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#upload"></use>
            </svg>
            <span class="fw-500">點擊上傳附件（照片或文件）</span>
            <span class="text-muted small mt-1">支援 JPG、PNG、HEIC、PDF，可多選；圖片會自動壓縮</span>
            <input type="file" class="hidden" multiple accept="image/*,.heic,.heif,application/pdf"
                   (change)="onFilesSelected($event)">
          </label>
        }

        @if (entries().length > 0) {
          <div class="flex flex-col gap-2">
            @for (e of entries(); track e.uid) {
              <div class="flex items-center gap-2 border rounded px-3 py-2">
                <svg class="sa-icon text-muted" style="stroke: currentColor">
                  <use [attr.href]="'/assets/icons/sprite.svg#' + (isImage(e.fileName) ? 'image' : 'file-text')"></use>
                </svg>
                <span class="fw-500 text-truncate flex-1">{{ e.fileName }}</span>
                <button type="button" class="btn btn-sm btn-outline-secondary"
                        (click)="openPreview(e)">檢視</button>
                @if (!disabled()) {
                  <button type="button" class="btn btn-sm btn-ghost-danger inline-flex items-center"
                          (click)="remove(e.uid)" title="移除">
                    <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                  </button>
                }
              </div>
            }
          </div>
        } @else {
          <div class="text-center text-muted py-3 small">尚未上傳附件</div>
        }
      </div>
    </div>

    @if (previewFile) {
      <app-file-preview-modal [file]="previewFile" (closed)="closePreview()" />
    }
  `,
})
export class AttachmentsUpload {
  /** 編輯模式時帶入的既有附件 */
  existing = input<AttachmentItem[] | null | undefined>(null);
  /** 唯讀（檢視）時隱藏上傳與刪除 */
  disabled = input<boolean>(false);

  private imageCompression = inject(ImageCompressionService);
  private sanitizer        = inject(DomSanitizer);

  entries = signal<AttachmentEntry[]>([]);
  private uidSeq = 0;
  private initialized = false;

  /** 首次取得 existing 輸入時回填（避免重複初始化覆蓋使用者編輯） */
  ngOnChanges() {
    if (this.initialized) return;
    const items = this.existing();
    if (items && items.length > 0) {
      this.entries.set(items.map(it => ({
        uid:        this.uidSeq++,
        fileName:   it.fileName,
        fileUrl:    it.fileUrl,
        previewUrl: it.fileUrl ?? '',
      })));
      this.initialized = true;
    }
  }

  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const rawFiles = Array.from(input.files);
    input.value = '';

    for (const raw of rawFiles) {
      const file = await this.imageCompression.compress(raw, {maxSize: 1600, quality: 0.85});
      this.entries.update(list => [...list, {
        uid:        this.uidSeq++,
        fileName:   file.name,
        previewUrl: URL.createObjectURL(file),
        file,
      }]);
    }
  }

  remove(uid: number) {
    const entry = this.entries().find(e => e.uid === uid);
    if (entry?.file && entry.previewUrl.startsWith('blob:')) URL.revokeObjectURL(entry.previewUrl);
    this.entries.update(list => list.filter(e => e.uid !== uid));
  }

  isImage(name: string): boolean {
    return /\.(jpe?g|png|gif|webp|bmp|heic|heif)$/i.test(name);
  }

  /** 新上傳的檔案（順序與 getMeta() 的 fileIndex 對齊） */
  getNewFiles(): File[] {
    return this.entries().filter(e => e.file).map(e => e.file!);
  }

  /** 後端 attachments JSON：既有檔保留 fileUrl，新檔以 fileIndex 對應 attachmentFiles */
  getMeta(): {fileName: string; fileUrl: string | null; fileIndex: number}[] {
    let fileIndex = 0;
    return this.entries().map(e => e.file
      ? {fileName: e.fileName, fileUrl: null,            fileIndex: fileIndex++}
      : {fileName: e.fileName, fileUrl: e.fileUrl ?? null, fileIndex: -1});
  }

  // ── 預覽 ────────────────────────────────────────────────────────────────
  previewFile: PreviewFileData | null = null;
  openPreview(e: AttachmentEntry) {
    const url = e.previewUrl || e.fileUrl || '';
    this.previewFile = {name: e.fileName, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }
}
