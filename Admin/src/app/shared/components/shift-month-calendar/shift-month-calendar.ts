import {Component, OnInit, effect, input, output, signal, viewChild} from '@angular/core';
import {FullCalendarComponent, FullCalendarModule} from '@fullcalendar/angular';
import {CalendarOptions, DayCellContentArg} from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin, {DateClickArg} from '@fullcalendar/interaction';
import zhTwLocale from '@fullcalendar/core/locales/zh-tw';

import {
  DAY_TYPE_LABELS,
  ShiftDayType,
  ShiftScheduleDay,
  dateKey,
} from '../../../features/admin/shift-schedules/models/shift-schedule.model';

/**
 * 共用排班月曆（個人排班 / 改班申請 / 簽核詳情頁 三處共用，2026-09-28 由個人排班頁抽出）。
 *
 * 純呈現元件：資料全由父層傳入，點格只 emit `cellClick`，要不要切換狀態由父層決定。
 * FullCalendar `dayGridMonth` 只借月格骨架，每格狀態不是 event（見 frontend-design.md §12.8）。
 *
 * ⚠ 重繪一律走 `getApi().render()`：`dayCellClassNames` / `dayCellContent` 是同一個函式參考，
 * 換 options 物件 FullCalendar 會判定「沒變」而不重畫。本元件以 effect 監看所有 input，變了就重畫。
 * ⚠ 首次渲染吃 `initialDate`（ngOnInit 時由 input 組出），之後的月份切換走 `gotoDate()`。
 */
@Component({
  selector: 'app-shift-month-calendar',
  imports: [FullCalendarModule],
  template: `
    <div class="flex flex-wrap items-center gap-3 small text-secondary mb-3">
      <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--statutory-off"></i>例假日</span>
      <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--rest-day"></i>休假日</span>
      <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--public-holiday"></i>國定假日</span>
      <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--activity"></i>活動日</span>
      <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--leave"></i>已請假</span>
      @if (showChangedLegend()) {
        <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--changed"></i>本次調整</span>
      }
    </div>
    <!-- 手機採橫向捲動而非壓縮欄寬：7 欄硬塞進 390px 會讓長假名一字一行、列高爆開 -->
    <div class="shift-calendar-scroll" [class.shift-month-calendar--static]="!interactive()">
      <div class="shift-calendar-inner">
        @if (options(); as opts) {
          <full-calendar #calendar [options]="opts"></full-calendar>
        }
      </div>
    </div>
  `,
  styleUrl: './shift-month-calendar.scss',
})
export class ShiftMonthCalendar implements OnInit {
  year = input.required<number>();
  month = input.required<number>();
  /** 每格的附加資訊（唯讀、假日名稱、活動、請假）。 */
  days = input<ShiftScheduleDay[]>([]);
  /** 目前要呈現的日別（key = yyyy-MM-dd）；未給的格子取 days 內的 dayType。 */
  dayTypes = input<Record<string, ShiftDayType>>({});
  /** 改班異動：key = yyyy-MM-dd、value = 原日別。有值的格子加虛線框並顯示「原：xx」。 */
  changedFrom = input<Record<string, ShiftDayType>>({});
  /** 可點選（false ＝ 唯讀檢視，不 emit 也不顯示手指游標）。 */
  interactive = input(true);
  showChangedLegend = input(false);

  cellClick = output<{key: string; cell: ShiftScheduleDay}>();

  private calendarRef = viewChild<FullCalendarComponent>('calendar');
  options = signal<CalendarOptions | null>(null);

  private meta: Record<string, ShiftScheduleDay> = {};

  constructor() {
    effect(() => {
      const meta: Record<string, ShiftScheduleDay> = {};
      for (const d of this.days()) meta[dateKey(d.date)] = d;
      this.meta = meta;

      // 讀取所有會影響畫面的 input，任一變動都重畫
      this.dayTypes();
      this.changedFrom();
      const date = this.firstOfMonth();

      const api = this.calendarRef()?.getApi();
      api?.gotoDate(date);
      api?.render();
    });
  }

