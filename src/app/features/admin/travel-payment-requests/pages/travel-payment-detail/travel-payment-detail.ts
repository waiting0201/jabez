import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {TravelPaymentRequestService} from '../../services/travel-payment-request.service';
import {TravelPaymentPdfService} from '../../services/travel-payment-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {TravelPaymentRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/travel-payment-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';

@Component({
  selector: 'app-travel-payment-detail',
  templateUrl: './travel-payment-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline],
})
export class TravelPaymentDetail implements OnInit {
  private service = inject(TravelPaymentRequestService);
  private pdfService = inject(TravelPaymentPdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  request = signal<TravelPaymentRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
    });
    this.taskService.getById(id, 'travel_payment').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // draft 狀態可能尚無簽核記錄
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printRequest() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) {
      this.pdfService.printTravelPaymentRequest(
        r,
        t?.submittedBy ?? '',
        t?.approvalRecords ?? [],
        t?.flow,
        t?.submittedBySignatureUrl,
        undefined,
        r.paidAt,
        t?.travelPaymentDetail?.paidBySignatureUrl,
      );
    }
  }

  submitRequest() {
    const r = this.request();
    if (!r) return;
    this.service.submit(r.id).subscribe(updated => {
      this.request.set(updated);
    });
  }

  deleteRequest() {
    const r = this.request();
    if (!r || !confirm('確定要刪除此出差請款申請？')) return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(['/admin/travel-payment-requests']),
      error: () => this.deleting.set(false),
    });
  }
}
