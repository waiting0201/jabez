import {Component, computed, inject, OnInit, signal, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {EMPTY, Observable, catchError, tap} from 'rxjs';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {FilePreviewLoader} from '../../../../../shared/services/file-preview-loader';
import {AttachmentsList} from '../../../../../shared/components/attachments-list';
import {InstallmentsEditorComponent} from '../../../../../shared/components/installments-editor';
import {WriteOffSummaryComponent} from '../../../../../shared/components/write-off-summary';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';
import {AdvanceRequestService} from '../../../advance-requests/services/advance-request.service';
import {roundLabel} from '../../../advance-requests/models/advance-request.model';
import {AdvancePdfService} from '../../../advance-requests/services/advance-pdf.service';
import {WriteOffRequestService} from '../../../write-off-requests/services/write-off-request.service';
import {WriteOffPdfService} from '../../../write-off-requests/services/write-off-pdf.service';
import {PaymentPdfService} from '../../../payment-requests/services/payment-pdf.service';
import {TravelRequestService} from '../../../travel-requests/services/travel-request.service';
import {TravelWriteOffRequestService} from '../../../travel-write-off-requests/services/travel-write-off-request.service';
import {TravelWriteOffPdfService} from '../../../travel-write-off-requests/services/travel-write-off-pdf.service';
import {TravelPaymentRequestService} from '../../../travel-payment-requests/services/travel-payment-request.service';
import {TravelPaymentPdfService} from '../../../travel-payment-requests/services/travel-payment-pdf.service';
import {PreReviewPdfService} from '../../../pre-review-requests/services/pre-review-pdf.service';
import {ApprovalTaskService} from '../../services/approval-task.service';
import {
  ApprovalTask, ApprovalRecord, TaskStatus,
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
  InstallmentDto, InstallmentInput, UpsertInstallmentsRequest, WriteOffTaskDetailItem,
  PAYMENT_INSTALLMENT_STATUS_LABELS, PAYMENT_INSTALLMENT_STATUS_CLASSES,
} from '../../models/approval-task.model';
import {LeaveType, formatLeaveDuration} from '../../../leave-requests/models/leave-request.model';

/**
 * 財務撥款步驟的部門 Code（須與後端 DepartmentCodes.FinanceStep、
 * approval-task-list.ts 的 FINANCE_STEP_DEPT_CODES 三處同步）：
 * 僅財務管理部，含舊短碼 'FIN' 與改制後英文全名，避免組織改制後判定失效。
 */
const FINANCE_STEP_DEPT_CODES = new Set(['FIN', 'Financial Management Department']);

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-approval-task-review',
  templateUrl: './approval-task-review.html',
  imports: [RouterLink, ReactiveFormsModule, AsyncPipe, DatePipe, DecimalPipe, FilePreviewModal, AttachmentsList, InstallmentsEditorComponent, WriteOffSummaryComponent, ScrollIntoViewDirective],
})
export class ApprovalTaskReview implements OnInit {
  private service           = inject(ApprovalTaskService);
  private paymentService    = inject(PaymentRequestService);
  private advanceService    = inject(AdvanceRequestService);
  protected advancePdfService = inject(AdvancePdfService);
  private writeOffService     = inject(WriteOffRequestService);
  protected writeOffPdfService = inject(WriteOffPdfService);
  protected paymentPdfService = inject(PaymentPdfService);
  private travelService               = inject(TravelRequestService);
  private travelWriteOffService       = inject(TravelWriteOffRequestService);
  protected travelWriteOffPdfService  = inject(TravelWriteOffPdfService);
  private travelPaymentService        = inject(TravelPaymentRequestService);
  protected travelPaymentPdfService   = inject(TravelPaymentPdfService);
  protected preReviewPdfService       = inject(PreReviewPdfService);
  private auth              = inject(AuthService);
  private route             = inject(ActivatedRoute);
  private router            = inject(Router);
  private fb                = inject(FormBuilder);
  private sanitizer         = inject(DomSanitizer);
  private previewLoader     = inject(FilePreviewLoader);

  task$!: Observable<ApprovalTask | undefined>;
  taskId = 0;
  applicationType = '';
  taskStatus = signal<TaskStatus>('pending');
  errorMsg = signal('');
  showNoteError = false;

