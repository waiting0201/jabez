import {Component, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {EmployeePayroll} from '../../features/admin/payroll/models/payroll.model';
import {LEAVE_TYPE_LABELS, LeaveType, formatLeaveDuration} from '../../features/admin/leave-requests/models/leave-request.model';

/**
 * 共用「單月薪資明細」卡片 — 應發項目 / 扣款項目 / 實領 + 本月請假紀錄。
 *
 * 已採用：payroll-form（人事薪資調整頁，管理端）、my-profile「過往薪資」Tab（員工自助）。
 * 兩端共用同一份版型，確保員工看到的數字與人事頁完全一致。
 * 樣式與其他 detail 卡片一致：card border-0 shadow-sm + card-header + card-body p-0。
 */
@Component({
  selector: 'app-payroll-detail-card',
  imports: [DatePipe, DecimalPipe],
  template: `
    @if (payroll(); as e) {
    <!-- 員工薪資摘要（唯讀） -->
    <div class="card border-0 shadow-sm mb-4">
      <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
        <svg class="sa-icon"><use href="/assets/icons/sprite.svg#dollar-sign"></use></svg>
        薪資明細
      </div>
      <div class="card-body p-0">
        <table class="table table-hover mb-0">
          <tbody>
            <tr>
              <td class="fw-500 ps-4" style="width:40%">員工姓名</td>
              <td class="fw-600">{{ e.employeeName }}</td>
            </tr>
            <tr>
              <td class="fw-500 ps-4">部門 / 職稱</td>
              <td>{{ e.departmentName || '---' }} / {{ e.jobTitleName || '---' }}</td>
            </tr>
            <tr>
              <td class="fw-500 ps-4">到職日</td>
              <td>{{ e.hireDate ? (e.hireDate | date:'yyyy/MM/dd') : '---' }}</td>
            </tr>
            <tr class="table-light">
              <td colspan="2" class="fw-600 ps-4 text-primary">
                <span class="inline-flex items-center gap-1">
                  <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus-circle"></use></svg>
                  應發項目
                </span>
              </td>
            </tr>
            <tr>
              <td class="ps-6">
                底薪
                @if (e.parentalLeaveDays > 0) {
                  <span class="text-muted small ms-1">（育嬰留停 {{ e.parentalLeaveDays | number:'1.0-1' }} 天，按在職比例計算）</span>
                }
              </td>
              <td class="text-right pe-4">{{ e.baseSalary | number:'1.0-0' }}</td>
            </tr>
            <tr>
              <td class="ps-6">伙食費</td>
              <td class="text-right pe-4">{{ e.mealAllowance | number:'1.0-0' }}</td>
            </tr>
            <tr>
              <td class="ps-6">加班費</td>
              <td class="text-right pe-4">{{ e.overtimePay | number:'1.0-0' }}</td>
            </tr>
            <!-- 5 種加給：有值才顯示 -->
            @if (e.otherAllowanceAmount) {
              <tr>
                <td class="ps-6">其他加給</td>
                <td class="text-right pe-4">{{ e.otherAllowanceAmount | number:'1.0-0' }}</td>
              </tr>
            }
            @if (e.adjustmentDifference) {
              <tr>
                <td class="ps-6">代扣代付款</td>
                <td class="text-right pe-4">{{ e.adjustmentDifference | number:'1.0-0' }}</td>
              </tr>
            }
            <tr>
              <td class="ps-6">日薪（底薪 ÷ 30）</td>
              <td class="text-right pe-4 text-muted">{{ e.dailySalary | number:'1.0-0' }}</td>
            </tr>
            <tr>
              <td class="ps-6">假日執行活動天數</td>
              <td class="text-right pe-4">{{ e.holidayTravelDays }} 天</td>
            </tr>
            <tr>
              <td class="ps-6">假日津貼</td>
              <td class="text-right pe-4">{{ e.holidayAllowance | number:'1.0-0' }}</td>
            </tr>
            <tr class="table-light">
              <td colspan="2" class="fw-600 ps-4 text-danger">
                <span class="inline-flex items-center gap-1">
                  <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#minus-circle"></use></svg>
                  扣款項目
                </span>
              </td>
            </tr>
            <tr>
              <td class="ps-6">勞保費（員工負擔）</td>
              <td class="text-right pe-4 text-danger">-{{ e.laborInsurance | number:'1.0-0' }}</td>
            </tr>
            <tr>
              <td class="ps-6">健保費（員工負擔）</td>
              <td class="text-right pe-4 text-danger">-{{ e.healthInsurance | number:'1.0-0' }}</td>
            </tr>
            @if (e.personalLeaveDays > 0) {
              <tr>
                <td class="ps-6">事假扣薪（{{ e.personalLeaveDays | number:'1.0-2' }} 天 = {{ e.personalLeaveDays * 8 | number:'1.0-1' }} 小時）</td>
                <td class="text-right pe-4 text-danger">-{{ e.personalLeaveDeduction | number:'1.0-0' }}</td>
              </tr>
            }
            @if (e.sickLeaveDays > 0) {
              <tr>
                <td class="ps-6">病假扣薪（{{ e.sickLeaveDays | number:'1.0-2' }} 天 = {{ e.sickLeaveDays * 8 | number:'1.0-1' }} 小時 × 半薪）</td>
                <td class="text-right pe-4 text-danger">-{{ e.sickLeaveDeduction | number:'1.0-0' }}</td>
              </tr>
            }
            @if (e.menstrualLeaveDays > 0) {
              <tr>
                <td class="ps-6">生理假扣薪（{{ e.menstrualLeaveDays | number:'1.0-2' }} 天 = {{ e.menstrualLeaveDays * 8 | number:'1.0-1' }} 小時 × 半薪）</td>
                <td class="text-right pe-4 text-danger">-{{ e.menstrualLeaveDeduction | number:'1.0-0' }}</td>
              </tr>
            }
            @if (e.familyCareLeaveDays > 0) {
              <tr>
                <td class="ps-6">家庭照顧假扣薪（{{ e.familyCareLeaveDays | number:'1.0-2' }} 天 = {{ e.familyCareLeaveDays * 8 | number:'1.0-1' }} 小時）</td>
                <td class="text-right pe-4 text-danger">-{{ e.familyCareLeaveDeduction | number:'1.0-0' }}</td>
              </tr>
            }
            @if (e.laborPensionSelfDeduction > 0) {
              <tr>
                <td class="ps-6">勞工退休金自提（{{ e.laborPensionSelfContributionRate }}%）</td>
                <td class="text-right pe-4 text-danger">-{{ e.laborPensionSelfDeduction | number:'1.0-0' }}</td>
              </tr>
            }
            <!-- 實領薪水：唯讀情境（員工自助）才顯示；payroll-form 另有可即時預覽的實領卡片 -->
            @if (showNetSalary()) {
              <tr class="table-light">
                <td class="fw-600 ps-4">實領薪水</td>
                <td class="text-right pe-4 fw-600 text-lg" [class.text-danger]="e.netSalary < 0">
                  NT$ {{ e.netSalary | number:'1.0-0' }}
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>

    <!--
      育嬰留停當月的實領為負數：底薪與加給已按在職天數折減，但勞健保仍收全額，
      差額為員工應補繳的保費（見 docs/business/leave-rules.md）。
    -->
    @if (showNetSalary() && e.parentalLeaveDays > 0 && e.netSalary < 0) {
      <div class="alert alert-warning flex items-start gap-2 py-2 mb-4">
        <svg class="sa-icon shrink-0 mt-1" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
        <span class="small">
          本月實領為<strong>負數</strong>：育嬰留停期間薪資按在職天數折減，但勞健保仍收全額，
          差額 <strong>NT$ {{ -e.netSalary | number:'1.0-0' }}</strong> 為應補繳的保費，請洽人事。
        </span>
      </div>
    }

    <!-- 本月請假紀錄 -->
    @if (e.leaveDetails && e.leaveDetails.length > 0) {
      <div class="card border-0 shadow-sm mb-4">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600 text-[#7C5E8C]">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#calendar"></use></svg>
          本月請假紀錄
        </div>
        <div class="card-body p-0">
          <table class="table table-sm mb-0">
            <thead>
              <tr class="bg-[var(--bg-elevated)]">
                <th class="ps-4">假別</th>
                <th>期間</th>
                <th class="text-right pe-4">期間</th>
              </tr>
            </thead>
            <tbody>
              @for (ld of e.leaveDetails; track $index) {
                <tr>
                  <td class="ps-4">{{ getLeaveTypeLabel(ld.leaveType) }}</td>
                  <td>{{ ld.startDate | date:'MM/dd HH:mm' }} ~ {{ ld.endDate | date:'MM/dd HH:mm' }}</td>
                  <td class="text-right pe-4">{{ formatLeaveDuration(ld.leaveType, ld.hours) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
    }
  `,
})
export class PayrollDetailCard {
  /** 單月薪資計算結果（EmployeePayrollDto） */
  payroll = input.required<EmployeePayroll>();

  /** 是否在表尾附上「實領薪水」與育嬰留停負數警語（唯讀情境用；payroll-form 有自己的即時預覽卡片） */
  showNetSalary = input(false);

  getLeaveTypeLabel(type: string): string {
    return (LEAVE_TYPE_LABELS as Record<string, string>)[type] ?? type;
  }

  /** 依假別單位格式化時數顯示 */
  formatLeaveDuration(type: string, hours: number): string {
    return formatLeaveDuration(type as LeaveType, hours);
  }
}
