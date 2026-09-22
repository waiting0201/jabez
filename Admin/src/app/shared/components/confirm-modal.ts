import {Component, Input, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';

/** 確認對話框的結果。`reason` 只在有開原因欄位時有值。 */
export interface ConfirmModalResult {
  confirmed: boolean;
  reason: string | null;
}

/**
 * 共用確認對話框。
 *
 * 專案原本**沒有**共用 confirm 元件，20+ 處都用原生 `window.confirm`（無法附加輸入欄位、
 * 樣式也跟系統不一致）。四週彈性工時的下班打卡需要「確認 ＋ 視情況附一個必填原因」，
 * 規格明訂**不另開第二個視窗**，故在此建立可重用的版本。
 *
 * 用法：
 * ```ts
 * const ref = this.modal.open(ConfirmModal, {centered: true});
 * ref.componentInstance.title = '確認下班打卡';
 * ref.componentInstance.message = '目前出勤 8 小時 30 分';
 * ref.componentInstance.reasonLabel = '早退原因';
 * ref.componentInstance.reasonRequired = true;
 * const result: ConfirmModalResult = await ref.result;   // 取消時 reject，需 catch
 * ```
 */
@Component({
  selector: 'app-confirm-modal',
  imports: [FormsModule],
  template: `
    <div class="modal-header">
      <h5 class="modal-title flex items-center gap-2">
        @if (tone === 'warning') {
          <svg class="sa-icon" style="stroke: currentColor; color: var(--yellow)">
            <use href="/assets/icons/sprite.svg#alert-triangle"></use>
          </svg>
        }
        {{ title }}
      </h5>
      <button type="button" class="btn-close" (click)="cancel()"></button>
    </div>

    <div class="modal-body">
      @if (message) { <p class="mb-2">{{ message }}</p> }
      @if (detail) { <p class="text-secondary small mb-3">{{ detail }}</p> }

      @if (reasonLabel) {
        <label class="form-label">
          {{ reasonLabel }}
          @if (reasonRequired) { <span style="color: var(--red)">*</span> }
          @else { <span class="text-secondary small">（選填）</span> }
        </label>
        <textarea class="form-control" rows="3" maxlength="500"
                  [ngModel]="reason()" (ngModelChange)="onReasonChange($event)"
                  [placeholder]="reasonPlaceholder"></textarea>
        @if (showError()) {
          <div class="small mt-1" style="color: var(--red)">請填寫{{ reasonLabel }}。</div>
        }
      }
    </div>

    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="cancel()">{{ cancelText }}</button>
      <button type="button" class="btn btn-primary" (click)="confirm()">{{ confirmText }}</button>
    </div>
  `,
})
export class ConfirmModal {
  activeModal = inject(NgbActiveModal);

  @Input() title = '請確認';
  @Input() message = '';
  @Input() detail = '';
  @Input() confirmText = '確定';
  @Input() cancelText = '取消';
  @Input() tone: 'default' | 'warning' = 'default';

  /** 有值才顯示原因欄位 */
  @Input() reasonLabel = '';
  @Input() reasonRequired = false;
  @Input() reasonPlaceholder = '';

  reason = signal('');
  showError = signal(false);

  /** 一開始輸入就把「請填寫…」的錯誤收掉，不要讓它一直掛在下面 */
  onReasonChange(value: string): void {
    this.reason.set(value);
    if (value.trim()) this.showError.set(false);
  }

  confirm(): void {
    if (this.reasonLabel && this.reasonRequired && !this.reason().trim()) {
      this.showError.set(true);
      return;
    }
    const result: ConfirmModalResult = {
      confirmed: true,
      reason: this.reason().trim() || null,
    };
    this.activeModal.close(result);
  }

  cancel(): void {
    // 以 dismiss 收尾：呼叫端用 .then/.catch 或 await + try 區分「確定」與「取消」
    this.activeModal.dismiss();
  }
}
