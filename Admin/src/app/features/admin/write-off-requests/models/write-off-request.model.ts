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

export interface WriteOffItem {
  id: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  cashAmount: number;
  checkAmount: number;
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
  approvalStepOrder?: number;           // 此 designee 屬於哪個 designated step 的 stepOrder
  selectedDepartmentId?: number | null; // 需選部門時的選定部門
  status?: string;
  reviewedAt?: string;
  comment?: string;
}

export interface WriteOffRequest {
  id: number;
  requestNo: string;
  advanceRequestId: number;
  advanceRequestNo: string;
  writeOffNo: number;
  projectCode: string;
  projectName: string;
  activityName: string;
  activityPeriod: string;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  note?: string;
  approvalStatus: ApprovalStatus;
  submittedBy?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
  items: WriteOffItem[];
  designatedReviewers?: DesignatedReviewer[];
  advanceGrandTotal: number;
  advanceWrittenOffTotal: number;
  advanceIsClosed: boolean;
  estimatedRefundDate?: string;
  refundedAt?: string;
  /** 關聯預支單的應退差額（系統自動計算） */
  advanceRefundAmount?: number;
  /** 關聯預支單的實際退款金額（財務手動填入） */
  advanceRefundedAmount?: number;
  /** 整單批次附件（照片 / PDF） */
  attachments?: import('../../approval-tasks/models/approval-task.model').AttachmentItem[];
}

/** AdvanceRequest summary for dropdown selection */
export interface AdvanceSummary {
  id: number;
  requestNo: string;
  projectCode: string;
  activityName: string;
  grandTotal: number;
  writtenOffTotal: number;
  paidAt?: string;
}

export const ITEM_CATEGORIES = ['交通費', '活動費', '設計費', '人事費', '餐費', '雜支', '收款人', '廠商'] as const;
