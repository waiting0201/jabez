export type LeaveType =
  | 'annual' | 'personal' | 'sick' | 'compensatory'
  | 'marriage' | 'bereavement' | 'official'
  | 'maternity' | 'miscarriage_3m' | 'miscarriage_2to3m' | 'miscarriage_under2m'
  | 'prenatal_checkup' | 'paternity'
  | 'ceremonial_festival'
  | 'senior_executive'
  | 'menstrual';

/** 時間單位：小時 / 半天(4hr) / 整天(8hr) */
export type LeaveTimeUnit = 'hour' | 'half_day' | 'day';

export type ApprovalStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned';

export const LEAVE_TYPE_LABELS: Record<LeaveType, string> = {
  annual:              '年假(特休假)',
  personal:            '事假',
  sick:                '病假',
  compensatory:        '補休',
  official:            '公假',
  marriage:            '婚假',
  maternity:           '產假',
  miscarriage_3m:      '流產假(3個月以上)',
  miscarriage_2to3m:   '流產假(2-3個月)',
  miscarriage_under2m: '流產假(未滿2個月)',
  prenatal_checkup:    '產檢假',
  paternity:           '陪產假',
  bereavement:         '喪假',
  ceremonial_festival: '歲時祭儀假',
  senior_executive:    '高階主管假',
  menstrual:           '生理假',
};

/**
 * 各假別時間單位對應（需與後端 LeaveRequestHandler.TimeUnitMap 保持同步）
 * - hour: 事假 / 病假 / 產檢假 / 陪產假
 * - half_day: 特休 / 補休 / 高階主管假（4 小時）
 * - day: 公假 / 婚假 / 產假 / 喪假 / 歲時祭儀假 / 流產假系列（8 小時）
 */
export const LEAVE_TIME_UNIT: Record<LeaveType, LeaveTimeUnit> = {
  personal:            'hour',
  sick:                'hour',
  prenatal_checkup:    'hour',
  paternity:           'hour',
  annual:              'half_day',
  compensatory:        'half_day',
  senior_executive:    'half_day',
  official:            'day',
  marriage:            'day',
  maternity:           'day',
  bereavement:         'day',
  ceremonial_festival: 'day',
  miscarriage_3m:      'day',
  miscarriage_2to3m:   'day',
  miscarriage_under2m: 'day',
  menstrual:           'day',
};

/** 格式化時數顯示（依單位） */
export function formatLeaveDuration(leaveType: LeaveType, hours: number): string {
  const unit = LEAVE_TIME_UNIT[leaveType];
  if (unit === 'hour') return `${Math.round(hours * 10) / 10} 小時`;
  // half_day / day 都以「天」為單位顯示（0.5 天 = 4 小時）
  const days = Math.round((hours / 8) * 10) / 10;
  return `${days} 天`;
}

