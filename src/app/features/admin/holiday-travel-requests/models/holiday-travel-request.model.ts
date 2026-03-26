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

export interface DesignatedReviewer {
  id?: number;
  reviewerId: string;
  reviewerName?: string;
  stepOrder: number;
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

/** 費用明細（含發票上傳資訊） */
export interface HolidayTravelRequestItem {
  id?: number;
  category: string;
  seqNo: number;
  itemName: string;
  unitPrice: number;
  quantity: string;
  totalPrice: number;
  note?: string;
  sortOrder: number;
  /** 發票號碼（OCR 自動填入） */
  invoiceNo?: string;
  /** 發票日期 */
  invoiceDate?: string;
  /** 上傳的檔案名稱 */
  fileName?: string;
  /** 已存檔的 API 存取 URL */
  fileUrl?: string;
}

/** 參與執行人員 */
export interface TravelParticipant {
  userId: string;
  userName?: string;
  sortOrder: number;
}

/** 常用分類選項 */
export const ITEM_CATEGORIES = ['交通費', '住宿費', '餐費', '雜支'] as const;

export interface HolidayTravelRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  /** 執行活動地點（前端標籤，DB 欄位：destination） */
  destination: string;
  /** 開始日期（DB 欄位：startDate） */
  startDate: Date;
  /** 結束日期（DB 欄位：endDate） */
  endDate: Date;
  grandTotal: number;
  /** 活動主旨及內容（DB 欄位：purpose） */
  purpose: string;
  projectId?: number;
  projectCode?: string;
  projectName?: string;
  /** 假日天數（由後端計算，startDate 至 endDate 的天數） */
  holidayDays?: number;
  /** 參與執行人員 */
  participants?: TravelParticipant[];
  approvalStatus: ApprovalStatus;
  designatedReviewers?: DesignatedReviewer[];
  items: HolidayTravelRequestItem[];
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
  /** 預計撥款日 */
  estimatedPaymentDate?: string;
  /** 實際撥款日 */
  paidAt?: string;
}
