export type UserStatus = 'active' | 'inactive';

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
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
  agentUserId?: string;
  agentName?: string;
  createdAt: Date;
}
