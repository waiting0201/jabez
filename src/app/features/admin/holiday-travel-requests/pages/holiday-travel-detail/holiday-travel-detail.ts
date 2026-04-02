import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HolidayTravelRequestService} from '../../services/holiday-travel-request.service';
import {HolidayTravelPdfService} from '../../services/holiday-travel-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {HolidayTravelRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/holiday-travel-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';

@Component({
  selector: 'app-holiday-travel-detail',
  templateUrl: './holiday-travel-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal],
})
export class HolidayTravelDetail implements OnInit {
  private service     = inject(HolidayTravelRequestService);
  private pdfService  = inject(HolidayTravelPdfService);
  private taskService = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private sanitizer   = inject(DomSanitizer);

  request      = signal<HolidayTravelRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting     = signal(false);

  /** 檔案預覽 modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
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
    this.taskService.getById(id, 'holiday_travel').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // draft 狀態可能尚無簽核記錄
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printHolidayTravel() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) {
      this.pdfService.printHolidayTravelRequest(
        r,
        t?.submittedBy ?? '',
        t?.approvalRecords ?? [],
        t?.flow,
        t?.submittedBySignatureUrl,
        undefined,
        r.paidAt,
        t?.travelDetail?.paidBySignatureUrl,
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
    if (!r || !confirm('確定要刪除此假日執行活動申請？')) return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(['/admin/holiday-travel-requests']),
      error: () => this.deleting.set(false),
    });
  }
}
