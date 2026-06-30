import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {PreReviewRequestService} from '../../services/pre-review-request.service';
import {PreReviewPdfService} from '../../services/pre-review-pdf.service';
import {FilePreviewLoader} from '../../../../../shared/services/file-preview-loader';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {PreReviewRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, PAYMENT_TYPE_LABELS, PAYMENT_TYPE_CLASSES} from '../../models/pre-review-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AttachmentsList} from '../../../../../shared/components/attachments-list';

@Component({
  selector: 'app-pre-review-detail',
  templateUrl: './pre-review-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, AttachmentsList],
})
export class PreReviewDetail implements OnInit {
  private service     = inject(PreReviewRequestService);
  private pdfService  = inject(PreReviewPdfService);
  private taskService = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private previewLoader = inject(FilePreviewLoader);

  request      = signal<PreReviewRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting     = signal(false);

  /** 檔案預覽 modal */
  previewFile: PreviewFileData | null = null;
  async openPreview(name: string, url: string) {
    if (!url) return;
    // 報價單存於私有容器 quotes，需透過 JWT 代理抓 blob，不能直接把 blob URL 丟進 iframe
    this.previewFile = await this.previewLoader.load(url, name);
  }
  closePreview() {
    this.previewLoader.revoke(this.previewFile);
    this.previewFile = null;
  }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;
  readonly typeLabel   = PAYMENT_TYPE_LABELS;
  readonly typeClass   = PAYMENT_TYPE_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
    });
    this.taskService.getById(id, 'pre_review').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // draft 狀態可能尚無簽核記錄
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printRequest() {
    const t = this.approvalTask();
    if (t) this.pdfService.printPreReviewRequest(t);
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
    if (!r || !confirm('確定要刪除此預審申請？')) return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(['/admin/pre-review-requests']),
      error: () => this.deleting.set(false),
    });
  }
}
