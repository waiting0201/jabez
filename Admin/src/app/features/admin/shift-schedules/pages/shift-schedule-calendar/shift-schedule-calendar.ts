import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {ToastrService} from 'ngx-toastr';

import {ShiftScheduleService} from '../../services/shift-schedule.service';
import {ShiftChangeService} from '../../services/shift-change.service';
import {ShiftChangeRequest} from '../../models/shift-change.model';
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  ApprovalStatus,
} from '../../../payment-requests/models/payment-request.model';
import {
  DAY_TYPE_LABELS,
  ShiftDayType,
  ShiftScheduleDay,
  ShiftScheduleMonth,
  dateKey,
  nextDayType,
} from '../../models/shift-schedule.model';
import {ShiftMonthCalendar, lockedCellMessage} from '../../../../../shared/components/shift-month-calendar/shift-month-calendar';
/**
 * 個人排班排例／休（四週彈性工時 · 功能 A）。
 *
 * 月曆為共用元件 `<app-shift-month-calendar>`（與改班申請、簽核頁共用，FullCalendar 細節收在該元件內）；
 * 每格的狀態存在本元件的 `dayTypes` signal，點格時切換後傳回元件重畫。
 *
 * 三條規則不在前端重算（後端 `ShiftScheduleValidator` 為單一真相）：
 * 畫面只顯示後端回傳的 `blocks` / `warnings`，前端僅即時算「已排幾天」這種純計數。
 */
@Component({
  selector: 'app-shift-schedule-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ShiftMonthCalendar],
  templateUrl: './shift-schedule-calendar.html',
  styleUrl: './shift-schedule-calendar.scss',
})
export class ShiftScheduleCalendar implements OnInit {
  private svc = inject(ShiftScheduleService);
  private changeSvc = inject(ShiftChangeService);
  private toastr = inject(ToastrService);

  /**
   * 我的改班申請（最近幾張）。原本申請人沒有任何入口看得到自己送出的單，
   * 找不到就重開一張，審核者因而收到重複的申請（2026-09-28 修正，後端同時限制同月一張）。
   */
  myChanges = signal<ShiftChangeRequest[]>([]);
  readonly statusLabels = APPROVAL_STATUS_LABELS;
  readonly statusClasses = APPROVAL_STATUS_CLASSES;

  readonly labels = DAY_TYPE_LABELS;

  // 預設看次月：排班的主要情境是「10–25 日排次月」
  private static readonly defaultPeriod = (() => {
    const d = new Date();
    const next = new Date(d.getFullYear(), d.getMonth() + 1, 1);
    return {year: next.getFullYear(), month: next.getMonth() + 1};
  })();

  year = signal(ShiftScheduleCalendar.defaultPeriod.year);
  month = signal(ShiftScheduleCalendar.defaultPeriod.month);

  loading = signal(false);
  saving = signal(false);
  data = signal<ShiftScheduleMonth | null>(null);

  /** 本機編輯中的日別（key = yyyy-MM-dd）。載入後即為後端狀態，使用者點格才改。 */
  readonly dayTypes = signal<Record<string, ShiftDayType>>({});
  private original = signal<Record<string, ShiftDayType>>({});

  readonly dirty = computed(() =>
    JSON.stringify(this.dayTypes()) !== JSON.stringify(this.original()));

  /** 即時計數（純計數，不是規則重算）。 */
  readonly statutoryOffCount = computed(() => this.countOf('statutory_off'));
  readonly restDayCount = computed(() => this.countOf('rest_day'));

  readonly requiredStatutoryOff = computed(() => this.data()?.validation.requiredStatutoryOff ?? 4);
  readonly requiredRestDay = computed(() => this.data()?.validation.requiredRestDay ?? 4);

  /** 例假是否已排滿 —— 唯一在前端判斷的一條，只為了即時提示，送出仍由後端把關。 */
  readonly statutoryOffSatisfied = computed(() =>
    this.statutoryOffCount() >= this.requiredStatutoryOff());

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y - 1, y, y + 1];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  ngOnInit(): void {
    this.load();
    this.changeSvc.getPaged(1, 5).subscribe({
      next: res => this.myChanges.set(res.items ?? []),
      error: () => this.myChanges.set([]),
    });
  }

  statusLabel(status: string): string {
    return this.statusLabels[status as ApprovalStatus] ?? status;
  }

  statusClass(status: string): string {
    return this.statusClasses[status as ApprovalStatus] ?? 'bg-secondary-subtle text-secondary';
  }

  load(): void {
    this.loading.set(true);
    this.svc.getMonth(this.year(), this.month()).subscribe({
      next: (res) => {
        this.apply(res);
        this.loading.set(false);
      },
      error: (err) => {
        this.toastr.error(err?.error?.message ?? '載入排班失敗');
        this.loading.set(false);
      },
    });
  }

  onPeriodChange(): void {
    this.load();
  }

  save(): void {
    if (this.saving()) return;      // in-flight 鎖：避免連按建出兩次寫入
    this.saving.set(true);

    const days = Object.entries(this.dayTypes())
      .filter(([, t]) => t !== 'work')       // 上班日是預設值，不必送
      .map(([date, dayType]) => ({date, dayType}));

    this.svc.save({year: this.year(), month: this.month(), days}).subscribe({
      next: (res) => {
        this.apply(res);
        this.saving.set(false);
        this.toastr.success('排班已儲存');
      },
      error: (err) => {
        this.toastr.error(err?.error?.message ?? '排班儲存失敗');
        this.saving.set(false);
      },
    });
  }

  reset(): void {
    this.dayTypes.set({...this.original()});
  }

  // ── 月曆點格 ────────────────────────────────────────────────

  onCellClick({key, cell}: {key: string; cell: ShiftScheduleDay}): void {
    if (cell.readOnly) {
      this.toastr.info(lockedCellMessage(cell, this.data()?.editReason ?? '此日期不可變更。'));
      return;
    }

    const current = this.dayTypes()[key] ?? 'work';
    this.dayTypes.update((m) => ({...m, [key]: nextDayType(current)}));
  }

  // ── 內部 ────────────────────────────────────────────────────────

  private apply(res: ShiftScheduleMonth): void {
    const types: Record<string, ShiftDayType> = {};
    for (const d of res.days) types[dateKey(d.date)] = d.dayType;

    this.data.set(res);
    this.dayTypes.set(types);
    this.original.set({...types});
  }

  private countOf(type: ShiftDayType): number {
    return Object.values(this.dayTypes()).filter((t) => t === type).length;
  }
}
