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
}

export type ClockActionType = 'clock-in' | 'clock-out' | 'overtime-start' | 'overtime-end';

export interface ClockActionRequest {
  latitude?: number;
  longitude?: number;
  overtimeRequestId?: number;
}
