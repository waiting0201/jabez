import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {combineLatest, of} from 'rxjs';
import {switchMap} from 'rxjs/operators';
import {ToastrService} from 'ngx-toastr';
import {ApprovalTaskService, BatchApprovePending} from '../../services/approval-task.service';
import {
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
  PAYMENT_STATE_LABELS, PAYMENT_STATE_CLASSES,
  PAYMENT_INSTALLMENT_STATUS_LABELS, PAYMENT_INSTALLMENT_STATUS_CLASSES,
  PaymentInstallmentStatus,
  ApprovalTask, ApprovalTaskApplicant,
} from '../../models/approval-task.model';
import {ApplicationType} from '../../../approvals/models/approval.model';
import {roundLabel} from '../../../advance-requests/models/advance-request.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {AuthService, FINANCIAL_AND_ABOVE_DEPT_CODES} from '../../../../../core/auth/services/auth.service';

/**
 * 財務撥款步驟專用部門代碼（僅財務管理部，不含總監室/會計室）。
 * 用於「總監待簽核」tab 的可見性判斷；須與 approval-task-review.ts 的
 * FINANCE_STEP_DEPT_CODES、後端 DepartmentCodes.FinanceStep 三處同步。
 */
const FINANCE_STEP_DEPT_CODES = new Set(['FIN', 'Financial Management Department']);

@Component({
  selector: 'app-approval-task-list',
  templateUrl: './approval-task-list.html',
  imports: [RouterLink, DatePipe],
})
export class ApprovalTaskList {
  private service = inject(ApprovalTaskService);
  private auth = inject(AuthService);
  private toastr = inject(ToastrService);

  /** Superadmin 或總監室/財務部/會計部才顯示撥款/退款子篩選 */
  canSeePaymentFilter = computed(() =>
    this.auth.isSuperAdmin() || FINANCIAL_AND_ABOVE_DEPT_CODES.has(this.auth.departmentCode() ?? '')
  );

  /**
   * 申請人下拉：僅財務體系部門或 Superadmin 可見。
   * 與後端 ApprovalTaskHandler.CanFilterByApplicant（DepartmentCodes.FinancialAndAbove）同步。
   */
  canSeeApplicantFilter = computed(() =>
    this.auth.isSuperAdmin() || FINANCIAL_AND_ABOVE_DEPT_CODES.has(this.auth.departmentCode() ?? '')
  );

  /** 「總監待簽核」tab：僅財務管理部或 Superadmin 可見 */
  canSeeDirectorPendingTab = computed(() =>
    this.auth.isSuperAdmin() || FINANCE_STEP_DEPT_CODES.has(this.auth.departmentCode() ?? '')
  );

  /** 是否具備全選核准權限（待審核 tab 才啟用 UI） */
  canBatchApprove = computed(() =>
    this.auth.isSuperAdmin() || this.auth.hasPermission('approval-tasks:batch-approve')
  );

  readonly PAGE_SIZE = 20;
  activeTab = signal<'pending' | 'approved' | 'rejected' | 'director_pending'>('pending');
  paymentStatus = signal<'' | 'paid' | 'unpaid' | 'partial'>('');
  applicationTypeFilter = signal<'' | ApplicationType>('');
  submittedByFilter = signal('');
  page = signal(1);

  /** 類型下拉選項：[ApplicationType, 中文 label][] */
  appTypeOptions = computed(() => Object.entries(APPLICATION_TYPE_LABELS) as [ApplicationType, string][]);

  /** 申請人下拉選項：僅財務體系部門載入（其他人不呼叫，後端亦擋 403） */
  applicantOptions = toSignal(
    toObservable(this.canSeeApplicantFilter).pipe(
      switchMap(allowed => allowed ? this.service.getApplicants() : of([] as ApprovalTaskApplicant[])),
    ),
    {initialValue: [] as ApprovalTaskApplicant[]}
  );

  /** 已勾選的任務 key 集合，格式：${applicationType}:${id} */
  selectedKeys = signal<Set<string>>(new Set());

  /** 批次核准後需補填撥款/退款日的提醒清單（null = 不顯示 banner） */
  pendingPaymentResult = signal<BatchApprovePending[] | null>(null);

  /** 批次核准執行中，避免重複提交 */
  submitting = signal(false);

  /** 重新載入當頁資料的觸發訊號（批次核准完成後遞增） */
  private reloadTrigger = signal(0);

  switchTab(tab: 'pending' | 'approved' | 'rejected' | 'director_pending') {
    this.activeTab.set(tab);
    this.paymentStatus.set('');
    this.applicationTypeFilter.set('');
    this.submittedByFilter.set('');
    this.page.set(1);
    this.selectedKeys.set(new Set());
  }

  setPaymentStatus(status: '' | 'paid' | 'unpaid' | 'partial') {
    this.paymentStatus.set(status);
    this.page.set(1);
  }

  setApplicationTypeFilter(value: string) {
    this.applicationTypeFilter.set((value || '') as '' | ApplicationType);
    this.page.set(1);
  }

