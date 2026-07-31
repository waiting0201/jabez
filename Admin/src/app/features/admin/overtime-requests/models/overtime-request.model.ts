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

/** 加班申請的關聯專案明細（一列一專案，含該案預估時數） */
export interface OvertimeProject {
  projectId: number;
  projectCode: string;
  projectName: string;
  estimatedHours: number;
}

export interface OvertimeRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  overtimeDate: Date;
  projects: OvertimeProject[];
  /** 預估總時數（= projects 各列加總，由後端計算，前端唯讀） */
  estimatedHours: number;
  reason: string;
  approvalStatus: ApprovalStatus;
  designatedReviewers?: DesignatedReviewer[];
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
}

/** 新增 / 更新送出的 payload（總時數由後端加總，不送） */
export interface OvertimeRequestPayload {
  overtimeDate: Date;
  projects: {projectId: number; estimatedHours: number}[];
  reason: string;
  designatedReviewers?: DesignatedReviewer[];
}
