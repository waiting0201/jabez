import {Component, inject, signal, computed, OnInit} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
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
