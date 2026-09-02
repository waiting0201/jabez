import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {environment} from '@/environments/environment';
import {AttendanceService} from '@/app/features/dashboard/services/attendance.service';
import {dayToRange, FilterMode, monthToRange, shiftDateString, snapToIsoWeek, todayString} from '@/app/features/admin/reports/utils/date-range';
import {LEAVE_TYPE_LABELS, LeaveType} from '@/app/features/admin/leave-requests/models/leave-request.model';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';
import * as XLSX from 'xlsx';

export interface AttendanceRecordRow {
  /** 合併列的穩定 track key：打卡列 'a{id}'、請假虛擬列 'l{userId}_{yyyy-MM-dd}' */
  key: string;
  /** AttendanceRecord.Id；null＝請假虛擬列（DB 無對應紀錄，不可編輯） */
  id: number | null;
  userId: string;
  employeeName: string;
  recordDate: string;
  /** 當日所有假別中文，以「、」串接 */
  leaveType: string;
  /** 當日請假時數合計（顯示用字串，無請假為空） */
  leaveHours: string;
  /** 首張假單的完整區間（掛 tooltip 用） */
  leaveTime: string;
  /** 該日只有請假、沒有任何打卡紀錄 */
  isLeaveOnly: boolean;
  clockInTime: string;
  clockOutTime: string;
  /** 下班時間為系統自動補卡（登入時補打漏打的下班卡），非本人打卡 */
  isClockOutAuto: boolean;
  /** 該日打卡時勾選為出差 */
  isBusinessTrip: boolean;
  /** 管理者填寫的備註（僅編輯表單使用，清單不顯示） */
  remark: string;
  /** 下班 − 上班的實際跨度（小時，含午休）；缺任一端為 null */
  workHours: number | null;
  /** 上下班跨度超過 LONG_WORKDAY_HOURS 小時 */
  isLongWorkday: boolean;
  overtimeStartTime: string;
  overtimeEndTime: string;
  /** GPS 經緯度 */
  clockInLatitude: number | null;
  clockInLongitude: number | null;
  clockOutLatitude: number | null;
  clockOutLongitude: number | null;
  overtimeStartLatitude: number | null;
  overtimeStartLongitude: number | null;
  overtimeEndLatitude: number | null;
  overtimeEndLongitude: number | null;
  /** 原始 ISO 日期（供編輯表單組合 DateTime） */
  rawRecordDate: string | null;
  /** 原始 ISO 時間（供編輯表單用） */
  rawClockIn: string | null;
  rawClockOut: string | null;
  rawOvertimeStart: string | null;
  rawOvertimeEnd: string | null;
}

/** 工時跨度提示門檻（小時）：下班 − 上班（含午休）超過此值即於清單標示 */
const LONG_WORKDAY_HOURS = 9.5;

