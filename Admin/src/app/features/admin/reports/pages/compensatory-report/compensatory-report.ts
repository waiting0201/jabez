import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import * as XLSX from 'xlsx';
import {environment} from '@/environments/environment';
import {ToastrService} from 'ngx-toastr';
import {AuthService} from '@/app/core/auth/services/auth.service';
import {monthToRange} from '@/app/features/admin/reports/utils/date-range';

/** 加班補休時數總表：一位員工一列（對應後端 CompensatoryReportRowDto） */
export interface CompensatoryReportRow {
  employeeId: string;
  employeeName: string;
  departmentId: number | null;
  departmentName: string | null;
  /** 期間取得：1~2 小時（×1.34） */
  tier134Hours: number;
  /** 期間取得：3~8 小時（×1.67；平日第 3 小時起皆屬此級） */
  tier167Hours: number;
  /** 期間取得：9 小時起（×2.67，僅休假日） */
  tier267Hours: number;
  periodEarnedHours: number;
  periodUsedHours: number;
  /** 截至今日待補休（與個人資訊頁、請假表單同一公式） */
  availableHours: number;
  /** 期間取得換算金額；無 reports-overtime:amount 者後端回 null */
  amount: number | null;
}

@Component({
  selector: 'app-compensatory-report',
  templateUrl: './compensatory-report.html',
  imports: [CommonModule, FormsModule],
})
export class CompensatoryReport implements OnInit {
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);

  /**
   * 「金額」欄沿用加班紀錄的欄位級權限 reports-overtime:amount。
   * <th> / <td> / 合計列 / 空列 colspan / Excel 匯出共用這一個真相；後端亦會抹除（縱深防禦）。
   */
  readonly canSeeAmount = inject(AuthService).hasPermission('reports-overtime:amount');

  /** 期間（預設本月） */
  dateFrom = signal('');
  dateTo = signal('');

  /** 部門 / 員工篩選：前端以回傳資料過濾（不另打 API，/departments 需 departments:read） */
  selectedDepartmentId = signal('');
  selectedEmployeeId = signal('');

  rows = signal<CompensatoryReportRow[]>([]);
  loading = signal(false);

  /** 部門選項：由回傳資料去重產生（已受後端部門可見性限縮） */
  departments = computed(() => {
    const map = new Map<string, string>();
    for (const r of this.rows()) map.set(String(r.departmentId ?? ''), r.departmentName ?? '未分部門');
    return [...map.entries()]
      .map(([id, name]) => ({id, name}))
      .sort((a, b) => a.name.localeCompare(b.name, 'zh-Hant'));
  });

  /** 員工選項：隨部門連動 */
  employees = computed(() => {
    const dept = this.selectedDepartmentId();
    return this.rows().filter(r => !dept || String(r.departmentId ?? '') === dept);
  });

  filteredRows = computed(() => {
    const emp = this.selectedEmployeeId();
    return this.employees().filter(r => !emp || r.employeeId === emp);
  });

  totals = computed(() => {
    const t = {tier134Hours: 0, tier167Hours: 0, tier267Hours: 0, periodEarnedHours: 0, periodUsedHours: 0, availableHours: 0, amount: 0};
    for (const r of this.filteredRows()) {
      t.tier134Hours += r.tier134Hours;
      t.tier167Hours += r.tier167Hours;
      t.tier267Hours += r.tier267Hours;
      t.periodEarnedHours += r.periodEarnedHours;
      t.periodUsedHours += r.periodUsedHours;
      t.availableHours += r.availableHours;
      t.amount += r.amount ?? 0;
    }
    return t;
  });

  ngOnInit() {
    const now = new Date();
    const range = monthToRange(now.getFullYear(), now.getMonth() + 1);
    this.dateFrom.set(range.dateFrom);
    this.dateTo.set(range.dateTo);
    this.search();
  }

  onDepartmentChange(value: string) {
    this.selectedDepartmentId.set(value);
    // 已選員工不在新部門內時清掉，避免篩出空表卻看不出原因
    if (this.selectedEmployeeId() && !this.employees().some(e => e.employeeId === this.selectedEmployeeId())) {
      this.selectedEmployeeId.set('');
    }
  }

  search() {
    if (!this.dateFrom() || !this.dateTo()) {
      this.toastr.warning('請選擇期間起迄日');
      return;
    }
    if (this.dateFrom() > this.dateTo()) {
      this.toastr.warning('迄日不得早於起日');
      return;
    }
    this.loading.set(true);
    const params = {dateFrom: this.dateFrom(), dateTo: this.dateTo()};
    this.http.get<any>(`${environment.apiUrl}/reports/compensatory`, {params}).subscribe({
      next: (res) => {
        const items = res?.data ?? res ?? [];
        this.rows.set(items.map((r: any) => ({
          employeeId: r.employeeId,
          employeeName: r.employeeName ?? '—',
          departmentId: r.departmentId ?? null,
          departmentName: r.departmentName ?? null,
          tier134Hours: Number(r.tier134Hours ?? 0),
          tier167Hours: Number(r.tier167Hours ?? 0),
          tier267Hours: Number(r.tier267Hours ?? 0),
          periodEarnedHours: Number(r.periodEarnedHours ?? 0),
          periodUsedHours: Number(r.periodUsedHours ?? 0),
          availableHours: Number(r.availableHours ?? 0),
          // ★ 受 reports-overtime:amount 管制；exportExcel() 是另一份獨立欄位表，動到本欄時兩處一起改
          amount: r.amount ?? null,
        })));
        // 重新查詢後原選項可能已不存在
        if (this.selectedDepartmentId() && !this.departments().some(d => d.id === this.selectedDepartmentId())) {
          this.selectedDepartmentId.set('');
        }
        if (this.selectedEmployeeId() && !this.employees().some(e => e.employeeId === this.selectedEmployeeId())) {
          this.selectedEmployeeId.set('');
        }
        this.loading.set(false);
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
      },
    });
  }

  exportExcel() {
    const wsData = this.filteredRows().map(r => ({
      '部門': r.departmentName ?? '',
      '員工姓名': r.employeeName,
      '1~2小時(×1.34)': r.tier134Hours,
      '3~8小時(×1.67)': r.tier167Hours,
      '9~12小時(×2.67)': r.tier267Hours,
      '期間取得合計': r.periodEarnedHours,
      '期間已休': r.periodUsedHours,
      '待補休(截至今日)': r.availableHours,
      // 條件式 spread：無權者整欄不存在（填空字串會建出一整欄空白，被讀成 0）
      ...(this.canSeeAmount ? {'金額': r.amount ?? ''} : {}),
    }));
    const ws = XLSX.utils.json_to_sheet(wsData);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, '加班補休時數總表');
    XLSX.writeFile(wb, `加班補休時數總表_${this.dateFrom()}_${this.dateTo()}.xlsx`);
  }
}
