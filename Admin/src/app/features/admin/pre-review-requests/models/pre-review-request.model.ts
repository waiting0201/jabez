export type PaymentType    = 'vendor' | 'designer';
export type ApprovalStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned';

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  vendor:   '協力廠商',
  designer: '設計師',
};

export const PAYMENT_TYPE_CLASSES: Record<PaymentType, string> = {
  vendor:   'bg-info-subtle text-info',
  designer: 'bg-accent-subtle text-accent',
};

export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
  draft:    '草稿',
  pending:  '待核准',
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

/** 品項類別預設清單（含「其他」自訂選項） */
export const ITEM_CATEGORIES = ['活動硬體', '設計師', '製作產品', '採購產品', '採購庶務', '其他'] as const;
export type ItemCategory = (typeof ITEM_CATEGORIES)[number];

export interface PreReviewItem {
  id: string;
  itemCategory?: string;  // 品項類別（預設值或自訂文字）
  itemName?: string;      // 項目
  description?: string;   // 說明
  itemDate?: string;      // 日期（YYYY-MM-DD）
  amount: number;
  note?: string;
  fileName: string;
  fileUrl?: string;       // Azure Blob Storage URL（from API）
  previewUrl?: string;    // local blob URL（new upload preview）
}

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

export interface PreReviewRequest {
  id: number;
  requestNo: string;
  employeeId?: string;
  type: PaymentType;
  projectId: number;
  projectCode: string;
  projectName: string;
  items: PreReviewItem[];
  totalAmount: number;
  taxAmount?: number;     // 稅金（手動輸入）
  approvalStatus: ApprovalStatus;
  reason?: string;
  designatedReviewers?: DesignatedReviewer[];
  vendorId?: number;
  vendorName?: string;
  vendorTaxId?: string;
  createdAt: Date;
  // 整單批次附件
  attachments?: import('../../approval-tasks/models/approval-task.model').AttachmentItem[];
}
