export interface CalendarDay {
  id: number;
  date: string;        // "2026-01-01T00:00:00"
  isHoliday: boolean;
  description: string;
  year: number;
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
