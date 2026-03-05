import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import {AttendanceService} from '@/app/features/dashboard/services/attendance.service';

const LEAVE_TYPE_LABELS: Record<string, string> = {
  annual: '特休',
  personal: '事假',
  sick: '病假',
  compensatory: '補休',
};

export interface AttendanceRecordRow {
  id?: number;
  employeeName: string;
  recordDate: string;
  leaveType: string;
  leaveTime: string;
  clockInTime: string;
  clockOutTime: string;
  overtimeStartTime: string;
  overtimeEndTime: string;
  /** 原始 ISO 日期（供編輯表單組合 DateTime） */
  rawRecordDate: string | null;
  /** 原始 ISO 時間（供編輯表單用） */
  rawClockIn: string | null;
  rawClockOut: string | null;
  rawOvertimeStart: string | null;
  rawOvertimeEnd: string | null;
}

@Component({
  selector: 'app-attendance-report',
  templateUrl: './attendance-report.html',
  imports: [CommonModule, FormsModule],
})
export class AttendanceReport implements OnInit {
  private http = inject(HttpClient);
  private attendanceService = inject(AttendanceService);

  /** 篩選條件 */
  selectedEmployeeId = signal('');
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

  /** 編輯 Modal */
  editingRecord = signal<AttendanceRecordRow | null>(null);
  editForm = signal({clockIn: '', clockOut: '', overtimeStart: '', overtimeEnd: ''});
  saving = signal(false);

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    this.loadEmployees();
    this.search();
  }

  loadEmployees() {
    this.http.get<any>(`${environment.apiUrl}/users`).subscribe({
      next: (res) => {
        const items = res?.items ?? res ?? [];
        this.employees.set(
          items.map((u: any) => ({id: u.id, code: u.employeeCode ?? '', name: u.name}))
        );
      },
    });
  }

  search() {
    this.loading.set(true);

    const params: any = {page: 1, pageSize: 100};
    if (this.selectedEmployeeId()) params.employeeId = this.selectedEmployeeId();
    if (this.selectedYear()) params.year = this.selectedYear();
    if (this.selectedMonth()) params.month = this.selectedMonth();

    this.http.get<any>(`${environment.apiUrl}/attendances`, {params}).subscribe({
      next: (res) => {
        const items = res?.items ?? res ?? [];
        this.records.set(
          items.map((r: any) => ({
            id: r.id,
            employeeName: r.userName ?? '—',
            recordDate: r.recordDate ? new Date(r.recordDate).toLocaleDateString('zh-TW') : '',
            leaveType: r.leaveType ? LEAVE_TYPE_LABELS[r.leaveType as string] ?? r.leaveType : '',
            leaveTime: r.leaveStartDate && r.leaveEndDate
              ? `${new Date(r.leaveStartDate).toLocaleDateString('zh-TW')} ~ ${new Date(r.leaveEndDate).toLocaleDateString('zh-TW')}`
              : '',
            clockInTime: r.clockInTime ? new Date(r.clockInTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            clockOutTime: r.clockOutTime ? new Date(r.clockOutTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            overtimeStartTime: r.overtimeStartTime ? new Date(r.overtimeStartTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
            overtimeEndTime: r.overtimeEndTime ? new Date(r.overtimeEndTime).toLocaleTimeString('zh-TW', {hour: '2-digit', minute: '2-digit'}) : '',
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
        this.loading.set(false);
      },
    });
  }

  /** 開啟編輯 Modal */
  openEdit(row: AttendanceRecordRow) {
    this.editingRecord.set(row);
    this.editForm.set({
      clockIn: this.toTimeInput(row.rawClockIn),
      clockOut: this.toTimeInput(row.rawClockOut),
      overtimeStart: this.toTimeInput(row.rawOvertimeStart),
      overtimeEnd: this.toTimeInput(row.rawOvertimeEnd),
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

    // 使用原始 recordDate 組合完整 DateTime
    const dateStr = record.rawRecordDate
      ? new Date(record.rawRecordDate).toISOString().substring(0, 10)
      : new Date().toISOString().substring(0, 10);

    const body = {
      clockInTime: form.clockIn ? `${dateStr}T${form.clockIn}:00` : null,
      clockOutTime: form.clockOut ? `${dateStr}T${form.clockOut}:00` : null,
      overtimeStartTime: form.overtimeStart ? `${dateStr}T${form.overtimeStart}:00` : null,
      overtimeEndTime: form.overtimeEnd ? `${dateStr}T${form.overtimeEnd}:00` : null,
    };

    console.log('[AttendanceReport] saveEdit body:', body);

    this.attendanceService.update(record.id, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEdit();
        this.search();
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

  exportExcel() {
    // TODO: 匯出 Excel
  }
}
