import {Component, inject, signal, computed, OnInit} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import * as XLSX from 'xlsx';
import {PayrollService} from '../../services/payroll.service';
import {MonthlyPayroll} from '../../models/payroll.model';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-payroll-list',
  templateUrl: './payroll-list.html',
  imports: [DecimalPipe, FormsModule, RouterLink, HasPermissionDirective],
})
export class PayrollList implements OnInit {
  private service = inject(PayrollService);
  private toastr  = inject(ToastrService);
  private route   = inject(ActivatedRoute);
  sending = signal(false);
  exporting = signal(false);

  private now = new Date();
  selectedYear  = signal(this.now.getFullYear());
  selectedMonth = signal(this.now.getMonth() + 1);

  ngOnInit() {
    // 若從編輯頁返回帶有 year/month queryParams，沿用同月份
    const y = Number(this.route.snapshot.queryParamMap.get('year'));
    const m = Number(this.route.snapshot.queryParamMap.get('month'));
    if (y && m >= 1 && m <= 12) {
      this.selectedYear.set(y);
      this.selectedMonth.set(m);
    }
    // 自動載入當前年月的薪資資料，確保與後端計算一致（不再依賴手動查詢）
    this.search();
  }

  yearMonth = computed(() => {
    const y = this.selectedYear();
    const m = String(this.selectedMonth()).padStart(2, '0');
    return `${y}-${m}`;
  });

  loading = signal(false);
  payroll = signal<MonthlyPayroll | null>(null);
  errorMsg = signal('');

  employees = computed(() => this.payroll()?.employees ?? []);

  /** 2 種加給總計（其他加給 + 代扣代付款） */
  totalAllowances(p: MonthlyPayroll): number {
    return p.totalOtherAllowance
         + p.totalAdjustmentDifference;
  }

  onMonthChange(value: string) {
    const [y, m] = value.split('-').map(Number);
    if (y && m) {
      this.selectedYear.set(y);
      this.selectedMonth.set(m);
    }
  }

  search() {
    this.loading.set(true);
    this.errorMsg.set('');
    this.service.getMonthly(this.selectedYear(), this.selectedMonth()).subscribe({
      next: data => {
        this.payroll.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.payroll.set(null);
        this.errorMsg.set('載入薪資資料失敗，請稍後再試。');
      },
    });
  }

