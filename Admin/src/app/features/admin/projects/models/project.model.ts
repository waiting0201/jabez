export type ProjectStatus = 'active' | 'closed';

export interface ProjectPaymentSchedule {
  id: string;                 // UUID（後端 Guid）
  periodNo: number;           // 期別順序（1, 2, 3...）
  billingDate?: string | null;
  billingAmount?: number | null;
  invoiceDate?: string | null;
  invoiceAmount?: number | null;
  depositDate?: string | null;
  depositAmount?: number | null;
  deductionNote?: string | null;
}

export interface Project {
  id: number;
  code: string;
  name: string;
  status: ProjectStatus;
  startDate: string;
  endDate?: string;
  departmentId: number;
  departmentName?: string;
  receivedAmount?: number;    // 實收金額（後端衍生：SUM(ProjectPaymentSchedules.depositAmount)）
  contractAmount?: number;    // 契約金額
  businessAmount?: number;    // 業務執行金額
  remainingAmount?: number;   // 剩餘金額（系統導入時的契約剩餘預算，選填）
  googleDriveUrl?: string;
  createdAt: Date;
  paymentSchedules?: ProjectPaymentSchedule[];
}

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  active: '進行中',
  closed: '已結案',
};

export const PROJECT_STATUS_CLASSES: Record<ProjectStatus, string> = {
  active: 'bg-success-subtle text-success',
  closed: 'bg-secondary-subtle text-secondary',
};
