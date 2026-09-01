import {
  PaymentType, InvoiceItem, DesignatedReviewer,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  PAYMENT_STATE_LABELS, PAYMENT_STATE_CLASSES,
} from '../../payment-requests/models/payment-request.model';
import {LeaveType, LEAVE_TYPE_LABELS} from '../../leave-requests/models/leave-request.model';
import {ApplicationType, APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES} from '../../approvals/models/approval.model';
import {TravelPaymentRequestItem} from '../../travel-payment-requests/models/travel-payment-request.model';
import {PreReviewItem} from '../../pre-review-requests/models/pre-review-request.model';
import {ParticipantDate} from '../../holiday-travel-requests/models/holiday-travel-request.model';

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
  jobTitleLevel?: number;  // 職稱層級（PDF 簽名欄判定總監 Level=1 用，避免依賴職稱名稱）
  useDirectSupervisor?: boolean;
  useApplicantDesignated?: boolean;
  note?: string;
}

export interface ApprovalFlow {
  id: number;
  name: string;
  steps: ApprovalFlowStep[];
}

/** 整單批次附件（照片 / PDF），請款一般請款 / 預支沖銷共用 */
export interface AttachmentItem {
  id: number;
  fileName: string;
  fileUrl?: string;
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
  attachments?: AttachmentItem[];
}

export interface LeaveTaskDetail {
  leaveRequestId: number;
  leaveType: LeaveType;
  startDate: Date;
  endDate: Date;
  hours: number;
  reason: string;
}

/** 銷假申請詳情（含原請假單資訊與被取消的逐日清單） */
export interface LeaveRevocationTaskDetail {
  leaveRevocationId: number;
  leaveRequestId: number;
  leaveType: LeaveType;
  leaveStartDate: Date;
  leaveEndDate: Date;
  leaveHours: number;      // 原請假時數
  leaveReason: string;
  revokedHours: number;
  reason: string;
  dates: {date: Date; hours: number}[];
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
  /** 個人假日天數（參與者為 COALESCE(個人, 整單)，逐日勾選上/下半天者為 0.5 的倍數；申請人固定為整單） */
  days: number;
  /** round(BaseSalary / 30) × days，與 PayrollReadService 公式一致 */
  allowance: number;
  isApplicant: boolean;
  /** 逐日勾選的參與日期 + 時段（null / 空＝全程參與；申請人不逐日故恆為空） */
  dates?: ParticipantDate[];
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
  /** 是否已結案（沖銷完成）；假日執行活動不走沖銷，恆為 false */
  isClosed?: boolean;
  closedAt?: string;
  /** 應退還差額（沖銷累計 > 出差金額時系統自動計算） */
  refundAmount?: number;
  /** 實際退款金額（財務手動填入） */
  refundedAmount?: number;
}

/** 加班申請的關聯專案明細（含該案預估時數） */
export interface OvertimeTaskProject {
  projectId: number;
  projectCode: string;
  projectName: string;
  estimatedHours: number;
}

export interface OvertimeTaskDetail {
  overtimeRequestId: number;
  overtimeDate: Date;
  /** 預估總時數（= projects 各列加總） */
  estimatedHours: number;
  reason: string;
  projects?: OvertimeTaskProject[];
  /** 補償方式（compensatory 補休 / pay 加班費） */
  compensationType: 'compensatory' | 'pay';
  /** 加班費快照（補休型為 null） */
  overtimePayAmount?: number | null;
  payableHours?: number | null;
  isHolidayOvertime?: boolean | null;
}

export interface AdvanceTaskDetailItem {
  id: number;
  /** 所屬預支批次（1 = 原始預支，≥2 = 第N次追加） */
  roundNo: number;
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
  /** 各預支批次（含 Round 1 原始預支）；共用 advance-requests model 的 AdvanceRound */
  rounds?: import('../../advance-requests/models/advance-request.model').AdvanceRound[];
  /** 本次送簽的批次號；> 1 表示正在簽核追加批次 */
  currentRoundNo: number;
  /** 是否已結案（所有沖銷已完成，差額已核對）；與預支申請清單同一 badge */
  isClosed?: boolean;
  closedAt?: string;
  /** 應退還差額（沖銷累計 > 預支時系統自動計算） */
  refundAmount?: number;
  /** 實際退款金額（財務手動填入） */
  refundedAmount?: number;
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
  /** 支票已由公司直接付給廠商（財務勾選）*/
  checkPaid?: boolean;
  checkPaidAt?: string;
  checkPaidBy?: string;
}

