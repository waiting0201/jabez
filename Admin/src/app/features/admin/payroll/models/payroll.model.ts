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
  positionAllowance: number;
  dutyAllowance: number;
  /** 後端 DTO 名為 OtherAllowanceAmount 以避開與舊欄位衝突 */
  otherAllowanceAmount: number;
  adjustmentDifference: number;
  overseasAllowance: number;
  /** 勞退自提率（%，null = 0%，直接欄位、非覆寫） */
  laborPensionSelfContributionRate: number | null;
  laborPensionSelfDeduction: number;
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
  totalHolidayAllowance: number;
  totalOtherAddition: number;
  totalLaborInsurance: number;
  totalHealthInsurance: number;
  totalPersonalLeaveDeduction: number;
  totalSickLeaveDeduction: number;
  totalFamilyCareLeaveDeduction: number;
  totalOtherDeduction: number;
  totalNetSalary: number;
  totalPositionAllowance: number;
  totalDutyAllowance: number;
  totalOtherAllowance: number;
  totalAdjustmentDifference: number;
  totalOverseasAllowance: number;
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
