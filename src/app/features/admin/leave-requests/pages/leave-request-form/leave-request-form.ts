import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
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

  // TODO: 後端就緒後改從 API 取得當前使用者的可補休時數
  readonly overtimeHours = 16;

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    leaveType:  ['annual' as LeaveType, Validators.required],
    startDate:  ['', Validators.required],
    endDate:    ['', Validators.required],
    days:       [1, [Validators.required, Validators.min(0.5)]],
    reason:     ['', Validators.required],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = !['draft', 'pending', 'returned'].includes(r.approvalStatus);
        this.form.patchValue({
          leaveType:  r.leaveType,
          startDate:  r.startDate instanceof Date
            ? r.startDate.toISOString().split('T')[0]
            : String(r.startDate),
          endDate: r.endDate instanceof Date
            ? r.endDate.toISOString().split('T')[0]
            : String(r.endDate),
          days:       r.days,
          reason:     r.reason,
        });
        if (this.isReadOnly) this.form.disable();
      });
    }
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.isReadOnly) return;
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
    if (this.form.invalid || this.isReadOnly) return;
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

  private _buildPayload() {
    const v = this.form.value;
    return {
      leaveType: v.leaveType as LeaveType,
      startDate: new Date(v.startDate!),
      endDate:   new Date(v.endDate!),
      days:      +v.days!,
      reason:    v.reason!,
    };
  }
}
