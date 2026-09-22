export interface ActiveLeave {
  id: number;
  leaveType: string;
  startDate: string; // ISO datetime (Asia/Taipei)
  endDate: string;
}

export interface TodayAttendance {
  id: number;
  userId: string;
  recordDate: string;
  clockInTime?: string;
  clockInLatitude?: number;
  clockInLongitude?: number;
  clockOutTime?: string;
  clockOutLatitude?: number;
  clockOutLongitude?: number;
  overtimeStartTime?: string;
  overtimeStartLatitude?: number;
  overtimeStartLongitude?: number;
  overtimeEndTime?: string;
  overtimeEndLatitude?: number;
  overtimeEndLongitude?: number;
  overtimeRequestId?: number;
  /** 當日已核准請假時段（含尚未開始 / 已結束的時段；空陣列代表當日無請假） */
  todayLeaves: ActiveLeave[];
  /**
   * 今日免下班卡即可打「加班開始」（休假日或全日請假）。
   * 由後端計算，與 POST /attendances/overtime-start 的放行條件同源，前端不自行重組規則。
   */
  canOvertimeWithoutClockOut: boolean;
  /** 該日已被標記為出差（供打卡頁勾選框帶回既有狀態） */
  isBusinessTrip: boolean;

  // ── 四週彈性工時（切換日之前一律為預設值，前端據此維持舊行為）──────────
  /** 新制是否已對今日生效。false 時不顯示確認對話框、不判早退逾時 */
  flexibleEnabled: boolean;
  /** 今日日別：work / rest_day / statutory_off / public_holiday */
  dayType?: ShiftDayTypeValue | null;
  /** 本人是否為今日活動日的預定人力（國定假日據此解鎖上下班打卡） */
  isActivityAssignee: boolean;
  /** 今日可否打上下班卡。**前端不自行重組日別規則**，只吃這個旗標 */
  canClockInOut: boolean;
  /** 不可打卡時的說明，可直接顯示給使用者 */
  clockLockReason?: string | null;
  /** 應下班時間 ＝ 實際上班打卡 ＋ 9 小時（請上午半天假者為 ＋4 小時）；尚未打上班卡為 null */
  expectedClockOutTime?: string | null;
}

export type ShiftDayTypeValue = 'work' | 'rest_day' | 'statutory_off' | 'public_holiday';

export const SHIFT_DAY_TYPE_LABELS: Record<ShiftDayTypeValue, string> = {
  work: '上班日',
  rest_day: '休假日',
  statutory_off: '例假日',
  public_holiday: '國定假日',
};

export type ClockActionType = 'clock-in' | 'clock-out' | 'overtime-start' | 'overtime-end';

export interface ClockActionRequest {
  latitude?: number;
  longitude?: number;
  overtimeRequestId?: number;
  /** 本次打卡為出差：後端以此值覆寫當日紀錄的 isBusinessTrip */
  isBusinessTrip?: boolean;
  /** 下班打卡的早退／逾時原因。非出差且早退／逾時時為必填，後端會擋 */
  reason?: string | null;
}
