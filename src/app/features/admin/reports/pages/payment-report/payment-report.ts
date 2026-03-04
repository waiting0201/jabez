import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import * as XLSX from 'xlsx';

const PAYMENT_TYPE_LABELS: Record<string, string> = {
  vendor: '廠商請款',
  travel: '員工差旅',
  advance: '員工預支',
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
  selectedYear = signal('');
  selectedMonth = signal('');
  selectedPaymentStatus = signal('');

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
    this.search();
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
    if (this.selectedYear()) params['year'] = this.selectedYear();
    if (this.selectedMonth()) params['month'] = this.selectedMonth();
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

        const year = this.selectedYear() || '全部';
        const month = this.selectedMonth() || '全部';
        XLSX.writeFile(wb, `請款統計_${year}_${month}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exporting.set(false);
      },
    });
  }
}
