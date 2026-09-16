import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import * as XLSX from 'xlsx';
import {environment} from '@/environments/environment';
import {ToastrService} from 'ngx-toastr';
import {AuthService} from '@/app/core/auth/services/auth.service';
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
  /** 補償方式（compensatory 補休 / pay 加班費） */
  compensationType: 'compensatory' | 'pay';
  /** 加班費快照（補休型為 null） */
  overtimePayAmount: number | null;
}

/**
 * 單次 Excel 匯出的筆數上限，與後端 OvertimeReportHandler.ExportMaxPageSize 相同。
 * 兩處必須一起改 —— 前端送得比後端上限大時會被 clamp 回去並靜默截斷。
 */
const EXPORT_MAX_ROWS = 5000;

@Component({
  selector: 'app-overtime-report',
  templateUrl: './overtime-report.html',
  imports: [CommonModule, FormsModule],
})
export class OvertimeReport implements OnInit {
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);

  /**
   * 「加班費」欄為薪資性資訊（依核准當下底薪試算），需獨立權限 reports-overtime:amount。
   * 用 component 欄位而非 *appHasPermission —— <th> / <td> / 空列 colspan / Excel 匯出
   * 四處要共用同一個真相，漏改任一處就跑版或外洩。
   * 後端 OvertimeReportHandler 亦會對無此權限者把 overtimePayAmount 抹為 null（縱深防禦）。
   */
  readonly canSeeAmount = inject(AuthService).hasPermission('reports-overtime:amount');

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
            // 這兩欄漏了會靜默顯示錯誤：compensationType 為 undefined 時 badge 一律落到「補休」，
            // 選加班費的單看起來像選了補休；overtimePayAmount 為 undefined 則讓「加班費」欄印出空白而非「—」
            // ⚠ overtimePayAmount 受 reports-overtime:amount 管制 —— 無權者後端已回 null，
            //   此處照收即可（畫面由 canSeeAmount 整欄隱藏）。
            //   ★ 動到本欄時，exportExcel() 的 wsData 是另一份獨立欄位表，務必一起改。
            compensationType: r.compensationType === 'pay' ? 'pay' : 'compensatory',
            overtimePayAmount: r.overtimePayAmount ?? null,
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

    // export=true 讓後端放寬 pageSize 上限（一般列表仍為 100），避免匯出被截斷。
    // 原本送 pageSize: 9999 但後端 Math.Clamp(ps, 1, 100) 會壓回 100 ——
    // 匯出永遠只有前 100 筆且毫無提示（2026-09 修正，上限見 OvertimeReportHandler.ExportMaxPageSize）。
    const params: any = {page: 1, pageSize: EXPORT_MAX_ROWS, export: 'true'};
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

        // 仍超出單次上限時明講，不要再讓使用者拿到一份「看起來完整」的殘缺報表
        const total = data?.totalCount ?? items.length;
        if (total > items.length) {
          this.toastr.warning(
            `本次查詢共 ${total} 筆，超出單次匯出上限 ${EXPORT_MAX_ROWS} 筆，僅匯出前 ${items.length} 筆。請縮小日期區間後分次匯出。`,
            '匯出不完整'
          );
        }

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
            '補償方式': r.compensationType === 'pay' ? '加班費' : '補休',
            // ★ 這是本檔第二份獨立欄位表（另一份在 fetchData()）。
            //   「加班費」受 reports-overtime:amount 管制，漏改這裡＝Excel 外洩＝等於沒擋。
            //   用條件式 spread 而非填空字串：json_to_sheet 對 undefined / '' 仍會建出一整欄空白，
            //   會被讀成「這個月都是 0」。spread 不影響欄序（欄序取自第一筆物件的 key 順序）。
            ...(this.canSeeAmount
              ? {'加班費': r.overtimePayAmount != null ? Number(r.overtimePayAmount) : ''}
              : {}),
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
