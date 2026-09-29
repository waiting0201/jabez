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
  /**
   * 國定假日出勤天數（上月）。「出勤」＝當日有上班打卡 ∪ 有已核准加班單，**依日期去重**。
   * 切換日（四週彈性工時）之前恆為 0。
   */
  publicHolidayWorkDays: number;
  /** 國定假日出勤加倍工資 ＝ 出勤天數 × 日薪（出勤即加發一日，不按時數折算） */
  publicHolidayDoublePay: number;
  /** 補休未休完時數（上月到期的 lot 剩餘合計） */
  compensatorySettlementHours: number;
  /** 補休未休完加班津貼 ＝ Σ(剩餘時數 × 原始加班費率快照) × 時薪 */
  compensatorySettlementAmount: number;
  /** 薪資單上的項目名稱，例「116年1-6月補休時數未完畢津貼」 */
  compensatorySettlementNote: string | null;
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
  totalPublicHolidayDoublePay: number;
  totalCompensatorySettlementAmount: number;
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