@Component({
  selector: 'app-attendance-report',
  templateUrl: './attendance-report.html',
  imports: [CommonModule, FormsModule, HasPermissionDirective],
})
export class AttendanceReport implements OnInit {
  private http = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);
  private attendanceService = inject(AttendanceService);

  /** 篩選條件 */
  selectedEmployeeId = signal('');

  /** 時段模式：日 / 週 / 月（預設月）*/
  filterMode = signal<FilterMode>('month');

  /** 日模式：'YYYY-MM-DD' */
  selectedDate = signal('');
  /** 週模式：'YYYY-MM-DD'（任一天，由系統 snap 到該週週一→週日） */
  selectedWeekDate = signal('');
  /** 月模式：年 / 月 */
  selectedYear = signal('');
  selectedMonth = signal('');

  /** 員工清單（供下拉選單） */
  employees = signal<{id: string; code: string; name: string}[]>([]);

  /** 年份選項 */
  years = signal<number[]>([]);

  /** 月份選項（1-12） */
  months = Array.from({length: 12}, (_, i) => i + 1);

  /** 紀錄 */
  records = signal<AttendanceRecordRow[]>([]);
  loading = signal(false);

  /** 分頁 */
  currentPage = signal(1);
  totalCount = signal(0);
  totalPages = signal(1);
  private pageSize = 20;

  /** 桌機版頁碼列表（-1 = 省略號） */
  pageNumbers = computed(() => buildPageNumbers(this.currentPage(), this.totalPages()));

  /** 地圖 Modal */
  mapModal = signal<{label: string; lat: number; lng: number} | null>(null);
  mapIframeUrl = computed(() => {
    const m = this.mapModal();
    if (!m) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.google.com/maps?q=${m.lat},${m.lng}&z=16&output=embed`
    );
  });

  /** 編輯 Modal */
  editingRecord = signal<AttendanceRecordRow | null>(null);
  editForm = signal({clockIn: '', clockOut: '', overtimeStart: '', overtimeEnd: '', remark: ''});
  saving = signal(false);

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    // 預先填入日 / 週 預設值，使用者切換模式即可使用
    const today = todayString(now);
    this.selectedDate.set(today);
    this.selectedWeekDate.set(today);
    this.loadEmployees();
    this.search();
  }

  /** 週模式 snap 結果（含週號 / 起訖日），供 UI 顯示「W18：04/27 ~ 05/03」 */
  weekRange = computed(() => snapToIsoWeek(this.selectedWeekDate()));

  /** 依 filterMode 計算 dateFrom/dateTo（回傳 null 代表使用者尚未選擇日期/週） */
  private computeDateRange(): { dateFrom: string; dateTo: string } | null {
    const mode = this.filterMode();
    if (mode === 'day') {
      return dayToRange(this.selectedDate());
    }
    if (mode === 'week') {
      const r = this.weekRange();
      return r ? { dateFrom: r.dateFrom, dateTo: r.dateTo } : null;
    }
    // month
    const year = Number(this.selectedYear());
    const month = Number(this.selectedMonth());
    if (!year || !month) return null;
    return monthToRange(year, month);
  }

  /** 上 / 下週切換：將 selectedWeekDate 加減 7 天 */
  shiftWeek(days: number) {
    const cur = this.selectedWeekDate();
    if (!cur) return;
    this.selectedWeekDate.set(shiftDateString(cur, days));
  }

  /** 重置為本週 */
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

  search() {
    this.currentPage.set(1);
    this.fetchData();
  }

  goToPage(page: number) {
    // 分頁鈕以 .disabled class 呈現（僅樣式，不會擋 click），故在此夾住邊界
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) return;
    this.currentPage.set(page);
    this.fetchData();
  }

  private fetchData() {
    this.loading.set(true);

    const params: any = {page: this.currentPage(), pageSize: this.pageSize};
    if (this.selectedEmployeeId()) params.employeeId = this.selectedEmployeeId();
    const range = this.computeDateRange();
    if (range) {
      params.dateFrom = range.dateFrom;
      params.dateTo = range.dateTo;
    }

    this.http.get<any>(`${environment.apiUrl}/attendances`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        this.totalCount.set(data?.totalCount ?? 0);
        this.totalPages.set(data?.totalPages ?? 1);

        this.records.set(
          items.map((r: any) => ({
            key: r.id != null ? `a${r.id}` : `l${r.userId}_${String(r.recordDate ?? '').substring(0, 10)}`,
            id: r.id ?? null,
            userId: r.userId,
            isLeaveOnly: r.id == null,
            employeeName: r.userName ?? '—',
            recordDate: r.recordDate ? new Date(r.recordDate).toLocaleDateString('zh-TW') : '',
            leaveType: this.formatLeaveTypes(r),
            leaveHours: r.leaveHours != null ? String(Math.round(Number(r.leaveHours) * 10) / 10) : '',
            leaveTime: r.leaveStartDate && r.leaveEndDate
              ? `${new Date(r.leaveStartDate).toLocaleDateString('zh-TW')} ~ ${new Date(r.leaveEndDate).toLocaleDateString('zh-TW')}`
              : '',
            clockInTime: r.clockInTime ? new Date(r.clockInTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            clockOutTime: r.clockOutTime ? new Date(r.clockOutTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            isClockOutAuto: !!r.isClockOutAuto,
            isBusinessTrip: !!r.isBusinessTrip,
            remark: r.remark ?? '',
            workHours: this.computeWorkHours(r.clockInTime, r.clockOutTime),
            isLongWorkday: this.isLongWorkday(r.clockInTime, r.clockOutTime),
            overtimeStartTime: r.overtimeStartTime ? new Date(r.overtimeStartTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            overtimeEndTime: r.overtimeEndTime ? new Date(r.overtimeEndTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            clockInLatitude: r.clockInLatitude ?? null,
            clockInLongitude: r.clockInLongitude ?? null,
            clockOutLatitude: r.clockOutLatitude ?? null,
            clockOutLongitude: r.clockOutLongitude ?? null,
            overtimeStartLatitude: r.overtimeStartLatitude ?? null,
            overtimeStartLongitude: r.overtimeStartLongitude ?? null,
            overtimeEndLatitude: r.overtimeEndLatitude ?? null,
            overtimeEndLongitude: r.overtimeEndLongitude ?? null,
            rawRecordDate: r.recordDate ?? null,
            rawClockIn: r.clockInTime ?? null,
            rawClockOut: r.clockOutTime ?? null,
            rawOvertimeStart: r.overtimeStartTime ?? null,
            rawOvertimeEnd: r.overtimeEndTime ?? null,
          }))
        );
        this.loading.set(false);
      },
      error: () => {
        this.records.set([]);
        this.totalCount.set(0);
        this.totalPages.set(1);
        this.loading.set(false);
      },
    });
  }

  /**
   * 下班 − 上班的實際跨度（小時，含午休），未捨入。
   * 缺任一端或跨度非正值時回 null（例如只打上班卡、或人工改到下班早於上班）。
   */
  private rawWorkHours(rawIn: string | null, rawOut: string | null): number | null {
    if (!rawIn || !rawOut) return null;
    const diff = new Date(rawOut).getTime() - new Date(rawIn).getTime();
    if (!(diff > 0)) return null;
    return diff / 3600000;
  }

  /** 顯示用工時（四捨五入至小數一位）。刻意與 isLongWorkday 分離：捨入後比較會讓 9:31 被捨成 9.5 而漏標 */
  private computeWorkHours(rawIn: string | null, rawOut: string | null): number | null {
    const h = this.rawWorkHours(rawIn, rawOut);
    return h == null ? null : Math.round(h * 10) / 10;
  }

  /** 工時跨度是否超過門檻（清單 badge 與 Excel 匯出共用，避免門檻值散落兩處）。以未捨入值比較 */
  private isLongWorkday(rawIn: string | null, rawOut: string | null): boolean {
    const h = this.rawWorkHours(rawIn, rawOut);
    return h != null && h > LONG_WORKDAY_HOURS;
  }

  /** 清單 / tooltip 顯示用的門檻值 */
  readonly longWorkdayHours = LONG_WORKDAY_HOURS;

  /**
   * 當日所有假別中文，以「、」串接（同日可能上午事假 + 下午特休）。
   * 舊版回應沒有 leaves 陣列時退回單一 leaveType 相容欄位。
   */
  private formatLeaveTypes(r: any): string {
    if (r.leaves?.length) {
      return r.leaves
        .map((l: any) => LEAVE_TYPE_LABELS[l.leaveType as LeaveType] ?? l.leaveType)
        .join('、');
    }
    return r.leaveType ? LEAVE_TYPE_LABELS[r.leaveType as LeaveType] ?? r.leaveType : '';
  }

  /** 開啟地圖 Modal */
  openMap(label: string, lat: number, lng: number) {
    this.mapModal.set({label, lat, lng});
  }

  /** 關閉地圖 Modal */
  closeMap() {
    this.mapModal.set(null);
  }

  /** 開啟編輯 Modal */
  openEdit(row: AttendanceRecordRow) {
    this.editingRecord.set(row);
    this.editForm.set({
      clockIn: this.toTimeInput(row.rawClockIn),
      clockOut: this.toTimeInput(row.rawClockOut),
      overtimeStart: this.toTimeInput(row.rawOvertimeStart),
      overtimeEnd: this.toTimeInput(row.rawOvertimeEnd),
      remark: row.remark,
    });
  }

  /** 關閉 Modal */
  closeEdit() {
    this.editingRecord.set(null);
  }

  /** 儲存修改 */
  saveEdit() {
    const record = this.editingRecord();
    if (!record?.id) return;

    this.saving.set(true);
    const form = this.editForm();

    // 使用原始 recordDate 組合完整 DateTime。
    // 後端回傳 "2026-08-12T00:00:00"（無時區標記）→ new Date() 以本地時間解析，
    // 再 toISOString() 轉 UTC 會讓台北 +8 的午夜退回前一天，導致修改後的打卡時間被存到前一日；
    // 故一律用字串切割，不經過 Date 物件。
    const dateStr = record.rawRecordDate
      ? record.rawRecordDate.slice(0, 10)
      : todayString();

    const body = {
      clockInTime: form.clockIn ? `${dateStr}T${form.clockIn}:00` : null,
      clockOutTime: form.clockOut ? `${dateStr}T${form.clockOut}:00` : null,
      overtimeStartTime: form.overtimeStart ? `${dateStr}T${form.overtimeStart}:00` : null,
      overtimeEndTime: form.overtimeEnd ? `${dateStr}T${form.overtimeEnd}:00` : null,
      remark: form.remark?.trim() || null,
    };

    this.attendanceService.update(record.id, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEdit();
        this.fetchData();   // 停留在當前頁，不重置頁碼
      },
      error: (err) => {
        console.error('[AttendanceReport] saveEdit error:', err);
        this.saving.set(false);
      },
    });
  }

  /** 將 ISO 日期字串轉為 HH:mm 格式（供 input[type=time] 使用） */
  private toTimeInput(isoStr: string | null): string {
    if (!isoStr) return '';
    const d = new Date(isoStr);
    const h = d.getHours().toString().padStart(2, '0');
    const m = d.getMinutes().toString().padStart(2, '0');
    return `${h}:${m}`;
  }

  exporting = signal(false);

  exportExcel() {
    this.exporting.set(true);

    // export=true 讓後端放寬 pageSize 上限（一般列表仍為 100），避免匯出被截斷
    const params: any = {page: 1, pageSize: 5000, export: 'true'};
    if (this.selectedEmployeeId()) params.employeeId = this.selectedEmployeeId();
    const range = this.computeDateRange();
    if (range) {
      params.dateFrom = range.dateFrom;
      params.dateTo = range.dateTo;
    }

    this.http.get<any>(`${environment.apiUrl}/attendances`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        const wsData = items.map((r: any) => ({
          '員工姓名': r.userName ?? '—',
          '日期': r.recordDate ? new Date(r.recordDate).toLocaleDateString('zh-TW') : '',
          '請假類型': this.formatLeaveTypes(r),
          '當日請假時數': r.leaveHours != null ? Number(r.leaveHours) : '',
          '請假區間': r.leaveStartDate && r.leaveEndDate
            ? `${new Date(r.leaveStartDate).toLocaleDateString('zh-TW')} ~ ${new Date(r.leaveEndDate).toLocaleDateString('zh-TW')}`
            : '',
          '上班時間': r.clockInTime ? new Date(r.clockInTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
          // 系統補卡的下班時間加註來源，避免匯出後看不出是不是本人打的
          '下班時間': r.clockOutTime
            ? new Date(r.clockOutTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'})
              + (r.isClockOutAuto ? '（系統補卡）' : '')
            : '',
          '加班開始': r.overtimeStartTime ? new Date(r.overtimeStartTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
          '加班結束': r.overtimeEndTime ? new Date(r.overtimeEndTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
          // 虛擬列＝當日有已核准請假但完全沒打卡，匯出後仍需分辨得出來；出差與逾時註記與畫面 badge 同源
          '備註': [
            r.id == null ? '請假（未打卡）' : '',
            r.isBusinessTrip ? '出差' : '',
            this.isLongWorkday(r.clockInTime, r.clockOutTime) ? `超過 ${LONG_WORKDAY_HOURS} 小時` : '',
          ].filter(Boolean).join('；'),
        }));

        const ws = XLSX.utils.json_to_sheet(wsData);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, '出缺勤紀錄');

        XLSX.writeFile(wb, `出缺勤紀錄_${this.exportSuffix()}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exporting.set(false);
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
