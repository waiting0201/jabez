import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {AdvancePdfService} from '../../services/advance-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {AdvanceRequest, WriteOffRecord, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/advance-request.model';

@Component({
  selector: 'app-advance-detail',
  templateUrl: './advance-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, FilePreviewModal],
})
export class AdvanceDetail implements OnInit {
  private service = inject(AdvanceRequestService);
  private pdfService = inject(AdvancePdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);

  /** File preview modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  request = signal<AdvanceRequest | null>(null);
  writeOffs = signal<WriteOffRecord[]>([]);
  approvalTask = signal<ApprovalTask | null>(null);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  get canWriteOff(): boolean {
    const r = this.request();
    return !!r && r.approvalStatus === 'approved' && !!r.paidAt;
  }

  get writeOffTotal(): number {
    return this.writeOffs().reduce((s, w) => s + w.grandTotal, 0);
  }

  get balance(): number {
    const r = this.request();
    return (r?.grandTotal ?? 0) - this.writeOffTotal;
  }

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.loadData(id);
  }

  private loadData(id: number) {
    this.service.getById(id).subscribe(r => this.request.set(r));
    this.service.getWriteOffs(id).subscribe(wos => this.writeOffs.set(wos));
    this.taskService.getById(id, 'advance').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // 可能尚無簽核記錄（draft 狀態）
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printAdvance() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printAdvanceRequest(r, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
  }

  printWriteOff(wo: WriteOffRecord) {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printWriteOff(r, wo, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
  }

  deleteWriteOff(wo: WriteOffRecord) {
    const r = this.request();
    if (!r) return;
    if (confirm(`確定要刪除第 ${wo.writeOffNo} 次沖銷嗎？`)) {
      this.service.deleteWriteOff(r.id, wo.id).subscribe(() => this.loadData(r.id));
    }
  }
}
