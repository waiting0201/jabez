import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {TravelPaymentRequestService} from '../../services/travel-payment-request.service';
import {TravelPaymentPdfService} from '../../services/travel-payment-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {TravelPaymentRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/travel-payment-request.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';

@Component({
  selector: 'app-travel-payment-detail',
  templateUrl: './travel-payment-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable],
})
export class TravelPaymentDetail implements OnInit {
  private service = inject(TravelPaymentRequestService);
  private pdfService = inject(TravelPaymentPdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);
  private modal = inject(NgbModal);

  request = signal<TravelPaymentRequest | null>(null);
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
      );
    }
  }

  submitRequest() {
    const r = this.request();
    if (!r) return;
    this.service.submit(r.id).subscribe(updated => {
      this.request.set(updated);
      const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
      ref.componentInstance.formType = 'travel_payment';
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
