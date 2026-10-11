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
