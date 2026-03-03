import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {of} from 'rxjs';

export interface AttendanceRecordRow {
  id?: number;
  employeeCode: string;
  employeeName: string;
  recordDate: string;
  leaveType: string;
  leaveTime: string;
  clockInTime: string;
  clockOutTime: string;
  overtimeRange: string;
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
    this.years.set([currentYear - 1, currentYear, currentYear + 1]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    this.loadEmployees();
    this.search();
  }

  loadEmployees() {
    // TODO: 從後端 API 載入員工清單
    // this.http.get<any[]>(`${environment.apiUrl}/users`).subscribe(...)
  }

  search() {
    this.loading.set(true);
    // TODO: 連接後端 API — GET /reports/attendance?employeeId=&year=&month=
    // 目前使用靜態預覽資料
    setTimeout(() => {
      this.records.set([]);
      this.loading.set(false);
    }, 300);
  }

  exportExcel() {
    // TODO: 匯出 Excel
  }
}
