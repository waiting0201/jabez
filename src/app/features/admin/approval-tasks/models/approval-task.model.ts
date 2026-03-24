import {PaymentType, InvoiceItem} from '../../payment-requests/models/payment-request.model';
import {LeaveType, LEAVE_TYPE_LABELS} from '../../leave-requests/models/leave-request.model';
import {ApplicationType, APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES} from '../../approvals/models/approval.model';

export type TaskStatus = 'pending' | 'approved' | 'rejected' | 'returned';

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  pending:  '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

export const TASK_STATUS_CLASSES: Record<TaskStatus, string> = {
  pending:  'bg-warning-subtle text-warning-emphasis',
  approved: 'bg-success-subtle text-success',
  rejected: 'bg-danger-subtle text-danger',
  returned: 'bg-secondary-subtle text-secondary',
};

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  vendor:        '廠商請款',
  general:       '一般請款',
  business_trip: '員工公出請款',
};

export {LEAVE_TYPE_LABELS};

export {APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES};

// ── Detail interfaces ────────────────────────────────────────────────────────

export interface ApprovalFlowStep {
  stepOrder: number;
  departmentName?: string;
  departmentCode?: string;
  jobTitleName?: string;
  useDirectSupervisor?: boolean;
  useApplicantDesignated?: boolean;
  note?: string;
}

export interface ApprovalFlow {
  id: number;
  name: string;
  steps: ApprovalFlowStep[];
}

export interface PaymentTaskDetail {
  paymentRequestId: number;
  paymentType: PaymentType;
  projectCode: string;
  invoices: InvoiceItem[];
  totalAmount: number;
  estimatedPaymentDate?: string;
  paidAt?: string;
  paidBySignatureUrl?: string;
}

export interface LeaveTaskDetail {
  leaveRequestId: number;
  leaveType: LeaveType;
  startDate: Date;
  endDate: Date;
  hours: number;
  reason: string;
}

export interface TravelTaskDetail {
  travelRequestId: number;
  destination: string;
  startDate: Date;
  endDate: Date;
  estimatedCost: number;
  purpose: string;
  projectCode?: string;
  isHolidayTravel: boolean;
}

export interface OvertimeTaskDetail {
  overtimeRequestId: number;
  overtimeDate: Date;
  estimatedHours: number;
  reason: string;
  projectCodes?: string[];
}

export interface AdvanceTaskDetail {
  advanceRequestId: number;
  requestNo: string;
  projectCode: string;
  activityName: string;
  grandTotal: number;
  estimatedPaymentDate?: string;
  paidAt?: string;
  paidBySignatureUrl?: string;
}

export interface WriteOffTaskDetailItem {
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
}

export interface WriteOffTaskDetail {
  writeOffRequestId: number;
  requestNo: string;
  advanceRequestNo: string;
  projectCode: string;
  projectName: string;
  grandTotal: number;
  cashTotal: number;
  checkTotal: number;
  note?: string;
  items: WriteOffTaskDetailItem[];
  paidAt?: string;
  paidBySignatureUrl?: string;
}

// ── ApprovalRecord ───────────────────────────────────────────────────────────

export interface ApprovalRecord {
  stepOrder: number;
  action: 'approved' | 'returned' | 'rejected';
  reviewedBy: string;
  reviewedAt: Date;
  reviewNote?: string;
  onBehalfOf?: string;    // 代理審核：代替誰審核
  isEscalated: boolean;   // 是否為升級審核
  reviewerSignatureUrl?: string;  // 審核者簽名檔 URL
}

// ── ApprovalTask (polymorphic) ───────────────────────────────────────────────

export interface ApprovalTask {
  id: number;
  applicationType: ApplicationType;
  title: string;
  submittedBy: string;
  submittedAt: Date;
  status: TaskStatus;
  currentStepOrder: number;
  reviewedAt?: Date;
  reviewNote?: string;
  flow?: ApprovalFlow;
  paymentDetail?: PaymentTaskDetail;
  leaveDetail?: LeaveTaskDetail;
  travelDetail?: TravelTaskDetail;
  overtimeDetail?: OvertimeTaskDetail;
  advanceDetail?: AdvanceTaskDetail;
  writeOffDetail?: WriteOffTaskDetail;
  approvalRecords: ApprovalRecord[];
  submittedBySignatureUrl?: string;  // 申請人簽名檔 URL
}
