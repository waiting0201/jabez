import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {PaymentRequestService} from '../../services/payment-request.service';
import {
  PaymentRequest,
  PAYMENT_TYPE_LABELS, PAYMENT_TYPE_CLASSES,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/payment-request.model';

@Component({
  selector: 'app-payment-list',
  templateUrl: './payment-list.html',
  imports: [RouterLink, AsyncPipe, DecimalPipe, DatePipe],
})
export class PaymentList {
  private service = inject(PaymentRequestService);
  requests$ = this.service.getAll();

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