  /** 匯出總表：一位員工一列 × 33 欄（全部薪資相關欄位）+ 合計列。
   *  GET /payroll 本身不分頁、已回傳全月完整欄位，故直接讀 payroll() signal，不再打一次 API。 */
  exportExcel() {
    const p = this.payroll();
    if (!p || p.employees.length === 0) {
      this.toastr.warning('查無資料可匯出。', '提示');
      return;
    }

    this.exporting.set(true);
    try {
      const headers = [
        '員工姓名', '部門', '職稱', '到職日',
        '底薪', '伙食費', '加班費', '加班費(加班申請)', '其他加給', '代扣代付款', '日薪',
        '假日活動天數', '假日津貼', '其他加項', '其他加項說明',
        '勞保費', '健保費', '健保眷屬口數（計費）',
        '事假天數', '事假扣薪', '病假天數', '病假扣薪',
        '生理假天數', '生理假扣薪', '家庭照顧假天數', '家庭照顧假扣薪',
        '其他扣項', '其他扣項說明', '勞退自提率(%)', '勞退自提扣款',
        '育嬰留停天數', '備註', '實領薪水',
      ];

      const summary = `人事薪資總表　${p.year} 年 ${p.month} 月　員工人數：${p.employees.length}　匯出時間：${new Date().toLocaleString('zh-TW')}`;

      const rows = p.employees.map(e => [
        e.employeeName,
        e.departmentName ?? '',
        e.jobTitleName ?? '',
        e.hireDate ? new Date(e.hireDate).toLocaleDateString('zh-TW') : '',
        e.baseSalary, e.mealAllowance, e.overtimePay, e.calculatedOvertimePay, e.otherAllowanceAmount, e.adjustmentDifference, e.dailySalary,
        e.holidayTravelDays, e.holidayAllowance, e.otherAddition, e.otherAdditionNote ?? '',
        e.laborInsurance, e.healthInsurance, e.cappedDependentCount,
        e.personalLeaveDays, e.personalLeaveDeduction, e.sickLeaveDays, e.sickLeaveDeduction,
        e.menstrualLeaveDays, e.menstrualLeaveDeduction, e.familyCareLeaveDays, e.familyCareLeaveDeduction,
        e.otherDeduction, e.otherDeductionNote ?? '', e.laborPensionSelfContributionRate ?? 0, e.laborPensionSelfDeduction,
        e.parentalLeaveDays, e.note ?? '', e.netSalary,
      ]);

      // 合計列一律取後端既有合計欄，不在前端自行加總（與畫面 summary cards 同一份真相）。
      // 後端未提供合計的欄位（日薪 / 各請假天數 / 眷屬口數 / 自提率 / 說明欄）留空。
      const totalRow: (string | number)[] = [
        '合計', '', '', '',
        p.totalBaseSalary, p.totalMealAllowance, p.totalOvertimePay, p.totalCalculatedOvertimePay, p.totalOtherAllowance, p.totalAdjustmentDifference, '',
        '', p.totalHolidayAllowance, p.totalOtherAddition, '',
        p.totalLaborInsurance, p.totalHealthInsurance, '',
        '', p.totalPersonalLeaveDeduction, '', p.totalSickLeaveDeduction,
        '', p.totalMenstrualLeaveDeduction, '', p.totalFamilyCareLeaveDeduction,
        p.totalOtherDeduction, '', '', p.totalLaborPensionSelfDeduction,
        p.totalParentalLeaveDays, '', p.totalNetSalary,
      ];

      const aoa: (string | number)[][] = [[summary], [], headers, ...rows, totalRow];
      const ws = XLSX.utils.aoa_to_sheet(aoa);

      // 欄寬（單位：字元）— 中文字佔 2 格，故不能直接用 header.length
      const displayWidth = (t: string) => [...t].reduce((n, ch) => n + (ch.charCodeAt(0) > 0x2e80 ? 2 : 1), 0);
      ws['!cols'] = headers.map(h => ({wch: Math.max(displayWidth(h) + 2, 10)}));

      // 數字格式：金額欄套千分位；天數 / 口數 / 自提率維持 General
      //（'#,##0.##' 會讓整數顯示成「6.」多一個小數點，故不套）
      const rawNumberCols = new Set([11, 17, 18, 20, 22, 24, 28, 30]);
      const headerRowIdx = 2;
      const lastRowIdx = aoa.length - 1;
      for (let r = headerRowIdx + 1; r <= lastRowIdx; r++) {
        for (let c = 4; c < headers.length; c++) {
          if (rawNumberCols.has(c)) continue;
          const cell = ws[XLSX.utils.encode_cell({r, c})];
          if (cell && typeof cell.v === 'number') cell.z = '#,##0';
        }
      }

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, '人事薪資總表');
      // 檔名取「實際載入資料」的年月，而非月份選擇器的值 —— 使用者改了選擇器但未按查詢時，兩者會不一致
      XLSX.writeFile(wb, `人事薪資總表_${p.year}-${String(p.month).padStart(2, '0')}.xlsx`);
      this.toastr.success(`已匯出 ${p.employees.length} 筆資料。`, '匯出完成');
    } catch (e) {
      console.error('export xlsx failed', e);
      this.toastr.error('匯出檔案產生失敗。', '匯出失敗');
    } finally {
      this.exporting.set(false);
    }
  }

  sendSlips() {
    if (this.sending()) return;
    this.sending.set(true);
    this.errorMsg.set('');
    this.service.sendSlips(this.selectedYear(), this.selectedMonth()).subscribe({
      next: res => {
        this.sending.set(false);
        if (res.sent > 0) {
          this.toastr.success(`已寄送 ${res.sent}/${res.total} 封薪資明細。`, '寄送完成');
        } else {
          this.toastr.warning('沒有需要寄送的員工。', '提示');
        }
        if (res.errors?.length > 0) {
          this.errorMsg.set(`部分寄送失敗：${res.errors.join('；')}`);
        }
        // 寄送後重新載入，確保列表顯示與信件內容一致
        this.search();
      },
      error: () => {
        this.sending.set(false);
        this.errorMsg.set('寄送薪資明細失敗，請稍後再試。');
      },
    });
  }
}
