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
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

/** 參與執行人員 */
export interface TravelParticipant {
  userId: string;
  userName?: string;
  sortOrder: number;
}

export interface HolidayTravelRequest {
  id: number;
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
