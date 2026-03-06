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
  private service        = inject(ApprovalTaskService);
  private paymentService = inject(PaymentRequestService);
  private auth           = inject(AuthService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private fb             = inject(FormBuilder);
  private sanitizer      = inject(DomSanitizer);

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
    return step?.departmentName === '財務部';
  }

  /** 判斷已核准後是否可編輯撥款日：Superadmin、或曾審核過財務部步驟的使用者 */
  canEditPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow || !task.approvalRecords?.length) return false;
    // 找出流程中所有財務部步驟的 stepOrder
    const financeStepOrders = task.flow.steps
      .filter(s => s.departmentName === '財務部')
      .map(s => s.stepOrder);
    if (!financeStepOrders.length) return false;
    // 檢查當前使用者是否審核過這些步驟
    const userName = this.auth.currentUser()?.name;
    return task.approvalRecords.some(
      r => financeStepOrders.includes(r.stepOrder) && r.reviewedBy === userName
    );
  }

  /** 更新已核准請款的撥款日期 */
  updatePaymentDate(task: ApprovalTask) {
    if (!task.paymentDetail) return;
    const {estimatedPaymentDate, paidAt} = this.paymentDateForm;
    if (!estimatedPaymentDate && !paidAt) return;
    this.paymentDateMsg.set('');
    this.paymentDateError.set('');
    this.paymentService.updatePaymentDate(
      task.paymentDetail.paymentRequestId,
      estimatedPaymentDate || undefined,
      paidAt || undefined,
    ).subscribe({
      next: () => {
        this.paymentDateMsg.set('撥款日期已更新。');
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

  submit() {
    if (this.taskStatus() !== 'pending') return;
    const action = this.form.value.action as TaskStatus;
    const note   = this.form.value.reviewNote?.trim() ?? '';
    const estimatedPaymentDate = this.form.value.estimatedPaymentDate || undefined;
    const paidAt = this.form.value.paidAt || undefined;
    if ((action === 'rejected' || action === 'returned') && !note) {
      this.showNoteError = true;
      return;
    }
    this.showNoteError = false;
    this.errorMsg.set('');
    this.service.review(this.taskId, this.applicationType, action, note, estimatedPaymentDate, paidAt).subscribe({
      next: () => this.router.navigate(['/admin/approval-tasks']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '審核失敗，請稍後再試。');
      },
    });
  }
}
