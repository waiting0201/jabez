import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';

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
}

@Component({
  selector: 'app-attendance-report',
  templateUrl: './attendance-report.html',
  imports: [CommonModule, FormsModule],
})
export class AttendanceReport implements OnInit {
  private http = inject(HttpClient);

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
    // TODO: 匯出 Excel
  }
}
