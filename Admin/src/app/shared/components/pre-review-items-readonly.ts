import {ChangeDetectionStrategy, Component, inject, input, signal} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {PreReviewItem} from '../../features/admin/pre-review-requests/models/pre-review-request.model';
import {FilePreviewModal, PreviewFileData} from './file-preview-modal';
import {FilePreviewLoader} from '../services/file-preview-loader';

/**
 * 預審單報價品項唯讀表（品項 + 未稅小計 / 稅金 / 含稅總計）。
 * 預審詳情頁、簽核頁（pre_review）、預支關聯預審浮動視窗共用；只渲染 table，外層卡片由父層決定。
 * 報價單存於私有容器 quotes，預覽走 FilePreviewLoader（JWT 代理）。
 * ⚠ totalAmount 是**未稅小計**，含稅 = totalAmount + (taxAmount ?? 0)。
 */
@Component({
  selector: 'app-pre-review-items-readonly',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, FilePreviewModal],
  template: `
    <div class="table-responsive">
      <table class="table table-sm mb-0">
        <thead class="table-light">
          <tr>
            <th style="width:40px"></th>
            <th>品項類別</th>
            <th>日期</th>
            <th>項目</th>
            <th>說明</th>
            <th class="text-right">金額</th>
            <th>備註</th>
          </tr>
        </thead>
        <tbody>
          @for (item of items(); track item.id) {
            <tr>
              <td class="align-middle text-center">
                @if (item.fileUrl) {
                  <button type="button" class="btn btn-sm btn-ghost-secondary p-1"
                          (click)="openPreview(item.fileName || '報價單', item.fileUrl!)"
                          [title]="item.fileName">
                    <svg class="sa-icon" style="stroke:currentColor"><use href="/assets/icons/sprite.svg#file-text"></use></svg>
                  </button>
                }
              </td>
              <td class="small">{{ item.itemCategory || '—' }}</td>
              <td class="small">{{ item.itemDate ? (item.itemDate | date:'yyyy-MM-dd') : '—' }}</td>
              <td class="small">{{ item.itemName || '—' }}</td>
              <td class="small">{{ item.description || '—' }}</td>
              <td class="text-right small fw-500">{{ item.amount | number:'1.0-0' }}</td>
              <td class="small text-muted">{{ item.note || '—' }}</td>
            </tr>
          }
        </tbody>
        <tfoot>
          <tr class="table-light">
            <td colspan="5" class="text-right fw-500 small">未稅小計</td>
            <td class="text-right fw-600">{{ totalAmount() | number:'1.0-0' }}</td>
            <td></td>
          </tr>
          <tr class="table-light">
            <td colspan="5" class="text-right fw-500 small">稅金</td>
            <td class="text-right fw-500">{{ (taxAmount() ?? 0) | number:'1.0-0' }}</td>
            <td></td>
          </tr>
          <tr class="table-light">
            <td colspan="5" class="text-right fw-600 small">含稅總計</td>
            <td class="text-right fw-700 text-[--forest]">{{ (totalAmount() + (taxAmount() ?? 0)) | number:'1.0-0' }}</td>
            <td></td>
          </tr>
        </tfoot>
      </table>
    </div>

    @if (previewFile(); as pf) {
      <app-file-preview-modal [file]="pf" (closed)="closePreview()" />
    }
  `,
})
export class PreReviewItemsReadonly {
  items       = input<PreReviewItem[]>([]);
  /** 未稅小計 */
  totalAmount = input(0);
  taxAmount   = input<number | null | undefined>(0);

  private previewLoader = inject(FilePreviewLoader);

  previewFile = signal<PreviewFileData | null>(null);
  async openPreview(name: string, url: string) {
    if (!url) return;
    this.previewFile.set(await this.previewLoader.load(url, name));
  }
  closePreview() {
    this.previewLoader.revoke(this.previewFile());
    this.previewFile.set(null);
  }
}
