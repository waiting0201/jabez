import { EmployeePayroll } from '../../admin/payroll/models/payroll.model';

/** 員工自助查詢：單月薪資紀錄（payroll 為當月即時重算結果，非月結快照） */
export interface MyPayrollMonth {
  year: number;
  month: number;
  isCurrentMonth: boolean;
  payroll: EmployeePayroll;
}

/** 員工自助查詢：近 N 個月薪資紀錄（新到舊） */
export interface MyPayrollHistory {
  months: MyPayrollMonth[];
}
