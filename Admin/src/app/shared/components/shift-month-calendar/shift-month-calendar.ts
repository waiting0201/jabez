import {Component, ElementRef, OnInit, effect, inject, input, output, signal, viewChild} from '@angular/core';
import {FullCalendarComponent, FullCalendarModule} from '@fullcalendar/angular';
import {CalendarOptions, DayCellContentArg} from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin, {DateClickArg} from '@fullcalendar/interaction';
import zhTwLocale from '@fullcalendar/core/locales/zh-tw';

import {
  DAY_TYPE_LABELS,
  ShiftDayType,
  ShiftScheduleAdjacentDay,
  ShiftScheduleDay,
  dateKey,
} from '../../../features/admin/shift-schedules/models/shift-schedule.model';

/**
 * 共用排班月曆（個人排班 / 改班申請 / 簽核詳情頁 三處共用，2026-09-28 由個人排班頁抽出）。
 *
 * 純呈現元件：資料全由父層傳入，點格只 emit `cellClick`，要不要切換狀態由父層決定。
 * `leaveDates` 有值的格子另顯示「請假」鈕，點了 emit `leaveClick`（不會同時觸發 `cellClick`）。
 * FullCalendar `dayGridMonth` 只借月格骨架，每格狀態不是 event（見 frontend-design.md §12.8）。
 *
 * ⚠ 重繪一律走 `getApi().render()`：`dayCellClassNames` / `dayCellContent` 是同一個函式參考，
 * 換 options 物件 FullCalendar 會判定「沒變」而不重畫。本元件以 effect 監看所有 input，變了就重畫。
 * ⚠ 首次渲染吃 `initialDate`（ngOnInit 時由 input 組出），之後的月份切換走 `gotoDate()`。
 * `adjacentDays` 有給（非 null）才顯示前後月的格子：唯讀、淡化、不 emit；未給的呼叫端維持只顯示當月。
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
      @if (adjacentDays()) {
        <span class="flex items-center gap-1"><i class="shift-swatch shift-swatch--adjacent"></i>前後月（僅供對照）</span>
      }
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
  /**
   * 顯示「請假」鈕的日子（key = yyyy-MM-dd）。哪些日子可以請假由父層決定（只有本人、已存為上班日、今天以後），
   * 本元件只負責畫。⚠ 同 dayTypes，必須是穩定參考。
   */
  leaveDates = input<Record<string, true>>({});
  /**
   * 前後月的日別（僅供對照）。null ＝ 不顯示前後月的格子（改班申請 / 簽核頁維持只看當月）。
   * ⚠ 是否顯示於 ngOnInit 決定一次（`showNonCurrentDates`），故呼叫端要嘛一律給、要嘛一律不給。
   */
  adjacentDays = input<ShiftScheduleAdjacentDay[] | null>(null);

  cellClick = output<{key: string; cell: ShiftScheduleDay}>();
  leaveClick = output<string>();

  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  private calendarRef = viewChild<FullCalendarComponent>('calendar');
  options = signal<CalendarOptions | null>(null);

  private meta: Record<string, ShiftScheduleDay> = {};
  private adjacent: Record<string, ShiftScheduleAdjacentDay> = {};

  constructor() {
    effect(() => {
      const meta: Record<string, ShiftScheduleDay> = {};
      for (const d of this.days()) meta[dateKey(d.date)] = d;
      this.meta = meta;

      const adjacent: Record<string, ShiftScheduleAdjacentDay> = {};
      for (const d of this.adjacentDays() ?? []) adjacent[dateKey(d.date)] = d;
      this.adjacent = adjacent;

      // 讀取所有會影響畫面的 input，任一變動都重畫
      this.dayTypes();
      this.changedFrom();
      this.leaveDates();
      const date = this.firstOfMonth();

      const api = this.calendarRef()?.getApi();
      api?.gotoDate(date);
      api?.render();
    });
  }

  ngOnInit(): void {
    // 「請假」鈕是 dayCellContent 產生的原生 <button>：以委派監聽接住滑鼠與鍵盤（Enter / 空白鍵）兩種點擊。
    // 不能只靠 dateClick —— 鍵盤觸發的 click 不會經過 FullCalendar 的 pointer 事件。
    this.host.nativeElement.addEventListener('click', (e) => {
      const btn = (e.target as HTMLElement).closest<HTMLElement>('[data-leave-date]');
      if (btn?.dataset['leaveDate']) this.leaveClick.emit(btn.dataset['leaveDate']);
    });

    this.options.set({
      plugins: [dayGridPlugin, interactionPlugin],
      initialView: 'dayGridMonth',
      initialDate: this.firstOfMonth(),
      locale: zhTwLocale,
      firstDay: 1,                 // 週一起始，與排班「連續上班天數」的直覺一致
      height: 'auto',
      headerToolbar: false,        // 月份切換由父層控制
      fixedWeekCount: false,
      // 前後月的格子只在有對照資料時顯示；點了也不會 emit（meta 只有當月）
      showNonCurrentDates: this.adjacentDays() !== null,
      dayCellClassNames: (arg) => this.cellClasses(arg),
      dayCellContent: (arg) => this.cellContent(arg),
      dateClick: (arg) => this.onDateClick(arg),
    });
  }

  private onDateClick(arg: DateClickArg): void {
    if (!this.interactive()) return;
    // 點的是「請假」鈕 → 交給上面的委派監聽，不切換日別
    if ((arg.jsEvent.target as HTMLElement).closest('[data-leave-date]')) return;
    const cell = this.meta[arg.dateStr];
    if (cell) this.cellClick.emit({key: arg.dateStr, cell});
  }

  private typeOf(key: string): ShiftDayType {
    return this.dayTypes()[key] ?? this.meta[key]?.dayType ?? 'work';
  }

  private cellClasses(arg: DayCellContentArg): string[] {
    const key = keyOf(arg.date);
    if (arg.isOther) {
      const type = this.adjacent[key]?.dayType;
      return ['shift-cell', 'shift-cell--adjacent', type ? `shift-cell--${type.replace('_', '-')}` : 'shift-cell--unknown'];
    }
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
    if (arg.isOther) return this.adjacentContent(arg.date, key);
    const cell = this.meta[key];
    const type = this.typeOf(key);

    const parts = [`<div class="shift-cell__num">${arg.dayNumberText.replace('日', '')}</div>`];

    // 國定假日（含彈性休假日）顯示名稱本身；其餘日別名稱與狀態並列
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

    // 只有「目前仍是上班日」的格子給請假鈕：使用者剛把它點成休假、還沒儲存時就不該出現
    if (this.leaveDates()[key] && type === 'work') {
      parts.push(`<button type="button" class="shift-cell__leave-btn" data-leave-date="${key}"`
        + ` aria-label="${key} 請假">請假</button>`);
    }
    return {html: parts.join('')};
  }

  /** 前後月的格子：日期加註月份以免與當月混淆；未定案的月份標「未排定」。 */
  private adjacentContent(date: Date, key: string): {html: string} {
    const day = this.adjacent[key];
    const parts = [`<div class="shift-cell__num">${date.getMonth() + 1}/${date.getDate()}</div>`];

    let label = '';
    if (!day || day.dayType === null) label = '未排定';
    else if (day.dayType === 'public_holiday') label = day.holidayName ?? DAY_TYPE_LABELS.public_holiday;
    else if (day.dayType !== 'work') label = DAY_TYPE_LABELS[day.dayType];

    if (label) parts.push(`<div class="shift-cell__label" title="${escapeHtml(label)}">${escapeHtml(label)}</div>`);
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
