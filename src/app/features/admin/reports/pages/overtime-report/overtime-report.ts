import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import {dayToRange, FilterMode, monthToRange, shiftDateString, snapToIsoWeek, todayString} from '@/app/features/admin/reports/utils/date-range';

export interface OvertimeReportRow {
  id: number;
  employeeName: string;
  overtimeDate: string;
  projectCodes: string[];
  projectNames: string[];
  estimatedHours: string;
  actualHours: string | null;
  reason: string;
}

@Component({
  selector: 'app-overtime-report',
  templateUrl: './overtime-report.html',
  imports: [CommonModule, FormsModule],
})
export class OvertimeReport implements OnInit {
  private http = inject(HttpClient);

  /** 篩選條件 */
  selectedEmployeeId = signal('');
  selectedProjectId = signal('');

  /** 時段模式：日 / 週 / 月（預設月） */
  filterMode = signal<FilterMode>('month');
  selectedDate = signal('');
  /** 週模式：'YYYY-MM-DD'（任一天，由系統 snap 到該週週一→週日） */
  selectedWeekDate = signal('');
  selectedYear = signal('');
  selectedMonth = signal('');

  /** 員工清單 */
  employees = signal<{id: string; code: string; name: string}[]>([]);

  /** 專案清單 */
  projects = signal<{id: number; code: string}[]>([]);

  /** 年份選項 */
  years = signal<number[]>([]);

  /** 月份選項 */
  months = Array.from({length: 12}, (_, i) => i + 1);

  /** 紀錄 */
  records = signal<OvertimeReportRow[]>([]);
  loading = signal(false);

  /** 分頁 */
  currentPage = signal(1);
  totalCount = signal(0);
  totalPages = signal(1);
  private pageSize = 20;

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    const today = todayString(now);
    this.selectedDate.set(today);
    this.selectedWeekDate.set(today);
    this.loadEmployees();
    this.loadProjects();
    this.search();
  }

  /** 週模式 snap 結果（含週號 / 起訖日） */
  weekRange = computed(() => snapToIsoWeek(this.selectedWeekDate()));

  private computeDateRange(): { dateFrom: string; dateTo: string } | null {
    const mode = this.filterMode();
    if (mode === 'day') return dayToRange(this.selectedDate());
    if (mode === 'week') {
      const r = this.weekRange();
      return r ? { dateFrom: r.dateFrom, dateTo: r.dateTo } : null;
    }
    const year = Number(this.selectedYear());
    const month = Number(this.selectedMonth());
    if (!year || !month) return null;
    return monthToRange(year, month);
  }

  shiftWeek(days: number) {
    const cur = this.selectedWeekDate();
    if (!cur) return;
    this.selectedWeekDate.set(shiftDateString(cur, days));
  }

  resetToThisWeek() {
    this.selectedWeekDate.set(todayString());
  }

  loadEmployees() {
    // 套用部門 scope 過濾，避免下拉顯示無資料權限的員工
    this.http.get<any>(`${environment.apiUrl}/users/lookup?scope=department`).subscribe({
      next: (res) => {
        const items = res?.data ?? res?.items ?? res ?? [];
        this.employees.set(
          items.map((u: any) => ({id: u.id, code: u.employeeCode ?? '', name: u.name}))
        );
      },
    });
  }

  loadProjects() {
    this.http.get<any>(`${environment.apiUrl}/projects`).subscribe({
      next: (res) => {
        const items = res?.data?.items ?? res?.items ?? res ?? [];
        this.projects.set(
          items.map((p: any) => ({id: p.id, code: p.code}))
        );
      },
    });
  }

  search() {
    this.currentPage.set(1);
    this.fetchData();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
    this.fetchData();
  }

  private fetchData() {
    this.loading.set(true);

    const params: any = {page: this.currentPage(), pageSize: this.pageSize};
    if (this.selectedEmployeeId()) params.employeeId = this.selectedEmployeeId();
    if (this.selectedProjectId()) params.projectId = this.selectedProjectId();
    const range = this.computeDateRange();
    if (range) {
      params.dateFrom = range.dateFrom;
      params.dateTo = range.dateTo;
    }

    this.http.get<any>(`${environment.apiUrl}/reports/overtime`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        this.totalCount.set(data?.totalCount ?? 0);
        this.totalPages.set(data?.totalPages ?? 1);

        this.records.set(
          items.map((r: any) => ({
            id: r.id,
            employeeName: r.employeeName ?? '—',
            overtimeDate: r.overtimeDate ? new Date(r.overtimeDate).toLocaleDateString('zh-TW') : '',
            projectCodes: r.projectCodes ?? [],
            projectNames: r.projectNames ?? [],
            estimatedHours: Number(r.estimatedHours).toFixed(1),
            actualHours: r.actualHours != null ? Number(r.actualHours).toFixed(1) : null,
            reason: r.reason ?? '',
          }))
        );
        this.loading.set(false);
      },
      error: () => {
        this.records.set([]);
        this.loading.set(false);
      },
    });
  }
}
