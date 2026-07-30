import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {ToastrService} from 'ngx-toastr';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {AdvancePdfService} from '../../services/advance-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {
  AdvanceRequest, AdvanceRound, roundLabel,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/advance-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {ClosureInfoCardComponent} from '../../../../../shared/components/closure-info-card';

@Component({
  selector: 'app-advance-detail',
  templateUrl: './advance-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, InstallmentsTable, ClosureInfoCardComponent],
})
export class AdvanceDetail implements OnInit {
  private service = inject(AdvanceRequestService);
  private pdfService = inject(AdvancePdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

  request = signal<AdvanceRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;
  readonly roundLabel = roundLabel;

  ngOnInit() {
    this.load(+this.route.snapshot.paramMap.get('id')!);
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printAdvance() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printAdvanceRequest(r, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl);
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
