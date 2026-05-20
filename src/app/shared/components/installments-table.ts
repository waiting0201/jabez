import {Component, Input, computed, signal} from '@angular/core';
import {DatePipe, DecimalPipe, NgClass} from '@angular/common';
import {
  InstallmentDto,
  PaymentInstallmentStatus,
  PAYMENT_INSTALLMENT_STATUS_LABELS,
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 共用 read-only 分期撥款明細表（4 種申請類型通用）。
 * 用於申請表單頁、詳情頁 — 申請人或非財務角色都看得到自己/他人的撥款進度。
 * 編輯版本（含 FormArray）在 approval-task-review 內部實作。
 */
@Component({
  selector: 'app-installments-table',
  imports: [DatePipe, DecimalPipe, NgClass],
  template: `
    @if (installments() && installments()!.length > 0) {
      <div class="border rounded overflow-hidden bg-white">
        <div class="px-4 py-3 bg-[--bg-base] flex items-center justify-between flex-wrap gap-2 border-bottom">
          <div class="fw-500">
            撥款明細
            @if (paymentStatus) {
              <span class="badge ml-2" [ngClass]="statusClass">{{ statusLabel }}</span>
            }
            <span class="text-muted small fw-400 ml-2">已撥 {{ paidCount() }}/{{ installments()!.length }} 期</span>
          </div>
          @if (totalAmount != null) {
            <div class="text-muted small">
              撥款總額：<span class="fw-500 text-success">{{ paidSum() | number:'1.0-2' }}</span> /
              <span class="fw-500">{{ totalAmount | number:'1.0-2' }}</span> 元
            </div>
          }
        </div>
        <table class="table table-sm mb-0 small">
          <thead class="bg-[--bg-base]">
            <tr>
              <th class="text-center" style="width: 60px">期數</th>
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
                <td class="text-center align-middle fw-500">{{ ins.installmentNo }}</td>
                <td>{{ ins.expectedDate | date:'yyyy-MM-dd' }}</td>
                <td>
                  @if (ins.paidAt) {
                    {{ ins.paidAt | date:'yyyy-MM-dd' }}
                  } @else {
                    <span class="text-muted">—</span>
                  }
                </td>
                <td class="text-right">{{ ins.amount | number:'1.0-2' }}</td>
                <td class="text-muted">{{ ins.note || '—' }}</td>
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

  get statusLabel(): string {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_LABELS[this.paymentStatus] : '';
  }
  get statusClass(): string {
    return this.paymentStatus ? PAYMENT_INSTALLMENT_STATUS_CLASSES[this.paymentStatus] : '';
  }
  paidCount = computed(() => (this._installments() ?? []).filter(i => !!i.paidAt).length);
  paidSum = computed(() => (this._installments() ?? []).filter(i => !!i.paidAt).reduce((s, i) => s + (i.amount || 0), 0));
}