/** 同一張預支單底下的第 N 次沖銷 */
export interface WriteOffRound {
  id: number;
  writeOffNo: number;
  requestNo: string;
  grandTotal: number;
  approvalStatus: string;
  createdAt: string;
  /** 是否為目前檢視中的這張沖銷單 */
  isCurrent: boolean;
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
  /** 關聯預支單的結案時間 */
  advanceClosedAt?: string;
  /** 財務已於其簽核關卡登記結案，待整張沖銷單核准後才真正結案 */
  pendingClose?: boolean;
  /** 關聯預支單的應退差額（系統自動計算） */
  advanceRefundAmount?: number;
  /** 關聯預支單的實際退款金額（財務手動填入） */
  advanceRefundedAmount?: number;
  attachments?: AttachmentItem[];
  /** 關聯預支單的各預支批次（含追加）*/
  advanceRounds?: import('../../advance-requests/models/advance-request.model').AdvanceRound[];
  /** 同一預支單底下各次沖銷 */
  writeOffHistory?: WriteOffRound[];
  /** 本次沖銷造成的超支增額 = 公司應補撥金額（後端 WriteOffRefundCalculator 算好帶回）*/
  refundDue?: number;
  /** 本沖銷單的差額撥款分期（SUM 須等於 refundDue）*/
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
  /** 關聯預支單的撥款分期（簽核頁可編輯，與預支申請單同步）*/
  advanceInstallments?: InstallmentDto[];
  advancePaymentStatus?: PaymentInstallmentStatus;
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
  /** 關聯出差單的結案時間 */
  travelClosedAt?: string;
  /** 財務已於其簽核關卡登記結案，待整張沖銷單核准後才真正結案 */
  pendingClose?: boolean;
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

export interface PreReviewTaskDetail {
  preReviewRequestId: number;
  requestNo: string;
  paymentType: 'vendor' | 'designer';
  projectCode: string;
  projectName?: string;
  items: PreReviewItem[];
  totalAmount: number;
  taxAmount?: number;     // 稅金（手動輸入）
  reason?: string;
  vendorId?: number;
  vendorName?: string;
  vendorTaxId?: string;
  attachments?: AttachmentItem[];
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
  reviewerJobTitle?: string;      // 審核者職稱（顯示用）
  reviewerJobTitleLevel?: number; // 審核者職稱層級（PDF 簽名欄判定總監 Level=1 用，避免依賴職稱名稱）
  reviewerDepartmentName?: string; // 審核者部門（指定審核步驟顯示用，區分同名審核者）
  roundNo?: number;                // 簽核批次（僅 advance 追加預支會 > 1；舊資料視為 1）
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
  leaveRevocationDetail?: LeaveRevocationTaskDetail;
  travelDetail?: TravelTaskDetail;
  overtimeDetail?: OvertimeTaskDetail;
  advanceDetail?: AdvanceTaskDetail;
  writeOffDetail?: WriteOffTaskDetail;
  travelWriteOffDetail?: TravelWriteOffTaskDetail;
  travelPaymentDetail?: TravelPaymentTaskDetail;
  preReviewDetail?: PreReviewTaskDetail;
  approvalRecords: ApprovalRecord[];
  /** 指定審核者（含 approvalStepOrder，供 PDF 簽名欄判斷哪些步驟為指定審核，含例外指定審核命中者） */
  designatedReviewers?: DesignatedReviewer[];
  submittedBySignatureUrl?: string;  // 申請人簽名檔 URL
}

/** 簽核作業「申請人」下拉選項（僅財務體系部門可取得） */
export interface ApprovalTaskApplicant {
  id: string;
  name: string;
}
