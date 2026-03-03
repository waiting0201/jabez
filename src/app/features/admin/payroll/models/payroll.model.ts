export interface EmployeePayroll {
  employeeId: string;
  employeeName: string;
  departmentName: string | null;
  jobTitleName: string | null;
  hireDate: string | null;
  baseSalary: number;
  dailySalary: number;
  holidayTravelDays: number;
  holidayAllowance: number;
  laborInsurance: number;
  healthInsurance: number;
  netSalary: number;
}

export interface MonthlyPayroll {
  year: number;
  month: number;
  employees: EmployeePayroll[];
  totalBaseSalary: number;
  totalHolidayAllowance: number;
  totalLaborInsurance: number;
  totalHealthInsurance: number;
  totalNetSalary: number;
}
