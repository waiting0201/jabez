import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {PaymentRequestService} from '../../services/payment-request.service';
import {
  PaymentRequest,
  PAYMENT_TYPE_LABELS, PAYMENT_TYPE_CLASSES,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/payment-request.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';

@Component({
  selector: 'app-payment-list',
  templateUrl: './payment-list.html',
  imports: [RouterLink, DecimalPipe, DatePipe],
})
export class PaymentList {
  private service = inject(PaymentRequestService);

  readonly PAGE_SIZE = 20;
  page = signal(1);

  private result = toSignal(
    toObservable(this.page).pipe(
      switchMap(p => this.service.getPaged(p, this.PAGE_SIZE))
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

  delete(r: PaymentRequest) {
    if (confirm(`確定要刪除此請款申請嗎？`)) {
      this.service.delete(r.id).subscribe();
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
