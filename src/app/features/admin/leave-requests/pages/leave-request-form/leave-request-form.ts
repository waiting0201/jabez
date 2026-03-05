import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, ValidatorFn, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveType, ApprovalStatus,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/leave-request.model';

@Component({
  selector: 'app-leave-request-form',
  templateUrl: './leave-request-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class LeaveRequestForm implements OnInit {
  private fb      = inject(FormBuilder);
  private service = inject(LeaveRequestService);
  private route   = inject(ActivatedRoute);
  private router  = inject(Router);

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');

  /** 可補休時數（從 API 取得） */
  compensatoryHours = signal<{ totalOvertimeHours: number; usedCompensatoryHours: number; availableHours: number } | null>(null);
  compensatoryLoading = signal(false);

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  /** 驗證分鐘必須為 00 或 30 */
  private static halfHourValidator: ValidatorFn = (ctrl) => {
    const val = ctrl.value as string;
    if (!val) return null;
    const minutes = new Date(val).getMinutes();
    return (minutes === 0 || minutes === 30) ? null : {halfHour: true};
  };

  form = this.fb.group({
    leaveType:  ['annual' as LeaveType, Validators.required],
    startDate:  ['', [Validators.required, LeaveRequestForm.halfHourValidator]],
    endDate:    ['', [Validators.required, LeaveRequestForm.halfHourValidator]],
    reason:     ['', Validators.required],
  });

  /** 從開始/結束時間自動計算時數 */
  get calculatedHours(): number {
    const start = this.form.get('startDate')?.value;
    const end = this.form.get('endDate')?.value;
    if (!start || !end) return 0;
    const diff = new Date(end).getTime() - new Date(start).getTime();
    if (diff <= 0) return 0;
    return Math.round(diff / (1000 * 60 * 30)) * 0.5; // 四捨五入至 0.5 小時
  }

  ngOnInit() {
    this.loadCompensatoryHours();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft';
        this.form.patchValue({
          leaveType:  r.leaveType,
          startDate:  this._toDatetimeLocal(r.startDate),
          endDate:    this._toDatetimeLocal(r.endDate),
          reason:     r.reason,
        });
        if (this.isReadOnly) this.form.disable();
      });
    }
  }

  /** 補休時數是否足夠 */
  get isCompensatoryExceeded(): boolean {
    if (this.form.get('leaveType')?.value !== 'compensatory') return false;
    const hours = this.compensatoryHours();
    if (!hours) return false;
    const requestedHours = this.calculatedHours;
    return requestedHours > hours.availableHours;
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.isReadOnly || this.calculatedHours <= 0) return;
    const payload = this._buildPayload();
    const obs = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/leave-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid || this.isReadOnly || this.calculatedHours <= 0) return;
    if (this.isCompensatoryExceeded) {
      const hours = this.compensatoryHours()!;
      this.errorMsg.set(`補休時數不足。申請 ${this.calculatedHours} 小時，可用 ${hours.availableHours} 小時。`);
      return;
    }
    const payload = this._buildPayload();
    const save$ = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => this.router.navigate(['/admin/leave-requests']),
          error: (err: HttpErrorResponse) => {
            this.errorMsg.set(err.error?.message || '送出失敗，請稍後再試。');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 載入可補休時數 */
  private loadCompensatoryHours() {
    this.compensatoryLoading.set(true);
    this.service.getCompensatoryHours().subscribe({
      next: data => {
        this.compensatoryHours.set(data);
        this.compensatoryLoading.set(false);
      },
      error: () => this.compensatoryLoading.set(false),
    });
  }

  private _buildPayload() {
    const v = this.form.value;
    return {
      leaveType: v.leaveType as LeaveType,
      startDate: v.startDate!,   // 直接送字串，避免 new Date() 轉 UTC
      endDate:   v.endDate!,
      hours:     this.calculatedHours,
      reason:    v.reason!,
    };
  }

  /** 將日期轉為 datetime-local 輸入格式 yyyy-MM-ddTHH:mm（台北時區） */
  private _toDatetimeLocal(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d.getTime())) return '';
    // 使用台北時區格式化，取得各欄位值
    const parts = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Asia/Taipei',
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', hour12: false,
    }).formatToParts(d);
    const get = (type: string) => parts.find(p => p.type === type)?.value ?? '00';
    return `${get('year')}-${get('month')}-${get('day')}T${get('hour')}:${get('minute')}`;
  }
}
