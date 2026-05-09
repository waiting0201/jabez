export type UserStatus = 'active' | 'inactive';

/** 輕量級使用者資料（供指定審核者下拉選單用） */
export interface UserLookup {
  id: string;
  name: string;
  jobTitleId?: number;
  status: string;
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
  agentUserId?: string;
  agentName?: string;
  birthday?: Date;
  isIndigenous?: boolean;
  indigenousProofUrl?: string;
  /** 低收入戶身份 */
  isLowIncome?: boolean;
  lowIncomeProofUrl?: string | null;
  /** 殘障身份 */
  isDisabled?: boolean;
  disabledProofUrl?: string | null;
  /** 健保金額手動覆寫（null = 走 lookup） */
  healthInsuranceOverride?: number | null;
  /** 勞保金額手動覆寫（null = 走 lookup） */
  laborInsuranceOverride?: number | null;
  /** 頭像顯示參數（圓形裁切框內的位置 / 縮放） */
  avatarPositionX?: number;
  avatarPositionY?: number;
  avatarScale?: number;
  createdAt: Date;
}
