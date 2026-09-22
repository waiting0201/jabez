import {Component, computed, inject, OnInit, signal, viewChild} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ToastrService} from 'ngx-toastr';
import {FullCalendarComponent, FullCalendarModule} from '@fullcalendar/angular';
import {CalendarOptions, DayCellContentArg} from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin, {DateClickArg} from '@fullcalendar/interaction';
import zhTwLocale from '@fullcalendar/core/locales/zh-tw';

import {ShiftScheduleService} from '../../services/shift-schedule.service';
import {
  DAY_TYPE_LABELS,
  ShiftDayType,
  ShiftScheduleDay,
  ShiftScheduleMonth,
  dateKey,
  nextDayType,
} from '../../models/shift-schedule.model';

/**
 * 個人排班排例／休（四週彈性工時 · 功能 A）。
 *
 * 月曆採 FullCalendar `dayGridMonth`（專案第一個 FullCalendar 使用點）：
 * 只借它的月格骨架與月份切換，**每格的狀態不是 FullCalendar 的 event**，
 * 而是本元件的 `dayTypes` signal，透過 `dayCellClassNames` 上色、`dayCellContent` 補文字。
 * 這樣「一格一個四選一狀態」才不必硬塞進 event 模型。
 *
 * 三條規則不在前端重算（後端 `ShiftScheduleValidator` 為單一真相）：
 * 畫面只顯示後端回傳的 `blocks` / `warnings`，前端僅即時算「已排幾天」這種純計數。
 */
@Component({
  selector: 'app-shift-schedule-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule, FullCalendarModule],
  templateUrl: './shift-schedule-calendar.html',
  styleUrl: './shift-schedule-calendar.scss',
})
export class ShiftScheduleCalendar implements OnInit {
  private svc = inject(ShiftScheduleService);
  private toastr = inject(ToastrService);

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
  private dayTypes = signal<Record<string, ShiftDayType>>({});
  private original = signal<Record<string, ShiftDayType>>({});

  /** 每格的附加資訊（唯讀、國定假日名稱、活動日），不隨編輯改變。 */
  private meta = signal<Record<string, ShiftScheduleDay>>({});

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

  /**
   * ⚠ **重繪一定要走 `getApi().render()`，不能靠換掉 options 物件**。
   * `dayCellClassNames` / `dayCellContent` 是同一個函式參考，FullCalendar 比對 options 時
   * 會認定「沒有變更」而不重繪 —— 症狀是狀態與計數都對、只有格子顏色停在舊值。
   */
  private calendarRef = viewChild<FullCalendarComponent>('calendar');

  calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, interactionPlugin],
    initialView: 'dayGridMonth',
    // ⚠ 首次渲染吃 initialDate，之後的月份切換才走 gotoDate() ——
    // ngOnInit 的載入早於 view 就緒，此時 calendarRef() 還是 undefined，
    // 只靠 gotoDate 會讓月曆停在「今天所在的月」而與資料對不上。
    initialDate: `${ShiftScheduleCalendar.defaultPeriod.year}-`
               + `${String(ShiftScheduleCalendar.defaultPeriod.month).padStart(2, '0')}-01`,
    locale: zhTwLocale,
    firstDay: 1,                 // 週一起始，與排班「連續上班天數」的直覺一致
    height: 'auto',
    headerToolbar: false,        // 月份切換用頁面上的年／月下拉，不用 FullCalendar 自己的工具列
    fixedWeekCount: false,
    showNonCurrentDates: false,  // 只顯示當月，避免使用者點到別月的格子
    dayCellClassNames: (arg) => this.cellClasses(arg),
    dayCellContent: (arg) => this.cellContent(arg),
    dateClick: (arg) => this.onDateClick(arg),
  };

  ngOnInit(): void {
    this.load();
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
    this.redraw();
  }

  // ── FullCalendar 掛鉤 ───────────────────────────────────────────

  private onDateClick(arg: DateClickArg): void {
    const key = arg.dateStr;
    const cell = this.meta()[key];
    if (!cell) return;

    if (cell.readOnly) {
      this.toastr.info(cell.holidayName
        ? `${cell.holidayName}為國定假日，不佔例假／休假配額，無法變更。`
        : (this.data()?.editReason ?? '此日期不可變更。'));
      return;
    }

    const current = this.dayTypes()[key] ?? 'work';
    this.dayTypes.update((m) => ({...m, [key]: nextDayType(current)}));
    this.redraw();
  }

  /** FullCalendar 不知道 signal 變了，得明確要它重畫。 */
  private redraw(): void {
    this.calendarRef()?.getApi()?.render();
  }

  private cellClasses(arg: DayCellContentArg): string[] {
    const key = this.keyOf(arg.date);
    const cell = this.meta()[key];
    const type = this.dayTypes()[key] ?? cell?.dayType ?? 'work';

    const classes = [`shift-cell`, `shift-cell--${type.replace('_', '-')}`];
    if (cell?.readOnly) classes.push('shift-cell--readonly');
    if (cell?.isActivityDay) classes.push('shift-cell--activity');
    return classes;
  }

  private cellContent(arg: DayCellContentArg): {html: string} {
    const key = this.keyOf(arg.date);
    const cell = this.meta()[key];
    const type = this.dayTypes()[key] ?? cell?.dayType ?? 'work';

    const parts = [`<div class="shift-cell__num">${arg.dayNumberText.replace('日', '')}</div>`];

    // 國定假日顯示名稱本身（比「國定假日」四個字有用），其餘顯示狀態
    const label = cell?.holidayName ?? (type === 'work' ? '' : DAY_TYPE_LABELS[type]);
    if (label) parts.push(`<div class="shift-cell__label">${escapeHtml(label)}</div>`);

    if (cell?.isActivityDay) {
      const title = cell.activityTitle ?? '活動日';
      const mine = cell.isActivityAssignee ? ' shift-cell__activity--mine' : '';
      parts.push(`<div class="shift-cell__activity${mine}">${escapeHtml(title)}</div>`);
    }
    return {html: parts.join('')};
  }

  // ── 內部 ────────────────────────────────────────────────────────

  private apply(res: ShiftScheduleMonth): void {
    const types: Record<string, ShiftDayType> = {};
    const meta: Record<string, ShiftScheduleDay> = {};

    for (const d of res.days) {
      const key = dateKey(d.date);
      types[key] = d.dayType;
      meta[key] = d;
    }

    this.data.set(res);
    this.meta.set(meta);
    this.dayTypes.set(types);
    this.original.set({...types});

    // 月份切換走 API，不改 initialDate（那只在第一次建立時生效）
    const api = this.calendarRef()?.getApi();
    api?.gotoDate(`${res.year}-${String(res.month).padStart(2, '0')}-01`);
    api?.render();
  }

  private countOf(type: ShiftDayType): number {
    return Object.values(this.dayTypes()).filter((t) => t === type).length;
  }

  /** 以本地時間組 yyyy-MM-dd，避免 toISOString() 的 UTC 位移把日期退一天。 */
  private keyOf(d: Date): string {
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }
}

function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) =>
    ({'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[c] ?? c));
}
