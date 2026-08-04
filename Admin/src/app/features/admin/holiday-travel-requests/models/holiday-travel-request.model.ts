export type ApprovalStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned';

export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
  draft:    '草稿',
  pending:  '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

export const APPROVAL_STATUS_CLASSES: Record<ApprovalStatus, string> = {
  draft:    'bg-blue-subtle text-blue-emphasis',
  pending:  'bg-warning-subtle text-warning-emphasis',
  approved: 'bg-success-subtle text-success',
  rejected: 'bg-danger-subtle text-danger',
  returned: 'bg-secondary-subtle text-secondary',
};

export interface DesignatedReviewer {
  id?: number;
  reviewerId: string;
  reviewerName?: string;
  stepOrder: number;
  approvalStepOrder?: number;           // 此 designee 屬於哪個 designated step 的 stepOrder
  selectedDepartmentId?: number | null; // 需選部門時的選定部門
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

/**
 * 參與時段：全天 / 上半天 / 下半天（半天以 0.5 天計入假日津貼）。
 * 需與後端 Constants.cs 的 ParticipantDateSlots 保持同步。
 */
export type ParticipantDaySlot = 'full' | 'am' | 'pm';

export const PARTICIPANT_SLOT_LABELS: Record<ParticipantDaySlot, string> = {
  full: '全天',
  am:   '上午',
  pm:   '下午',
};

/** 時段天數權重：全天 1、上/下半天 0.5 */
export function participantSlotWeight(slot: ParticipantDaySlot): number {
  return slot === 'full' ? 1 : 0.5;
}

/** 天數顯示：整數不補小數（3）、半天顯示一位（2.5） */
export function formatParticipantDays(days: number): string {
  return Number.isInteger(days) ? String(days) : days.toFixed(1);
}

/** 個人參與日期 + 時段 */
export interface ParticipantDate {
  /** yyyy-MM-dd */
  date: string;
  slot: ParticipantDaySlot;
}

/** 參與執行人員 */
export interface TravelParticipant {
  userId: string;
  userName?: string;
  sortOrder: number;
  /** 個人參與日期（空/未提供＝全程參與） */
  dates?: ParticipantDate[];
  /** 個人假日天數（Submit 時後端計算，半天以 0.5 計；null＝全程參與，沿用整單 holidayDays） */
  holidayDays?: number | null;
}

export interface HolidayTravelRequest {
  id: number;
  requestNo: string;
  employeeId?: string;
  approvalItemId?: number;
  /** 執行活動地點（前端標籤，DB 欄位：destination） */
  destination: string;
  /** 開始日期（DB 欄位：startDate） */
  startDate: Date;
  /** 結束日期（DB 欄位：endDate） */
  endDate: Date;
  /** 活動主旨及內容（DB 欄位：purpose） */
  purpose: string;
  projectId?: number;
  projectCode?: string;
  projectName?: string;
  /** 假日天數（由後端計算，startDate 至 endDate 的天數） */
  holidayDays?: number;
  /** 參與執行人員 */
  participants?: TravelParticipant[];
  approvalStatus: ApprovalStatus;
  designatedReviewers?: DesignatedReviewer[];
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
  /** 申請總額（由後端計算 SUM(items.totalPrice)）*/
  grandTotal?: number;
  // 分期撥款（共用 InstallmentDto / PaymentInstallmentStatus 定義於 approval-tasks model）
  installments?: import('../../approval-tasks/models/approval-task.model').InstallmentDto[];
  paymentStatus?: import('../../approval-tasks/models/approval-task.model').PaymentInstallmentStatus;
}
