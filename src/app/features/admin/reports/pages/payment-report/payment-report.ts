import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import {environment} from '@/environments/environment';
import {dayToRange, FilterMode, monthToRange, shiftDateString, snapToIsoWeek, todayString} from '@/app/features/admin/reports/utils/date-range';
import * as XLSX from 'xlsx';

/** 6 個類別 — 與後端 PaymentReportReadService 常數對應 */
export const CATEGORY_OPTIONS = [
  { value: 'payment',         label: '請款' },
  { value: 'advance',         label: '預支' },
  { value: 'writeoff',        label: '預支沖銷' },
  { value: 'travel-payment',  label: '出差請款' },
  { value: 'travel',          label: '出差預支' },
  { value: 'travel-writeoff', label: '出差預支沖銷' },
] as const;

/** PaymentRequest 子類型 → 中文 label */
const PAYMENT_TYPE_LABELS: Record<string, string> = {
  vendor:          '廠商請款',
  general:         '一般請款',
  business_trip:   '員工公出請款',
  advance:         '預支',
  writeoff:        '預支沖銷',
  'travel-payment': '出差請款',
  travel:          '出差預支',
  'travel-writeoff': '出差預支沖銷',
};

const STATUS_LABELS: Record<string, string> = {
  pending: '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

/** 匯出 Excel 右側 4 欄表頭 — 依類別決定 */
const ITEM_HEADERS: Record<string, [string, string, string, string]> = {
  'payment':          ['發票號碼', '品名', '發票日期', '發票金額'],
  'advance':          ['類別',     '品名', '數量',     '金額'],
  'writeoff':         ['發票號碼', '品名', '發票日期', '金額'],
  'travel-payment':   ['發票號碼', '品名', '發票日期', '發票金額'],
  'travel':           ['發票號碼', '品名', '發票日期', '金額'],
  'travel-writeoff':  ['發票號碼', '品名', '發票日期', '金額'],
};

/** 匯出筆數預警門檻：超過時先 confirm 再匯出 */
const EXPORT_WARN_THRESHOLD = 1000;

export interface PaymentReportItem {
  col1: string | null;        // 發票號碼 or 類別
  itemName: string | null;    // 品名
  col3Text: string | null;    // 數量（字串）— advance 專用
  col3Date: string | null;    // 發票日期 — 其他類別
  amount: number | null;      // 明細金額
}

export interface PaymentReportRow {
  id: number;
  requestNo: string;
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
  items: PaymentReportItem[];
}

/** 用於在 table 展開為「主表 row × items」多列；isFirstRow=true 時才輸出請款層欄位 */
export interface FlatPaymentRow {
  key: string;
  record: PaymentReportRow;
  item: PaymentReportItem | null;
  isFirstRow: boolean;
}

/** 對應後端 PaymentExportRowDto */
interface PaymentExportRow {
  parentId: number;
  requestNo: string;
  employeeName: string;
  type: string;
  projectCode: string;
  projectName: string;
  approvalStatus: string;
  createdAt: string;
  paidAt: string | null;
  paymentTotalAmount: number;
  itemCol1: string | null;
  itemName: string | null;
  itemCol3Text: string | null;
  itemCol3Date: string | null;
  itemAmount: number | null;
}

@Component({
  selector: 'app-payment-report',
  templateUrl: './payment-report.html',
  styles: [`
    /* 同筆主表的多列：第一列加上分隔線；後續列移除 td 上下分隔，視覺合併為一組 */
    .payment-report-table tbody tr.row-group-start td { border-top: 2px solid var(--bs-border-color, #dee2e6); }
    .payment-report-table tbody tr:not(.row-group-start) td { border-top: 0; }
  `],
  imports: [CommonModule, FormsModule],
})
export class PaymentReport implements OnInit {
  private http   = inject(HttpClient);
  private toastr = inject(ToastrService);

  /** 類別下拉選單 */
  readonly categoryOptions = CATEGORY_OPTIONS;
  selectedCategory = signal<string>('');

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

  /** advance 類別：明細第 3 欄為「數量」（字串）；其他類別為「發票日期」 */
  isAdvanceCategory = computed(() => this.selectedCategory() === 'advance');

  /** 明細 4 欄表頭（依類別決定） */
  itemHeaders = computed<readonly [string, string, string, string]>(() => {
    return ITEM_HEADERS[this.selectedCategory()] ?? ['', '', '', ''] as any;
  });

  /** 攤平：每筆主表 × items → 多列 FlatPaymentRow（無 items 仍輸出 1 列） */
  flatRows = computed<FlatPaymentRow[]>(() => {
    const result: FlatPaymentRow[] = [];
    for (const r of this.records()) {
      if (!r.items || r.items.length === 0) {
        result.push({ key: `${r.id}-0`, record: r, item: null, isFirstRow: true });
      } else {
        r.items.forEach((item, idx) => {
          result.push({ key: `${r.id}-${idx}`, record: r, item, isFirstRow: idx === 0 });
        });
      }
    }
    return result;
  });

  /** 明細金額合計（跨全部 items） */
  itemTotal = computed(() => {
    let sum = 0;
    for (const r of this.records()) {
      for (const it of (r.items ?? [])) sum += it.amount ?? 0;
    }
    return sum;
  });

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    const today = todayString(now);
    this.selectedDate.set(today);
    this.selectedWeekDate.set(today);
    // 不自動 search()：類別未選不打 API
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

  private categoryLabel(): string {
    return CATEGORY_OPTIONS.find(o => o.value === this.selectedCategory())?.label ?? '—';
  }

  /** 將篩選條件轉成可讀字串（寫入 Excel 第一列摘要） */
  private filterSummaryLine(): string {
    const cat = `類別：${this.categoryLabel()}`;
    const period = `時段：${this.exportSuffix()}`;
    const statusLabel: Record<string, string> = { paid: '已付', unpaid: '未付' };
    const status = `付款狀態：${statusLabel[this.selectedPaymentStatus()] ?? '全部'}`;
    return `${cat}　${period}　${status}`;
  }

  search() {
    if (!this.selectedCategory()) {
      this.toastr.warning('請先選擇類別', '提示');
      return;
    }
    this.currentPage.set(1);
    this.fetchData();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
    this.fetchData();
  }

  private buildParams(paged = true): Record<string, string | number> {
    const params: Record<string, string | number> = {};
    params['category'] = this.selectedCategory();
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
    const items: PaymentReportItem[] = (r.items ?? []).map((it: any) => ({
      col1: it.col1 ?? null,
      itemName: it.itemName ?? null,
      col3Text: it.col3Text ?? null,
      col3Date: it.col3Date ?? null,
      amount: it.amount ?? null,
    }));
    return {
      id: r.id,
      requestNo: r.requestNo ?? '',
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
      items,
    };
  }

  /** 顯示明細日期：advance 類別取 col3Text（數量），其他取 col3Date 格式化 */
  itemCol3Display(item: PaymentReportItem | null): string {
    if (!item) return '';
    if (this.isAdvanceCategory()) return item.col3Text ?? '';
    return item.col3Date ? new Date(item.col3Date).toLocaleDateString('zh-TW') : '';
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
    if (!this.selectedCategory()) {
      this.toastr.warning('請先選擇類別', '提示');
      return;
    }
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
    const category = this.selectedCategory();
    const itemHeaders = ITEM_HEADERS[category] ?? ['', '', '', ''];

    const headers = [
      '單號', '員工姓名', '類型', '專案',
      ...itemHeaders,
      '總金額', '簽核狀態', '付款日期', '申請日期',
    ];

    // 第 1 列：篩選條件摘要；第 2 列空；第 3 列：表頭；第 4 列起：資料
    const aoa: (string | number | null)[][] = [
      [this.filterSummaryLine()],
      [],
      headers,
    ];

    // 請款層欄位去重：同筆只在第一列輸出；同時記錄各筆 totalAmount 以利合計
    let lastParentId: number | null = null;
    const perRequestTotals = new Map<number, number>();
    const isAdvance = category === 'advance';

    for (const r of rows) {
      const isFirstRow = r.parentId !== lastParentId;
      if (isFirstRow) {
        lastParentId = r.parentId;
        perRequestTotals.set(r.parentId, r.paymentTotalAmount ?? 0);
      }

      const itemCol3 = isAdvance
        ? (r.itemCol3Text ?? '')
        : this.toIsoDate(r.itemCol3Date);

      const projectCombined = isFirstRow
        ? [r.projectCode ?? '—', r.projectName ?? ''].filter(v => v !== '').join('\n')
        : '';

      aoa.push([
        isFirstRow ? (r.requestNo ?? '') : '',
        isFirstRow ? (r.employeeName ?? '—') : '',
        isFirstRow ? (PAYMENT_TYPE_LABELS[r.type] ?? r.type) : '',
        projectCombined,
        // 明細層 4 欄永遠輸出
        r.itemCol1 ?? '',
        r.itemName ?? '',
        itemCol3,
        r.itemAmount ?? null,
        // 後段主表欄位（同筆只在第一列）
        isFirstRow ? (r.paymentTotalAmount ?? 0) : '',
        isFirstRow ? (STATUS_LABELS[r.approvalStatus] ?? r.approvalStatus) : '',
        isFirstRow ? this.toIsoDate(r.paidAt) : '',
        isFirstRow ? this.toIsoDate(r.createdAt) : '',
      ]);
    }

    // 合計列：對齊 UI tfoot（colspan=7 合計 → 明細金額 → 總金額 → colspan=3 空白）
    const requestTotal = Array.from(perRequestTotals.values()).reduce((sum, v) => sum + v, 0);
    const itemTotal = rows.reduce((sum, r) => sum + (r.itemAmount ?? 0), 0);
    aoa.push([
      '合計', '', '', '', '', '', '',
      itemTotal,        // col 8 = 明細金額合計
      requestTotal,     // col 9 = 單據總金額（去重）
      '', '', '',
    ]);

    const ws = XLSX.utils.aoa_to_sheet(aoa);

    // 欄寬（單位：字元）— 對應 headers 順序
    ws['!cols'] = [
      { wch: 18 }, // 單號
      { wch: 12 }, // 員工姓名
      { wch: 12 }, // 類型
      { wch: 24 }, // 專案（多行：代碼 + 名稱）
      { wch: 14 }, // 明細 Col1
      { wch: 20 }, // 品名
      { wch: 12 }, // 明細 Col3
      { wch: 14 }, // 明細金額
      { wch: 14 }, // 總金額
      { wch: 10 }, // 簽核狀態
      { wch: 12 }, // 付款日期
      { wch: 12 }, // 申請日期
    ];

    // 金額欄千分位格式（H 欄 c=7 明細金額、I 欄 c=8 總金額）
    const headerRowIdx = 2;
    const totalRowIdx = aoa.length - 1;
    const numberFmt = '#,##0';
    for (let r = headerRowIdx + 1; r <= totalRowIdx; r++) {
      const itemCell = ws[XLSX.utils.encode_cell({ r, c: 7 })];
      if (itemCell && typeof itemCell.v === 'number') itemCell.z = numberFmt;
      const totalCell = ws[XLSX.utils.encode_cell({ r, c: 8 })];
      if (totalCell && typeof totalCell.v === 'number') totalCell.z = numberFmt;
    }

    // 「專案」欄（c=3）自動換行（含 code\nname 兩行）
    for (let r = headerRowIdx + 1; r < totalRowIdx; r++) {
      const cell = ws[XLSX.utils.encode_cell({ r, c: 3 })];
      if (cell) {
        cell.s = { ...(cell.s ?? {}), alignment: { wrapText: true, vertical: 'top' } };
      }
    }

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, '款項統計');
    XLSX.writeFile(wb, `款項統計_${this.categoryLabel()}_${this.exportSuffix()}.xlsx`);
  }
}
