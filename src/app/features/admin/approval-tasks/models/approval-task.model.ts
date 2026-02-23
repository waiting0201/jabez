import {PaymentType, InvoiceItem} from '../../payment-requests/models/payment-request.model';
import {LeaveType} from '../../leave-requests/models/leave-request.model';
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
  vendor:  '廠商請款',
  travel:  '員工差旅',
  advance: '員工預支',
};

export const LEAVE_TYPE_LABELS: Record<LeaveType, string> = {
  annual:       '年假',
  personal:     '事假',
  sick:         '病假',
  compensatory: '補休',
};

export {APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES};

// ── Detail interfaces ────────────────────────────────────────────────────────

export interface ApprovalFlowStep {
  stepOrder: number;
  departmentName?: string;
  jobTitleName?: string;
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
}

export interface LeaveTaskDetail {
  leaveRequestId: number;
  leaveType: LeaveType;
  startDate: Date;
  endDate: Date;
  days: number;
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
}

// ── ApprovalRecord ───────────────────────────────────────────────────────────

export interface ApprovalRecord {
  stepOrder: number;
  action: 'approved' | 'returned' | 'rejected';
  reviewedBy: string;
  reviewedAt: Date;
  reviewNote?: string;
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
  approvalRecords: ApprovalRecord[];
}
