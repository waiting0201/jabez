import {Component, computed, inject, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {DatePipe, CurrencyPipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {User} from '../../models/user.model';
import {Role} from '../../../roles/models/role.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.html',
  imports: [RouterLink, DatePipe, CurrencyPipe],
})
export class UserList {
  private userService = inject(UserService);
  private roleService = inject(RoleService);

  private roles = toSignal(this.roleService.getAll(), {initialValue: [] as Role[]});

  readonly PAGE_SIZE = 20;
  page = signal(1);
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({ page: this.page(), refresh: this.refresh() }))).pipe(
      switchMap(({ page }) => this.userService.getPaged(page, this.PAGE_SIZE))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<User>}
  );

  pagedUsers  = computed(() => this.result().items);
  totalCount  = computed(() => this.result().totalCount);
  totalPages  = computed(() => this.result().totalPages);
  pageNumbers = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  getRoleNames(roleIds: string[]): string {
    const r = this.roles();
    return roleIds.map(id => r.find(role => role.id === id)?.name ?? id).join(', ');
  }

  delete(user: User) {
    if (confirm(`確定要刪除員工「${user.name}」嗎？`)) {
      this.userService.delete(user.id).subscribe(() => this.refresh.update(v => v + 1));
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
