export type ApplicationType = 'payment_request' | 'leave' | 'travel' | 'overtime' | 'advance' | 'write_off' | 'travel_write_off' | 'holiday_travel' | 'travel_payment';

export const APPLICATION_TYPE_LABELS: Record<ApplicationType, string> = {
  payment_request:  '請款申請',
  leave:            '請假申請',
  travel:           '出差預支申請',
  overtime:         '加班申請',
  advance:          '預支申請',
  write_off:        '預支沖銷申請',
  travel_write_off: '出差預支沖銷申請',
  holiday_travel:   '假日執行活動申請',
  travel_payment:   '出差請款申請',
};

export const APPLICATION_TYPE_CLASSES: Record<ApplicationType, string> = {
  payment_request:  'bg-info-subtle text-info',
  leave:            'bg-success-subtle text-success',
  travel:           'bg-primary-subtle text-primary',
  overtime:         'bg-warning-subtle text-warning-emphasis',
  advance:          'bg-purple-subtle text-purple',
  write_off:        'bg-teal-subtle text-teal',
  travel_write_off: 'bg-cyan-subtle text-cyan',
  holiday_travel:   'bg-indigo-subtle text-indigo',
  travel_payment:   'bg-orange-subtle text-orange',
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
  departmentId?: number;     // null/undefined = 該類型的通用預設流程；有值 = 某部門專屬流程
  departmentName?: string;
  steps: ApprovalStep[];
  createdAt: Date;
}

/** 輕量流程摘要（不含敏感設定，免 approvals:read 權限即可呼叫） */
export interface ApprovalFlowSummary {
  id: number;
  applicationType?: ApplicationType;
  steps: ApprovalFlowStepSummary[];
}

export interface ApprovalFlowStepSummary {
  stepOrder: number;
  useApplicantDesignated: boolean;
}
