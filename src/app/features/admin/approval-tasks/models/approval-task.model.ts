import {
  PaymentType, InvoiceItem,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  PAYMENT_STATE_LABELS, PAYMENT_STATE_CLASSES,
} from '../../payment-requests/models/payment-request.model';
import {LeaveType, LEAVE_TYPE_LABELS} from '../../leave-requests/models/leave-request.model';
import {ApplicationType, APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES} from '../../approvals/models/approval.model';
import {TravelPaymentRequestItem} from '../../travel-payment-requests/models/travel-payment-request.model';

export type TaskStatus = 'pending' | 'approved' | 'rejected' | 'returned';

// 直接重用請款列表的 mapping 作為單一真相來源（pending 自動顯示為「待核准」與請款列表一致）
// 註：APPROVAL_STATUS_LABELS 多 'draft' key 不影響使用 — TaskStatus 是 ApprovalStatus 的子集
export const TASK_STATUS_LABELS  = APPROVAL_STATUS_LABELS;
export const TASK_STATUS_CLASSES = APPROVAL_STATUS_CLASSES;

export const PAYMENT_TYPE_LABELS: Record<PaymentType, string> = {
  vendor:        '廠商請款',
  general:       '一般請款',
  business_trip: '員工公出請款',
};

export {LEAVE_TYPE_LABELS};

export {APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES};

export {PAYMENT_STATE_LABELS, PAYMENT_STATE_CLASSES};

// ── Installments（分期撥款，4 種申請類型共用）────────────────────────────────────

/** 撥款 status 三態（後端 PaymentInstallmentStatus 對應）*/
export type PaymentInstallmentStatus = 'Unpaid' | 'PartiallyPaid' | 'FullyPaid';

export const PAYMENT_INSTALLMENT_STATUS_LABELS: Record<PaymentInstallmentStatus, string> = {
  Unpaid:        '未撥款',
  PartiallyPaid: '部分撥款',
  FullyPaid:     '已全數撥款',
};

export const PAYMENT_INSTALLMENT_STATUS_CLASSES: Record<PaymentInstallmentStatus, string> = {
  Unpaid:        'bg-secondary',
  PartiallyPaid: 'bg-warning',
  FullyPaid:     'bg-success',
};

/** 分期撥款明細（顯示用 DTO）*/
export interface InstallmentDto {
  id: number;
  installmentNo: number;
  expectedDate: string;
  paidAt?: string;
  amount: number;
  note?: string;
  paidByUserId?: string;
  paidByName?: string;
  paidBySignatureUrl?: string;
}

/** 分期撥款輸入（upsert request）— Id 缺省表示新增列 */
export interface InstallmentInput {
  id?: number;
  installmentNo: number;
  expectedDate: string;
  paidAt?: string;
  amount: number;
  note?: string;
}

export interface UpsertInstallmentsRequest {
  installments: InstallmentInput[];
  approvalStatus?: string;
}

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
  requestNo: string;
  paymentType: PaymentType;
  projectCode: string;
  projectName?: string;
  invoices: InvoiceItem[];
  totalAmount: number;
  reason?: string;
  vendorId?: number;
  vendorName?: string;
  vendorTaxId?: string;
  vendorContactPerson?: string;
  vendorPhone?: string;
  vendorBankAccount?: string;
  vendorAddress?: string;
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
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
  requestNo: string;
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
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  /** 假日活動每位人員津貼預估（僅 isHolidayTravel=true 時提供） */
  holidayAllowances?: HolidayAllowance[];
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
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
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  items: AdvanceTaskDetailItem[];
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
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
  refundedBySignatureUrl?: string;
  travelIsClosed?: boolean;
  /** 關聯出差單的應退差額（系統自動計算） */
  travelRefundAmount?: number;
  /** 關聯出差單的實際退款金額（財務手動填入） */
  travelRefundedAmount?: number;
}

export interface TravelPaymentTaskDetail {
  travelPaymentRequestId: number;
  requestNo: string;
  destination: string;
  startDate: Date;
  endDate: Date;
  grandTotal: number;
  purpose: string;
  projectCode?: string;
  projectName?: string;
  items: TravelPaymentRequestItem[];
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
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
