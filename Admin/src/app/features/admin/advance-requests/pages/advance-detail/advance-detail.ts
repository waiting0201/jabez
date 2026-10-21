import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {ToastrService} from 'ngx-toastr';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {AdvancePdfService} from '../../services/advance-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {
  AdvanceRequest, AdvanceRound, WriteOffRecord, roundLabel,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/advance-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {CategorySubtotalsComponent} from '../../../../../shared/components/category-subtotals';
import {ClosureInfoCardComponent} from '../../../../../shared/components/closure-info-card';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';

@Component({
  selector: 'app-advance-detail',
  templateUrl: './advance-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, InstallmentsTable, ClosureInfoCardComponent, FilePreviewModal, CategorySubtotalsComponent],
})
export class AdvanceDetail implements OnInit {
  private service = inject(AdvanceRequestService);
  private pdfService = inject(AdvancePdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);
  private sanitizer = inject(DomSanitizer);

  request = signal<AdvanceRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;
  readonly roundLabel = roundLabel;

  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  ngOnInit() {
    this.load(+this.route.snapshot.paramMap.get('id')!);
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printAdvance() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printAdvanceRequest(r, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
  }

  // ── 沖銷與繳回紀錄 ──────────────────────────────────────────────────────

  /** 沖銷與繳回只計已核准（與沖銷頁 write-off-summary 的結清對帳同基準）*/
  private approvedWriteOffs(r: AdvanceRequest): WriteOffRecord[] {
    return (r.writeOffRecords ?? []).filter(w => w.approvalStatus === 'approved');
  }

  approvedWrittenOffTotal(r: AdvanceRequest): number {
    return this.approvedWriteOffs(r).reduce((s, w) => s + w.grandTotal, 0);
  }

  approvedReturnedTotal(r: AdvanceRequest): number {
    return this.approvedWriteOffs(r).reduce((s, w) => s + (w.returnedAmount ?? 0), 0);
  }

  /** 尚未結清＝預支總額 − 已核准費用核銷 − 已核准同仁繳回 */
  unsettled(r: AdvanceRequest): number {
    return r.grandTotal - this.approvedWrittenOffTotal(r) - this.approvedReturnedTotal(r);
  }

  /** 沖銷大於預支（尚未結清為負）且公司已補撥足額 → 已結清（與 write-off-summary 同判準）*/
  overspendSettled(r: AdvanceRequest): boolean {
    const u = this.unsettled(r);
    return u < 0 && (r.overspendPaidTotal ?? 0) >= -u;
  }

  // ── 追加預支 ──────────────────────────────────────────────────────────────

  /** 各預支批次（後端未回傳時不顯示批次清單，退回單一預支日期）*/
  rounds(r: AdvanceRequest): AdvanceRound[] | null {
    return r.rounds?.length ? r.rounds : null;
  }

  roundDate(r: AdvanceRequest, roundNo: number): string | null {
    return r.rounds?.find(x => x.roundNo === roundNo)?.advanceDate ?? null;
  }

  /** 明細列是否為該批次第一列（同批次第二列起批次欄留白）*/
  isFirstOfRound(r: AdvanceRequest, index: number): boolean {
    return index === 0 || r.items[index - 1].roundNo !== r.items[index].roundNo;
  }

  /** 追加簽核中：總額變動中，此期間暫停沖銷 */
  isSupplementInFlight(r: AdvanceRequest): boolean {
    return r.currentRoundNo > 1 && (r.approvalStatus === 'pending' || r.approvalStatus === 'returned');
  }

  canAddSupplement(r: AdvanceRequest): boolean {
    return r.approvalStatus === 'approved' && !r.isClosed;
  }

  cancelSupplement(r: AdvanceRequest) {
    if (!confirm(`確定要取消${roundLabel(r.currentRoundNo)}預支？該批次明細會一併刪除，原預支申請還原為已核准。`)) return;
    this.deleting.set(true);
    this.service.deleteSupplement(r.id, r.currentRoundNo).subscribe({
      next: () => {
        this.toastr.success('追加預支已取消。');
        this.deleting.set(false);
        this.load(r.id);
      },
      error: () => this.deleting.set(false),
    });
  }

  private load(id: number) {
    this.service.getById(id).subscribe(r => this.request.set(r));
    this.taskService.getById(id, 'advance').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // 可能尚無簽核記錄（draft 狀態）
    });
  }
}
