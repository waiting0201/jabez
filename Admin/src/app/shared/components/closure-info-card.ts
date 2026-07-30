import {Component, computed, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';

/**
 * 共用「結案資訊」卡片 — 預支 / 出差預支的結案與退款狀態。
 * 欄位順序固定：結案狀態 / 結案時間 / 應退還差額 / 實際退款金額 / 預計退款日 / 退款日。
 *
 * 已採用：advance-detail、travel-detail、approval-task-review（advance / travel 本單，
 * 以及 write_off 沖銷單所關聯的預支單）、write-off-detail。
 * 六個欄位全空時整張卡不渲染（沖銷單關聯的預支單尚未結案也未有差額時即為此情形）。
 */
@Component({
  selector: 'app-closure-info-card',
  imports: [DatePipe, DecimalPipe],
  template: `
    @if (hasAny()) {
      <div class="card border-0 shadow-sm {{ cardClass() }}">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-muted" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#check-circle"></use>
          </svg>
          {{ title() }}
        </div>
        <div class="card-body">
          <div class="row g-3">

            <!-- 結案狀態 -->
            @if (isClosed()) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">結案狀態</div>
                <div class="fw-500">
                  <span class="badge bg-elevated text-secondary">已結案</span>
                </div>
              </div>
              @if (closedAt()) {
                <div class="col-6 col-md-3">
                  <div class="text-muted small">結案時間</div>
                  <div class="fw-500">{{ closedAt() | date:'yyyy-MM-dd' }}</div>
                </div>
              }
            } @else if (alwaysShow()) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">結案狀態</div>
                <div class="fw-500">
                  <span class="badge bg-warning-subtle text-warning-emphasis">未結案</span>
                </div>
              </div>
            }

            <!-- 差額退款 -->
            @if (showRefund() && refundAmount() != null && refundAmount()! > 0) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">應退還差額</div>
                <div class="fw-600 text-lg" style="color: var(--red)">
                  \${{ refundAmount() | number:'1.0-0' }}
                </div>
              </div>
            }
            @if (showRefund() && refundedAmount() != null) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">實際退款金額</div>
                <div class="fw-600 text-lg">\${{ refundedAmount() | number:'1.0-0' }}</div>
              </div>
            }

            @if (showRefund() && estimatedRefundDate()) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">預計退款日</div>
                <div class="fw-500">{{ estimatedRefundDate() | date:'yyyy-MM-dd' }}</div>
              </div>
            }

            @if (showRefund() && refundedAt()) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">退款日</div>
                <div class="flex items-center gap-2">
                  <svg class="sa-icon" style="color: var(--green); stroke: currentColor; width: 16px; height: 16px">
                    <use href="/assets/icons/sprite.svg#check-circle"></use>
                  </svg>
                  <span class="fw-500" style="color: var(--green)">{{ refundedAt() | date:'yyyy-MM-dd' }}</span>
                </div>
              </div>
            } @else if (showRefund() && refundAmount() != null && refundAmount()! > 0) {
              <div class="col-6 col-md-3">
                <div class="text-muted small">退款日</div>
                <span class="text-muted small">尚未退款</span>
              </div>
            }

          </div>
        </div>
      </div>
    }
  `,
})
export class ClosureInfoCardComponent {
  /** 允許 undefined：各 model 的 isClosed 有 boolean 與 boolean? 兩種宣告 */
  isClosed            = input<boolean | undefined>(false);
  closedAt            = input<string | undefined>();
  /** 應退還差額（沖銷累計 > 預支 / 出差金額時系統自動計算）*/
  refundAmount        = input<number | undefined>();
  /** 實際退款金額（財務手動填入）*/
  refundedAmount      = input<number | undefined>();
  estimatedRefundDate = input<string | undefined>();
  refundedAt          = input<string | undefined>();

  /** 卡片標題；沖銷頁需標明是「預支單」的結案資訊 */
  title    = input('結案資訊');
  /** 卡片間距：detail 頁卡片自帶 mb-6，簽核頁卡片改用 mt-6 */
  cardClass = input('mb-6');
  /**
   * 是否呈現差額退款四欄（應退還差額 / 實際退款金額 / 預計退款日 / 退款日）。
   * 沖銷頁傳 false：同一組欄位在該頁已以「撥款」語彙呈現（差額撥款分期 + 已核准卡片），
   * 兩種標籤並存會造成語意混淆，故沖銷頁只呈現結案狀態本身。
   */
  showRefund = input(true);
  /** 未結案時也顯示卡片（呈現「未結案」badge）；沖銷頁需一律看得到關聯預支單的結案狀態 */
  alwaysShow = input(false);

  hasAny = computed(() =>
    this.alwaysShow()
    || !!this.isClosed()
    || (this.showRefund() && (
      (this.refundAmount() != null && this.refundAmount()! > 0)
      || this.refundedAmount() != null
      || !!this.estimatedRefundDate()
      || !!this.refundedAt())));
}
