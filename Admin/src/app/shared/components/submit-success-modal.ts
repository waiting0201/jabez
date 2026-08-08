import {Component, inject, Input} from '@angular/core';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {APPLICATION_FORM_NAMES, PaperFormApplicationType} from '../../features/admin/approvals/models/approval.model';

/**
 * 共用「申請成功」彈窗 — 8 個申請表單頁送出後、6 個詳情頁草稿送出後一律開此元件，
 * 不得再各頁內嵌 <ng-template #successModal>。
 *
 * - formType：走「印出紙本寄回會計室」流程的 7 種財務單，文案自動帶入單別名稱
 *   （單別名稱單一真相 = APPLICATION_FORM_NAMES）
 * - message：不走紙本流程者自訂訊息（目前僅預審申請）
 *
 * 呼叫端以 NgbModal.open(SubmitSuccessModal, {...}) 開啟，
 * 表單頁接 ref.result 於關閉後導回列表；詳情頁不接、關閉後留在原頁。
 */
@Component({
  selector: 'app-submit-success-modal',
  template: `
    <div class="modal-header border-0 pb-0">
      <button type="button" class="btn-close" (click)="activeModal.close()"></button>
    </div>
    <div class="modal-body text-center py-6">
      <svg class="sa-icon sa-icon-3x text-success mb-4" style="stroke: currentColor">
        <use href="/assets/icons/sprite.svg#check-circle"></use>
      </svg>
      <h5 class="fw-600 mb-2">申請成功</h5>
      <p class="text-secondary mb-0">{{ text }}</p>
    </div>
    <div class="modal-footer border-0 justify-center pt-0">
      <button type="button" class="btn btn-primary px-6" (click)="activeModal.close()">確定</button>
    </div>
  `,
})
export class SubmitSuccessModal {
  activeModal = inject(NgbActiveModal);

  /** 單別：走「印出紙本寄回會計室」流程的 7 種財務單 */
  @Input() formType?: PaperFormApplicationType;
  /** 自訂訊息（不走紙本流程者，例：預審） */
  @Input() message?: string;

  get text(): string {
    if (this.message) return this.message;
    const name = this.formType ? APPLICATION_FORM_NAMES[this.formType] : '申請單';
    return `請於單位主管簽核完畢後，再印出${name}連同紙本單據寄回會計室進行行政財務流程。`;
  }
}
