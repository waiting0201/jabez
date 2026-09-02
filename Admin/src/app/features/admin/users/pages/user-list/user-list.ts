import {Component, computed, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {catchError, switchMap} from 'rxjs/operators';
import {of} from 'rxjs';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {User} from '../../models/user.model';
import {Role} from '../../../roles/models/role.model';
import {Department} from '../../../departments/models/department.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.html',
  imports: [FormsModule, RouterLink, DatePipe, HasPermissionDirective],
})
export class UserList {
  private userService = inject(UserService);
  private roleService = inject(RoleService);
  private deptService = inject(DepartmentService);

  private roles = toSignal(
    this.roleService.getAll().pipe(catchError(() => of([] as Role[]))),
    {initialValue: [] as Role[]},
  );

  departments = toSignal(
    this.deptService.getAll().pipe(catchError(() => of([] as Department[]))),
    {initialValue: [] as Department[]},
  );

  readonly PAGE_SIZE = 20;
  page = signal(1);
  searchInput = '';
  searchTerm = signal('');
  departmentId = signal<number | null>(null);
  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({
      page: this.page(),
      search: this.searchTerm(),
      departmentId: this.departmentId(),
      refresh: this.refresh(),
    }))).pipe(
      switchMap(({ page, search, departmentId }) =>
        this.userService.getPaged(page, this.PAGE_SIZE, search || undefined, departmentId ?? undefined))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<User>}
  );

  pagedUsers  = computed(() => this.result().items);
  totalCount  = computed(() => this.result().totalCount);
  totalPages  = computed(() => this.result().totalPages);
  pageNumbers = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  /** 是否處於篩選狀態（供無資料時的文案切換） */
  isFiltered = computed(() => !!this.searchTerm() || this.departmentId() !== null);

  doSearch() {
    this.searchTerm.set(this.searchInput.trim());
    this.page.set(1);
  }

  onDepartmentChange(value: string) {
    this.departmentId.set(value ? Number(value) : null);
    this.page.set(1);
  }

  resetFilter() {
    this.searchInput = '';
    this.searchTerm.set('');
    this.departmentId.set(null);
    this.page.set(1);
  }

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  getRoleNames(roleIds: string[]): string {
    const r = this.roles();
    return roleIds.map(id => r.find(role => role.id === id)?.name ?? id).join(', ');
  }

  delete(user: User) {
    if (confirm(`確定要刪除員工「${user.name}」嗎？`)) {
      this.userService.delete(user.id).subscribe(() => {
        if (this.pagedUsers().length === 1 && this.page() > 1) this.page.update(p => p - 1);
        else this.refresh.update(v => v + 1);
      });
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
