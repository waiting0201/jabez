import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import {dayToRange, FilterMode, monthToRange, shiftDateString, snapToIsoWeek, todayString} from '@/app/features/admin/reports/utils/date-range';
import * as XLSX from 'xlsx';

const PAYMENT_TYPE_LABELS: Record<string, string> = {
  vendor:        '廠商請款',
  general:       '一般請款',
  business_trip: '員工公出請款',
};

const STATUS_LABELS: Record<string, string> = {
  pending: '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

export interface PaymentReportRow {
  id: number;
  employeeName: string;
  type: string;
  typeLabel: string;
  projectCode: string;
  invoiceNos: string;
  totalAmount: number;
  approvalStatus: string;
  statusLabel: string;
  paidAt: string;
  createdAt: string;
}

@Component({
  selector: 'app-payment-report',
  templateUrl: './payment-report.html',
  imports: [CommonModule, FormsModule],
})
export class PaymentReport implements OnInit {
  private http = inject(HttpClient);

  /** 篩選條件 */
  selectedPaymentStatus = signal('');

  /** 時段模式：日 / 週 / 月（預設月）*/
  filterMode = signal<FilterMode>('month');
  selectedDate = signal('');
  /** 週模式：'YYYY-MM-DD'（任一天，由系統 snap 到該週週一→週日） */
  selectedWeekDate = signal('');
  selectedYear = signal('');
  selectedMonth = signal('');

  /** 年份選項 */
  years = signal<number[]>([]);

  /** 月份選項 */
  months = Array.from({length: 12}, (_, i) => i + 1);

  /** 紀錄 */
  records = signal<PaymentReportRow[]>([]);
  loading = signal(false);
  exporting = signal(false);

  /** 分頁 */
  currentPage = signal(1);
  totalCount = signal(0);
  totalPages = signal(1);
  private pageSize = 20;

  /** 合計 */
  totalAmount = signal(0);

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    const today = todayString(now);
    this.selectedDate.set(today);
    this.selectedWeekDate.set(today);
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

  search() {
    this.currentPage.set(1);
    this.fetchData();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
    this.fetchData();
  }

  private buildParams(paged = true): Record<string, string | number> {
    const params: Record<string, string | number> = {};
    if (paged) {
      params['page'] = this.currentPage();
      params['pageSize'] = this.pageSize;
    } else {
      params['page'] = 1;
      params['pageSize'] = 9999;
    }
    const range = this.computeDateRange();
    if (range) {
      params['dateFrom'] = range.dateFrom;
      params['dateTo'] = range.dateTo;
    }
    if (this.selectedPaymentStatus()) params['paymentStatus'] = this.selectedPaymentStatus();
    return params;
  }

  private mapRow(r: any): PaymentReportRow {
    return {
      id: r.id,
      employeeName: r.employeeName ?? '—',
      type: r.type,
      typeLabel: PAYMENT_TYPE_LABELS[r.type] ?? r.type,
      projectCode: r.projectCode ?? '—',
      invoiceNos: r.invoiceNos?.join(', ') ?? '',
      totalAmount: r.totalAmount ?? 0,
      approvalStatus: r.approvalStatus,
      statusLabel: STATUS_LABELS[r.approvalStatus] ?? r.approvalStatus,
      paidAt: r.paidAt ? new Date(r.paidAt).toLocaleDateString('zh-TW') : '',
      createdAt: r.createdAt ? new Date(r.createdAt).toLocaleDateString('zh-TW') : '',
    };
  }

  private fetchData() {
    this.loading.set(true);
    const params = this.buildParams();

    this.http.get<any>(`${environment.apiUrl}/reports/payment`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        this.totalCount.set(data?.totalCount ?? 0);
        this.totalPages.set(data?.totalPages ?? 1);

        const rows = items.map((r: any) => this.mapRow(r));
        this.records.set(rows);
        this.totalAmount.set(rows.reduce((sum: number, r: PaymentReportRow) => sum + r.totalAmount, 0));
        this.loading.set(false);
      },
      error: () => {
        this.records.set([]);
        this.totalAmount.set(0);
        this.loading.set(false);
      },
    });
  }

  exportExcel() {
    this.exporting.set(true);
    const params = this.buildParams(false);

    this.http.get<any>(`${environment.apiUrl}/reports/payment`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        const rows = items.map((r: any) => this.mapRow(r));

        const wsData = rows.map((r: PaymentReportRow) => ({
          '員工姓名': r.employeeName,
          '請款類型': r.typeLabel,
          '專案代碼': r.projectCode,
          '發票號碼': r.invoiceNos,
          '總金額': r.totalAmount,
          '簽核狀態': r.statusLabel,
          '付款日期': r.paidAt,
          '申請日期': r.createdAt,
        }));

        const ws = XLSX.utils.json_to_sheet(wsData);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, '請款統計');

        XLSX.writeFile(wb, `請款統計_${this.exportSuffix()}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exporting.set(false);
      },
    });
  }
}
