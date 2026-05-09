export type PaymentType     = 'vendor' | 'general' | 'business_trip';
export type ApprovalStatus  = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned';

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  vendor:        '廠商請款',
  general:       '一般請款',
  business_trip: '員工公出請款',
};

export const PAYMENT_TYPE_CLASSES: Record<PaymentType, string> = {
  vendor:        'bg-info-subtle text-info',
  general:       'bg-accent-subtle text-accent',
  business_trip: 'bg-primary-subtle text-primary',
};

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

export interface InvoiceItem {
  id: string;
  fileName: string;
  invoiceNo: string;
  invoiceDate?: string;  // 發票日期（YYYY-MM-DD）
  amount: number;
  itemName?: string;     // 項目
  note?: string;         // 備註
  fileUrl?: string;      // Azure Blob Storage URL (from API)
  previewUrl?: string;   // local blob URL (new upload preview)
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

export interface PaymentRequest {
  id: number;
  employeeId?: string;
  type: PaymentType;
  projectId: number;
  projectCode: string;
  projectName: string;
  invoices: InvoiceItem[];
  totalAmount: number;
  approvalStatus: ApprovalStatus;
  estimatedPaymentDate?: string;
  paidAt?: string;
  reason?: string;
  designatedReviewers?: DesignatedReviewer[];
  vendorId?: number;
  vendorName?: string;
  vendorTaxId?: string;
  createdAt: Date;
}
