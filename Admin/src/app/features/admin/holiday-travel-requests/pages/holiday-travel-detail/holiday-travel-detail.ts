import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {HolidayTravelRequestService} from '../../services/holiday-travel-request.service';
import {HolidayTravelPdfService} from '../../services/holiday-travel-pdf.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {HolidayTravelRequest, TravelParticipant, formatParticipantDates, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/holiday-travel-request.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';

@Component({
  selector: 'app-holiday-travel-detail',
  templateUrl: './holiday-travel-detail.html',
  imports: [RouterLink, DatePipe, DecimalPipe, ApprovalTimeline, InstallmentsTable],
})
export class HolidayTravelDetail implements OnInit {
  private service     = inject(HolidayTravelRequestService);
  private pdfService  = inject(HolidayTravelPdfService);
  private taskService = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private modal       = inject(NgbModal);

  request      = signal<HolidayTravelRequest | null>(null);
  approvalTask = signal<ApprovalTask | null>(null);
  deleting     = signal(false);

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

  /** 參與人員個人參與日期（M/d，半天附時段；頓號分隔；無日期＝全程參與，回空字串） */
  participantDates(p: TravelParticipant): string {
    return formatParticipantDates(p.dates);
  }

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
      );
    }
  }

  submitRequest() {
    const r = this.request();
    if (!r) return;
    this.service.submit(r.id).subscribe(updated => {
      this.request.set(updated);
      const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
      ref.componentInstance.formType = 'holiday_travel';
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
