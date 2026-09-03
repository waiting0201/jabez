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
  approvalStepOrder?: number;           // 此 designee 屬於哪個 designated step 的 stepOrder
  selectedDepartmentId?: number | null; // 需選部門時的選定部門
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

/**
 * 補償方式（整單二擇一）：
 * - compensatory 補休 → 時數計入補休池（見請假申請的「補休」假別）
 * - pay 加班費       → 依勞基法分段累進倍率試算，隨「加班日次月」薪資發放
 */
export type OvertimeCompensationType = 'compensatory' | 'pay';

export const COMPENSATION_TYPE_LABELS: Record<OvertimeCompensationType, string> = {
  compensatory: '補休',
  pay:          '加班費',
};

export const COMPENSATION_TYPE_CLASSES: Record<OvertimeCompensationType, string> = {
  compensatory: 'bg-secondary-subtle text-secondary',
  pay:          'bg-primary-subtle text-primary',
};

/** 加班費試算的單一分段（倍率 / 該段時數 / 該段金額） */
export interface OvertimePaySegment {
  multiplier: number;
  hours: number;
  amount: number;
}

/** 加班費試算結果（GET /overtime-requests/estimate） */
export interface OvertimePayEstimate {
  overtimeDate: string;
  /** 日別（排班制員工恆為 false，六日與國定假日視為工作日） */
  isHoliday: boolean;
  hourlyRate: number;
  requestedHours: number;
  /** = min(requestedHours, capHours) */
  payableHours: number;
  /** 超出上限、不計酬的時數 */
  excessHours: number;
  capHours: number;
  amount: number;
  segments: OvertimePaySegment[];
  /** false → 該員工未設定底薪，amount 必為 0 */
  hasBaseSalary: boolean;
  /** 同日已有已核准的假日執行活動 → 可能與假日津貼雙重給付 */
  hasHolidayTravelConflict: boolean;
}

/** 加班申請的關聯專案明細（一列一專案，含該案預估時數） */
export interface OvertimeProject {
  projectId: number;
  projectCode: string;
  projectName: string;
  estimatedHours: number;
}

// 讀取用（後端序列化的日期為 ISO 字串）；送出用見 OvertimeRequestPayload
export interface OvertimeRequest {
  id: number;
  requestNo?: string | null;  // OT-yyyyMMdd-NNN；送簽時取號，草稿為 null
  employeeId?: string;
  approvalItemId?: number;
  // 後端序列化為 ISO 字串（"2026-03-24T00:00:00"），runtime 實際型別是 string 而非 Date
  overtimeDate: string;
  projects: OvertimeProject[];
  /** 預估總時數（= projects 各列加總，由後端計算，前端唯讀） */
  estimatedHours: number;
  reason: string;
  approvalStatus: ApprovalStatus;
  /** 補償方式（補休 / 加班費，整單二擇一） */
  compensationType: OvertimeCompensationType;
  /** 加班費快照（補休型為 null；核准當下寫入，日後調薪不回溯） */
  overtimePayAmount?: number | null;
  hourlyRateSnapshot?: number | null;
  payableHours?: number | null;
  isHolidayOvertime?: boolean | null;
  designatedReviewers?: DesignatedReviewer[];
  createdAt: Date;
  submittedAt: Date | null;   // 送簽日期（申請日期）；草稿為 null
  reviewedAt?: Date;
  reviewNote?: string;
}

/** 新增 / 更新送出的 payload（總時數由後端加總，不送） */
export interface OvertimeRequestPayload {
  overtimeDate: Date;
  projects: {projectId: number; estimatedHours: number}[];
  reason: string;
  compensationType: OvertimeCompensationType;
  designatedReviewers?: DesignatedReviewer[];
}
