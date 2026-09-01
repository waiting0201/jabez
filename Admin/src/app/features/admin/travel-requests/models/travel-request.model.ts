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

export interface TravelRequestItem {
  id?: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  note?: string;
  sortOrder: number;
}

/** 常用分類選項 */
export const ITEM_CATEGORIES = ['交通費', '住宿費', '餐費', '人事費', '雜支'] as const;

export interface TravelRequest {
  id: number;
  requestNo: string;
  employeeId?: string;
  approvalItemId?: number;
  destination: string;
  // 後端序列化為 ISO 字串（"2026-03-24T00:00:00"），runtime 實際型別是 string 而非 Date
  startDate: string;
  endDate: string;
  /** 預支款需求日（選填）：申請人希望款項撥入的日期 */
  advanceNeededDate?: string;
  grandTotal: number;
  purpose: string;
  projectId?: number;
  projectCode?: string;
  projectName?: string;
  isHolidayTravel?: boolean;
  approvalStatus: ApprovalStatus;
  designatedReviewers?: DesignatedReviewer[];
  items: TravelRequestItem[];
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
  /** 是否已結案（沖銷完成） */
  isClosed?: boolean;
  /** 結案時間 */
  closedAt?: string;
  /** 應退還差額（>0 表示需退款） */
  refundAmount?: number;
  /** 實際退款金額（財務手動填入） */
  refundedAmount?: number;
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  // 分期撥款（共用 InstallmentDto / PaymentInstallmentStatus 定義於 approval-tasks model）
  installments?: import('../../approval-tasks/models/approval-task.model').InstallmentDto[];
  paymentStatus?: import('../../approval-tasks/models/approval-task.model').PaymentInstallmentStatus;
}
