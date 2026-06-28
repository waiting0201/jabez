import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';
import {AttendanceReminderLogService} from '../../services/attendance-reminder-log.service';
import {
  AttendanceReminderLog,
  AttendanceReminderLogStats,
  ERROR_CATEGORY_LABELS,
  ErrorCategory,
  LogStatus,
  REMINDER_TYPE_LABELS,
  ReminderType,
  STATUS_LABELS,
  TRIGGER_SOURCE_LABELS,
  TriggerSource,
} from '../../models/attendance-reminder-log.model';

@Component({
  selector: 'app-attendance-reminder-log-list',
  templateUrl: './attendance-reminder-log-list.html',
  imports: [CommonModule, FormsModule, RouterLink],
})
export class AttendanceReminderLogList implements OnInit {
  private service = inject(AttendanceReminderLogService);
  private http    = inject(HttpClient);
  private toastr  = inject(ToastrService);

  /** 篩選條件 */
  fromDate      = signal('');
  toDate        = signal('');
  reminderType  = signal<ReminderType | ''>('');
  status        = signal<LogStatus | ''>('');
  errorCategory = signal<ErrorCategory | ''>('');
  triggerSource = signal<TriggerSource | ''>('');

  /** 紀錄與統計 */
  records   = signal<AttendanceReminderLog[]>([]);
  stats     = signal<AttendanceReminderLogStats | null>(null);
  loading   = signal(false);
  triggering = signal(false);

  /** 分頁 */
  currentPage = signal(1);
  totalCount  = signal(0);
  totalPages  = signal(1);
  private pageSize = 20;

  /** Label 對照表（給 template 用） */
  reminderTypeLabels = REMINDER_TYPE_LABELS;
  statusLabels       = STATUS_LABELS;
  errorCategoryLabels = ERROR_CATEGORY_LABELS;
  triggerSourceLabels = TRIGGER_SOURCE_LABELS;

  /** 7 天趨勢最大值（畫長條圖用） */
  maxBar = computed(() => {
    const days = this.stats()?.last7Days ?? [];
    return Math.max(1, ...days.map(d => d.pushed + d.failed));
  });

  ngOnInit() {
    // 預設今天 - 7 天前
    const today = new Date();
    const fromDefault = new Date(today);
    fromDefault.setDate(today.getDate() - 6);
    this.fromDate.set(this.toIsoDate(fromDefault));
    this.toDate.set(this.toIsoDate(today));

    this.loadStats();
    this.search();
  }

  search() {
    this.currentPage.set(1);
    this.fetchData();
  }

  resetFilters() {
    this.reminderType.set('');
    this.status.set('');
    this.errorCategory.set('');
    this.triggerSource.set('');
    this.search();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
    this.fetchData();
  }

  /** 手動觸發推播（Superadmin 限定） */
  triggerNow(type: 'clockIn' | 'clockOut') {
    this.triggering.set(true);
    this.http.post<{
      type: string;
      recipientCount: number;
      pushedCount: number;
      failureCount: number;
      batchId: string;
    }>(`${environment.apiUrl}/admin/attendance-reminder/run`, null, {params: {type}}).subscribe({
      next: (res) => {
        this.toastr.success(
          `${type === 'clockIn' ? '上班' : '下班'}提醒已觸發：對象 ${res.recipientCount} 人，成功 ${res.pushedCount}，失敗 ${res.failureCount}`,
          '推播完成');
        this.triggering.set(false);
        this.loadStats();
        this.search();
      },
      error: (err) => {
        this.toastr.error(err?.error?.message ?? '觸發失敗', '錯誤');
        this.triggering.set(false);
      },
    });
  }

  private fetchData() {
    this.loading.set(true);
    this.service.getPaged({
      from: this.fromDate() || undefined,
      to:   this.toDate()   || undefined,
      reminderType: this.reminderType() || undefined,
      status:       this.status() || undefined,
      errorCategory: this.errorCategory() || undefined,
      triggerSource: this.triggerSource() || undefined,
      page: this.currentPage(),
      pageSize: this.pageSize,
    }).subscribe({
      next: (res) => {
        this.records.set(res.items ?? []);
        this.totalCount.set(res.totalCount ?? 0);
        this.totalPages.set(Math.max(1, res.totalPages ?? 1));
        this.loading.set(false);
      },
      error: (err) => {
        this.records.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        this.toastr.error(err?.error?.message ?? '載入失敗', '錯誤');
      },
    });
  }

  private loadStats() {
    this.service.getStats().subscribe({
      next: (s) => this.stats.set(s),
      error: () => this.stats.set(null),
    });
  }

  private toIsoDate(d: Date): string {
    const yr = d.getFullYear();
    const mo = String(d.getMonth() + 1).padStart(2, '0');
    const da = String(d.getDate()).padStart(2, '0');
    return `${yr}-${mo}-${da}`;
  }

  /** 格式化台北時間 → 字串 (MM/dd HH:mm:ss) */
  formatTaipei(iso: string): string {
    if (!iso) return '—';
    const d = new Date(iso);
    return `${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`;
  }

  /** BatchId 短碼（前 8 字） */
  shortBatchId(id: string): string {
    return id ? id.substring(0, 8) : '';
  }
}
