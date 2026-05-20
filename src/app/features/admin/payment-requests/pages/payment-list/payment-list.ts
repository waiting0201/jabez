import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {PaymentRequestService} from '../../services/payment-request.service';
import {
  PaymentRequest, PaymentState,
  PAYMENT_TYPE_LABELS, PAYMENT_TYPE_CLASSES,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  PAYMENT_STATE_LABELS, PAYMENT_STATE_CLASSES,
} from '../../models/payment-request.model';
import {
  PAYMENT_INSTALLMENT_STATUS_LABELS,
  PAYMENT_INSTALLMENT_STATUS_CLASSES,
  PaymentInstallmentStatus,
} from '../../../approval-tasks/models/approval-task.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {AuthService} from '@core/auth/services/auth.service';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-payment-list',
  templateUrl: './payment-list.html',
  imports: [RouterLink, DecimalPipe, DatePipe, HasPermissionDirective],
})
export class PaymentList {
  private service = inject(PaymentRequestService);
  private auth = inject(AuthService);

  canWrite()  { return this.auth.hasPermission('payment-requests:write'); }
  canDelete() { return this.auth.hasPermission('payment-requests:delete'); }

  readonly PAGE_SIZE = 20;
  page = signal(1);
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({ page: this.page(), refresh: this.refresh() }))).pipe(
      switchMap(({ page }) => this.service.getPaged(page, this.PAGE_SIZE))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<PaymentRequest>}
  );

  pagedRequests = computed(() => this.result().items);
  totalCount    = computed(() => this.result().totalCount);
  totalPages    = computed(() => this.result().totalPages);
  pageNumbers   = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  readonly typeLabel    = PAYMENT_TYPE_LABELS;
  readonly typeClass    = PAYMENT_TYPE_CLASSES;
  readonly statusLabel  = APPROVAL_STATUS_LABELS;
  readonly statusClass  = APPROVAL_STATUS_CLASSES;
  readonly paymentStateLabel = PAYMENT_STATE_LABELS;
  readonly paymentStateClass = PAYMENT_STATE_CLASSES;
  readonly installmentStatusLabel = PAYMENT_INSTALLMENT_STATUS_LABELS;
  readonly installmentStatusClass = PAYMENT_INSTALLMENT_STATUS_CLASSES;

  paymentState(r: PaymentRequest): PaymentState | null {
    if (r.approvalStatus !== 'pending' && r.approvalStatus !== 'approved') return null;
    return r.paidAt ? 'paid' : 'unpaid';
  }

  /** 分期撥款三態 badge（核准後才顯示，與 paymentState 二態並存供過渡期參考）*/
  installmentStatusOf(r: PaymentRequest): PaymentInstallmentStatus | null {
    if (r.approvalStatus !== 'approved') return null;
    return r.paymentStatus ?? null;
  }

  delete(r: PaymentRequest) {
    if (confirm(`確定要刪除此請款申請嗎？`)) {
      this.service.delete(r.id).subscribe(() => this.refresh.update(v => v + 1));
    }
  }
}

function buildPageNumbers(current: number, total: number): number[] {
  if (total <= 9) return Array.from({length: total}, (_, i) => i + 1);
  const pages: number[] = [];
  let prev = 0;
  for (let i = 1; i <= total; i++) {
    if (i === 1 || i === total || (i >= current - 2 && i <= current + 2)) {
      if (prev && i - prev > 1) pages.push(-1);
      pages.push(i);
      prev = i;
    }
  }
  return pages;
}
