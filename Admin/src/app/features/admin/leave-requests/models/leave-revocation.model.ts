import {ApprovalStatus, DesignatedReviewer, LeaveTimeUnit, LeaveType} from './leave-request.model';

/** 可銷 / 已銷的單日明細 */
export interface LeaveRevocationDate {
  date: string;
  hours: number;
}

/** GET /leave-requests/{id}/revocable-dates 回應（已排除已銷、進行中、今天以前的日期） */
export interface RevocableDatesResult {
  leaveRequestId: number;
  leaveType: LeaveType;
  timeUnit: LeaveTimeUnit;
  startDate: string;
  endDate: string;
  hours: number;
  reason: string;
  dates: LeaveRevocationDate[];
  totalRevocableHours: number;
}

export interface LeaveRevocation {
  id: number;
  leaveRequestId: number;
  employeeName?: string;
  reason: string;
  revokedHours: number;
  approvalStatus: ApprovalStatus;
  approvalItemId?: number;
  currentStepOrder?: number;
  reviewedById?: string;
  dates?: LeaveRevocationDate[];
  designatedReviewers?: DesignatedReviewer[];
  createdAt: string;
  submittedAt: string | null;   // 送簽日期（申請日期）；草稿為 null
  reviewedAt?: string;
  reviewNote?: string;
  // 原請假單資訊
  leaveType?: LeaveType;
  leaveStartDate?: string;
  leaveEndDate?: string;
  leaveHours?: number;
  leaveOriginalHours?: number;
  leaveApprovalStatus?: ApprovalStatus;
}

export interface CreateLeaveRevocationRequest {
  dates: string[];
  reason: string;
  designatedReviewers?: {reviewerId: string; stepOrder: number; approvalStepOrder?: number; selectedDepartmentId?: number | null}[];
}
