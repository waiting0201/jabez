import {ShiftDayType} from './shift-schedule.model';

/** 改班申請的單日明細：哪一天、從什麼改成什麼 */
export interface ShiftChangeDate {
  date: string;
  fromDayType: ShiftDayType;
  toDayType: ShiftDayType;
  /** 國定假日 → 唯讀，不可申請變更 */
  isPublicHoliday?: boolean;
}

/** 可申請改班的日期清單（含逐日現況與目前配額） */
export interface ChangeableShiftDates {
  year: number;
  month: number;
  monthStatus: string | null;
  dates: ShiftChangeDate[];
  statutoryOffCount: number;
  restDayCount: number;
  requiredStatutoryOff: number;
  requiredRestDay: number;
}

export interface ShiftChangeRequest {
  id: number;
  requestNo: string | null;      // SC-yyyyMMdd-NNN；草稿為 null
  employeeId: string | null;
  employeeName: string;
  departmentName: string | null;
  year: number;
  month: number;
  reason: string;
  approvalStatus: string;
  createdAt: string;
  submittedAt: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  approvalItemId?: number | null;
  currentStepOrder?: number | null;
  dates?: ShiftChangeDate[];
  designatedReviewers?: unknown[];
}

export interface SaveShiftChangeRequest {
  year: number;
  month: number;
  reason: string;
  dates: {date: string; toDayType: ShiftDayType}[];
  designatedReviewers?: unknown[];
}
