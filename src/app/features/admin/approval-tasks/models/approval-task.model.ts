import {PaymentType, InvoiceItem} from '../../payment-requests/models/payment-request.model';
import {LeaveType, LEAVE_TYPE_LABELS} from '../../leave-requests/models/leave-request.model';
import {ApplicationType, APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES} from '../../approvals/models/approval.model';
import {TravelPaymentRequestItem} from '../../travel-payment-requests/models/travel-payment-request.model';

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
  projectName?: string;
  invoices: InvoiceItem[];
  totalAmount: number;
  estimatedPaymentDate?: string;
  paidAt?: string;
  reason?: string;
  paidBySignatureUrl?: string;
  vendorId?: number;
  vendorName?: string;
  vendorTaxId?: string;
  vendorContactPerson?: string;
  vendorPhone?: string;
  vendorBankAccount?: string;
  vendorAddress?: string;
}

export interface LeaveTaskDetail {
  leaveRequestId: number;
  leaveType: LeaveType;
  startDate: Date;
  endDate: Date;
  hours: number;
  reason: string;
}

export interface TravelTaskDetailItem {
  id: number;
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

/** 假日活動每位人員（申請人 + 參與者）的津貼預估 */
export interface HolidayAllowance {
  userId: string;
  userName: string;
  /** round(BaseSalary / 30) × HolidayDays，與 PayrollReadService 公式一致 */
  allowance: number;
  isApplicant: boolean;
}

export interface TravelTaskDetail {
  travelRequestId: number;
  destination: string;
  startDate: Date;
  endDate: Date;
  grandTotal: number;
  purpose: string;
  projectCode?: string;
  isHolidayTravel: boolean;
  /** 假日天數（僅假日執行活動使用） */
  holidayDays?: number;
  items: TravelTaskDetailItem[];
  /** 預計撥款日 */
  estimatedPaymentDate?: string;
  /** 實際撥款日 */
  paidAt?: string;
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  paidBySignatureUrl?: string;
  /** 假日活動每位人員津貼預估（僅 isHolidayTravel=true 時提供） */
  holidayAllowances?: HolidayAllowance[];
}

export interface OvertimeTaskDetail {
  overtimeRequestId: number;
  overtimeDate: Date;
  estimatedHours: number;
  reason: string;
  projectCodes?: string[];
}

export interface AdvanceTaskDetailItem {
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
  fileName?: string;
  fileUrl?: string;
}

export interface AdvanceTaskDetail {
  advanceRequestId: number;
  requestNo: string;
  projectCode: string;
  activityName: string;
  grandTotal: number;
  /** 預計撥款日 */
  estimatedPaymentDate?: string;
  /** 實際撥款日 */
  paidAt?: string;
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  items: AdvanceTaskDetailItem[];
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
  invoiceDate?: string;
  fileName?: string;
  fileUrl?: string;
}

export interface WriteOffTaskDetail {
  writeOffRequestId: number;
  advanceRequestId: number;
  requestNo: string;
  advanceRequestNo: string;
  projectCode: string;
  projectName: string;
  grandTotal: number;
  cashTotal: number;
  checkTotal: number;
  note?: string;
  items: WriteOffTaskDetailItem[];
  /** 預計退款日（改自 estimatedPaymentDate） */
  estimatedRefundDate?: string;
  refundedAt?: string;
  advanceGrandTotal: number;
  otherWrittenOffTotal: number;
  paidBySignatureUrl?: string;
  refundedBySignatureUrl?: string;
  advanceIsClosed?: boolean;
  /** 關聯預支單的應退差額（系統自動計算） */
  advanceRefundAmount?: number;
  /** 關聯預支單的實際退款金額（財務手動填入） */
  advanceRefundedAmount?: number;
}

export interface TravelWriteOffTaskDetailItem {
  id: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  note?: string;
  invoiceNo?: string;
  invoiceDate?: string;
  fileName?: string;
  fileUrl?: string;
}

export interface TravelWriteOffTaskDetail {
  travelWriteOffRequestId: number;
  travelRequestId: number;
  requestNo: string;
  travelRequestNo: string;
  projectCode: string;
  projectName: string;
  destination: string;
  startDate: string;
  endDate: string;
  purpose: string;
  grandTotal: number;
  note?: string;
  items: TravelWriteOffTaskDetailItem[];
  travelGrandTotal: number;
  otherWrittenOffTotal: number;
  /** 預計退款日（改自 estimatedPaymentDate） */
  estimatedRefundDate?: string;
  refundedAt?: string;
  paidBySignatureUrl?: string;
  refundedBySignatureUrl?: string;
  travelIsClosed?: boolean;
  /** 關聯出差單的應退差額（系統自動計算） */
  travelRefundAmount?: number;
  /** 關聯出差單的實際退款金額（財務手動填入） */
  travelRefundedAmount?: number;
}

export interface TravelPaymentTaskDetail {
  travelPaymentRequestId: number;
  destination: string;
  startDate: Date;
  endDate: Date;
  grandTotal: number;
  purpose: string;
  projectCode?: string;
  projectName?: string;
  estimatedPaymentDate?: string;
  paidAt?: string;
  items: TravelPaymentRequestItem[];
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
  reviewerJobTitle?: string;      // 審核者職稱（PDF 簽名欄判定總監等特殊角色用）
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
  travelWriteOffDetail?: TravelWriteOffTaskDetail;
  travelPaymentDetail?: TravelPaymentTaskDetail;
  approvalRecords: ApprovalRecord[];
  submittedBySignatureUrl?: string;  // 申請人簽名檔 URL
}
