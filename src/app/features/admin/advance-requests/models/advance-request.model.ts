export type ApprovalStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned';
export type WriteOffStatus = 'none' | 'partial' | 'completed';

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

export interface AdvanceRequestItem {
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
  sortOrder: number;
}

export interface WriteOffSummary {
  id: number;
  writeOffNo: number;
  grandTotal: number;
  createdAt: string;
}

export interface DesignatedReviewer {
  id?: number;
  reviewerId: string;
  reviewerName?: string;
  stepOrder: number;
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

export interface AdvanceRequest {
  id: number;
  requestNo: string;
  projectId: number;
  projectCode: string;
  projectName: string;
  activityName: string;
  activityPeriod: string;
  advanceDate: string;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  approvalStatus: ApprovalStatus;
  submittedBy?: string;
  createdAt: string;
  estimatedPaymentDate?: string;
  paidAt?: string;
  reviewedAt?: string;
  reviewNote?: string;
  designatedReviewers?: DesignatedReviewer[];
  items: AdvanceRequestItem[];
  writeOffs: WriteOffSummary[];
}

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
  fileName?: string;
  fileUrl?: string;
  sortOrder: number;
}

export interface WriteOffRecord {
  id: number;
  writeOffNo: number;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  note?: string;
  submittedBy?: string;
  createdAt: string;
  items: WriteOffItem[];
}

/** 常用分類選項 */
export const ITEM_CATEGORIES = ['交通費', '活動費', '設計費', '雜支'] as const;
