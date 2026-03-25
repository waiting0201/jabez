import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {combineLatest} from 'rxjs';
import {switchMap} from 'rxjs/operators';
import {ApprovalTaskService} from '../../services/approval-task.service';
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

  /** Superadmin 或總監室/財務部/會計部才顯示撥款/退款子篩選 */
  canSeePaymentFilter = computed(() =>
    this.auth.isSuperAdmin() || PAYMENT_FILTER_DEPT_CODES.has(this.auth.departmentCode() ?? '')
  );

  readonly PAGE_SIZE = 20;
  activeTab = signal<'pending' | 'approved'>('pending');
  paymentStatus = signal<'' | 'paid' | 'unpaid'>('');
  page = signal(1);

  switchTab(tab: 'pending' | 'approved') {
    this.activeTab.set(tab);
    this.paymentStatus.set('');
    this.page.set(1);
  }

  setPaymentStatus(status: '' | 'paid' | 'unpaid') {
    this.paymentStatus.set(status);
    this.page.set(1);
  }

  private result = toSignal(
    combineLatest([toObservable(this.page), toObservable(this.activeTab), toObservable(this.paymentStatus)]).pipe(
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