  // ── 分期撥款（5 種申請類型共用；表單邏輯在 shared/components/installments-editor）─────
  /** 本申請單自身的撥款明細編輯器（payment_request / advance / travel / travel_payment / write_off）*/
  installmentsEditor = viewChild<InstallmentsEditorComponent>('installmentsEditor');
  /** 預支沖銷專用：關聯預支單的撥款明細編輯器（資料與預支申請單同步）*/
  advanceInstallmentsEditor = viewChild<InstallmentsEditorComponent>('advanceInstallmentsEditor');

  installmentsMsg   = signal('');
  installmentsError = signal('');
  advInstallmentsMsg   = signal('');
  advInstallmentsError = signal('');
  readonly installmentStatusLabel = PAYMENT_INSTALLMENT_STATUS_LABELS;
  readonly installmentStatusClass = PAYMENT_INSTALLMENT_STATUS_CLASSES;

  /** 預支沖銷：原始餘額（沖銷累計 > 預支時為負） */
  writeOffRawBalance(task: ApprovalTask): number {
    const d = task.writeOffDetail;
    if (!d) return 0;
    return d.advanceGrandTotal - d.otherWrittenOffTotal - d.grandTotal;
  }
  /** 出差沖銷：原始餘額（沖銷累計 > 出差時為負） */
  travelWriteOffRawBalance(task: ApprovalTask): number {
    const d = task.travelWriteOffDetail;
    if (!d) return 0;
    return d.travelGrandTotal - d.otherWrittenOffTotal - d.grandTotal;
  }
  /** 預支沖銷是否超支（需退款） */
  isWriteOffOverspent(task: ApprovalTask): boolean {
    return !!task.writeOffDetail && this.writeOffRawBalance(task) < 0;
  }
  /** 出差沖銷是否超支（需退款） */
  isTravelWriteOffOverspent(task: ApprovalTask): boolean {
    return !!task.travelWriteOffDetail && this.travelWriteOffRawBalance(task) < 0;
  }
  /** 沖銷類型是否需要顯示退款輸入卡片（僅超支情境） */
  shouldShowRefundCard(task: ApprovalTask): boolean {
    if (task.applicationType === 'write_off') return this.isWriteOffOverspent(task);
    if (task.applicationType === 'travel_write_off') return this.isTravelWriteOffOverspent(task);
    return false;
  }

  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  /** 私有容器（quotes 報價單 / request-attachments）需透過 JWT 代理抓 blob，不能直接丟進 iframe */
  async openProxyPreview(name: string, url: string) {
    if (!url) return;
    this.previewFile = await this.previewLoader.load(url, name);
  }
  closePreview() {
    this.previewLoader.revoke(this.previewFile);
    this.previewFile = null;
  }

  readonly statusLabel    = TASK_STATUS_LABELS;
  readonly statusClass    = TASK_STATUS_CLASSES;
  readonly appTypeLabel   = APPLICATION_TYPE_LABELS;
  readonly appTypeClass   = APPLICATION_TYPE_CLASSES;
  readonly payTypeLabel   = PAYMENT_TYPE_LABELS;
  readonly leaveTypeLabel = LEAVE_TYPE_LABELS;

  /** 依假別單位格式化時數顯示 */
  formatLeaveDuration(type: string, hours: number): string {
    return formatLeaveDuration(type as LeaveType, hours);
  }

  form = this.fb.group({
    action:               ['approved', Validators.required],
    reviewNote:           [''],
    estimatedRefundDate:  [''],
    refundedAt:           [''],
    closeAdvance:         [false],
  });

