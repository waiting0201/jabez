export type LeaveType =
  | 'annual' | 'personal' | 'sick' | 'compensatory'
  | 'marriage' | 'bereavement' | 'official'
  | 'maternity' | 'miscarriage_3m' | 'miscarriage_2to3m' | 'miscarriage_under2m'
  | 'prenatal_checkup' | 'paternity'
  | 'ceremonial_festival'
  | 'senior_executive'
  | 'menstrual'
  | 'family_care'
  | 'parental_leave' | 'parental_leave_daily';

/** 時間單位：小時 / 半天(4hr) / 整天(8hr) */
export type LeaveTimeUnit = 'hour' | 'half_day' | 'day';

export type ApprovalStatus = 'draft' | 'pending' | 'approved' | 'rejected' | 'returned' | 'cancelled';

export const LEAVE_TYPE_LABELS: Record<LeaveType, string> = {
  annual:              '年假(特休假)',
  personal:            '事假',
  family_care:         '家庭照顧假',
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
  parental_leave:       '育嬰留職停薪',
  parental_leave_daily: '育嬰留停(單日)',
};

/**
 * 各假別時間單位對應（需與後端 LeaveRequestHandler.TimeUnitMap 保持同步）
 * - hour: 事假 / 家庭照顧假 / 病假 / 產檢假 / 陪產假
 * - half_day: 特休 / 補休 / 高階主管假（4 小時）
 * - day: 公假 / 婚假 / 產假 / 喪假 / 歲時祭儀假 / 流產假系列 / 育嬰留停（8 小時）
 */
export const LEAVE_TIME_UNIT: Record<LeaveType, LeaveTimeUnit> = {
  personal:            'hour',
  family_care:         'hour',
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
  parental_leave:       'day',
  parental_leave_daily: 'day',
};

/**
 * 工作日型假別：天數 / 時數以「扣除國定假日與六日後的實際工作日」計算（顯示請假日清單）。
 * 不適用者為歲時祭儀假與育嬰留職停薪（依法／依語意為連續日曆天）；
 * 產假區間仍為起始日 +55 天，但只計其中工作日。
 * 須與後端 LeaveDayExpander.WorkingDayLeaveTypes 保持同步。
 *
 * parental_leave（長期留停）刻意不列入：留停整段期間都不在職（含六日與國定假日），
 * 且工作日型假別送出時後端會強制要求區間橫跨的每個年度行事曆皆已匯入，
 * 育嬰留停跨 1~2 年會因未來年度行事曆未匯入而無法送件。
 * parental_leave_daily（彈性單日）為一般工作日請假語意，仍列入。
 */
export const WORKING_DAY_LEAVE_TYPES: LeaveType[] =
  ['annual', 'personal', 'sick', 'compensatory', 'official', 'senior_executive',
   'marriage', 'maternity', 'bereavement',
   'miscarriage_3m', 'miscarriage_2to3m', 'miscarriage_under2m',
   'prenatal_checkup', 'paternity', 'menstrual', 'family_care',
   'parental_leave_daily'];

/**
 * 工作日標準時段（與 half_day 的 am 08:00–12:00 / pm 13:00–17:00 一致，全日 8 小時）。
 * 供 hour 單位跨日請假的逐日累加使用；須與後端 LeaveRequestHandler 的同名常數保持同步。
 */
export const WORKDAY_START_HOUR = 8;
export const WORKDAY_END_HOUR   = 17;

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
  family_care:         'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
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
  parental_leave:       'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
  parental_leave_daily: 'bg-[rgba(124,94,140,0.12)] text-[#7C5E8C]',
};

