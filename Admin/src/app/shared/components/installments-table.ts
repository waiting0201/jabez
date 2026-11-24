import {Component, Input, computed, signal} from '@angular/core';
import {DatePipe, DecimalPipe, NgClass} from '@angular/common';
import {
  InstallmentDto,
  PaymentInstallmentStatus,
  PAYMENT_INSTALLMENT_STATUS_LABELS,
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
  INSTALLMENT_PAYMENT_METHOD_LABELS,
} from '../../features/admin/approval-tasks/models/approval-task.model';
import {summarizeByPaymentMethod} from '../utils/installment-payment-method';

/**
 * 共用 read-only 撥款明細表（4 種申請類型通用）。
 * 用於申請表單頁、詳情頁 — 申請人或非財務角色都看得到自己/他人的撥款進度。
 * 編輯版本（含 FormArray）在 approval-task-review 內部實作。
 * 樣式與其他 detail 卡片一致：card border-0 shadow-sm + card-header + card-body p-0。
 */
@Component({
  selector: 'app-installments-table',
  imports: [DatePipe, DecimalPipe, NgClass],
  template: `
    @if (installments() && installments()!.length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center justify-between gap-2 fw-600 flex-wrap">
          <div class="flex items-center gap-2 flex-wrap">
            <svg class="sa-icon text-primary" style="stroke: currentColor">
              <use href="/assets/icons/sprite.svg#credit-card"></use>
            </svg>
            撥款明細
            @if (paymentStatus) {
              <span class="badge" [ngClass]="statusClass">{{ statusLabel }}</span>
            }
            <span class="text-muted small fw-400">已撥 {{ paidCount() }} / {{ installments()!.length }} 期</span>
          </div>
          @if (totalAmount != null) {
            <div class="text-muted small fw-400">
              撥款總額：<span class="fw-500 text-success">{{ paidSum() | number:'1.0-2' }}</span> /
              <span class="fw-500">{{ totalAmount | number:'1.0-2' }}</span> 元
            </div>
          }
        </div>
        <div class="card-body p-0">
          @if (pmSummary(); as pm) {
            <!-- 預支單：現金 / 月結對照（2026-11-24）-->
            <div class="small px-4 py-2 border-bottom flex flex-wrap gap-x-4 gap-y-1">
              <span><span class="text-muted">現金</span> {{ pm.cashTotal | number:'1.0-2' }} 元（已撥 {{ pm.cashPaid | number:'1.0-2' }}）</span>
              <span><span class="text-muted">月結</span> {{ pm.checkTotal | number:'1.0-2' }} 元（已撥 {{ pm.checkPaid | number:'1.0-2' }}）</span>
              @if (pm.unclassified > 0) {
                <span class="text-[--yellow]">未分類 {{ pm.unclassified | number:'1.0-2' }} 元</span>
              }
            </div>
          }
          <div class="table-responsive">
            <table class="table table-sm mb-0" style="min-width: 720px">
              <thead class="table-light">
                <tr>
                  <th class="text-center" style="width: 60px">期數</th>
                  @if (checkTotal != null) { <th class="text-center" style="width: 80px">款別</th> }
                  <th style="width: 140px">預計撥款日</th>
                  <th style="width: 140px">實際撥款日</th>
                  <th class="text-right" style="width: 140px">金額</th>
                  <th>備註</th>
                  <th class="text-center" style="width: 80px">狀態</th>
                </tr>
              </thead>
              <tbody>
                @for (ins of installments(); track ins.id) {
                  <tr [class.bg-[--bg-base]]="ins.paidAt">
                    <td class="text-center align-middle fw-500 small">{{ ins.installmentNo }}</td>
                    @if (checkTotal != null) {
                      <td class="text-center small">
                        @if (ins.paymentMethod) {
                          {{ paymentMethodLabels[ins.paymentMethod] }}
                        } @else {
                          <span class="text-muted">未分類</span>
                        }
                      </td>
                    }
                    <td class="small">{{ ins.expectedDate | date:'yyyy-MM-dd' }}</td>
                    <td class="small">
                      @if (ins.paidAt) {
                        {{ ins.paidAt | date:'yyyy-MM-dd' }}
                      } @else {
                        <span class="text-muted">—</span>
                      }
                    </td>
                    <td class="text-right small fw-500">{{ ins.amount | number:'1.0-2' }}</td>
                    <td class="small text-muted">{{ ins.note || '—' }}</td>
                    <td class="text-center">
                      @if (ins.paidAt) {
                        <span class="badge bg-success">已撥</span>
                      } @else {
                        <span class="badge bg-secondary">未撥</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `,
})
export class InstallmentsTable {
  private _installments = signal<InstallmentDto[] | undefined>(undefined);
  @Input({required: true}) set installmentsInput(v: InstallmentDto[] | undefined) {
    this._installments.set(v);
  }
  installments = computed(() => this._installments());

  @Input() paymentStatus?: PaymentInstallmentStatus;
  /** 申請總額（用於顯示已撥/總額對照）*/
  @Input() totalAmount?: number;
  /** 預支單的月結（支票）合計；有傳（含 0）才顯示款別欄與現金 / 月結對照，其他類型不傳 */
  @Input() checkTotal?: number | null;

  protected readonly paymentMethodLabels = INSTALLMENT_PAYMENT_METHOD_LABELS;

  get statusLabel(): string {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_LABELS[this.paymentStatus] : '';
  }
  get statusClass(): string {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_CLASSES[this.paymentStatus] : '';
  }
  paidCount = computed(() => (this._installments() ?? []).filter(i => !!i.paidAt).length);
  /** checkTotal 為一般 @Input（非 signal），故以 getter 計算 */
  pmSummary() {
    if (this.checkTotal == null || this.totalAmount == null) return null;
    return summarizeByPaymentMethod(
      (this._installments() ?? []).map(i => ({amount: i.amount, paymentMethod: i.paymentMethod, paid: !!i.paidAt})),
      this.totalAmount, this.checkTotal);
  }
  paidSum = computed(() => (this._installments() ?? []).filter(i => !!i.paidAt).reduce((s, i) => s + (i.amount || 0), 0));
}
