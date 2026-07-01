import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {EMPTY, Observable, catchError, tap} from 'rxjs';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {FilePreviewLoader} from '../../../../../shared/services/file-preview-loader';
import {AttachmentsList} from '../../../../../shared/components/attachments-list';
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
import {TravelPaymentRequestService} from '../../../travel-payment-requests/services/travel-payment-request.service';
import {TravelPaymentPdfService} from '../../../travel-payment-requests/services/travel-payment-pdf.service';
import {PreReviewPdfService} from '../../../pre-review-requests/services/pre-review-pdf.service';
import {ApprovalTaskService} from '../../services/approval-task.service';
import {
  ApprovalTask, ApprovalRecord, TaskStatus,
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
  InstallmentDto, InstallmentInput, UpsertInstallmentsRequest,
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
  imports: [RouterLink, ReactiveFormsModule, AsyncPipe, DatePipe, DecimalPipe, FilePreviewModal, AttachmentsList, ScrollIntoViewDirective],
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

  // ── 分期撥款 form（4 種申請類型共用）─────────────────────────────────────────
  /** 分期撥款明細表單 — 每列 {id?, expectedDate, paidAt, amount, note} */
  installmentsForm = this.fb.array<FormGroup<{
    id:           FormControl<number | null>;
    expectedDate: FormControl<string>;
    paidAt:       FormControl<string>;
    amount:       FormControl<number>;
    note:         FormControl<string>;
  }>>([]);
  installmentsMsg   = signal('');
  installmentsError = signal('');
  /** 載入時保留 server 端的 paidAt 狀態用於 readonly 判斷（避免 form 變動後失去原本狀態）*/
  private installmentLockedIds = new Set<number>();
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
        this.initInstallmentsForm(task);
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

  // ── 分期撥款 form helpers ───────────────────────────────────────────────────

  /** 取得當前申請的撥款明細（4 種類型擇一）*/
  getInstallments(task: ApprovalTask): InstallmentDto[] | undefined {
    return task.paymentDetail?.installments
        ?? task.advanceDetail?.installments
        ?? task.travelDetail?.installments
        ?? task.travelPaymentDetail?.installments;
  }

  /** 取得當前申請的總金額（4 種類型擇一）*/
  getInstallmentTotal(task: ApprovalTask): number {
    return task.paymentDetail?.totalAmount
        ?? task.advanceDetail?.grandTotal
        ?? task.travelDetail?.grandTotal
        ?? task.travelPaymentDetail?.grandTotal
        ?? 0;
  }

  /** 是否為支援分期撥款的申請類型 */
  isInstallmentApp(task: ApprovalTask): boolean {
    return ['payment_request', 'advance', 'travel', 'travel_payment'].includes(task.applicationType);
  }

  /** 取得當前撥款 status（後端三態）*/
  getPaymentStatus(task: ApprovalTask): string | undefined {
    return task.paymentDetail?.paymentStatus
        ?? task.advanceDetail?.paymentStatus
        ?? task.travelDetail?.paymentStatus
        ?? task.travelPaymentDetail?.paymentStatus;
  }

  /** Status badge class（容忍未知值）*/
  getStatusBadgeClass(status: string): string {
    return (this.installmentStatusClass as Record<string, string>)[status] ?? 'bg-secondary';
  }
  /** Status badge label（容忍未知值）*/
  getStatusBadgeLabel(status: string): string {
    return (this.installmentStatusLabel as Record<string, string>)[status] ?? status;
  }

  /** 初始化分期表單；若 task 已有 installments 則填入，否則建立 1 列空 row（金額自動帶總額）*/
  initInstallmentsForm(task: ApprovalTask) {
    this.installmentsForm.clear();
    this.installmentLockedIds.clear();
    if (!this.isInstallmentApp(task)) return;

    const list = this.getInstallments(task) ?? [];
    if (list.length > 0) {
      for (const ins of list) {
        if (ins.paidAt) this.installmentLockedIds.add(ins.id);
        this.installmentsForm.push(this.buildInstallmentRow({
          id:           ins.id,
          installmentNo: ins.installmentNo,
          expectedDate: ins.expectedDate?.toString().slice(0, 10) ?? '',
          paidAt:       ins.paidAt?.toString().slice(0, 10) ?? '',
          amount:       ins.amount,
          note:         ins.note ?? '',
        }));
      }
    } else {
      // 預設 1 列，金額帶申請總額
      this.installmentsForm.push(this.buildInstallmentRow({
        id: undefined,
        installmentNo: 1,
        expectedDate: '',
        paidAt: '',
        amount: this.getInstallmentTotal(task),
        note: '',
      }));
    }
  }

  private buildInstallmentRow(v: {id?: number; installmentNo: number; expectedDate: string; paidAt: string; amount: number; note: string}) {
    return this.fb.group({
      id:           this.fb.control<number | null>(v.id ?? null),
      expectedDate: this.fb.nonNullable.control(v.expectedDate, Validators.required),
      paidAt:       this.fb.nonNullable.control(v.paidAt),
      amount:       this.fb.nonNullable.control(v.amount, [Validators.required, Validators.min(1)]),
      note:         this.fb.nonNullable.control(v.note),
    });
  }

  /** 是否為已鎖定列（已撥款 = 不可改 expectedDate/amount/paidAt、不可刪）*/
  isInstallmentLocked(row: FormGroup): boolean {
    const id = row.get('id')?.value;
    return id != null && this.installmentLockedIds.has(id);
  }

  /** 加一列（自動推算 installmentNo = 目前列數 + 1，金額為剩餘缺口；SUM ≥ 總額時不執行）*/
  addInstallmentRow(task: ApprovalTask) {
    if (!this.canAddInstallmentRow(task)) return;
    const remaining = this.getInstallmentTotal(task) - this.installmentsSum();
    this.installmentsForm.push(this.buildInstallmentRow({
      installmentNo: this.installmentsForm.length + 1,
      expectedDate: '',
      paidAt: '',
      amount: Math.max(1, remaining),
      note: '',
    }));
  }

  /** 移除一列（已撥款的列不可移除）*/
  removeInstallmentRow(index: number) {
    const row = this.installmentsForm.at(index);
    if (this.isInstallmentLocked(row)) return;
    this.installmentsForm.removeAt(index);
  }

  /** 各筆金額加總 */
  installmentsSum(): number {
    return this.installmentsForm.controls.reduce((acc, c) => acc + (Number(c.get('amount')?.value) || 0), 0);
  }

  /** SUM 是否等於申請總額（容忍 0.01 浮點誤差）*/
  isInstallmentsSumValid(task: ApprovalTask): boolean {
    return Math.abs(this.installmentsSum() - this.getInstallmentTotal(task)) <= 0.01;
  }

  /** 取得除了 index 列以外其他列的金額加總（用於計算單期 max）*/
  installmentsSumExcludingRow(index: number): number {
    return this.installmentsForm.controls.reduce((acc, c, i) =>
      i === index ? acc : acc + (Number(c.get('amount')?.value) || 0), 0);
  }

  /** 某列金額 input 的 max：剩餘額度（總額 − 其他列已填）*/
  installmentRowMax(task: ApprovalTask, index: number): number {
    return this.getInstallmentTotal(task) - this.installmentsSumExcludingRow(index);
  }

  /** 是否可新增一期：SUM < 申請總額（≥ 時禁用以避免新增 0 元空期）；FullyPaid 後也禁用 */
  canAddInstallmentRow(task: ApprovalTask): boolean {
    if (this.isFullyPaid(task)) return false;
    return this.installmentsSum() < this.getInstallmentTotal(task) - 0.01;
  }

  /** 是否已全數撥款（FullyPaid）— 所有列都鎖定，無可修改 */
  isFullyPaid(task: ApprovalTask): boolean {
    return this.getPaymentStatus(task) === 'FullyPaid';
  }

  /** 送出 upsert（4 種類型各自 dispatch 到對應 service）*/
  submitInstallments(task: ApprovalTask) {
    this.installmentsMsg.set('');
    this.installmentsError.set('');

    if (!this.isInstallmentsSumValid(task)) {
      this.installmentsError.set(`各筆金額加總（${this.installmentsSum().toFixed(2)}）需等於申請總額（${this.getInstallmentTotal(task).toFixed(2)}）。`);
      return;
    }
    if (this.installmentsForm.invalid) {
      this.installmentsError.set('請填妥所有預計撥款日與金額。');
      this.installmentsForm.markAllAsTouched();
      return;
    }

    // 組成 request — installmentNo 依當前順序重編
    const inputs: InstallmentInput[] = this.installmentsForm.controls.map((row, idx) => ({
      id:            row.get('id')!.value ?? undefined,
      installmentNo: idx + 1,
      expectedDate:  row.get('expectedDate')!.value,
      paidAt:        row.get('paidAt')!.value || undefined,
      amount:        Number(row.get('amount')!.value),
      note:          row.get('note')!.value || undefined,
    }));
    const body: UpsertInstallmentsRequest = {installments: inputs};

    let update$: Observable<any>;
    if (task.paymentDetail)             update$ = this.paymentService.upsertInstallments(task.paymentDetail.paymentRequestId, body);
    else if (task.advanceDetail)        update$ = this.advanceService.upsertInstallments(task.advanceDetail.advanceRequestId, body);
    else if (task.travelDetail)         update$ = this.travelService.upsertInstallments(task.travelDetail.travelRequestId, body);
    else if (task.travelPaymentDetail)  update$ = this.travelPaymentService.upsertInstallments(task.travelPaymentDetail.travelPaymentRequestId, body);
    else { this.installmentsError.set('不支援的申請類型。'); return; }

    update$.subscribe({
      next: () => {
        this.installmentsMsg.set(`已更新 ${inputs.length} 筆撥款明細。`);
        // 重新載入 task 資料以反映新狀態（已撥款列鎖定 / paymentStatus 三態）
        this.task$ = this.service.getById(this.taskId, this.applicationType).pipe(
          tap(t => { if (t) { this.taskStatus.set(t.status); this.initInstallmentsForm(t); } }),
          catchError((err: HttpErrorResponse) => { this.errorMsg.set(err.error?.message || '載入簽核作業失敗。'); return EMPTY; }),
        );
      },
      error: (err: HttpErrorResponse) => {
        this.installmentsError.set(err.error?.message || '更新撥款明細失敗。');
      },
    });
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
      if (!this.isInstallmentsSumValid(task)) {
        this.installmentsError.set(`各筆金額加總（${this.installmentsSum().toFixed(2)}）需等於申請總額（${this.getInstallmentTotal(task).toFixed(2)}）。`);
        return;
      }
      if (this.installmentsForm.invalid) {
        this.installmentsError.set('請填妥所有預計撥款日與金額。');
        this.installmentsForm.markAllAsTouched();
        return;
      }
      installments = this.installmentsForm.controls.map((row, idx) => ({
        id:            row.get('id')!.value ?? undefined,
        installmentNo: idx + 1,
        expectedDate:  row.get('expectedDate')!.value,
        paidAt:        row.get('paidAt')!.value || undefined,
        amount:        Number(row.get('amount')!.value),
        note:          row.get('note')!.value || undefined,
      }));
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
