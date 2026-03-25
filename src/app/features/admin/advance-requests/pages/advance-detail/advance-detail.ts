import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {AdvancePdfService} from '../../services/advance-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {AdvanceRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/advance-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';

@Component({
  selector: 'app-advance-detail',
  templateUrl: './advance-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline],
})
export class AdvanceDetail implements OnInit {
  private service = inject(AdvanceRequestService);
  private pdfService = inject(AdvancePdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);

  request = signal<AdvanceRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
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
}
