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
}

export type ClockActionType = 'clock-in' | 'clock-out' | 'overtime-start' | 'overtime-end';

export interface ClockActionRequest {
  latitude?: number;
  longitude?: number;
  overtimeRequestId?: number;
  /** 本次打卡為出差：後端以此值覆寫當日紀錄的 isBusinessTrip */
  isBusinessTrip?: boolean;
}
