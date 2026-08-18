import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveRequest, LeaveType,
  LEAVE_TYPE_LABELS, LEAVE_TYPE_CLASSES, LEAVE_TIME_UNIT,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  formatLeaveDuration,
} from '../../models/leave-request.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {AuthService} from '@core/auth/services/auth.service';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-leave-request-list',
  templateUrl: './leave-request-list.html',
  imports: [RouterLink, DatePipe, HasPermissionDirective],
})
export class LeaveRequestList {
  private service = inject(LeaveRequestService);
  private auth = inject(AuthService);

  canWrite()  { return this.auth.hasPermission('leave-requests:write'); }
  canDelete() { return this.auth.hasPermission('leave-requests:delete'); }

  readonly PAGE_SIZE = 20;
  page = signal(1);
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({ page: this.page(), refresh: this.refresh() }))).pipe(
      switchMap(({ page }) => this.service.getPaged(page, this.PAGE_SIZE))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<LeaveRequest>}
  );

  pagedRequests = computed(() => this.result().items);
  totalCount    = computed(() => this.result().totalCount);
  totalPages    = computed(() => this.result().totalPages);
  pageNumbers   = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  readonly typeLabel   = LEAVE_TYPE_LABELS;
  readonly typeClass   = LEAVE_TYPE_CLASSES;
  readonly timeUnit    = LEAVE_TIME_UNIT;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  /** 依假別單位格式化時數 */
  formatDuration(leaveType: string, hours: number): string {
    return formatLeaveDuration(leaveType as LeaveType, hours);
  }

  /** 可銷假：已核准且假期尚未結束（後端 LoadRevocableLeaveAsync 有相同守門） */
  /**
   * 育嬰留職停薪（長期）暫不開放銷假：非工作日型假別會逐日展開整段日曆天，
   * 2 年留停會在銷假頁產出 700+ 個逐日 chip，UI 無法使用。
   * 提前復職請洽人事以編輯／重新申請處理。彈性單日（parental_leave_daily）不受此限。
   */
  canRevoke(r: LeaveRequest): boolean {
    return this.canWrite()
        && r.approvalStatus === 'approved'
        && r.leaveType !== 'parental_leave'
        && new Date(r.endDate).getTime() >= Date.now();
  }

  /** 部分銷假：曾銷假（originalHours 有值）但尚未全數取消 */
  isPartiallyRevoked(r: LeaveRequest): boolean {
    return r.originalHours != null && r.approvalStatus === 'approved' && r.originalHours > r.hours;
  }

  revokedHours(r: LeaveRequest): number {
    return r.originalHours != null ? r.originalHours - r.hours : 0;
  }

  delete(r: LeaveRequest) {
    if (confirm(`確定要刪除此請假申請嗎？`)) {
      this.service.delete(r.id).subscribe(() => this.refresh.update(v => v + 1));
    }
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
