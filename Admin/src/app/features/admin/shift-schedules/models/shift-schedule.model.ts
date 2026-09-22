/**
 * 個人排班（四週彈性工時 · 功能 A）
 *
 * 日別四值與後端 `Api/Common/WorkDayType.cs` 的 `WorkDayTypes` 一一對應，**兩處必須同步**。
 * 國定假日（public_holiday）由公司行事曆解析，員工不可勾選、不佔 4 例 4 休配額。
 */
export type ShiftDayType = 'work' | 'rest_day' | 'statutory_off' | 'public_holiday';

/** 員工可勾選的三種（國定假日唯讀，不在其中）。點擊月曆格時依此順序循環。 */
export const SELECTABLE_DAY_TYPES: ShiftDayType[] = ['work', 'statutory_off', 'rest_day'];

export const DAY_TYPE_LABELS: Record<ShiftDayType, string> = {
  work:           '上班日',
  statutory_off:  '例假日',
  rest_day:       '休假日',
  public_holiday: '國定假日',
};

/**
 * 月曆格的配色。三種可勾選狀態各一色，國定假日另一色 —— 與〈出勤／排休總覽表〉的三色一致。
 * 值為 tailwind.css 的 CIS token，不可改寫成字面色碼。
 */
export const DAY_TYPE_STYLES: Record<ShiftDayType, { bg: string; fg: string; border: string }> = {
  work:           { bg: 'var(--bg-surface)',  fg: 'var(--text-primary)',   border: 'var(--border)' },
  statutory_off:  { bg: '#F0E4E4',            fg: 'var(--red)',            border: 'var(--red)' },
  rest_day:       { bg: '#E7EFE1',            fg: 'var(--green)',          border: 'var(--green)' },
  public_holiday: { bg: 'var(--bg-elevated)', fg: 'var(--text-secondary)', border: 'var(--border)' },
};

export interface ShiftScheduleDay {
  date: string;                  // ISO yyyy-MM-ddTHH:mm:ss
  dayType: ShiftDayType;
  holidayName: string | null;    // 國定假日名稱（國慶日 / 補假…）
  readOnly: boolean;             // 國定假日、或此刻不可編輯的日期
  isActivityDay: boolean;        // 主管排定的活動日（疊加旗標，與 dayType 並存）
  activityTitle: string | null;
  isActivityAssignee: boolean;   // 本人是否為該活動日的預定人力
}

/**
 * 擋存判準結果。**進入畫面時就要顯示** —— 空白月曆必然擋存，
 * 不可等到按下儲存才報錯（客戶已確認此為預期行為）。
 */
export interface ShiftScheduleValidation {
  canSave: boolean;
  blocks: string[];
  warnings: string[];
  statutoryOffCount: number;
  restDayCount: number;
  requiredStatutoryOff: number;
  requiredRestDay: number;
}

/** 可編輯情形。同 `Api/Common/ShiftScheduleWindow.cs` 的 `ShiftScheduleEditMode`。 */
export type ShiftEditMode = 'open' | 'grace_period' | 'same_day_only' | 'closed';

export interface ShiftScheduleMonth {
  userId: string;
  userName: string;
  year: number;
  month: number;
  editable: boolean;
  editMode: ShiftEditMode;
  editReason: string;
  status: 'draft' | 'committed' | 'auto' | null;
  committedAt: string | null;
  autoAssignedAt: string | null;
  days: ShiftScheduleDay[];
  validation: ShiftScheduleValidation;
}

export interface SaveShiftScheduleRequest {
  year: number;
  month: number;
  days: { date: string; dayType: ShiftDayType }[];
}

/** 點一下切到下一個狀態：上班日 → 例假日 → 休假日 → 上班日。 */
export function nextDayType(current: ShiftDayType): ShiftDayType {
  const i = SELECTABLE_DAY_TYPES.indexOf(current);
  return SELECTABLE_DAY_TYPES[(i + 1) % SELECTABLE_DAY_TYPES.length];
}

/** 取日期字串的 yyyy-MM-dd 部分。刻意用字串切割避免 UTC 位移（同 leave-request.model 的 isoTime）。 */
export function dateKey(iso: string): string {
  return iso.slice(0, 10);
}
