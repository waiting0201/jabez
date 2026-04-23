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
  employeeId?: string;
  approvalItemId?: number;
  destination: string;
  startDate: Date;
  endDate: Date;
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
  /** 預計撥款日 */
  estimatedPaymentDate?: string;
  /** 實際撥款日 */
  paidAt?: string;
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
}