  setSubmittedByFilter(value: string) {
    this.submittedByFilter.set(value || '');
    this.page.set(1);
  }

  // ── 選取狀態 ──────────────────────────────────────────────────────────
  private keyOf(t: ApprovalTask): string { return `${t.applicationType}:${t.id}`; }

  isSelected(t: ApprovalTask): boolean { return this.selectedKeys().has(this.keyOf(t)); }

  toggleSelect(t: ApprovalTask): void {
    const key = this.keyOf(t);
    const next = new Set(this.selectedKeys());
    if (next.has(key)) next.delete(key); else next.add(key);
    this.selectedKeys.set(next);
  }

  isAllSelected = computed(() => {
    const tasks = this.pagedTasks();
    if (tasks.length === 0) return false;
    const selected = this.selectedKeys();
    return tasks.every(t => selected.has(this.keyOf(t)));
  });

  toggleSelectAll(): void {
    const tasks = this.pagedTasks();
    if (this.isAllSelected()) {
      const next = new Set(this.selectedKeys());
      tasks.forEach(t => next.delete(this.keyOf(t)));
      this.selectedKeys.set(next);
    } else {
      const next = new Set(this.selectedKeys());
      tasks.forEach(t => next.add(this.keyOf(t)));
      this.selectedKeys.set(next);
    }
  }

  /** 執行批次核准：發送 API → 顯示結果 Toast → 若有需補填項目則開啟 banner */
  submitBatchApprove(): void {
    if (this.submitting() || this.selectedKeys().size === 0) return;

    const items = Array.from(this.selectedKeys()).map(key => {
      const [applicationType, idStr] = key.split(':');
      return { applicationType, id: Number(idStr) };
    });

    this.submitting.set(true);
    this.service.batchApprove(items).subscribe({
      next: result => {
        const { succeeded, failed, pendingPayment } = result;
        if (failed.length === 0) {
          this.toastr.success(`已核准 ${succeeded} 筆`, '批次核准完成');
        } else {
          this.toastr.warning(`已核准 ${succeeded} 筆，失敗 ${failed.length} 筆：\n` +
            failed.slice(0, 3).map(f => `・${this.appTypeLabel[f.applicationType as keyof typeof this.appTypeLabel] ?? f.applicationType} #${f.id}：${f.reason}`).join('\n'),
            '批次核准完成', { enableHtml: false });
        }
        this.pendingPaymentResult.set(pendingPayment.length > 0 ? pendingPayment : null);
        this.selectedKeys.set(new Set());
        this.submitting.set(false);
        this.reloadTrigger.update(n => n + 1);
      },
      error: err => {
        this.submitting.set(false);
        const msg = err?.error?.message ?? '批次核准失敗';
        this.toastr.error(msg, '批次核准失敗');
      },
    });
  }

  dismissPendingReminder(): void { this.pendingPaymentResult.set(null); }

  private result = toSignal(
    combineLatest([
      toObservable(this.page),
      toObservable(this.activeTab),
      toObservable(this.paymentStatus),
      toObservable(this.applicationTypeFilter),
      toObservable(this.submittedByFilter),
      toObservable(this.reloadTrigger),
    ]).pipe(
      switchMap(([p, status, ps, at, sb]) => this.service.getPaged(p, this.PAGE_SIZE, status, ps || undefined, at || undefined, sb || undefined))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<ApprovalTask>}
  );

  pagedTasks  = computed(() => this.result().items);
  totalCount  = computed(() => this.result().totalCount);
  totalPages  = computed(() => this.result().totalPages);
  pageNumbers = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  readonly statusLabel    = TASK_STATUS_LABELS;
  readonly statusClass    = TASK_STATUS_CLASSES;
  readonly appTypeLabel   = APPLICATION_TYPE_LABELS;
  readonly appTypeClass   = APPLICATION_TYPE_CLASSES;
  readonly payTypeLabel   = PAYMENT_TYPE_LABELS;
  readonly leaveTypeLabel = LEAVE_TYPE_LABELS;

