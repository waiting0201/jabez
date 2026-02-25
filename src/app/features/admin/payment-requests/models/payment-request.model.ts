export type PaymentType     = 'vendor' | 'travel' | 'advance';
export type ApprovalStatus  = 'pending' | 'approved' | 'rejected' | 'returned';

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  vendor:  '廠商請款',
  travel:  '員工差旅',
  advance: '員工預支',
};

export const PAYMENT_TYPE_CLASSES: Record<PaymentType, string> = {
  vendor:  'bg-info-subtle text-info',
  travel:  'bg-primary-subtle text-primary',
  advance: 'bg-warning-subtle text-warning-emphasis',
};

export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
  pending:  '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

export const APPROVAL_STATUS_CLASSES: Record<ApprovalStatus, string> = {
  pending:  'bg-warning-subtle text-warning-emphasis',
  approved: 'bg-success-subtle text-success',
  rejected: 'bg-danger-subtle text-danger',
  returned: 'bg-secondary-subtle text-secondary',
};

export interface InvoiceItem {
  id: string;
  fileName: string;
  invoiceNo: string;
  amount: number;
  previewUrl?: string;   // blob URL (new upload) or API URL (existing record)
}

export interface PaymentRequest {
  id: number;
  employeeId?: string;
  type: PaymentType;
  projectId: number;
  projectCode: string;
  invoices: InvoiceItem[];
  totalAmount: number;
  approvalStatus: ApprovalStatus;
  createdAt: Date;
}
