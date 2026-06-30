import {ChangeDetectionStrategy, Component, inject, input} from '@angular/core';
import {FilePreviewModal, PreviewFileData} from './file-preview-modal';
import {FilePreviewLoader} from '../services/file-preview-loader';
import {AttachmentItem} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 整單批次附件唯讀檢視（照片 / PDF），用於申請詳情頁與簽核審核頁。
 * 無附件時不渲染任何內容（由父層自行決定是否包卡片標題）。
 */
@Component({
  selector: 'app-attachments-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FilePreviewModal],
  template: `
    @if (attachments()?.length) {
      <div class="flex flex-col gap-2">
        @for (a of attachments(); track a.id) {
          <div class="flex items-center gap-2 border rounded px-3 py-2">
            <svg class="sa-icon text-muted" style="stroke: currentColor">
              <use [attr.href]="'/assets/icons/sprite.svg#' + (isImage(a.fileName) ? 'image' : 'file-text')"></use>
            </svg>
            <span class="fw-500 text-truncate flex-1">{{ a.fileName }}</span>
            @if (a.fileUrl) {
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="openPreview(a)">檢視</button>
            }
          </div>
        }
      </div>
    } @else {
      <div class="text-center text-muted py-3 small">無附件</div>
    }

    @if (previewFile) {
      <app-file-preview-modal [file]="previewFile" (closed)="closePreview()" />
    }
  `,
})
export class AttachmentsList {
  attachments = input<AttachmentItem[] | null | undefined>(null);

  private previewLoader = inject(FilePreviewLoader);

  isImage(name: string): boolean {
    return /\.(jpe?g|png|gif|webp|bmp|heic|heif)$/i.test(name);
  }

  previewFile: PreviewFileData | null = null;
  async openPreview(a: AttachmentItem) {
    if (!a.fileUrl) return;
    // 私有容器（request-attachments）需透過 JWT 代理抓 blob，不能直接把 blob URL 丟進 iframe
    this.previewFile = await this.previewLoader.load(a.fileUrl, a.fileName);
  }
  closePreview() {
    this.previewLoader.revoke(this.previewFile);
    this.previewFile = null;
  }
}
