export interface CalendarDay {
  id: number;
  date: string;        // "2026-01-01T00:00:00"
  isHoliday: boolean;
  description: string;
  year: number;
}

/** 匯入結果摘要（POST /calendar-days/import 回傳的是摘要，不是日期清單） */
export interface CalendarImportResult {
  year: number;
  totalDays: number;
  holidays: number;
  workdays: number;
}

export interface CalendarDayCreateDto {
  date: string;
  isHoliday: boolean;
  description?: string;
}

export interface CalendarDayUpdateDto {
  isHoliday: boolean;
  description?: string;
}

/**
 * 加倍工資日（選舉投票日等）：不改任何人的日別，當天照個人排班出勤，
 * 有上班打卡者於次月薪資加發 1 日日薪（併入「國定假日／加倍工資日」一行）。
 * 與行事曆分開存放，年度匯入不會覆蓋。
 */
export interface DoublePayDay {
  id: number;
  date: string;        // "2026-11-28T00:00:00"
  name: string;
}

export interface DoublePayDayCreateDto {
  date: string;        // yyyy-MM-dd
  name: string;
}
