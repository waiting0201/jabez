import {Component, input, output} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';

/**
 * 共用「同仁匯回預支餘額」唯讀區塊 — 沖銷詳情頁 / 彙總頁 / 簽核頁共用。
 * 無繳回（金額為 null 或 0）時整塊不顯示。證明檔點擊後由父層開 file-preview-modal。
 */
@Component({
  selector: 'app-write-off-returned-info',
  imports: [DatePipe, DecimalPipe],
  template: `
    @if (amount()) {
      <div class="row g-3 mt-2">
        <div class="col-12 text-muted small fw-500">同仁匯回預支餘額</div>
        <div class="col-6 col-md-3">
          <div class="text-muted small">繳回金額</div>
          <div class="fw-600">{{ amount() | number:'1.0-0' }}</div>
        </div>
        <div class="col-6 col-md-3">
          <div class="text-muted small">匯款／繳回日期</div>
          <div class="fw-500">{{ returnedAt() ? (returnedAt() | date:'yyyy-MM-dd') : '—' }}</div>
        </div>
        <div class="col-12 col-md-6">
          <div class="text-muted small">匯款證明</div>
          @if (proofUrl()) {
            <button type="button" class="btn btn-sm btn-outline-secondary inline-flex items-center gap-1"
                    (click)="preview.emit({name: proofName() || '匯款證明', url: proofUrl()!})">
              <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#eye"></use></svg>
              {{ proofName() || '匯款證明' }}
            </button>
          } @else {
            <span class="text-muted small">—</span>
          }
        </div>
      </div>
    }
  `,
})
export class WriteOffReturnedInfo {
  amount    = input<number | null | undefined>(null);
  returnedAt = input<string | null | undefined>(null);
  proofName = input<string | null | undefined>(null);
  proofUrl  = input<string | null | undefined>(null);
  preview   = output<{name: string; url: string}>();
}