export const LEAVE_TYPE_CLASSES: Record<LeaveType, string> = {
  annual:              'bg-[rgba(105,159,52,0.12)] text-[#4A6B3A]',
  personal:            'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  sick:                'bg-[rgba(184,137,42,0.12)] text-[#B8892A]',
  compensatory:        'bg-[rgba(140,115,85,0.12)] text-[#8C7355]',
  official:            'bg-[rgba(74,107,58,0.12)] text-[#4A6B3A]',
  marriage:            'bg-[rgba(160,64,64,0.12)] text-[#A04040]',
  maternity:           'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  miscarriage_3m:      'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  miscarriage_2to3m:   'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  miscarriage_under2m: 'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  prenatal_checkup:    'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  paternity:           'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  bereavement:         'bg-[rgba(82,83,88,0.12)] text-[#525358]',
  ceremonial_festival: 'bg-[rgba(140,115,85,0.12)] text-[#8C7355]',
  senior_executive:    'bg-[rgba(105,159,52,0.12)] text-[#4A6B3A]',
  menstrual:           'bg-[rgba(160,64,64,0.12)] text-[#A04040]',
};

/** 假別分組（供下拉選單 optgroup 使用） */
export const LEAVE_TYPE_GROUPS: { label: string; types: LeaveType[] }[] = [
  { label: '一般假別', types: ['annual', 'personal', 'sick', 'official', 'compensatory'] },
  { label: '婚假',     types: ['marriage'] },
  { label: '產假類別', types: ['maternity', 'miscarriage_3m', 'miscarriage_2to3m', 'miscarriage_under2m', 'prenatal_checkup', 'paternity'] },
  { label: '喪假',     types: ['bereavement'] },
  { label: '其他假別', types: ['ceremonial_festival'] },
  // 生理假僅女性可見（實際顯示由前端依女性身分過濾）
  { label: '生理假',   types: ['menstrual'] },
  // 高階主管假僅協理以上可見（實際顯示由前端依 auth.isSeniorExecutive() 過濾）
  { label: '高階主管假', types: ['senior_executive'] },
];

/** 假別天數上限（前端顯示用，實際驗證在後端） */
export const LEAVE_TYPE_DAYS_LIMIT: Partial<Record<LeaveType, number>> = {
  marriage:            8,
  maternity:           56,
  miscarriage_3m:      28,
  miscarriage_2to3m:   7,
  miscarriage_under2m: 5,
  prenatal_checkup:    7,
  paternity:           7,
  ceremonial_festival: 3,
  menstrual:           12,
};

// ── 喪假親屬關係 ──

export type BereavementRelationship =
  | 'spouse' | 'parent' | 'adoptive_parent' | 'step_parent'
  | 'grandparent' | 'child' | 'spouse_parent' | 'spouse_adoptive_parent'
  | 'great_grandparent' | 'sibling' | 'spouse_grandparent';

export const BEREAVEMENT_RELATIONSHIP_LABELS: Record<BereavementRelationship, string> = {
  spouse:                '配偶',
  parent:                '父母',
  adoptive_parent:       '養父母',
  step_parent:           '繼父母',
  grandparent:           '祖父母(含外祖父母)',
  child:                 '子女',
  spouse_parent:         '配偶之父母',
  spouse_adoptive_parent:'配偶之養父母或繼父母',
  great_grandparent:     '曾祖父母',
  sibling:               '兄弟姊妹',
  spouse_grandparent:    '配偶之祖父母',
};

export const BEREAVEMENT_DAYS: Record<BereavementRelationship, number> = {
  spouse: 8, parent: 8, adoptive_parent: 8, step_parent: 8,
  grandparent: 6, child: 6, spouse_parent: 6, spouse_adoptive_parent: 6,
  great_grandparent: 3, sibling: 3, spouse_grandparent: 3,
};

/** 喪假關係分組（依天數分類顯示） */
export const BEREAVEMENT_GROUPS: { days: number; relationships: BereavementRelationship[] }[] = [
  { days: 8, relationships: ['spouse', 'parent', 'adoptive_parent', 'step_parent'] },
  { days: 6, relationships: ['grandparent', 'child', 'spouse_parent', 'spouse_adoptive_parent'] },
  { days: 3, relationships: ['great_grandparent', 'sibling', 'spouse_grandparent'] },
];

// ── 核批狀態 ──

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

// ── 介面定義 ──

export interface DesignatedReviewer {
  id?: number;
  reviewerId: string;
  reviewerName?: string;
  stepOrder: number;
  status?: string;       // pending | approved | returned
  reviewedAt?: string;
  comment?: string;
}

export interface LeaveRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  hours: number;
  reason: string;
  approvalStatus: ApprovalStatus;
  bereavementRelationship?: string;
  designatedReviewers?: DesignatedReviewer[];
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
}

export interface AnnualQuota {
  totalDays: number;
  usedDays: number;
  availableDays: number;
  seniorityYears: number;
  seniorityMonths: number;
  message?: string;
}

export interface CompensatoryHours {
  totalOvertimeHours: number;
  usedCompensatoryHours: number;
  availableHours: number;
}

export interface CeremonialQuota {
  totalDays: number;
  usedDays: number;
  availableDays: number;
  isIndigenous: boolean;
  message?: string;
}

/** 生理假配額（限女性，每月 1 天、全年 12 天） */
export interface MenstrualQuota {
  isFemale: boolean;
  annualTotalDays: number;
  annualUsedDays: number;
  annualAvailableDays: number;
  monthlyTotalDays: number;
  monthlyUsedDays: number;
  monthlyAvailableDays: number;
  message?: string;
}

/** 婚假配額（上限 8 天，不限年度） */
export interface MarriageQuota {
  maxDays: number;
  usedDays: number;
  remainingDays: number;
}

/** 產假狀態（一次請完制） */
export interface MaternityStatus {
  hasActiveRequest: boolean;
  activeRequestId?: number;
  startDate?: string;
  endDate?: string;
  approvalStatus?: string;
}

/** 喪假配額（依親屬關係） */
export interface BereavementQuota {
  relationship: string;
  maxDays: number;
  usedDays: number;
  remainingDays: number;
}

/** 高階主管假適用性（JobTitle.Level ≤ 3） */
export interface SeniorExecutiveEligibility {
  isEligible: boolean;
  jobTitleLevel?: number;
}
