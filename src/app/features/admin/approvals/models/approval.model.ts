export type ApplicationType = 'payment_request' | 'leave' | 'travel' | 'overtime';

export const APPLICATION_TYPE_LABELS: Record<ApplicationType, string> = {
  payment_request: '請款申請',
  leave:           '請假申請',
  travel:          '出差申請',
  overtime:        '加班申請',
};

export const APPLICATION_TYPE_CLASSES: Record<ApplicationType, string> = {
  payment_request: 'bg-info-subtle text-info',
  leave:           'bg-success-subtle text-success',
  travel:          'bg-primary-subtle text-primary',
  overtime:        'bg-warning-subtle text-warning-emphasis',
};

export interface ApprovalStep {
  id: number;
  stepOrder: number;
  departmentId?: number;
  departmentName?: string;
  jobTitleId?: number;
  jobTitleName?: string;
  note?: string;
}

export interface ApprovalItem {
  id: number;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
  applicationType?: ApplicationType;
  steps: ApprovalStep[];
  createdAt: Date;
}
