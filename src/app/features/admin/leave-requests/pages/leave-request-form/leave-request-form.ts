import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveType,
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
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'pending' && r.approvalStatus !== 'returned';
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

  submit() {
    if (this.form.invalid || this.isReadOnly) return;
    const v = this.form.value;
    const payload = {
      leaveType:  v.leaveType as LeaveType,
      startDate:  new Date(v.startDate!),
      endDate:    new Date(v.endDate!),
      days:       +v.days!,
      reason:     v.reason!,
    };
    const obs = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(['/admin/leave-requests']));
  }
}
