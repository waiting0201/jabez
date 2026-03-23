import {Component, inject, OnInit, signal, computed} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {AdvancePdfService} from '../../services/advance-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {AdvanceRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/advance-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {ToastrService} from 'ngx-toastr';

@Component({
  selector: 'app-advance-detail',
  templateUrl: './advance-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FormsModule],
})
export class AdvanceDetail implements OnInit {
  private service = inject(AdvanceRequestService);
  private pdfService = inject(AdvancePdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  private toastr = inject(ToastrService);

  request = signal<AdvanceRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);

  /** 財務部或 Superadmin 才能確認退款匯款 */
  canConfirmRefund = computed(() => this.auth.isSuperAdmin() || this.auth.isFinanceDept());

  /** 退款日期輸入暫存值 */
  refundDateInput = signal<string>('');
  refundSaving = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
      // 預填今日日期作為退款日期
      this.refundDateInput.set(new Date().toISOString().slice(0, 10));
    });
    this.taskService.getById(id, 'advance').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // 可能尚無簽核記錄（draft 狀態）
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printAdvance() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) this.pdfService.printAdvanceRequest(r, r.submittedBy ?? '', t?.approvalRecords ?? [], t?.flow, t?.submittedBySignatureUrl, t?.advanceDetail?.paidBySignatureUrl, t?.advanceDetail?.paidAt);
  }

  /** 確認退款匯款日期 */
  confirmRefund() {
    const r = this.request();
    const date = this.refundDateInput();
    if (!r || !date) return;

    this.refundSaving.set(true);
    this.service.updateRefundDate(r.id, date).subscribe({
      next: () => {
        // 更新 request signal 中的 refundedAt
        this.request.update(req => req ? {...req, refundedAt: date} : req);
        this.toastr.success('已確認退款匯款日期');
        this.refundSaving.set(false);
      },
      error: () => {
        this.toastr.error('確認失敗，請重試');
        this.refundSaving.set(false);
      },
    });
  }
}
