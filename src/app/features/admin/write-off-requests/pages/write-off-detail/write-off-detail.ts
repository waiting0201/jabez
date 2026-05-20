import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {WriteOffRequestService} from '../../services/write-off-request.service';
import {WriteOffPdfService} from '../../services/write-off-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {WriteOffRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/write-off-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';

@Component({
  selector: 'app-write-off-request-detail',
  templateUrl: './write-off-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, FilePreviewModal, ApprovalTimeline],
})
export class WriteOffRequestDetail implements OnInit {
  private service     = inject(WriteOffRequestService);
  private pdfService  = inject(WriteOffPdfService);
  private taskService = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private sanitizer   = inject(DomSanitizer);

  /** File preview modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  request      = signal<WriteOffRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  submitting   = signal(false);
  errorMsg     = signal('');

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.loadData(id);
  }

  private loadData(id: number) {
    this.service.getById(id).subscribe(r => this.request.set(r));
    this.taskService.getById(id, 'write_off').subscribe({
      next: t  => this.approvalTask.set(t),
      error: () => {}, // 草稿狀態尚無簽核任務
    });
  }

  /** 送出申請（draft → pending） */
  submit() {
    const r = this.request();
    if (!r) return;
    if (confirm('確定要送出預支沖銷申請嗎？送出後將進入簽核流程。')) {
      this.submitting.set(true);
      this.errorMsg.set('');
      this.service.submit(r.id).subscribe({
        next: updated => {
          this.request.set(updated);
          this.submitting.set(false);
          this.loadData(r.id);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMsg.set(err.error?.message || '送出失敗，請稍後再試。');
          this.submitting.set(false);
        },
      });
    }
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  /** 列印預支沖銷申請表 PDF */
  printWriteOff() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printWriteOff(r, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl, t?.writeOffDetail?.refundedAt, t?.writeOffDetail?.refundedBySignatureUrl);
  }

  /** 刪除申請（僅 draft） */
  delete() {
    const r = this.request();
    if (!r) return;
    if (confirm('確定要刪除此預支沖銷申請嗎？此操作無法復原。')) {
      this.service.delete(r.id).subscribe({
        next: () => this.router.navigate(['/admin/write-off-requests']),
        error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '刪除失敗。'),
      });
    }
  }
}
