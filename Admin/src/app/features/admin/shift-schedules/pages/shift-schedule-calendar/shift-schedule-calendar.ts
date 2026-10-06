import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Router, RouterLink} from '@angular/router';
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
  ShiftScheduleAdjustment,
  ShiftScheduleDay,
  ShiftScheduleMonth,
  dateKey,
  isLockedOffCell,
  nextDayType,
} from '../../models/shift-schedule.model';
import {NotificationService} from '../../../notifications/services/notification.service';
import {AuthService} from '../../../../../core/auth/services/auth.service';
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
  private router = inject(Router);
  private auth = inject(AuthService);
  private notification = inject(NotificationService);

  /**
   * 活動日覆蓋本人班表的通知（未按「我知道了」者）。主管把活動日排在本人的例假／休假上時，
   * 系統已改為上班日並自動補排，這裡告訴本人改了什麼（鈴鐺點進來就是看這張卡）。
   */
  adjustments = signal<ShiftScheduleAdjustment[]>([]);
  acknowledging = signal(false);

  /**
   * 我的改班申請（最近幾張）。原本申請人沒有任何入口看得到自己送出的單，
   * 找不到就重開一張，審核者因而收到重複的申請（2026-09-28 修正，後端同時限制同月一張）。
   */
  myChanges = signal<ShiftChangeRequest[]>([]);
  readonly statusLabels = APPROVAL_STATUS_LABELS;
  readonly statusClasses = APPROVAL_STATUS_CLASSES;

  readonly labels = DAY_TYPE_LABELS;

  // 預設看「此刻還能排的最近一個月」：次月開放至本月 25 日，26 日起次月已截止、改看下下月
  //（開放期＝前三個月 10 日 ～ 前一個月 25 日，後端 ShiftScheduleWindow 為單一真相，這裡只決定預設月份）
  private static readonly defaultPeriod = (() => {
    const d = new Date();
    const next = new Date(d.getFullYear(), d.getMonth() + (d.getDate() > 25 ? 2 : 1), 1);
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
  /** 例假＋休假合計應排天數（後端給的單一真相；尚未載入時依當月天數推回 8 / 9）。 */
  readonly requiredOffDays = computed(() => this.data()?.validation.requiredOffDays
    ?? (new Date(this.year(), this.month(), 0).getDate() === 31 ? 9 : 8));

  /**
   * 休假應排天數隨編輯中的例假即時變動：多排的例假由休假轉入（休息總天數不變），
   * 同後端 `ShiftScheduleValidator.RequiredRestDaysFor(year, month, statutoryOff)`。
   * 只影響計數分母的顯示，能不能存仍由後端判定。
   */
  readonly requiredRestDay = computed(() => Math.max(0,
    this.requiredOffDays() - Math.max(this.requiredStatutoryOff(), this.statutoryOffCount())));

  /** 例假是否已排滿 —— 唯一在前端判斷的一條，只為了即時提示，送出仍由後端把關。 */
  readonly statutoryOffSatisfied = computed(() =>
    this.statutoryOffCount() >= this.requiredStatutoryOff());

  /**
   * 格子上顯示「請假」鈕的日子：**已儲存**為上班日、今天（含）以後，且持有 leave-requests:write。
   * 以已儲存的班表為準而非編輯中的狀態 —— 後端算請假日看的是已存的個人排班，
   * 用未儲存的狀態會讓使用者在「還沒存的上班日」送出一張算成 0 天的假。
   * 國定假日本來就不是上班日，自然排除。
   */
  readonly leaveDates = computed<Record<string, true>>(() => {
    if (!this.auth.hasPermission('leave-requests:write')) return {};
    const today = todayKey();
    const result: Record<string, true> = {};
    for (const [key, type] of Object.entries(this.original())) {
      if (type === 'work' && key >= today) result[key] = true;
    }
    return result;
  });

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y - 1, y, y + 1];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  ngOnInit(): void {
    this.load();
    this.loadAdjustments();
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

  // ── 活動日覆蓋通知 ──────────────────────────────────────────

  /** 跳到該筆通知所在的月份。 */
  viewAdjustment(a: ShiftScheduleAdjustment): void {
    const [y, m] = dateKey(a.date).split('-').map(Number);
    if (y === this.year() && m === this.month()) return;
    if (this.dirty()) {
      this.toastr.warning('排班尚未儲存，請先儲存或還原變更後再切換月份。');
      return;
    }
    this.year.set(y);
    this.month.set(m);
    this.load();
  }

  acknowledgeAdjustments(): void {
    if (this.acknowledging()) return;
    this.acknowledging.set(true);
    this.svc.acknowledgeAdjustments().subscribe({
      next: () => {
        this.adjustments.set([]);
        this.acknowledging.set(false);
        this.notification.refresh().subscribe();
      },
      error: (err) => {
        this.toastr.error(err?.error?.message ?? '操作失敗');
        this.acknowledging.set(false);
      },
    });
  }

  /** 「11/15 例假日 → 已移至 11/16」這種 M/d 顯示。 */
  md(iso: string): string {
    const [, m, d] = dateKey(iso).split('-').map(Number);
    return `${m}/${d}`;
  }

  // ── 月曆點格 ────────────────────────────────────────────────

  onCellClick({key, cell}: {key: string; cell: ShiftScheduleDay}): void {
    if (cell.readOnly) {
      this.toastr.info(lockedCellMessage(cell, this.data()?.editReason ?? '此日期不可變更。'));
      return;
    }

    const current = this.dayTypes()[key] ?? 'work';

    // 活動日／請假鎖定格卻排著例假／休假：只能改成上班日（後端存檔時同樣要求），不參與三態循環
    if (cell.lockReason) {
      if (isLockedOffCell(cell, current)) {
        this.dayTypes.update((m) => ({...m, [key]: 'work'}));
        this.toastr.info('已改為上班日，請記得另選一天補排' + DAY_TYPE_LABELS[current] + '。');
      } else {
        this.toastr.info(lockedCellMessage(cell, this.data()?.editReason ?? '此日期不可變更。'));
      }
      return;
    }

    this.dayTypes.update((m) => ({...m, [key]: nextDayType(current)}));
  }

  /** 格子上的「請假」鈕：開新增請假表單並帶入該日。有未儲存的排班時先擋下，否則離開頁面會丟掉變更。 */
  onLeaveClick(date: string): void {
    if (this.dirty()) {
      this.toastr.warning('排班尚未儲存，請先儲存或還原變更後再請假。');
      return;
    }
    this.router.navigate(['/admin/leave-requests/new'], {queryParams: {date}});
  }

  // ── 內部 ────────────────────────────────────────────────────────

  private loadAdjustments(): void {
    this.svc.getAdjustments().subscribe({
      next: (rows) => this.adjustments.set(rows ?? []),
      error: () => this.adjustments.set([]),
    });
  }

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

/** 今天的 yyyy-MM-dd（本地時間，避免 toISOString() 的 UTC 位移）。 */
function todayKey(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
