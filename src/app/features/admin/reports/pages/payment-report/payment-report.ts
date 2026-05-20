import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
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

/** 匯出筆數預警門檻：超過時先 confirm 再匯出 */
const EXPORT_WARN_THRESHOLD = 1000;

export interface PaymentReportRow {
  id: number;
  employeeName: string;
  type: string;
  typeLabel: string;
  projectCode: string;
  projectName: string;
  invoiceNos: string;
  totalAmount: number;
  approvalStatus: string;
  statusLabel: string;
  paidAt: string;
  createdAt: string;
}

/** 對應後端 PaymentExportRowDto：一張發票一列 */
interface PaymentExportRow {
  paymentRequestId: number;
  employeeName: string;
  type: string;
  projectCode: string;
  projectName: string;
  approvalStatus: string;
  createdAt: string;
  paidAt: string | null;
  paymentTotalAmount: number;
  invoiceNo: string | null;
  invoiceItemName: string | null;
  invoiceDate: string | null;
  invoiceAmount: number | null;
}

@Component({
  selector: 'app-payment-report',
  templateUrl: './payment-report.html',
  imports: [CommonModule, FormsModule],
})
export class PaymentReport implements OnInit {
  private http   = inject(HttpClient);
  private toastr = inject(ToastrService);

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

  /** 將篩選條件轉成可讀字串（寫入 Excel 第一列摘要） */
  private filterSummaryLine(): string {
    const period = `時段：${this.exportSuffix()}`;
    const statusLabel: Record<string, string> = { paid: '已付', unpaid: '未付' };
    const status = `付款狀態：${statusLabel[this.selectedPaymentStatus()] ?? '全部'}`;
    return `${period}　${status}`;
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
      projectName: r.projectName ?? '',
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

  /** ISO 日期 (YYYY-MM-DD)，便於 Excel 排序與後續處理 */
  private toIsoDate(value: string | null | undefined): string {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d.getTime())) return '';
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  exportExcel() {
    this.exporting.set(true);
    const params = this.buildParams(false);

    this.http.get<any>(`${environment.apiUrl}/reports/payment/export`, {params}).subscribe({
      next: (res) => {
        try {
          const rows: PaymentExportRow[] = res?.data ?? res ?? [];

          if (rows.length === 0) {
            this.toastr.warning('查無資料可匯出。', '提示');
            this.exporting.set(false);
            return;
          }

          if (rows.length > EXPORT_WARN_THRESHOLD &&
              !confirm(`即將匯出 ${rows.length} 筆資料，可能需要一些時間，是否繼續？`)) {
            this.exporting.set(false);
            return;
          }

          this.buildAndDownloadXlsx(rows);
          this.toastr.success(`已匯出 ${rows.length} 筆資料。`, '匯出完成');
        } catch {
          this.toastr.error('匯出檔案產生失敗。', '匯出失敗');
        } finally {
          this.exporting.set(false);
        }
      },
      error: () => {
        this.toastr.error('匯出失敗，請稍後再試。', '錯誤');
        this.exporting.set(false);
      },
    });
  }

  private buildAndDownloadXlsx(rows: PaymentExportRow[]) {
    const headers = [
      '員工姓名', '請款類型', '專案代碼', '專案名稱', '簽核狀態',
      '申請日期', '付款日期', '請款單總金額',
      '發票號碼', '品名', '發票日期', '發票金額',
    ];

    // 第 1 列：篩選條件摘要；第 2 列空；第 3 列：表頭；第 4 列起：資料
    const aoa: (string | number | null)[][] = [
      [this.filterSummaryLine()],
      [],
      headers,
    ];

    let invoiceTotal = 0;
    for (const r of rows) {
      const amount = r.invoiceAmount ?? 0;
      invoiceTotal += amount;
      aoa.push([
        r.employeeName ?? '—',
        PAYMENT_TYPE_LABELS[r.type] ?? r.type,
        r.projectCode ?? '—',
        r.projectName ?? '',
        STATUS_LABELS[r.approvalStatus] ?? r.approvalStatus,
        this.toIsoDate(r.createdAt),
        this.toIsoDate(r.paidAt),
        r.paymentTotalAmount ?? 0,
        r.invoiceNo ?? '',
        r.invoiceItemName ?? '',
        this.toIsoDate(r.invoiceDate),
        r.invoiceAmount ?? null,
      ]);
    }

    // 末列：合計（請款單總額會跨多列重複，為避免誤導，僅顯示發票金額合計）
    aoa.push([
      '合計', '', '', '', '', '', '', '', '', '', '', invoiceTotal,
    ]);

    const ws = XLSX.utils.aoa_to_sheet(aoa);

    // 欄寬（單位：字元）— 對應 headers 順序
    ws['!cols'] = [
      { wch: 12 }, // 員工姓名
      { wch: 10 }, // 請款類型
      { wch: 12 }, // 專案代碼
      { wch: 24 }, // 專案名稱
      { wch: 10 }, // 簽核狀態
      { wch: 12 }, // 申請日期
      { wch: 12 }, // 付款日期
      { wch: 14 }, // 請款單總金額
      { wch: 14 }, // 發票號碼
      { wch: 20 }, // 品名
      { wch: 12 }, // 發票日期
      { wch: 14 }, // 發票金額
    ];

    // 金額欄千分位格式（H 欄=請款單總金額、L 欄=發票金額）
    const headerRowIdx = 2; // 第 3 列（0-based 2）
    const totalRowIdx = aoa.length - 1; // 末列為合計
    const numberFmt = '#,##0';
    for (let r = headerRowIdx + 1; r <= totalRowIdx; r++) {
      const totalCell = ws[XLSX.utils.encode_cell({ r, c: 7 })];
      if (totalCell && typeof totalCell.v === 'number') totalCell.z = numberFmt;
      const invoiceCell = ws[XLSX.utils.encode_cell({ r, c: 11 })];
      if (invoiceCell && typeof invoiceCell.v === 'number') invoiceCell.z = numberFmt;
    }

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, '款項統計');
    XLSX.writeFile(wb, `款項統計_${this.exportSuffix()}.xlsx`);
  }
}
