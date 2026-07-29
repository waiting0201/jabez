import {AdvanceRequest, AdvanceRequestItem, AdvanceRound} from '../../advance-requests/models/advance-request.model';
import {InstallmentDto, PaymentInstallmentStatus, WriteOffRound} from '../../approval-tasks/models/approval-task.model';

export type {WriteOffRound};

/**
 * 本次沖銷造成的超支增額（公司應補撥給員工的金額）。
 * 與後端 Api/Common/WriteOffRefundCalculator.cs 同一份公式：以「增額」而非「總超支」計算，
 * 讓每張沖銷單各自算得出、彼此不重疊。
 */
export const calcRefundDue = (
  advanceGrandTotal: number, otherWrittenOffTotal: number, currentGrandTotal: number): number => {
  const before = Math.max(0, otherWrittenOffTotal - advanceGrandTotal);
  const after  = Math.max(0, otherWrittenOffTotal + currentGrandTotal - advanceGrandTotal);
  return after - before;
};

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
  /** 支票已由公司直接付給廠商（財務於簽核頁勾選）*/
  checkPaid?: boolean;
  checkPaidAt?: string;
  checkPaidBy?: string;
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
  /** 關聯預支單的各預支批次（含追加）*/
  advanceRounds?: AdvanceRound[];
  /** 同一預支單底下各次沖銷 */
  writeOffHistory?: WriteOffRound[];
  /** 本次沖銷造成的超支增額 = 公司應補撥金額 */
  refundDue?: number;
  /** 本沖銷單的差額撥款分期（SUM 須等於 refundDue）*/
  installments?: InstallmentDto[];
  paymentStatus?: PaymentInstallmentStatus;
  /** 關聯預支單的撥款分期（唯讀對照）*/
  advanceInstallments?: InstallmentDto[];
  advancePaymentStatus?: PaymentInstallmentStatus;
}

/**
 * 依預支單彙總檢視：一張預支單的完整資訊 + 該單底下全部沖銷單的完整資訊。
 * 由清單母層（同一預支單的沖銷群組）「檢視」開啟。
 */
export interface AdvanceWriteOffOverview {
  advance: AdvanceRequest;
  writeOffs: WriteOffRequest[];
}

/** 支票已支付註記的更新 payload */
export interface UpdateCheckPaymentsRequest {
  items: {itemId: number; checkPaid: boolean}[];
}

/** AdvanceRequest summary for dropdown selection（含全批次費用明細，供表單對照） */
export interface AdvanceSummary {
  id: number;
  requestNo: string;
  projectCode: string;
  activityName: string;
  /** Round 1 預支日期 */
  advanceDate: string;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  writtenOffTotal: number;
  /** 各預支批次（含 Round 1；Round ≥2 為追加） */
  rounds: AdvanceRound[];
  /** 全批次費用明細，已依 roundNo, sortOrder 排序 */
  items: AdvanceRequestItem[];
}

export const ITEM_CATEGORIES = ['交通費', '活動費', '設計費', '人事費', '餐費', '雜支', '收款人', '廠商'] as const;