/** 假別分組（供下拉選單 optgroup 使用） */
export const LEAVE_TYPE_GROUPS: { label: string; types: LeaveType[] }[] = [
  { label: '一般假別', types: ['annual', 'personal', 'family_care', 'sick', 'official', 'compensatory'] },
  { label: '婚假',     types: ['marriage'] },
  { label: '產假類別', types: ['maternity', 'miscarriage_3m', 'miscarriage_2to3m', 'miscarriage_under2m', 'prenatal_checkup', 'paternity'] },
  { label: '喪假',     types: ['bereavement'] },
  { label: '其他假別', types: ['ceremonial_festival'] },
  // 生理假僅女性可見（實際顯示由前端依女性身分過濾）
  { label: '生理假',   types: ['menstrual'] },
  // 高階主管假僅協理以上可見（實際顯示由前端依 auth.isSeniorExecutive() 過濾）
  { label: '高階主管假', types: ['senior_executive'] },
  // 育嬰假僅在職滿 6 個月者可見（實際顯示由前端依 isParentalEligible() 過濾）
  { label: '育嬰假',   types: ['parental_leave', 'parental_leave_daily'] },
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
  family_care:         7,
  parental_leave:       730,   // 每名子女合計 2 年
  parental_leave_daily: 30,    // 每人每年 30 日（雙親合計 60 日，系統無法驗證）
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
  cancelled: '已銷假',
};

export const APPROVAL_STATUS_CLASSES: Record<ApprovalStatus, string> = {
  draft:    'bg-blue-subtle text-blue-emphasis',
  pending:  'bg-warning-subtle text-warning-emphasis',
  approved: 'bg-success-subtle text-success',
  rejected: 'bg-danger-subtle text-danger',
  returned: 'bg-secondary-subtle text-secondary',
  cancelled: 'bg-secondary-subtle text-secondary',
};

// ── 介面定義 ──

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

export interface LeaveRequest {
  id: number;
  employeeId?: string;
  approvalItemId?: number;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  hours: number;              // 剩餘有效時數（銷假核准後遞減）
  originalHours?: number;     // 原始請假時數；有值代表曾銷假（部分或全部）
  reason: string;
  approvalStatus: ApprovalStatus;
  bereavementRelationship?: string;
  childBirthDate?: string | null;      // 育嬰留停：子女出生日期
  continueInsurance?: boolean | null;  // 育嬰留停：期間是否續保勞健保（僅記錄意願）
  designatedReviewers?: DesignatedReviewer[];
  agentUserId?: string | null;   // 職務代理人（記錄 + 通知，不參與簽核）
  agentName?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewNote?: string;
}

/** working-days 端點回應：扣除國定假日與六日後的實際請假日清單 */
export interface WorkingDaysResult {
  hasCalendarData: boolean;   // 該區間是否已匯入行事曆（false 時僅扣六日、國定假未扣）
  holidayDates: string[];     // 被扣除的假日
  workingDates: string[];     // 實際請假日
  workingDays: number;        // 實際請假天數
}

export interface AnnualQuota {
  totalDays: number;
  usedDays: number;
  availableDays: number;
  seniorityYears: number;
  seniorityMonths: number;
  /** 已從年資扣除的育嬰留停天數（0＝未曾留停）；留停期間不計入工作年資，特休隨之暫停累積 */
  parentalLeaveExcludedDays?: number;
  message?: string;
}

export interface CompensatoryHours {
  /** 期初匯入時數（系統上線前累計） */
  openingHours: number;
  /** 舊補休剩餘（期初未消耗部分；到期後為 0） */
  openingRemaining: number;
  /** 期初到期日（116/6/30） */
  openingExpiry: string;
  /** 期初是否已到期 */
  openingExpired: boolean;
  /** 系統核准加班可補休時數 */
  totalOvertimeHours: number;
  usedCompensatoryHours: number;
  /** 合計可用 */
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

/**
 * 育嬰留職停薪配額。
 * 兩層額度：每名子女合計 730 天（2 年，兩種育嬰假別併計，需帶 childBirthDate 才算得出）
 * ＋ 彈性單日每人每年 30 日。「雙親合計 60 日」系統無法驗證，僅表單提示。
 */
export interface ParentalQuota {
  isEligible: boolean;          // 在職是否滿 6 個月
  seniorityMonths: number;
  childAgeValid: boolean;       // 子女是否未滿 3 歲
  childBirthDate?: string | null;
  totalDays: number;            // 730
  usedDays: number;
  availableDays: number;
  dailyYearLimit: number;       // 30
  dailyYearUsed: number;
  dailyYearAvailable: number;
  message?: string;
}

/** 高階主管假額度（每年 24 天，曆年歸零；year＝額度所屬曆年，依請假起始日決定） */
export interface SeniorExecutiveQuota {
  year: number;
  totalDays: number;
  usedDays: number;
  availableDays: number;
  isEligible: boolean;
  message?: string;
}