  ngOnInit() {
    this.applicationType = this.route.snapshot.paramMap.get('applicationType') ?? '';
    this.taskId = +this.route.snapshot.paramMap.get('id')!;
    this.task$  = this.service.getById(this.taskId, this.applicationType).pipe(
      tap(task => {
        if (!task) return;
        this.taskStatus.set(task.status);
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

  // ── 分期撥款 helpers ────────────────────────────────────────────────────────

  /** 取得當前申請的撥款明細（5 種類型擇一）*/
  getInstallments(task: ApprovalTask): InstallmentDto[] | undefined {
    return task.paymentDetail?.installments
        ?? task.advanceDetail?.installments
        ?? task.travelDetail?.installments
        ?? task.travelPaymentDetail?.installments
        ?? task.writeOffDetail?.installments;
  }

  /**
   * 取得當前申請的應撥總額（5 種類型擇一）。
   * 預支沖銷不是整單金額，而是「本次沖銷造成的超支增額」refundDue（後端 WriteOffRefundCalculator 算好帶回）。
   */
  getInstallmentTotal(task: ApprovalTask): number {
    if (task.writeOffDetail) return task.writeOffDetail.refundDue ?? 0;
    return task.paymentDetail?.totalAmount
        ?? task.advanceDetail?.grandTotal
        ?? task.travelDetail?.grandTotal
        ?? task.travelPaymentDetail?.grandTotal
        ?? 0;
  }

  /**
   * 是否為支援分期撥款的申請類型。
   * write_off 僅在本次沖銷超支（refundDue > 0）時才有撥款可言，未超支不顯示撥款區塊。
   */
  isInstallmentApp(task: ApprovalTask): boolean {
    if (task.applicationType === 'write_off') return (task.writeOffDetail?.refundDue ?? 0) > 0;
    return ['payment_request', 'advance', 'travel', 'travel_payment'].includes(task.applicationType);
  }

  /** 取得當前撥款 status（後端三態）*/
  getPaymentStatus(task: ApprovalTask): string | undefined {
    return task.paymentDetail?.paymentStatus
        ?? task.advanceDetail?.paymentStatus
        ?? task.travelDetail?.paymentStatus
        ?? task.travelPaymentDetail?.paymentStatus
        ?? task.writeOffDetail?.paymentStatus;
  }

  /** Status badge class（容忍未知值）*/
  getStatusBadgeClass(status: string): string {
    return (this.installmentStatusClass as Record<string, string>)[status] ?? 'bg-secondary';
  }
  /** Status badge label（容忍未知值）*/
  getStatusBadgeLabel(status: string): string {
    return (this.installmentStatusLabel as Record<string, string>)[status] ?? status;
  }

  /** 送出 upsert（5 種類型各自 dispatch 到對應 service）*/
  submitInstallments(task: ApprovalTask, inputs: InstallmentInput[]) {
    this.installmentsMsg.set('');
    this.installmentsError.set('');

    const body: UpsertInstallmentsRequest = {installments: inputs};

    let update$: Observable<any>;
    if (task.paymentDetail)             update$ = this.paymentService.upsertInstallments(task.paymentDetail.paymentRequestId, body);
    else if (task.advanceDetail)        update$ = this.advanceService.upsertInstallments(task.advanceDetail.advanceRequestId, body);
    else if (task.travelDetail)         update$ = this.travelService.upsertInstallments(task.travelDetail.travelRequestId, body);
    else if (task.travelPaymentDetail)  update$ = this.travelPaymentService.upsertInstallments(task.travelPaymentDetail.travelPaymentRequestId, body);
    else if (task.writeOffDetail)       update$ = this.writeOffService.upsertInstallments(task.writeOffDetail.writeOffRequestId, body);
    else { this.installmentsError.set('不支援的申請類型。'); return; }

    update$.subscribe({
      next: () => {
        this.installmentsMsg.set(`已更新 ${inputs.length} 筆撥款明細。`);
        this.reloadTask();
      },
      error: (err: HttpErrorResponse) => {
        this.installmentsError.set(err.error?.message || '更新撥款明細失敗。');
      },
    });
  }

  /**
   * 預支沖銷簽核頁專用：直接更新「關聯預支申請單」的撥款明細。
   * 走既有的 PATCH /advance-requests/{id}/installments，資料與預支申請單完全同步。
   */
  submitAdvanceInstallments(task: ApprovalTask, inputs: InstallmentInput[]) {
    this.advInstallmentsMsg.set('');
    this.advInstallmentsError.set('');

    const advanceId = task.writeOffDetail?.advanceRequestId;
    if (!advanceId) { this.advInstallmentsError.set('找不到關聯的預支申請單。'); return; }

    this.advanceService.upsertInstallments(advanceId, {installments: inputs}).subscribe({
      next: () => {
        this.advInstallmentsMsg.set(`已更新預支單 ${inputs.length} 筆撥款明細。`);
        this.reloadTask();
      },
      error: (err: HttpErrorResponse) => {
        this.advInstallmentsError.set(err.error?.message || '更新預支單撥款明細失敗。');
      },
    });
  }

  // ── 支票已支付註記（預支沖銷）─────────────────────────────────────────────

  checkPaymentSaving = signal(false);

  /**
   * 是否顯示「支票已支付」欄：僅財務體系部門或 Superadmin 可見（整欄隱藏，非僅唯讀）。
   * 與 canMarkCheckPaid 用同一組部門判定，看得到的人就是可勾選的人（狀態允許時）。
   */
  canSeeCheckPaid = computed(() => this.auth.isSuperAdmin() || this.auth.isFinanceDept());

  /**
   * 是否可勾選「支票已支付」：財務體系部門或 Superadmin，且單子在待審核 / 已核准狀態。
   * 支票由公司直接付給廠商，不走撥款分期，僅以此旗標註記已付出。
   */
  canMarkCheckPaid(task: ApprovalTask): boolean {
    if (task.applicationType !== 'write_off') return false;
    if (task.status !== 'pending' && task.status !== 'approved') return false;
    return this.auth.isSuperAdmin() || this.auth.isFinanceDept();
  }

  /** 有支票金額的明細筆數 */
  checkItemCount(items: {checkAmount: number}[]): number {
    return items.filter(i => i.checkAmount > 0).length;
  }

  /** 已註記支票支付的明細筆數 */
  checkPaidCount(items: {checkAmount: number; checkPaid?: boolean}[]): number {
    return items.filter(i => i.checkAmount > 0 && i.checkPaid).length;
  }

  toggleCheckPaid(task: ApprovalTask, item: WriteOffTaskDetailItem, ev: Event) {
    const d = task.writeOffDetail;
    if (!d) return;
    const checked = (ev.target as HTMLInputElement).checked;

    this.checkPaymentSaving.set(true);
    this.writeOffService.updateCheckPayments(d.writeOffRequestId, {
      items: [{itemId: item.id, checkPaid: checked}],
    }).subscribe({
      next: () => {
        item.checkPaid = checked;   // 樂觀更新，避免整頁重載打斷勾選節奏
        this.checkPaymentSaving.set(false);
      },
      error: (err: HttpErrorResponse) => {
        (ev.target as HTMLInputElement).checked = !checked;
        this.errorMsg.set(err.error?.message || '更新支票支付狀態失敗。');
        this.checkPaymentSaving.set(false);
      },
    });
  }

  /** 重新載入 task（反映已撥款列鎖定 / paymentStatus 三態 / 支票支付註記）*/
  private reloadTask() {
    this.task$ = this.service.getById(this.taskId, this.applicationType).pipe(
      tap(t => { if (t) this.taskStatus.set(t.status); }),
      catchError((err: HttpErrorResponse) => { this.errorMsg.set(err.error?.message || '載入簽核作業失敗。'); return EMPTY; }),
    );
  }

  /**
   * 判斷是否可設定撥款明細：
   * - 待審核：須輪到財務簽核步驟（currentStepOrder 指向財務部步驟）
   * - 已核准：currentStepOrder 已停在流程最後一步（可能非財務，如總監室），
   *   改比對登入者自身部門是否屬財務體系，對齊後端 UpsertInstallmentsAsync 的權限判斷
   */
  canSetPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (task.status === 'approved') return this.auth.isFinanceDept();
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return !!step?.departmentCode && FINANCE_STEP_DEPT_CODES.has(step.departmentCode);
  }

  /** 判斷是否顯示「預支結案」checkbox：預支沖銷申請 (write_off) 且當前步驟為財務部 */
  canCloseAdvance(task: ApprovalTask): boolean {
    if (task.applicationType !== 'write_off') return false;
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return !!step?.departmentCode && FINANCE_STEP_DEPT_CODES.has(step.departmentCode);
  }

  /** 判斷是否顯示「出差結案」checkbox：出差沖銷申請 (travel_write_off) 且當前步驟為財務部 */
  canCloseTravelRequest(task: ApprovalTask): boolean {
    if (task.applicationType !== 'travel_write_off') return false;
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return !!step?.departmentCode && FINANCE_STEP_DEPT_CODES.has(step.departmentCode);
  }

  /** 判斷已核准的沖銷申請是否可結案：財務部或 Superadmin，且關聯的預支/出差未結案 */
  canCloseAfterApproval(task: ApprovalTask): boolean {
    if (task.status !== 'approved') return false;
    if (!this.auth.isSuperAdmin() && !this.auth.isFinanceDept()) return false;
    if (task.applicationType === 'write_off')
      return !!task.writeOffDetail && !task.writeOffDetail.advanceIsClosed;
    if (task.applicationType === 'travel_write_off')
      return !!task.travelWriteOffDetail && !task.travelWriteOffDetail.travelIsClosed;
    return false;
  }

  /** 已核准後的沖銷申請是否已結案 */
  isClosedAfterApproval(task: ApprovalTask): boolean {
    if (task.applicationType === 'write_off')
      return !!task.writeOffDetail?.advanceIsClosed;
    if (task.applicationType === 'travel_write_off')
      return !!task.travelWriteOffDetail?.travelIsClosed;
    return false;
  }

  closeCaseLoading = signal(false);

  /** 執行結案 */
  closeCase(task: ApprovalTask) {
    const type = task.applicationType as 'write_off' | 'travel_write_off';
    this.closeCaseLoading.set(true);
    this.service.closeCase(task.id, type).pipe(
      catchError((err: HttpErrorResponse) => {
        this.closeCaseLoading.set(false);
        this.errorMsg.set(err.error?.message || '結案失敗');
        return EMPTY;
      }),
    ).subscribe(() => {
      this.closeCaseLoading.set(false);
      // 重新載入 task 資料
      this.task$ = this.service.getById(this.taskId, this.applicationType).pipe(
        tap(t => { if (t) this.taskStatus.set(t.status as TaskStatus); }),
      );
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
          task.writeOffDetail?.refundedAt,
          task.writeOffDetail?.refundedBySignatureUrl,
        );
      },
      error: () => {
        this.errorMsg.set('載入預支沖銷申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  /** 列印出差請款申請表 PDF */
  printTravelPaymentPdf(task: ApprovalTask) {
    if (!task.travelPaymentDetail || task.status !== 'approved') return;
    this.travelPaymentService.getById(task.travelPaymentDetail.travelPaymentRequestId).subscribe({
      next: r => {
        this.travelPaymentPdfService.printTravelPaymentRequest(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
          undefined,
        );
      },
      error: () => {
        this.errorMsg.set('載入出差請款申請資料失敗，無法匯出 PDF。');
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

  printPreReviewPdf(task: ApprovalTask) {
    if (!task.preReviewDetail || task.status !== 'approved') return;
    this.preReviewPdfService.printPreReviewRequest(task);
  }

  /** 計算陣列中指定數值欄位的加總 */
  sumField<T>(items: T[], field: keyof T): number {
    return items.reduce((acc, item) => acc + (item[field] as unknown as number), 0);
  }

  /** 追加預支批次標籤（與詳情頁 / PDF 共用同一份定義） */
  readonly roundLabel = roundLabel;

  /** 明細列是否為該批次第一列（同批次第二列起批次欄留白） */
  isFirstOfRound(items: {roundNo: number}[], index: number): boolean {
    return index === 0 || items[index - 1].roundNo !== items[index].roundNo;
  }

  submit(task: ApprovalTask) {
    if (this.taskStatus() !== 'pending') return;
    const action = this.form.value.action as TaskStatus;
    const note   = this.form.value.reviewNote?.trim() ?? '';
    const estimatedRefundDate = this.form.value.estimatedRefundDate || undefined;
    const refundedAt = this.form.value.refundedAt || undefined;
    const closeAdvance = this.form.value.closeAdvance ?? false;
    if ((action === 'rejected' || action === 'returned') && !note) {
      this.showNoteError = true;
      return;
    }
    this.showNoteError = false;

    // 撥款類 + 財務步驟核准：撥款明細必填，與審核一起送出
    let installments: InstallmentInput[] | undefined;
    if (action === 'approved' && this.isInstallmentApp(task) && this.canSetPaymentDate(task)) {
      this.installmentsError.set('');
      const editor = this.installmentsEditor();
      if (!editor) {
        this.installmentsError.set('請填寫撥款明細。');
        return;
      }
      if (!editor.valid()) {
        this.installmentsError.set(
          editor.sumValid()
            ? '請填妥所有預計撥款日與金額。'
            : `各筆金額加總（${editor.sum().toFixed(2)}）需等於應撥總額（${this.getInstallmentTotal(task).toFixed(2)}）。`);
        editor.markAllAsTouched();
        return;
      }
      installments = editor.value();
    }

    this.errorMsg.set('');
    this.service.review(this.taskId, this.applicationType, action, note, estimatedRefundDate, refundedAt, closeAdvance, installments).subscribe({
      next: () => this.router.navigate(['/admin/approval-tasks']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '審核失敗，請稍後再試。');
      },
    });
  }
}
