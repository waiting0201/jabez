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
  /**
   * 關帳日（yyyy-MM-dd）：隔天起帳務類申請（請款 / 預審 / 預支 / 預支沖銷 / 出差 / 出差請款 / 出差沖銷）看不到也不能選此專案；
   * 加班與下班打卡工時佔比不受影響。可改可清空（後端單一真相 Common/ProjectAccountingClose）
   */
  accountingCloseDate?: string | null;
  /** 僅更新用：true＝清空關帳日 */
  clearAccountingCloseDate?: boolean;
  createdAt: Date;
  paymentSchedules?: ProjectPaymentSchedule[];
}

/** 是否已過關帳日（關帳日當天仍可用，隔天起關帳；以本地日期比對） */
export function isAccountingClosed(closeDate: string | null | undefined, today = new Date()): boolean {
  if (!closeDate) return false;
  const t = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
  return t > closeDate.substring(0, 10);
}

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  active: '進行中',
  closed: '已結案',
};

export const PROJECT_STATUS_CLASSES: Record<ProjectStatus, string> = {
  active: 'bg-success-subtle text-success',
  closed: 'bg-secondary-subtle text-secondary',
};
