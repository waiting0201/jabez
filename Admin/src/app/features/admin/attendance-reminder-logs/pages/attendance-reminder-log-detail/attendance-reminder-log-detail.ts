import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {AttendanceReminderLogService} from '../../services/attendance-reminder-log.service';
import {
  AttendanceReminderLog,
  ERROR_CATEGORY_LABELS,
  REMINDER_TYPE_LABELS,
  TRIGGER_SOURCE_LABELS,
} from '../../models/attendance-reminder-log.model';

@Component({
  selector: 'app-attendance-reminder-log-detail',
  templateUrl: './attendance-reminder-log-detail.html',
  imports: [CommonModule, RouterLink],
})
export class AttendanceReminderLogDetail implements OnInit {
  private route   = inject(ActivatedRoute);
  private service = inject(AttendanceReminderLogService);

  rows    = signal<AttendanceReminderLog[]>([]);
  loading = signal(false);
  batchId = signal('');

  /** 批次啟動紀錄（第一筆，可能不存在） */
  batchStart = computed(() => this.rows().find(r => r.reminderType === 'batchStart'));

  /** 推播紀錄（排除 batchStart） */
  pushes = computed(() => this.rows().filter(r => r.reminderType !== 'batchStart'));

  /** 統計 */
  pushedCount = computed(() => this.pushes().filter(r => r.status === 'success').length);
  failedCount = computed(() => this.pushes().filter(r => r.status === 'failure').length);

  reminderTypeLabels  = REMINDER_TYPE_LABELS;
  errorCategoryLabels = ERROR_CATEGORY_LABELS;
  triggerSourceLabels = TRIGGER_SOURCE_LABELS;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('batchId') ?? '';
    this.batchId.set(id);
    if (!id) return;

    this.loading.set(true);
    this.service.getByBatchId(id).subscribe({
      next: (rows) => {
        this.rows.set(rows ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
      },
    });
  }

  formatTaipei(iso: string): string {
    if (!iso) return '—';
    const d = new Date(iso);
    return `${d.getFullYear()}/${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`;
  }
}
