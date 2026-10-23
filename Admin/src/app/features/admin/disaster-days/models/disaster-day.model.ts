/** 颱風假（天然災害停班）時段 */
export type DisasterSegment = 'full' | 'am' | 'pm';

export const DISASTER_SEGMENT_LABELS: Record<DisasterSegment, string> = {
  full: '全天',
  am: '上午',
  pm: '下午',
};

export interface DisasterDayOff {
  id: number;
  /** yyyy-MM-dd */
  date: string;
  county: string;
  countyName: string;
  segment: DisasterSegment;
  isPaid: boolean;
  source: 'dgpa' | 'manual';
  sourceText: string | null;
  status: 'active' | 'cancelled';
  createdAt: string;
  createdByName: string | null;
  cancelledAt: string | null;
  cancelledByName: string | null;
  affectedCount: number;
}

export interface DisasterDayCreateRequest {
  date: string;
  counties: string[];
  segment: DisasterSegment;
  isPaid?: boolean;
}

export interface DisasterDayUpdateRequest {
  segment?: DisasterSegment;
  isPaid?: boolean;
}

export interface DisasterAffectedEmployee {
  userId: string;
  name: string;
  departmentName: string;
  /** 當日有出勤（打卡與停班時段重疊） */
  worked: boolean;
  /** 不給薪時的扣薪天數（給薪 / 不扣為 null） */
  deductionDays: number | null;
}

export interface DisasterAdjustedLeave {
  leaveRequestId: number;
  requestNo: string | null;
  employeeName: string;
  leaveType: string;
  originalHours: number;
  hours: number;
}

export interface DisasterAffected {
  employees: DisasterAffectedEmployee[];
  adjustedLeaves: DisasterAdjustedLeave[];
  /** 未設定工作地縣市、無法判定的員工人數 */
  missingCountyCount: number;
}

export interface DisasterFetchManualItem {
  county: string;
  countyName: string;
  text: string;
}

export interface DisasterFetchLog {
  id: number;
  fetchedAt: string;
  sourceUpdatedAt: string | null;
  success: boolean;
  summary: string;
  manualItems: DisasterFetchManualItem[];
}

/** 個人：今日是否停班（GET /me/disaster-day-off） */
export interface MyDisasterDayOff {
  date: string;
  county: string;
  countyName: string;
  segment: DisasterSegment;
  isPaid: boolean;
}
