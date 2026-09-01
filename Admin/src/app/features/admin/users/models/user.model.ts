export type UserStatus = 'active' | 'inactive';

/** 輕量級使用者資料（供指定審核者下拉選單用） */
export interface UserLookup {
  id: string;
  name: string;
  jobTitleId?: number;
  departmentId?: number;
  status: string;
  /** 職稱層級（數字越小越高；供指定審核者判斷「部門最高層級」用） */
  jobTitleLevel?: number;
}

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  signatureUrl?: string;
  roleIds: string[];
  status: UserStatus;
  // 員工欄位
  departmentId?: number;
  departmentName?: string;
  jobTitleId?: number;
  jobTitleName?: string;
  hireDate?: Date;
  resignDate?: Date;
  baseSalary?: number;
  mealAllowance?: number;
  overtimePay?: number;
  sendPaySlip?: boolean;
  /** 期初補休時數（系統上線前累計，116/6/30 到期歸零） */
  compensatoryOpeningHours?: number;
  /** 排班制員工（賣店 / 營業所）：六日與國定假日視為工作日，可請六日的假 */
  isShiftWorker?: boolean;
  agentUserId?: string;
  agentName?: string;
  birthday?: Date;
  isIndigenous?: boolean;
  indigenousProofUrl?: string;
  /** 低收入戶身份 */
  isLowIncome?: boolean;
  lowIncomeProofUrl?: string | null;
  /** 身心障礙身份 */
  isDisabled?: boolean;
  disabledProofUrl?: string | null;
  /** 健保金額手動覆寫（null = 走 lookup） */
  healthInsuranceOverride?: number | null;
  /** 勞保金額手動覆寫（null = 走 lookup） */
  laborInsuranceOverride?: number | null;
  /** 勞退自提率（%，0~6 整數，null = 0%，直接欄位、非覆寫） */
  laborPensionSelfContributionRate?: number | null;
  /** 加給（自動同步自最新生效 SalaryAdjustmentRecord，可手動覆寫） */
  otherAllowance?: number | null;
  adjustmentDifference?: number | null;
  /** 頭像顯示參數（圓形裁切框內的位置 / 縮放） */
  avatarPositionX?: number;
  avatarPositionY?: number;
  avatarScale?: number;
  createdAt: Date;
}
