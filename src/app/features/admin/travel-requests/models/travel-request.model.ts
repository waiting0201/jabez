export type ApprovalStatus = 'pending' | 'approved' | 'rejected' | 'returned';

export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
  pending:  '待審核',
  approved: '已核准',
  rejected: '已拒絕',
  returned: '退回修改',
};

export const APPROVAL_STATUS_CLASSES: Record<ApprovalStatus, string> = {
  pending:  'bg-warning-subtle text-warning-emphasis',
  approved: 'bg-success-subtle text-success',
  rejected: 'bg-danger-subtle text-danger',
  returned: 'bg-secondary-subtle text-secondary',
};

export interface TravelRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  destination: string;
  startDate: Date;
  endDate: Date;
  estimatedCost: number;
  purpose: string;
  projectId?: number;
  projectCode?: string;
  approvalStatus: ApprovalStatus;
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
}
