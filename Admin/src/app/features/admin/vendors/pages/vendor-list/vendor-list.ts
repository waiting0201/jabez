import {Component, computed, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {ToastrService} from 'ngx-toastr';
import {VendorService} from '../../services/vendor.service';
import {Vendor} from '../../models/vendor.model';
import {PagedResult} from '@shared/models/paged-result.model';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-vendor-list',
  templateUrl: './vendor-list.html',
  imports: [FormsModule, RouterLink, DatePipe, HasPermissionDirective],
})
export class VendorList {
  private vendorService = inject(VendorService);
  private toastr = inject(ToastrService);

  readonly PAGE_SIZE = 20;
  page = signal(1);
  searchInput = '';
  searchTerm = signal('');
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({page: this.page(), search: this.searchTerm(), refresh: this.refresh()}))).pipe(
      switchMap(({page, search}) => this.vendorService.getPaged(page, this.PAGE_SIZE, search || undefined))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<Vendor>}
  );

  vendors    = computed(() => this.result().items);
  totalCount = computed(() => this.result().totalCount);
  totalPages = computed(() => this.result().totalPages);
  pageNumbers = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  doSearch() {
    this.searchTerm.set(this.searchInput.trim());
    this.page.set(1);
  }

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  delete(v: Vendor) {
    if (!confirm(`確定要刪除廠商「${v.name}」嗎？`)) return;
    this.vendorService.delete(v.id).subscribe({
      next: () => {
        this.toastr.success(`已刪除廠商「${v.name}」。`);
        // 刪掉當頁最後一筆時退回前一頁，避免停在空白頁
        if (this.vendors().length === 1 && this.page() > 1) this.page.update(p => p - 1);
        else this.refresh.update(n => n + 1);
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || '刪除失敗，請稍後再試。';
        this.toastr.error(msg, '無法刪除');
      },
    });
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
