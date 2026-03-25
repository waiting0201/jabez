import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {EMPTY, Observable, catchError, tap} from 'rxjs';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';
import {AdvanceRequestService} from '../../../advance-requests/services/advance-request.service';
import {AdvancePdfService} from '../../../advance-requests/services/advance-pdf.service';
import {WriteOffRequestService} from '../../../write-off-requests/services/write-off-request.service';
import {WriteOffPdfService} from '../../../write-off-requests/services/write-off-pdf.service';
import {PaymentPdfService} from '../../../payment-requests/services/payment-pdf.service';
import {TravelRequestService} from '../../../travel-requests/services/travel-request.service';
import {TravelWriteOffRequestService} from '../../../travel-write-off-requests/services/travel-write-off-request.service';
import {TravelWriteOffPdfService} from '../../../travel-write-off-requests/services/travel-write-off-pdf.service';
import {ApprovalTaskService} from '../../services/approval-task.service';
import {
  ApprovalTask, ApprovalRecord, TaskStatus,
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
} from '../../models/approval-task.model';

@Component({
  selector: 'app-approval-task-review',
  templateUrl: './approval-task-review.html',
  imports: [RouterLink, ReactiveFormsModule, AsyncPipe, DatePipe, DecimalPipe, FilePreviewModal],
})
export class ApprovalTaskReview implements OnInit {
  private service           = inject(ApprovalTaskService);
  private paymentService    = inject(PaymentRequestService);
  private advanceService    = inject(AdvanceRequestService);
  protected advancePdfService = inject(AdvancePdfService);
  private writeOffService     = inject(WriteOffRequestService);
  protected writeOffPdfService = inject(WriteOffPdfService);
  protected paymentPdfService = inject(PaymentPdfService);
  private travelService         = inject(TravelRequestService);
  private travelWriteOffService     = inject(TravelWriteOffRequestService);
  protected travelWriteOffPdfService = inject(TravelWriteOffPdfService);
  private auth              = inject(AuthService);
  private route             = inject(ActivatedRoute);
  private router            = inject(Router);
  private fb                = inject(FormBuilder);
  private sanitizer         = inject(DomSanitizer);

  task$!: Observable<ApprovalTask | undefined>;
  taskId = 0;
  applicationType = '';
  taskStatus = signal<TaskStatus>('pending');
  errorMsg = signal('');
  showNoteError = false;

  /** 已核准後：財務部/Superadmin 可更新撥款日 */
  canUpdatePaymentDate = computed(() => this.auth.isSuperAdmin() || this.auth.isFinanceDept());
  paymentDateForm = {estimatedPaymentDate: '', paidAt: ''};
  paymentDateMsg   = signal('');
  paymentDateError = signal('');

  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  readonly statusLabel    = TASK_STATUS_LABELS;
  readonly statusClass    = TASK_STATUS_CLASSES;
  readonly appTypeLabel   = APPLICATION_TYPE_LABELS;
  readonly appTypeClass   = APPLICATION_TYPE_CLASSES;
  readonly payTypeLabel   = PAYMENT_TYPE_LABELS;
  readonly leaveTypeLabel = LEAVE_TYPE_LABELS;

  form = this.fb.group({
    action:               ['approved', Validators.required],
    reviewNote:           [''],
    estimatedPaymentDate: [''],
    paidAt:               [''],
    closeAdvance:         [false],
  });

