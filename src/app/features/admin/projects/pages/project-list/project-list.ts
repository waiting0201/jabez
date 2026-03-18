import {Component, computed, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {ProjectService} from '../../services/project.service';
import {Project, PROJECT_STATUS_LABELS, PROJECT_STATUS_CLASSES} from '../../models/project.model';
import {PagedResult} from '../../../../../shared/models/paged-result.model';

@Component({
  selector: 'app-project-list',
  templateUrl: './project-list.html',
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
})
export class ProjectList {
  private projectService = inject(ProjectService);

  readonly PAGE_SIZE = 20;
  page = signal(1);
  searchInput = '';
  selectedYearInput: number | undefined;
  selectedStatusInput: string | undefined;
  private searchTerm = signal('');
  private filterYear = signal<number | undefined>(undefined);
  private filterStatus = signal<string | undefined>(undefined);
  yearOptions = toSignal(this.projectService.getYears(), {initialValue: [] as number[]});

  private refresh = signal(0);

  private result = toSignal(
    toObservable(computed(() => ({ page: this.page(), search: this.searchTerm(), year: this.filterYear(), status: this.filterStatus(), refresh: this.refresh() }))).pipe(
      switchMap(({ page, search, year, status }) => this.projectService.getPaged(page, this.PAGE_SIZE, search || undefined, year, status))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 1} as PagedResult<Project>}
  );

  pagedProjects = computed(() => this.result().items);
  totalCount    = computed(() => this.result().totalCount);
  totalPages    = computed(() => this.result().totalPages);
  pageNumbers   = computed(() => buildPageNumbers(this.page(), this.totalPages()));

  readonly statusLabel = PROJECT_STATUS_LABELS;
  readonly statusClass = PROJECT_STATUS_CLASSES;

  doSearch() {
    this.searchTerm.set(this.searchInput);
    this.filterYear.set(this.selectedYearInput);
    this.filterStatus.set(this.selectedStatusInput);
    this.page.set(1);
  }
  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  delete(project: Project) {
    if (project.status === 'closed') return;
    if (confirm(`確定要刪除專案「${project.code}」嗎？`)) {
      this.projectService.delete(project.id).subscribe(() => {
        this.refresh.update(v => v + 1);
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
