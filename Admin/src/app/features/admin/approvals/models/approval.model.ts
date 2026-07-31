export type ApplicationType = 'payment_request' | 'leave' | 'travel' | 'overtime' | 'advance' | 'write_off' | 'travel_write_off' | 'holiday_travel' | 'travel_payment' | 'pre_review';

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
  pre_review:       '預審申請',
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
  pre_review:       'bg-danger-subtle text-danger',
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
  designatedRequiresDepartment?: boolean;
  minDays?: number | null;   // 適用天數門檻：null/undefined = 一律適用；有值時僅當申請天數 >= minDays 才納入（目前供請假依天數分流）
  /** 例外指定審核名單（UserId[]）：名單內的申請人送單時，此步驟改由申請人自行指定審核者。
   *  非空即代表啟用例外（不另設 bool 旗標）；與 useApplicantDesignated 互斥。僅 getById 會帶出。 */
  exceptionUserIds?: string[];
  /** 例外指定審核的限定職稱（JobTitleId[]）：申請人只能從這些職稱的人員中指定審核者；空＝不限職稱。僅 getById 會帶出。 */
  designatedJobTitleIds?: number[];
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
  /** 「對呼叫者而言」的有效值：步驟原生設定 OR 例外指定審核名單命中呼叫者 */
  useApplicantDesignated: boolean;
  designatedRequiresDepartment: boolean;
  /**
   * 「對呼叫者而言」的限定職稱：僅在呼叫者命中此步驟例外名單時才非空。
   * 非空 → 指定審核者只能從這些職稱的人員中挑；空／未帶 → 不限職稱。
   */
  designatedJobTitleIds?: number[];
}
