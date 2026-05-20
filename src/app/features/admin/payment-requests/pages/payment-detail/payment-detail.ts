import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {PaymentRequestService} from '../../services/payment-request.service';
import {PaymentPdfService} from '../../services/payment-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {PaymentRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, PAYMENT_TYPE_LABELS, PAYMENT_TYPE_CLASSES} from '../../models/payment-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';

@Component({
  selector: 'app-payment-detail',
  templateUrl: './payment-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable],
})
export class PaymentDetail implements OnInit {
  private service = inject(PaymentRequestService);
  private pdfService = inject(PaymentPdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);

  request = signal<PaymentRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting = signal(false);

  /** 檔案預覽 modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    if (!url) return;
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;
  readonly typeLabel = PAYMENT_TYPE_LABELS;
  readonly typeClass = PAYMENT_TYPE_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
    });
    this.taskService.getById(id, 'payment_request').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // draft 狀態可能尚無簽核記錄
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printRequest() {
    const t = this.approvalTask();
    if (t) this.pdfService.printPaymentRequest(t);
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
    if (!r || !confirm('確定要刪除此請款申請？')) return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(['/admin/payment-requests']),
      error: () => this.deleting.set(false),
    });
  }
}
