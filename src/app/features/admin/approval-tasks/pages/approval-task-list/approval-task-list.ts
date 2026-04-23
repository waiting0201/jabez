import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {combineLatest} from 'rxjs';
import {switchMap} from 'rxjs/operators';
import {ToastrService} from 'ngx-toastr';
import {ApprovalTaskService, BatchApprovePending} from '../../services/approval-task.service';
import {
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
  ApprovalTask,
} from '../../models/approval-task.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {AuthService} from '../../../../../core/auth/services/auth.service';

/** 可看到撥款/退款篩選的部門代碼：總監室、行政財務部、會計部 */
const PAYMENT_FILTER_DEPT_CODES = new Set(['CEO', 'FIN', 'AC']);

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
    this.auth.isSuperAdmin() || PAYMENT_FILTER_DEPT_CODES.has(this.auth.departmentCode() ?? '')
  );

  /** 是否具備全選核准權限（待審核 tab 才啟用 UI） */
  canBatchApprove = computed(() =>
    this.auth.isSuperAdmin() || this.auth.hasPermission('approval-tasks:batch-approve')
  );

  readonly PAGE_SIZE = 20;
  activeTab = signal<'pending' | 'approved' | 'rejected'>('pending');
  paymentStatus = signal<'' | 'paid' | 'unpaid'>('');
  page = signal(1);

  /** 已勾選的任務 key 集合，格式：${applicationType}:${id} */
  selectedKeys = signal<Set<string>>(new Set());

  /** 批次核准後需補填撥款/退款日的提醒清單（null = 不顯示 banner） */
  pendingPaymentResult = signal<BatchApprovePending[] | null>(null);

  /** 批次核准執行中，避免重複提交 */
  submitting = signal(false);

  /** 重新載入當頁資料的觸發訊號（批次核准完成後遞增） */
  private reloadTrigger = signal(0);

  switchTab(tab: 'pending' | 'approved' | 'rejected') {
    this.activeTab.set(tab);
    this.paymentStatus.set('');
    this.page.set(1);
    this.selectedKeys.set(new Set());
  }

  setPaymentStatus(status: '' | 'paid' | 'unpaid') {
    this.paymentStatus.set(status);
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
      toObservable(this.reloadTrigger),
    ]).pipe(
      switchMap(([p, status, ps]) => this.service.getPaged(p, this.PAGE_SIZE, status, ps || undefined))
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
   * 取得已核准簽核作業的款項狀態（第二個 badge）。
   * - 撥款類（payment_request / advance / travel / holiday_travel）：看 paidAt
   * - 退款類（write_off / travel_write_off）：僅超支時適用，看 refundedAt
   * - 其他（leave / overtime）：無款項概念，回傳 null
   */
  getPaymentBadge(t: ApprovalTask): { label: string; cls: string } | null {
    if (t.status !== 'approved') return null;

    const type = t.applicationType;
    if (type === 'payment_request') {
      return t.paymentDetail?.paidAt
        ? { label: '款項已完成', cls: 'bg-success-subtle text-success' }
        : { label: '款項待處理', cls: 'bg-warning-subtle text-warning-emphasis' };
    }
    if (type === 'advance') {
      return t.advanceDetail?.paidAt
        ? { label: '款項已完成', cls: 'bg-success-subtle text-success' }
        : { label: '款項待處理', cls: 'bg-warning-subtle text-warning-emphasis' };
    }
    if (type === 'travel' || type === 'holiday_travel') {
      return t.travelDetail?.paidAt
        ? { label: '款項已完成', cls: 'bg-success-subtle text-success' }
        : { label: '款項待處理', cls: 'bg-warning-subtle text-warning-emphasis' };
    }
    if (type === 'write_off') {
      const d = t.writeOffDetail;
      if (!d) return null;
      const overspent = (d.advanceGrandTotal - d.otherWrittenOffTotal - d.grandTotal) < 0;
      if (!overspent) return null;
      return d.refundedAt
        ? { label: '款項已完成', cls: 'bg-success-subtle text-success' }
        : { label: '款項待處理', cls: 'bg-warning-subtle text-warning-emphasis' };
    }
    if (type === 'travel_write_off') {
      const d = t.travelWriteOffDetail;
      if (!d) return null;
      const overspent = (d.travelGrandTotal - d.otherWrittenOffTotal - d.grandTotal) < 0;
      if (!overspent) return null;
      return d.refundedAt
        ? { label: '款項已完成', cls: 'bg-success-subtle text-success' }
        : { label: '款項待處理', cls: 'bg-warning-subtle text-warning-emphasis' };
    }
    return null;
  }

  getSummary(t: ApprovalTask): string {
    if (t.paymentDetail) {
      return `${this.payTypeLabel[t.paymentDetail.paymentType]}・${t.paymentDetail.projectCode}（${t.paymentDetail.totalAmount.toLocaleString()} 元）`;
    }
    if (t.leaveDetail) {
      return `${this.leaveTypeLabel[t.leaveDetail.leaveType]}・${t.leaveDetail.hours} 小時`;
    }
    if (t.travelDetail) {
      return `${t.travelDetail.destination}（${t.travelDetail.grandTotal.toLocaleString()} 元）`;
    }
    if (t.overtimeDetail) {
      const dateStr = new Date(t.overtimeDetail.overtimeDate).toLocaleDateString('zh-TW');
      return `${dateStr}・${t.overtimeDetail.estimatedHours} 小時・${t.overtimeDetail.reason}`;
    }
    if (t.advanceDetail) {
      return `${t.advanceDetail.requestNo}・${t.advanceDetail.activityName}（${t.advanceDetail.grandTotal.toLocaleString()} 元）`;
    }
    return '—';
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
