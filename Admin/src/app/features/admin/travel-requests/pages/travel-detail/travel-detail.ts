import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {TravelRequestService} from '../../services/travel-request.service';
import {TravelPdfService} from '../../services/travel-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {TravelRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/travel-request.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {ClosureInfoCardComponent} from '../../../../../shared/components/closure-info-card';

@Component({
  selector: 'app-travel-detail',
  templateUrl: './travel-detail.html',
  imports: [RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, InstallmentsTable, ClosureInfoCardComponent],
})
export class TravelDetail implements OnInit {
  private service = inject(TravelRequestService);
  private pdfService = inject(TravelPdfService);
  private taskService = inject(ApprovalTaskService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  request = signal<TravelRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(id).subscribe(r => {
      this.request.set(r);
    });
    this.taskService.getById(id, 'travel').subscribe({
      next: t => this.approvalTask.set(t),
      error: () => {}, // draft 狀態可能尚無簽核記錄
    });
  }

  get pdfLoading() { return this.pdfService.pdfLoading; }

  printTravel() {
    const r = this.request();
    const t = this.approvalTask();
    if (r) {
      this.pdfService.printTravelRequest(
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
    });
  }

  deleteRequest() {
    const r = this.request();
    if (!r || !confirm('確定要刪除此出差預支申請？')) return;
    this.deleting.set(true);
    this.service.delete(r.id).subscribe({
      next: () => this.router.navigate(['/admin/travel-requests']),
      error: () => this.deleting.set(false),
    });
  }
}
