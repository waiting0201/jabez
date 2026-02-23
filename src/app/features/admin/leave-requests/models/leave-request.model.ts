export type LeaveType     = 'annual' | 'personal' | 'sick' | 'compensatory';
export type ApprovalStatus = 'pending' | 'approved' | 'rejected' | 'returned';

export const LEAVE_TYPE_LABELS: Record<LeaveType, string> = {
  annual:       '年假',
  personal:     '事假',
  sick:         '病假',
  compensatory: '補休',
};

export const LEAVE_TYPE_CLASSES: Record<LeaveType, string> = {
  annual:       'bg-primary-subtle text-primary',
  personal:     'bg-info-subtle text-info',
  sick:         'bg-warning-subtle text-warning-emphasis',
  compensatory: 'bg-secondary-subtle text-secondary',
};

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

export interface LeaveRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  leaveType: LeaveType;
  startDate: Date;
  endDate: Date;
  days: number;
  reason: string;
  approvalStatus: ApprovalStatus;
  createdAt: Date;
  reviewedAt?: Date;
  reviewNote?: string;
}
