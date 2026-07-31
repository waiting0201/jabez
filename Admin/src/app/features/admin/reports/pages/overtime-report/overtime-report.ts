import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import * as XLSX from 'xlsx';
import {environment} from '@/environments/environment';
import {dayToRange, FilterMode, monthToRange, shiftDateString, snapToIsoWeek, todayString} from '@/app/features/admin/reports/utils/date-range';

/** 加班單的關聯專案明細（含該案預估時數） */
export interface OvertimeReportProject {
  projectCode: string;
  projectName: string;
  estimatedHours: string;
}

export interface OvertimeReportRow {
  id: number;
  employeeName: string;
  overtimeDate: string;
  projects: OvertimeReportProject[];
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
  projects = signal<{id: number; code: string; name: string}[]>([]);

  /** 年份選項 */
  years = signal<number[]>([]);

  /** 月份選項 */
  months = Array.from({length: 12}, (_, i) => i + 1);

  /** 紀錄 */
  records = signal<OvertimeReportRow[]>([]);
  loading = signal(false);
  exporting = signal(false);

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

  /** 匯出檔名後綴：依 mode 給友善字串 */
  private exportSuffix(): string {
    const mode = this.filterMode();
    if (mode === 'day') return this.selectedDate() || '全部';
    if (mode === 'week') {
      const r = this.weekRange();
      return r ? `${r.isoYear}-W${String(r.weekNumber).padStart(2, '0')}` : '全部';
    }
    const year = this.selectedYear() || '全部';
    const month = this.selectedMonth() || '全部';
    return `${year}-${String(month).padStart(2, '0')}`;
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
          items.map((p: any) => ({id: p.id, code: p.code, name: p.name ?? ''}))
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
            projects: (r.projects ?? []).map((p: any) => ({
              projectCode: p.projectCode,
              projectName: p.projectName,
              estimatedHours: Number(p.estimatedHours).toFixed(1),
            })),
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

  exportExcel() {
    this.exporting.set(true);

    const params: any = {page: 1, pageSize: 9999};
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
        const wsData = items.map((r: any) => {
          // 專案沿用單欄合併文字：「PJ001 專案甲 2.5h、PJ002 專案乙 1.5h」
          const projectText = (r.projects ?? [])
            .map((p: any) => `${p.projectCode}${p.projectName ? ' ' + p.projectName : ''} ${Number(p.estimatedHours).toFixed(1)}h`)
            .join('、');
          return {
            '員工姓名': r.employeeName ?? '—',
            '加班日期': r.overtimeDate ? new Date(r.overtimeDate).toLocaleDateString('zh-TW') : '',
            '專案': projectText,
            '預估總時數': r.estimatedHours != null ? Number(r.estimatedHours).toFixed(1) : '',
            '實際時數': r.actualHours != null ? Number(r.actualHours).toFixed(1) : '',
            '事由': r.reason ?? '',
          };
        });

        const ws = XLSX.utils.json_to_sheet(wsData);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, '加班紀錄');

        XLSX.writeFile(wb, `加班紀錄_${this.exportSuffix()}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exporting.set(false);
      },
    });
  }
}