  ngOnInit(): void {
    this.options.set({
      plugins: [dayGridPlugin, interactionPlugin],
      initialView: 'dayGridMonth',
      initialDate: this.firstOfMonth(),
      locale: zhTwLocale,
      firstDay: 1,                 // 週一起始，與排班「連續上班天數」的直覺一致
      height: 'auto',
      headerToolbar: false,        // 月份切換由父層控制
      fixedWeekCount: false,
      showNonCurrentDates: false,  // 只顯示當月，避免點到別月的格子
      dayCellClassNames: (arg) => this.cellClasses(arg),
      dayCellContent: (arg) => this.cellContent(arg),
      dateClick: (arg) => this.onDateClick(arg),
    });
  }

  private onDateClick(arg: DateClickArg): void {
    if (!this.interactive()) return;
    const cell = this.meta[arg.dateStr];
    if (cell) this.cellClick.emit({key: arg.dateStr, cell});
  }

  private typeOf(key: string): ShiftDayType {
    return this.dayTypes()[key] ?? this.meta[key]?.dayType ?? 'work';
  }

  private cellClasses(arg: DayCellContentArg): string[] {
    const key = keyOf(arg.date);
    const cell = this.meta[key];
    const type = this.typeOf(key);

    const classes = ['shift-cell', `shift-cell--${type.replace('_', '-')}`];
    if (cell?.readOnly) classes.push('shift-cell--readonly');
    if (cell?.isActivityDay) classes.push('shift-cell--activity');
    if (cell?.leaveLabel) classes.push('shift-cell--leave');
    if (this.changedFrom()[key]) classes.push('shift-cell--changed');
    return classes;
  }

  private cellContent(arg: DayCellContentArg): {html: string} {
    const key = keyOf(arg.date);
    const cell = this.meta[key];
    const type = this.typeOf(key);

    const parts = [`<div class="shift-cell__num">${arg.dayNumberText.replace('日', '')}</div>`];

    // 國定假日顯示名稱本身；彈性休假日（可排班）名稱與狀態並列
    const typeLabel = type === 'work' || type === 'public_holiday' ? '' : DAY_TYPE_LABELS[type];
    const label = [cell?.holidayName, typeLabel].filter(Boolean).join('・');
    if (label) parts.push(`<div class="shift-cell__label" title="${escapeHtml(label)}">${escapeHtml(label)}</div>`);

    const from = this.changedFrom()[key];
    if (from) parts.push(`<div class="shift-cell__from">原：${DAY_TYPE_LABELS[from]}</div>`);

    if (cell?.leaveLabel) parts.push(`<div class="shift-cell__leave">${escapeHtml(cell.leaveLabel)}</div>`);

    if (cell?.isActivityDay) {
      const title = cell.activityTitle ?? '活動日';
      const mine = cell.isActivityAssignee ? ' shift-cell__activity--mine' : '';
      parts.push(`<div class="shift-cell__activity${mine}">${escapeHtml(title)}</div>`);
    }
    return {html: parts.join('')};
  }

  private firstOfMonth(): string {
    return `${this.year()}-${String(this.month()).padStart(2, '0')}-01`;
  }
}

/** 月曆格被點到但不可變更時，給使用者看的原因（三處共用同一份文案）。 */
export function lockedCellMessage(cell: ShiftScheduleDay, fallback: string): string {
  if (cell.dayType === 'public_holiday')
    return `${cell.holidayName ?? '該日'}為國定假日，不佔例假／休假配額，無法變更。`;
  if (cell.lockReason === 'leave')
    return `該日已請${cell.leaveLabel ?? '假'}，不可排定為例假日或休假日。`;
  if (cell.lockReason === 'activity')
    return `該日為您的活動日（${cell.activityTitle ?? '活動'}），不可排定為例假日或休假日。`;
  if (cell.lockReason === 'change')
    return '該日已在另一張進行中的改班申請內。';
  return fallback;
}

/** 以本地時間組 yyyy-MM-dd，避免 toISOString() 的 UTC 位移把日期退一天。 */
function keyOf(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${m}-${day}`;
}

function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, (c) =>
    ({'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[c] ?? c));
}
