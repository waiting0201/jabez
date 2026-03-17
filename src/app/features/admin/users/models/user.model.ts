export type UserStatus = 'active' | 'inactive';

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
