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
  otherDeduction: number;
  otherDeductionNote: string | null;
  note: string | null;
  netSalary: number;
  leaveDetails?: LeaveDetail[];
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
  totalOtherDeduction: number;
  totalNetSalary: number;
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
