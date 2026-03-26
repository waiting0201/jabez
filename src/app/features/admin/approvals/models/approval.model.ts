export type ApplicationType = 'payment_request' | 'leave' | 'travel' | 'overtime' | 'advance' | 'write_off' | 'travel_write_off' | 'holiday_travel';

export const APPLICATION_TYPE_LABELS: Record<ApplicationType, string> = {
  payment_request: '請款申請',
  leave:           '請假申請',
  travel:          '出差申請',
  overtime:        '加班申請',
  advance:         '預支申請',
  write_off:       '預支沖銷申請',
  travel_write_off: '出差沖銷申請',
  holiday_travel:   '假日出差申請',
};

export const APPLICATION_TYPE_CLASSES: Record<ApplicationType, string> = {
  payment_request: 'bg-info-subtle text-info',
  leave:           'bg-success-subtle text-success',
  travel:          'bg-primary-subtle text-primary',
  overtime:        'bg-warning-subtle text-warning-emphasis',
  advance:         'bg-purple-subtle text-purple',
  write_off:       'bg-teal-subtle text-teal',
  travel_write_off: 'bg-cyan-subtle text-cyan',
  holiday_travel:   'bg-indigo-subtle text-indigo',
};

export interface ApprovalStep {
  id: number;
  stepOrder: number;
  departmentId?: number;
  departmentName?: string;
  jobTitleId?: number;
  jobTitleName?: string;
  useApplicantDepartment?: boolean;
  useDirectSupervisor?: boolean;
  useApplicantDesignated?: boolean;
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