  ngOnInit() {
    this.applicationType = this.route.snapshot.paramMap.get('applicationType') ?? '';
    this.taskId = +this.route.snapshot.paramMap.get('id')!;
    this.task$  = this.service.getById(this.taskId, this.applicationType).pipe(
      tap(task => {
        if (!task) return;
        this.taskStatus.set(task.status);
        if (task.paymentDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.paymentDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.paymentDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
        if (task.advanceDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.advanceDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.advanceDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
        if (task.travelDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.travelDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.travelDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
        if (task.writeOffDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.writeOffDetail.estimatedRefundDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.writeOffDetail.refundedAt?.toString().slice(0, 10) ?? '';
        }
        if (task.travelWriteOffDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.travelWriteOffDetail.estimatedRefundDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.travelWriteOffDetail.refundedAt?.toString().slice(0, 10) ?? '';
        }
      }),
      catchError((err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '載入簽核作業失敗。');
        return EMPTY;
      }),
    );
  }

  getRecord(records: ApprovalRecord[], stepOrder: number): ApprovalRecord | undefined {
    return records.find(r => r.stepOrder === stepOrder);
  }

  /** 判斷當前簽核步驟是否為財務部，或登入者為 Superadmin */
  canSetPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return step?.departmentCode === 'FIN';
  }

  /** 判斷是否顯示「預支結案」checkbox：預支沖銷申請 (write_off) 且當前步驟為財務部 */
  canCloseAdvance(task: ApprovalTask): boolean {
    if (task.applicationType !== 'write_off') return false;
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return step?.departmentCode === 'FIN';
  }

  /** 判斷是否顯示「出差結案」checkbox：出差沖銷申請 (travel_write_off) 且當前步驟為財務部 */
  canCloseTravelRequest(task: ApprovalTask): boolean {
    if (task.applicationType !== 'travel_write_off') return false;
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return step?.departmentCode === 'FIN';
  }

  /** 判斷已核准後是否可編輯撥款日：Superadmin、或曾審核過財務部步驟的使用者 */
  canEditPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow || !task.approvalRecords?.length) return false;
    // 找出流程中所有財務部步驟（以部門代碼 'FIN' 判斷）的 stepOrder
    const financeStepOrders = task.flow.steps
      .filter(s => s.departmentCode === 'FIN')
      .map(s => s.stepOrder);
    if (!financeStepOrders.length) return false;
    // 檢查當前使用者是否審核過這些步驟
    const userName = this.auth.currentUser()?.name;
    return task.approvalRecords.some(
      r => financeStepOrders.includes(r.stepOrder) && r.reviewedBy === userName
    );
  }

  /** 更新已核准請款/預支的撥款日期 */
  updatePaymentDate(task: ApprovalTask) {
    const {estimatedPaymentDate, paidAt} = this.paymentDateForm;
    if (!estimatedPaymentDate && !paidAt) return;
    this.paymentDateMsg.set('');
    this.paymentDateError.set('');

    let update$: Observable<any>;
    let successMsg: string;
    if (task.travelWriteOffDetail) {
      // 出差沖銷：更新關聯的出差申請退款日
      update$ = this.travelService.updatePaymentDate(
        task.travelWriteOffDetail.travelRequestId,
        undefined, undefined,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
      successMsg = '退款日期已更新。';
    } else if (task.writeOffDetail) {
      // 預支沖銷：更新關聯的預支申請退款日
      update$ = this.advanceService.updatePaymentDate(
        task.writeOffDetail.advanceRequestId,
        undefined, undefined,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
      successMsg = '退款日期已更新。';
    } else if (task.advanceDetail) {
      update$ = this.advanceService.updatePaymentDate(
        task.advanceDetail.advanceRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
      successMsg = '撥款日期已更新。';
    } else if (task.travelDetail) {
      update$ = this.travelService.updatePaymentDate(
        task.travelDetail.travelRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
      successMsg = '撥款日期已更新。';
    } else if (task.paymentDetail) {
      update$ = this.paymentService.updatePaymentDate(
        task.paymentDetail.paymentRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
      successMsg = '撥款日期已更新。';
    } else {
      return;
    }

    update$.subscribe({
      next: () => {
        this.paymentDateMsg.set(successMsg);
        // 重新載入任務資料以反映更新
        this.task$ = this.service.getById(this.taskId, this.applicationType).pipe(
          tap(t => { if (t) this.taskStatus.set(t.status); }),
          catchError((err: HttpErrorResponse) => {
            this.errorMsg.set(err.error?.message || '載入簽核作業失敗。');
            return EMPTY;
          }),
        );
      },
      error: (err: HttpErrorResponse) => {
        this.paymentDateError.set(err.error?.message || '更新撥款日期失敗。');
      },
    });
  }

  /** 列印請款單 PDF */
  printPaymentPdf(task: ApprovalTask) {
    this.paymentPdfService.printPaymentRequest(task);
  }

  /** 列印預支申請表 PDF */
  printAdvancePdf(task: ApprovalTask) {
    if (!task.advanceDetail || task.status !== 'approved') return;
    this.advanceService.getById(task.advanceDetail.advanceRequestId).subscribe({
      next: r => {
        this.advancePdfService.printAdvanceRequest(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
          task.advanceDetail?.paidBySignatureUrl,
          task.advanceDetail?.paidAt,
        );
      },
      error: () => {
        this.errorMsg.set('載入預支申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  /** 列印預支沖銷申請表 PDF */
  printWriteOffPdf(task: ApprovalTask) {
    if (!task.writeOffDetail || task.status !== 'approved') return;
    this.writeOffService.getById(task.writeOffDetail.writeOffRequestId).subscribe({
      next: r => {
        this.writeOffPdfService.printWriteOff(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
          task.writeOffDetail?.paidBySignatureUrl,
          task.writeOffDetail?.refundedAt,
          task.writeOffDetail?.refundedBySignatureUrl,
        );
      },
      error: () => {
        this.errorMsg.set('載入預支沖銷申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  /** 列印出差沖銷申請表 PDF */
  printTravelWriteOffPdf(task: ApprovalTask) {
    if (!task.travelWriteOffDetail || task.status !== 'approved') return;
    this.travelWriteOffService.getById(task.travelWriteOffDetail.travelWriteOffRequestId).subscribe({
      next: r => {
        this.travelWriteOffPdfService.printTravelWriteOff(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
        );
      },
      error: () => {
        this.errorMsg.set('載入出差沖銷申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  /** 計算陣列中指定數值欄位的加總 */
  sumField<T>(items: T[], field: keyof T): number {
    return items.reduce((acc, item) => acc + (item[field] as unknown as number), 0);
  }

  submit() {
    if (this.taskStatus() !== 'pending') return;
    const action = this.form.value.action as TaskStatus;
    const note   = this.form.value.reviewNote?.trim() ?? '';
    const estimatedPaymentDate = this.form.value.estimatedPaymentDate || undefined;
    const paidAt = this.form.value.paidAt || undefined;
    const closeAdvance = this.form.value.closeAdvance ?? false;
    if ((action === 'rejected' || action === 'returned') && !note) {
      this.showNoteError = true;
      return;
    }
    this.showNoteError = false;
    this.errorMsg.set('');
    this.service.review(this.taskId, this.applicationType, action, note, estimatedPaymentDate, paidAt, closeAdvance).subscribe({
      next: () => this.router.navigate(['/admin/approval-tasks']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '審核失敗，請稍後再試。');
      },
    });
  }
}
