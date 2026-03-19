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
  createdAt: Date;
}
