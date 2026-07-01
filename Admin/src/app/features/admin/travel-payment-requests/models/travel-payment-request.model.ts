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

export interface TravelPaymentRequestItem {
  id?: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  note?: string;
  sortOrder: number;
  invoiceNo?: string;
  fileName?: string;
  fileUrl?: string;
  invoiceDate?: string;
}

/** 常用分類選項 */
export const ITEM_CATEGORIES = ['交通費', '住宿費', '餐費', '人事費', '雜支'] as const;

export interface TravelPaymentRequest {
  id: number;
  requestNo: string;
  employeeId?: string;
  employeeName?: string;
  approvalItemId?: number;
  destination: string;
  startDate: Date;
  endDate: Date;
  grandTotal: number;
  purpose: string;
  projectId?: number;
  projectCode?: string;
  projectName?: string;
  approvalStatus: ApprovalStatus;
  currentStepOrder?: number;
  reviewedById?: string;
  designatedReviewers?: DesignatedReviewer[];
  items: TravelPaymentRequestItem[];
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
  // 分期撥款（共用 InstallmentDto / PaymentInstallmentStatus 定義於 approval-tasks model）
  installments?: import('../../approval-tasks/models/approval-task.model').InstallmentDto[];
  paymentStatus?: import('../../approval-tasks/models/approval-task.model').PaymentInstallmentStatus;
}