  /**
   * 取得已核准或審核中簽核作業的款項狀態（第二個 badge）。
   * status gate 與請款列表 paymentState() 一致：pending 或 approved 才顯示。
   * - 撥款類（payment_request / advance / travel / travel_payment）：依 paymentStatus 三態（Unpaid / PartiallyPaid / FullyPaid）對應灰 / 黃 / 綠
   * - 退款類（write_off / travel_write_off）：僅超支時適用，看 refundedAt 兩態
   * - 其他（leave / overtime / holiday_travel）：無款項概念，回傳 null
   */
  getPaymentBadge(t: ApprovalTask): { label: string; cls: string } | null {
    if (t.status !== 'pending' && t.status !== 'approved') return null;

    const installmentBadge = (status?: string) => {
      const s = (status as PaymentInstallmentStatus | undefined) ?? 'Unpaid';
      return { label: PAYMENT_INSTALLMENT_STATUS_LABELS[s], cls: PAYMENT_INSTALLMENT_STATUS_CLASSES[s] };
    };

    const refundBadge = (isRefunded: boolean) => isRefunded
      ? { label: PAYMENT_STATE_LABELS.paid,   cls: PAYMENT_STATE_CLASSES.paid }
      : { label: PAYMENT_STATE_LABELS.unpaid, cls: PAYMENT_STATE_CLASSES.unpaid };

    const type = t.applicationType;
    if (type === 'payment_request')  return installmentBadge(t.paymentDetail?.paymentStatus);
    if (type === 'advance')          return installmentBadge(t.advanceDetail?.paymentStatus);
    if (type === 'travel')           return installmentBadge(t.travelDetail?.paymentStatus);
    // holiday_travel：津貼隨次月薪資發放、不走撥款流程，故不顯示款項 badge
    if (type === 'holiday_travel')   return null;
    if (type === 'travel_payment')   return installmentBadge(t.travelPaymentDetail?.paymentStatus);
    if (type === 'write_off') {
      const d = t.writeOffDetail;
      if (!d) return null;
      const overspent = (d.advanceGrandTotal - d.otherWrittenOffTotal - d.grandTotal) < 0;
      if (!overspent) return null;
      return refundBadge(!!d.refundedAt);
    }
    if (type === 'travel_write_off') {
      const d = t.travelWriteOffDetail;
      if (!d) return null;
      const overspent = (d.travelGrandTotal - d.otherWrittenOffTotal - d.grandTotal) < 0;
      if (!overspent) return null;
      return refundBadge(!!d.refundedAt);
    }
    return null;
  }

  getSummary(t: ApprovalTask): string {
    if (t.paymentDetail) {
      const d = t.paymentDetail;
      const vendorPart = d.paymentType === 'vendor' && d.vendorName ? `・${d.vendorName}` : '';
      return `${this.payTypeLabel[d.paymentType]}・${d.projectCode}${vendorPart}（${d.totalAmount.toLocaleString()} 元）`;
    }
    if (t.leaveDetail) {
      return `${this.leaveTypeLabel[t.leaveDetail.leaveType]}・${t.leaveDetail.hours} 小時`;
    }
    // 假日執行活動：列出每位人員（申請人 + 參與者）的津貼預估
    if (t.applicationType === 'holiday_travel' && t.travelDetail) {
      const days = t.travelDetail.holidayDays ?? 0;
      const list = t.travelDetail.holidayAllowances ?? [];
      const head = `${t.travelDetail.destination}・${days} 天`;
      if (list.length === 0) return head;
      const parts = list.map(a => `${a.userName} ${a.allowance.toLocaleString()} 元`).join('、');
      return `${head}｜${parts}`;
    }
    if (t.travelDetail) {
      return `${t.travelDetail.destination}（${t.travelDetail.grandTotal.toLocaleString()} 元）`;
    }
    if (t.overtimeDetail) {
      const dateStr = new Date(t.overtimeDetail.overtimeDate).toLocaleDateString('zh-TW');
      return `${dateStr}・${t.overtimeDetail.estimatedHours} 小時・${t.overtimeDetail.reason}`;
    }
    // 預支：加註本次送簽的批次（第1次 / 第N次追加）；追加批次另列本次金額與總額
    if (t.advanceDetail) {
      const d = t.advanceDetail;
      const round = d.currentRoundNo ?? 1;
      const head = `${d.activityName}・${roundLabel(round)}`;
      if (round <= 1) return `${head}（${d.grandTotal.toLocaleString()} 元）`;
      const roundTotal = d.rounds?.find(r => r.roundNo === round)?.grandTotal;
      const roundPart = roundTotal != null ? `本次 ${roundTotal.toLocaleString()} 元／` : '';
      return `${head}（${roundPart}總額 ${d.grandTotal.toLocaleString()} 元）`;
    }
    if (t.travelPaymentDetail) {
      return `${t.travelPaymentDetail.destination}（${t.travelPaymentDetail.grandTotal.toLocaleString()} 元）`;
    }
    return '—';
  }

  /** 取得申請單號（無單號類型如請假 / 加班回空字串）*/
  getRequestNo(t: ApprovalTask): string {
    return t.paymentDetail?.requestNo
        ?? t.travelDetail?.requestNo
        ?? t.advanceDetail?.requestNo
        ?? t.travelPaymentDetail?.requestNo
        ?? t.writeOffDetail?.requestNo
        ?? t.travelWriteOffDetail?.requestNo
        ?? '';
  }
}

function buildPageNumbers(current: number, total: number): number[] {
  if (total <= 9) return Array.from({length: total}, (_, i) => i + 1);
  const pages: number[] = [];
  let prev = 0;
  for (let i = 1; i <= total; i++) {
    if (i === 1 || i === total || (i >= current - 2 && i <= current + 2)) {
      if (prev && i - prev > 1) pages.push(-1);
      pages.push(i);
      prev = i;
    }
  }
  return pages;
}
