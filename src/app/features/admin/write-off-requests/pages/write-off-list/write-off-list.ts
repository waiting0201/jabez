import {Component, computed, effect, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {WriteOffRequestService} from '../../services/write-off-request.service';
import {WriteOffRequest, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/write-off-request.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {AuthService} from '@core/auth/services/auth.service';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

interface WriteOffGroup {
  key: number;                      // advanceRequestId
  advanceRequestNo: string;
  projectCode: string;
  count: number;
  advanceGrandTotal: number;
  advanceWrittenOffTotal: number;
  items: WriteOffRequest[];
}

@Component({
  selector: 'app-write-off-list',
  templateUrl: './write-off-list.html',
  imports: [RouterLink, DecimalPipe, DatePipe, HasPermissionDirective],
})
export class WriteOffList {
  private service = inject(WriteOffRequestService);
  private auth = inject(AuthService);

  canWrite()  { return this.auth.hasPermission('write-off-requests:write'); }
  canDelete() { return this.auth.hasPermission('write-off-requests:delete'); }

  readonly PAGE_SIZE = 20;
  page = signal(1);
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({page: this.page(), refresh: this.refresh()}))).pipe(
      switchMap(({page}) => this.service.getPaged(page, this.PAGE_SIZE))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<WriteOffRequest>}
  );

  pagedRequests = computed(() => this.result().items);
  totalCount    = computed(() => this.result().totalCount);
  totalPages    = computed(() => this.result().totalPages);
  pageNumbers   = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  // 依預支單分組（保持伺服器排序，不重排）
  groups = computed<WriteOffGroup[]>(() => {
    const map = new Map<number, WriteOffGroup>();
    for (const r of this.pagedRequests()) {
      let g = map.get(r.advanceRequestId);
      if (!g) {
        g = {
          key: r.advanceRequestId,
          advanceRequestNo: r.advanceRequestNo,
          projectCode: r.projectCode,
          count: 0,
          advanceGrandTotal: r.advanceGrandTotal,
          advanceWrittenOffTotal: r.advanceWrittenOffTotal,
          items: [],
        };
        map.set(r.advanceRequestId, g);
      }
      g.items.push(r);
      g.count++;
    }
    return [...map.values()];
  });

  private collapsed = signal<Set<number>>(new Set());

  isExpanded(key: number) { return !this.collapsed().has(key); }

  toggle(key: number) {
    this.collapsed.update(set => {
      const next = new Set(set);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }

  constructor() {
    // 資料變動（換頁、刪除 refresh）後，所有多筆群組重置為「預設收合」
    effect(() => {
      const gs = this.groups();
      const next = new Set<number>();
      for (const g of gs) if (g.count >= 2) next.add(g.key);
      this.collapsed.set(next);
    });
  }

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  delete(r: WriteOffRequest) {
    if (confirm('確定要刪除此預支沖銷申請嗎？')) {
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
