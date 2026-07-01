import {Component, inject, OnInit, signal, computed} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {ToastrService} from 'ngx-toastr';
import {PayrollService} from '../../services/payroll.service';
import {EmployeePayroll} from '../../models/payroll.model';
import {LEAVE_TYPE_LABELS, LeaveType, formatLeaveDuration} from '../../../leave-requests/models/leave-request.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-payroll-form',
  templateUrl: './payroll-form.html',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe, ScrollIntoViewDirective],
})
export class PayrollForm implements OnInit {
  private fb      = inject(FormBuilder);
  private service = inject(PayrollService);
  private route   = inject(ActivatedRoute);
  private router  = inject(Router);
  private toastr  = inject(ToastrService);

  employeeId = '';
  year  = 0;
  month = 0;

  emp      = signal<EmployeePayroll | null>(null);
  loading  = signal(false);
  saving   = signal(false);
  errorMsg = signal('');

  form = this.fb.group({
    otherAddition:      [0],
    otherAdditionNote:  [''],
    otherDeduction:     [0],
    otherDeductionNote: [''],
    note:               [''],
  });

  /** 即時預覽實領薪資 */
  previewNet = computed(() => {
    const e = this.emp();
    if (!e) return 0;
    const otherAdd = this.form.get('otherAddition')!.value ?? 0;
    const otherDed = this.form.get('otherDeduction')!.value ?? 0;
    return e.baseSalary + e.mealAllowance + e.overtimePay
         + e.holidayAllowance + otherAdd
         - e.laborInsurance - e.healthInsurance
         - e.personalLeaveDeduction - e.sickLeaveDeduction - e.menstrualLeaveDeduction
         - otherDed;
  });

  /** 返回列表的 queryParams（保留年月） */
  backLink = computed(() => `/admin/payroll`);

  periodLabel = computed(() => `${this.year} 年 ${this.month} 月`);

  readonly leaveTypeLabels = LEAVE_TYPE_LABELS;

  getLeaveTypeLabel(type: string): string {
    return (LEAVE_TYPE_LABELS as Record<string, string>)[type] ?? type;
  }

  /** 依假別單位格式化時數顯示 */
  formatLeaveDuration(type: string, hours: number): string {
    return formatLeaveDuration(type as LeaveType, hours);
  }

  ngOnInit() {
    this.employeeId = this.route.snapshot.paramMap.get('id') ?? '';
    this.year  = Number(this.route.snapshot.queryParamMap.get('year'))  || new Date().getFullYear();
    this.month = Number(this.route.snapshot.queryParamMap.get('month')) || (new Date().getMonth() + 1);

    this.loadData();
  }

  private loadData() {
    this.loading.set(true);
    this.errorMsg.set('');

    // 同時載入月薪資料和調整資料
    this.service.getMonthly(this.year, this.month).subscribe({
      next: data => {
        const match = data.employees.find(e => e.employeeId === this.employeeId);
        if (!match) {
          this.errorMsg.set('找不到該員工的薪資資料。');
          this.loading.set(false);
          return;
        }
        this.emp.set(match);

        // 用既有資料填入表單
        this.form.patchValue({
          otherAddition:      match.otherAddition ?? 0,
          otherAdditionNote:  match.otherAdditionNote ?? '',
          otherDeduction:     match.otherDeduction ?? 0,
          otherDeductionNote: match.otherDeductionNote ?? '',
          note:               match.note ?? '',
        });

        this.loading.set(false);
      },
      error: () => {
        this.errorMsg.set('載入薪資資料失敗。');
        this.loading.set(false);
      },
    });
  }

  submit() {
    if (this.saving()) return;
    this.saving.set(true);
    this.errorMsg.set('');

    const {otherAddition, otherAdditionNote, otherDeduction, otherDeductionNote, note} = this.form.value;
    this.service.upsertAdjustment(this.employeeId, this.year, this.month, {
      otherAddition:      otherAddition ?? 0,
      otherAdditionNote:  otherAdditionNote || null,
      otherDeduction:     otherDeduction ?? 0,
      otherDeductionNote: otherDeductionNote || null,
      note:               note || null,
    }).subscribe({
      next: () => {
        this.toastr.success('薪資調整已儲存。', '儲存成功');
        this.saving.set(false);
        // 帶上 year/month 讓列表頁重新載入該月最新計算結果（避免顯示快取值）
        this.router.navigate(['/admin/payroll'], {
          queryParams: {year: this.year, month: this.month},
        });
      },
      error: () => {
        this.errorMsg.set('儲存失敗，請稍後再試。');
        this.saving.set(false);
      },
    });
  }
}
