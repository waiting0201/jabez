import {Component, computed, inject, signal} from '@angular/core';
import {DatePipe} from '@angular/common';
import {toSignal, toObservable} from '@angular/core/rxjs-interop';
import {switchMap} from 'rxjs/operators';
import {PaymentReminderLogService} from '../../services/payment-reminder-log.service';
import {
  PaymentReminderLog,
  PaymentReminderLogPagedResult,
  STATUS_LABELS,
  STATUS_CLASSES,
} from '../../models/payment-reminder-log.model';

@Component({
  selector: 'app-payment-reminder-log-list',
  templateUrl: './payment-reminder-log-list.html',
  imports: [DatePipe],
})
export class PaymentReminderLogList {
  private service = inject(PaymentReminderLogService);

  readonly PAGE_SIZE = 30;
  page = signal(1);
  private refresh = signal(0);

  readonly statusLabel = STATUS_LABELS;
  readonly statusClass = STATUS_CLASSES;

  private result = toSignal(
    toObservable(computed(() => ({page: this.page(), refresh: this.refresh()}))).pipe(
      switchMap(({page}) => this.service.getPaged({page, pageSize: this.PAGE_SIZE}))
    ),
    {initialValue: {items: [], totalCount: 0, page: 1, pageSize: 30, totalPages: 1} as PaymentReminderLogPagedResult}
  );

  logs        = computed(() => this.result().items);
  totalCount  = computed(() => this.result().totalCount);
  totalPages  = computed(() => this.result().totalPages);

  goTo(p: number) { this.page.set(p); }
  prev() { if (this.page() > 1) this.page.update(p => p - 1); }
  next() { if (this.page() < this.totalPages()) this.page.update(p => p + 1); }

  running   = signal(false);
  runResult = signal<string>('');
  runError  = signal<string>('');

  manualRun() {
    this.running.set(true);
    this.runResult.set('');
    this.runError.set('');
    this.service.manualRun().subscribe({
      next: r => {
        this.running.set(false);
        this.runResult.set(`已執行：撈到 ${r.upcomingItemCount} 筆待提醒、推給 ${r.financeUserCount} 位財務人員（成功 ${r.successCount}, 跳過 ${r.skippedAlreadySent}, 失敗 ${r.failureCount}）`);
        this.refresh.update(v => v + 1);
      },
      error: err => {
        this.running.set(false);
        this.runError.set(err.error?.message || '手動觸發失敗。');
      },
    });
  }
}
