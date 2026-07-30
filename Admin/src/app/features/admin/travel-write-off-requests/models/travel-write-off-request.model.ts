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

export interface TravelWriteOffItem {
  id: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  note?: string;
  invoiceNo?: string;
  invoiceDate?: string;  // 發票日期（YYYY-MM-DD）
  fileName?: string;
  fileUrl?: string;
  sortOrder: number;
}

export interface DesignatedReviewer {
  id?: number;
  reviewerId: string;
  reviewerName?: string;
  stepOrder: number;
  status?: string;
  reviewedAt?: string;
  comment?: string;
  approvalStepOrder?: number;           // 此 designee 屬於哪個 designated step 的 stepOrder
  selectedDepartmentId?: number | null; // 需選部門時的選定部門
}

export interface TravelWriteOffRequest {
  id: number;
  requestNo: string;
  travelRequestId: number;
  travelRequestNo: string;
  writeOffNo: number;
  projectCode: string;
  projectName: string;
  destination: string;
  startDate: string;
  endDate: string;
  purpose: string;
  grandTotal: number;
  note?: string;
  approvalStatus: ApprovalStatus;
  submittedBy?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
  items: TravelWriteOffItem[];
  designatedReviewers?: DesignatedReviewer[];
  travelGrandTotal: number;
  travelWrittenOffTotal: number;
  /** 出差主單是否已結案 */
  travelIsClosed?: boolean;
  /** 關聯出差單的結案時間 */
  travelClosedAt?: string;
  /** 預計退款日（源自出差主單） */
  estimatedRefundDate?: string;
  /** 退款日（源自出差主單） */
  refundedAt?: string;
  /** 關聯出差單的應退差額（系統自動計算） */
  travelRefundAmount?: number;
  /** 關聯出差單的實際退款金額（財務手動填入） */
  travelRefundedAmount?: number;
}

/** TravelRequest summary for dropdown selection */
export interface TravelSummary {
  id: number;
  requestNo: string;
  destination: string;
  startDate: string;
  endDate: string;
  grandTotal: number;
  writtenOffTotal: number;
  projectCode?: string;
  purpose: string;
  isHolidayTravel?: boolean;
}

export const ITEM_CATEGORIES = ['交通費', '住宿費', '餐費', '人事費', '雜支'] as const;
