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

/**
 * 預支批次標籤（單一真相）：Round 1 = 原始預支，Round N(≥2) = 第 N 次追加。
 * detail / form / PDF / 簽核作業頁一律共用此函式，避免各處各寫一套。
 */
export const roundLabel = (roundNo: number): string =>
  roundNo <= 1 ? '第1次' : `第${roundNo}次追加`;

/** 預支批次：金額由該批次明細加總推導，Round 1 的日期取自父單 advanceDate */
export interface AdvanceRound {
  roundNo: number;
  advanceDate: string;
  /** 該批次的預支款需求日（選填） */
  advanceNeededDate?: string;
  reason?: string;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  itemCount: number;
}

export interface AdvanceRequestItem {
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

export interface AdvanceRequest {
  id: number;
  requestNo: string;
  projectId: number;
  projectCode: string;
  projectName: string;
  activityName: string;
  activityPeriod: string;
  advanceDate: string;
  /** 預支款需求日（選填）：申請人希望款項撥入的日期 */
  advanceNeededDate?: string;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  approvalStatus: ApprovalStatus;
  submittedBy?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
  designatedReviewers?: DesignatedReviewer[];
  items: AdvanceRequestItem[];
  writeOffs: WriteOffSummary[];
  /** 是否已結案（所有沖銷已完成，差額已核對） */
  isClosed: boolean;
  /** 結案時間 */
  closedAt?: string;
  /** 應退還差額（預支金額 - 沖銷總金額，>0 表示需退款） */
  refundAmount?: number;
  /** 實際退款金額（財務手動填入） */
  refundedAmount?: number;
  /** 預計退款日（沖銷退還差額） */
  estimatedRefundDate?: string;
  /** 差額退款完成時間 */
  refundedAt?: string;
  /** 沖銷紀錄含明細（僅 GetById 回傳，PDF 列印用） */
  writeOffRecords?: WriteOffRecord[];
  // 分期撥款（共用 InstallmentDto / PaymentInstallmentStatus 定義於 approval-tasks model）
  installments?: import('../../approval-tasks/models/approval-task.model').InstallmentDto[];
  paymentStatus?: import('../../approval-tasks/models/approval-task.model').PaymentInstallmentStatus;
  /** 各預支批次（僅 GetById 回傳）；含 Round 1 原始預支 */
  rounds?: AdvanceRound[];
  /** 最新已建立的批次號；> 1 且狀態為 pending/returned 表示有進行中的追加 */
  currentRoundNo: number;
}

export interface WriteOffSummary {
  id: number;
  writeOffNo: number;
  grandTotal: number;
  createdAt: string;
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
  requestNo: string;
  writeOffNo: number;
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  approvalStatus: ApprovalStatus;
  note?: string;
  submittedBy?: string;
  createdAt: string;
  items: WriteOffItem[];
}

/** 常用分類選項（與 write-off-request.model.ts 的同名常數必須保持一致：沖銷表單會從母預支單複製 category） */
export const ITEM_CATEGORIES = [
  '交通費', '活動費', '設計費', '人事費', '餐費', '雜支', '收款人', '廠商',
  '食材進貨', '備品耗材', '商品進貨', '臨時人力',
] as const;
