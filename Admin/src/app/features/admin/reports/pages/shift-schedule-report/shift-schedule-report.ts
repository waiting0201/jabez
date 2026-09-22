import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import * as XLSX from 'xlsx';
import {environment} from '@/environments/environment';

/** 與後端 WorkDayTypes 一一對應 */
type ShiftDayType = 'work' | 'rest_day' | 'statutory_off' | 'public_holiday';

interface OverviewCell {
  day: number;
  dayType: ShiftDayType;
  isActivityDay: boolean;
  isActivityAssignee: boolean;
}

interface OverviewRow {
  userId: string;
  userName: string;
  departmentName: string | null;
  statutoryOffCount: number;
  restDayCount: number;
  requiredStatutoryOff: number;
  requiredRestDay: number;
  quotaSatisfied: boolean;
  cells: OverviewCell[];
}

interface DayStat {
  day: number;
  date: string;
  weekday: number;
  isPublicHoliday: boolean;
  holidayName: string | null;
  workingCount: number;
  /** 週一至週五卻無人出勤 → 警示（週末與國定假日不算異常） */
  noCoverage: boolean;
}

interface Overview {
  year: number;
  month: number;
  daysInMonth: number;
  departmentId: number | null;
  dayStats: DayStat[];
  rows: OverviewRow[];
}

interface DeptLookup { id: number; name: string; parentId: number | null; }

const WEEKDAY_LABELS = ['日', '一', '二', '三', '四', '五', '六'];

/** 格子文字：只用一個字，31 欄才排得下 */
const CELL_TEXT: Record<ShiftDayType, string> = {
  work: '', statutory_off: '例', rest_day: '休', public_holiday: '節',
};

/**
 * 〈出勤／排休總覽表〉（四週彈性工時功能 B）。
 *
 * 一人一列 × 當月每日一欄，**三色**標示例假／休假／活動日（活動日是疊加旗標，用左側色條而非換底色）。
 * 表尾為每日出勤人數，週一至週五全員皆休者標紅警示。
 */
@Component({
  selector: 'app-shift-schedule-report',
  templateUrl: './shift-schedule-report.html',
  styleUrl: './shift-schedule-report.scss',
  imports: [CommonModule, FormsModule],
})
export class ShiftScheduleReport implements OnInit {
  private http = inject(HttpClient);
  private toastr = inject(ToastrService);

  readonly weekdayLabels = WEEKDAY_LABELS;
  readonly cellText = CELL_TEXT;

  year = signal(new Date().getFullYear());
  month = signal(new Date().getMonth() + 1);
  departmentId = signal<number | null>(null);

  loading = signal(false);
  exporting = signal(false);
  data = signal<Overview | null>(null);
  departments = signal<DeptLookup[]>([]);

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y - 1, y, y + 1];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  /** 尚未排滿配額的人數 —— 開放期內主管催排班的依據 */
  readonly unsatisfiedCount = computed(() =>
    (this.data()?.rows ?? []).filter(r => !r.quotaSatisfied).length);

  readonly noCoverageDays = computed(() =>
    (this.data()?.dayStats ?? []).filter(d => d.noCoverage));

  ngOnInit(): void {
    this.http.get<DeptLookup[]>(`${environment.apiUrl}/departments/lookup`)
      .subscribe({next: d => this.departments.set(d ?? []), error: () => {}});

    const now = new Date();
    const next = new Date(now.getFullYear(), now.getMonth() + 1, 1);
    this.year.set(next.getFullYear());
    this.month.set(next.getMonth() + 1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const params: Record<string, string | number> = {year: this.year(), month: this.month()};
    const dept = this.departmentId();
    if (dept) params['departmentId'] = dept;

    this.http.get<Overview>(`${environment.apiUrl}/reports/shift-schedule`, {params}).subscribe({
      next: res => { this.data.set(res); this.loading.set(false); },
      error: err => {
        this.toastr.error(err?.error?.message ?? '載入總覽表失敗');
        this.loading.set(false);
      },
    });
  }

  /**
   * 活動日的第三色只套在**被指派為預定人力的人**身上。
   * 若改用 `isActivityDay`（該部門當天有活動）會讓整欄的人全部亮起來，
   * 看不出誰真的要出勤 —— 那正是這張表要回答的問題。
   * 部門有活動但本人未被指派時，資訊仍保留在 tooltip。
   */
  cellClass(c: OverviewCell): string {
    const base = `ov-cell ov-cell--${c.dayType.replace('_', '-')}`;
    return c.isActivityAssignee ? `${base} ov-cell--activity` : base;
  }

  cellTitle(c: OverviewCell, s: DayStat | undefined): string {
    const parts: string[] = [];
    if (c.dayType === 'statutory_off') parts.push('例假日');
    else if (c.dayType === 'rest_day') parts.push('休假日');
    else if (c.dayType === 'public_holiday') parts.push(s?.holidayName ?? '國定假日');
    else parts.push('上班日');
    if (c.isActivityDay) parts.push(c.isActivityAssignee ? '活動日（預定人力）' : '活動日');
    return parts.join('・');
  }

  statOf(day: number): DayStat | undefined {
    return this.data()?.dayStats.find(s => s.day === day);
  }

  exportExcel(): void {
    const d = this.data();
    if (!d || this.exporting()) return;
    this.exporting.set(true);
    try {
      const header = ['員工姓名', '部門',
        ...d.dayStats.map(s => `${s.day}(${WEEKDAY_LABELS[s.weekday]})`),
        '例假日', '休假日', '狀態'];

      const body = d.rows.map(r => [
        r.userName,
        r.departmentName ?? '',
        ...r.cells.map(c => (CELL_TEXT[c.dayType] || '出') + (c.isActivityAssignee ? '*' : '')),
        `${r.statutoryOffCount}/${r.requiredStatutoryOff}`,
        `${r.restDayCount}/${r.requiredRestDay}`,
        r.quotaSatisfied ? '已排滿' : '尚未選滿',
      ]);

      const footer = ['出勤數', '', ...d.dayStats.map(s => s.workingCount), '', '', ''];

      const ws = XLSX.utils.aoa_to_sheet([header, ...body, footer]);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, '出勤排休總覽');
      XLSX.writeFile(wb, `出勤排休總覽_${d.year}${String(d.month).padStart(2, '0')}.xlsx`);
    } catch {
      this.toastr.error('匯出失敗');
    } finally {
      this.exporting.set(false);
    }
  }
}
