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
}

export type ClockActionType = 'clock-in' | 'clock-out' | 'overtime-start' | 'overtime-end';

export interface ClockActionRequest {
  latitude?: number;
  longitude?: number;
  overtimeRequestId?: number;
}
