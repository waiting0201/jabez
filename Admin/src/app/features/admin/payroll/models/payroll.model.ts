export interface EmployeePayroll {
  employeeId: string;
  employeeName: string;
  email: string | null;
  sendPaySlip: boolean;
  departmentName: string | null;
  jobTitleName: string | null;
  hireDate: string | null;
  baseSalary: number;
  mealAllowance: number;
  overtimePay: number;
  /** 加班申請試算加班費（上月加班、已核准且選「加班費」者的快照合計；與手填 overtimePay 併存） */
  calculatedOvertimePay: number;
  calculatedOvertimeHours: number;
  dailySalary: number;
  holidayTravelDays: number;
  holidayAllowance: number;
  otherAddition: number;
  otherAdditionNote: string | null;
  laborInsurance: number;
  healthInsurance: number;
  personalLeaveDays: number;
  personalLeaveDeduction: number;
  sickLeaveDays: number;
  sickLeaveDeduction: number;
  menstrualLeaveDays: number;
  menstrualLeaveDeduction: number;
  familyCareLeaveDays: number;
  familyCareLeaveDeduction: number;
  otherDeduction: number;
  otherDeductionNote: string | null;
  note: string | null;
  netSalary: number;
  leaveDetails?: LeaveDetail[];
  /** 5 種加給（同步自最新生效 SalaryAdjustmentRecord，計入 NetSalary） */
  /** 後端 DTO 名為 OtherAllowanceAmount 以避開與舊欄位衝突 */
  otherAllowanceAmount: number;
  adjustmentDifference: number;
  /** 勞退自提率（%，null = 0%，直接欄位、非覆寫） */
  laborPensionSelfContributionRate: number | null;
  laborPensionSelfDeduction: number;
  /** 健保眷屬實際人數 */
  dependentCount: number;
  /** 計費眷屬口數 = min(眷屬數, 3)，健保費 = 員工負擔 ×(1 + 此數) */
  cappedDependentCount: number;
  /**
   * 該月育嬰留職停薪日曆天數。> 0 代表底薪與各項加給已按「在職天數 ÷ 30」折減；
   * 整月留停者不會出現在薪資名單中。
   */
  parentalLeaveDays: number;
}

export interface LeaveDetail {
  leaveType: string;
  startDate: string;
  endDate: string;
  hours: number;
}

export interface MonthlyPayroll {
  year: number;
  month: number;
  employees: EmployeePayroll[];
  totalBaseSalary: number;
  totalMealAllowance: number;
  totalOvertimePay: number;
  totalCalculatedOvertimePay: number;
  totalHolidayAllowance: number;
  totalOtherAddition: number;
  totalLaborInsurance: number;
  totalHealthInsurance: number;
  totalPersonalLeaveDeduction: number;
  totalSickLeaveDeduction: number;
  totalMenstrualLeaveDeduction: number;
  totalFamilyCareLeaveDeduction: number;
  totalOtherDeduction: number;
  totalNetSalary: number;
  totalOtherAllowance: number;
  totalAdjustmentDifference: number;
  totalLaborPensionSelfDeduction: number;
  totalParentalLeaveDays: number;
}

export interface PayrollAdjustment {
  id: number;
  employeeId: string;
  year: number;
  month: number;
  otherAddition: number;
  otherAdditionNote: string | null;
  otherDeduction: number;
  otherDeductionNote: string | null;
  note: string | null;
}

export interface PayrollAdjustmentRequest {
  otherAddition: number;
  otherAdditionNote: string | null;
  otherDeduction: number;
  otherDeductionNote: string | null;
  note: string | null;
}
