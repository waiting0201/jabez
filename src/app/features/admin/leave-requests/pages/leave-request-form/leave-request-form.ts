import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, FormsModule, ReactiveFormsModule, ValidatorFn, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveType, ApprovalStatus, AnnualQuota, CompensatoryHours,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  LEAVE_TYPE_GROUPS, LEAVE_TYPE_LABELS, LEAVE_TYPE_DAYS_LIMIT,
  BEREAVEMENT_GROUPS, BEREAVEMENT_RELATIONSHIP_LABELS, BEREAVEMENT_DAYS,
  BereavementRelationship,
  DesignatedReviewer,
} from '../../models/leave-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';

@Component({
  selector: 'app-leave-request-form',
  templateUrl: './leave-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, ApprovalTimeline],
})
export class LeaveRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(LeaveRequestService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];

  /** 指定審核者條目清單（多人） */
  designatedEntries: {
    stepOrder: number;
    selectedJobTitleId: number | null;
    selectedUserId: string | null;
    filteredUsers: UserLookup[];
  }[] = [];

  /** 假別常數（供 template 使用） */
  readonly leaveTypeGroups = LEAVE_TYPE_GROUPS;
  readonly leaveTypeLabels = LEAVE_TYPE_LABELS;
  readonly leaveTypeDaysLimit = LEAVE_TYPE_DAYS_LIMIT;
  readonly bereavementGroups = BEREAVEMENT_GROUPS;
  readonly bereavementLabels = BEREAVEMENT_RELATIONSHIP_LABELS;
  readonly bereavementDays = BEREAVEMENT_DAYS;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  /** 可補休時數（從 API 取得） */
  compensatoryHours = signal<CompensatoryHours | null>(null);
  compensatoryLoading = signal(false);

  /** 年假額度（從 API 取得） */
  annualQuota = signal<AnnualQuota | null>(null);
  annualQuotaLoading = signal(false);

  addDesignatedEntry() {
    const nextOrder = this.designatedEntries.length + 1;
    this.designatedEntries.push({
      stepOrder: nextOrder,
      selectedJobTitleId: null,
      selectedUserId: null,
      filteredUsers: [],
    });
  }

  removeDesignatedEntry(i: number) {
    this.designatedEntries.splice(i, 1);
    this.designatedEntries.forEach((e, idx) => e.stepOrder = idx + 1);
  }

  onEntryJobTitleChange(i: number) {
    const e = this.designatedEntries[i];
    e.filteredUsers = e.selectedJobTitleId
      ? this.allUsers.filter(u => u.jobTitleId === e.selectedJobTitleId && u.status === 'active')
      : [];
    e.selectedUserId = null;
  }

  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

  /** 驗證分鐘必須為 00 或 30 */
  private static halfHourValidator: ValidatorFn = (ctrl) => {
    const val = ctrl.value as string;
    if (!val) return null;
    const minutes = new Date(val).getMinutes();
    return (minutes === 0 || minutes === 30) ? null : {halfHour: true};
  };

  form = this.fb.group({
    leaveType:               ['annual' as LeaveType, Validators.required],
    bereavementRelationship: ['' as string],
    startDate:               ['', [Validators.required, LeaveRequestForm.halfHourValidator]],
    endDate:                 ['', [Validators.required, LeaveRequestForm.halfHourValidator]],
    reason:                  ['', Validators.required],
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

  /** 當前選擇的假別 */
  get selectedLeaveType(): LeaveType {
    return this.form.get('leaveType')?.value as LeaveType || 'annual';
  }

  /** 當前選擇的喪假關係 */
  get selectedBereavementRelationship(): BereavementRelationship | null {
    const v = this.form.get('bereavementRelationship')?.value;
    return v ? v as BereavementRelationship : null;
  }

  /** 當前假別的天數上限 */
  get currentDaysLimit(): number | null {
    const type = this.selectedLeaveType;
    if (type === 'bereavement') {
      const rel = this.selectedBereavementRelationship;
      return rel ? this.bereavementDays[rel] ?? null : null;
    }
    return this.leaveTypeDaysLimit[type] ?? null;
  }

  ngOnInit() {
    this.loadCompensatoryHours();
    this.loadAnnualQuota();

    // 監聽假別變化
    this.form.get('leaveType')?.valueChanges.subscribe(type => {
      this.onLeaveTypeChange(type as LeaveType);
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟
    this.approvalSvc.getAll().subscribe(items => {
      this.hasDesignatedStep = items
        .filter(i => i.isActive && i.applicationType === 'leave')
        .some(i => i.steps.some(s => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; } });
        this.userSvc.getLookup().subscribe({
          next: users => {
            this.allUsers = users;
            this.designatedEntries.forEach(e => {
              if (e.selectedJobTitleId) {
                e.filteredUsers = users.filter(u => u.jobTitleId === e.selectedJobTitleId && u.status === 'active');
              }
            });
            this.cdr.markForCheck();
          },
        });
      }
      this.cdr.markForCheck();
    });

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
          leaveType:               r.leaveType,
          bereavementRelationship: r.bereavementRelationship ?? '',
          startDate:               this._toDatetimeLocal(r.startDate),
          endDate:                 this._toDatetimeLocal(r.endDate),
          reason:                  r.reason,
        });
        // 回填指定審核者清單
        if (r.designatedReviewers?.length) {
          this.designatedEntries = r.designatedReviewers.map(dr => ({
            stepOrder: dr.stepOrder,
            selectedJobTitleId: this.allUsers.find(u => u.id === dr.reviewerId)?.jobTitleId ?? null,
            selectedUserId: dr.reviewerId,
            filteredUsers: [],
          }));
          if (this.allUsers.length > 0) {
            this.designatedEntries.forEach(e => {
              if (e.selectedJobTitleId) {
                e.filteredUsers = this.allUsers.filter(u => u.jobTitleId === e.selectedJobTitleId && u.status === 'active');
              }
            });
          }
        }
        if (this.isReadOnly) this.form.disable();
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'leave').subscribe({
            next: task => {
              this.approvalFlow = task.flow ?? null;
              this.approvalRecords = task.approvalRecords ?? [];
              this.taskCurrentStepOrder = task.currentStepOrder;
              this.taskStatus = task.status;
              this.cdr.markForCheck();
            },
          });
        }
      });
    }
  }

  /** 假別變化時的處理 */
  private onLeaveTypeChange(type: LeaveType) {
    // 喪假：bereavementRelationship 必填
    if (type === 'bereavement') {
      this.form.get('bereavementRelationship')?.setValidators(Validators.required);
    } else {
      this.form.get('bereavementRelationship')?.clearValidators();
      this.form.get('bereavementRelationship')?.setValue('');
    }
    this.form.get('bereavementRelationship')?.updateValueAndValidity();

    // 年假：載入額度
    if (type === 'annual') this.loadAnnualQuota();
    // 補休：載入可用時數
    if (type === 'compensatory') this.loadCompensatoryHours();
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

  /** 載入年假額度 */
  private loadAnnualQuota() {
    this.annualQuotaLoading.set(true);
    this.service.getAnnualQuota().subscribe({
      next: data => {
        this.annualQuota.set(data);
        this.annualQuotaLoading.set(false);
      },
      error: () => this.annualQuotaLoading.set(false),
    });
  }

  private _buildPayload() {
    const v = this.form.value;
    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({ reviewerId: e.selectedUserId!, stepOrder: e.stepOrder }));
    return {
      leaveType:               v.leaveType as LeaveType,
      bereavementRelationship: v.leaveType === 'bereavement' ? v.bereavementRelationship || undefined : undefined,
      startDate:               v.startDate!,
      endDate:                 v.endDate!,
      hours:                   this.calculatedHours,
      reason:                  v.reason!,
      designatedReviewers:     reviewers.length > 0 ? reviewers : undefined,
    };
  }

  /** 將日期轉為 datetime-local 輸入格式 yyyy-MM-ddTHH:mm（台北時區） */
  private _toDatetimeLocal(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d.getTime())) return '';
    const parts = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Asia/Taipei',
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', hour12: false,
    }).formatToParts(d);
    const get = (type: string) => parts.find(p => p.type === type)?.value ?? '00';
    return `${get('year')}-${get('month')}-${get('day')}T${get('hour')}:${get('minute')}`;
  }
}
