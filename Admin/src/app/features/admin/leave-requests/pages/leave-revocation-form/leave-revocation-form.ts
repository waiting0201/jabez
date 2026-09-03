import {ChangeDetectorRef, Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {DatePipe} from '@angular/common';
import {LeaveRevocationService} from '../../services/leave-revocation.service';
import {LeaveRevocation, LeaveRevocationDate, RevocableDatesResult} from '../../models/leave-revocation.model';
import {
  ApprovalStatus, DesignatedReviewer, LeaveType,
  APPROVAL_STATUS_CLASSES, APPROVAL_STATUS_LABELS,
  LEAVE_TYPE_CLASSES, LEAVE_TYPE_LABELS, formatLeaveDuration,
} from '../../models/leave-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, PendingReviewer} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';

/** 逐日勾選 chip */
interface DayChip {
  date: string;    // yyyy-MM-dd
  hours: number;
  label: string;   // MM/dd (三)
}

const WEEKDAYS = ['日', '一', '二', '三', '四', '五', '六'];

/**
 * 銷假申請表單。
 * 三種模式由路由決定：
 *   /admin/leave-requests/:id/revoke   → 新增（:id 為原請假單）
 *   /admin/leave-revocations/:id/edit  → 編輯草稿 / 退回件
 *   /admin/leave-revocations/:id       → 唯讀檢視（data.mode === 'view'）
 *
 * 不複用 leave-request-form：該元件的欄位集合（8 種假別配額 / 時間單位切換）與銷假完全不同，
 * 銷假只需「原假單唯讀對照 + 逐日勾選 + 原因 + 指定審核者」。
 */
@Component({
  selector: 'app-leave-revocation-form',
  templateUrl: './leave-revocation-form.html',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, ApprovalTimeline, DesignatedReviewersPicker],
})
export class LeaveRevocationForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(LeaveRevocationService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private deptSvc     = inject(DepartmentService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);

  isEdit     = false;
  isReadOnly = false;
  isReturned = false;
  revocationId = 0;
  leaveRequestId = 0;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');
  saving   = signal(false);

  /** 原請假單摘要（新增模式來自 revocable-dates，編輯 / 檢視模式來自銷假單） */
  leaveInfo = signal<{leaveType: LeaveType; startDate: string; endDate: string; hours: number; reason: string} | null>(null);

  /** 可勾選的日子（唯讀模式為已勾選的日子） */
  dayChips = signal<DayChip[]>([]);
  selected = signal<Set<string>>(new Set());

  selectedDays  = computed(() => this.dayChips().filter(c => this.selected().has(c.date)));
  selectedHours = computed(() => this.selectedDays().reduce((s, c) => s + c.hours, 0));
  allSelected   = computed(() => this.dayChips().length > 0 && this.selectedDays().length === this.dayChips().length);

  form = this.fb.group({
    reason: ['', [Validators.required, Validators.maxLength(500)]],
  });

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 1;
  taskStatus = '';
  /** 目前關卡的待簽核者（後端解析；空陣列＝查無可簽核人員）*/
  pendingReviewers: PendingReviewer[] = [];

  /** 指定審核者 */
  hasDesignatedStep = false;
  designatedSteps: ApprovalFlowStepSummary[] = [];
  allUsers: UserLookup[] = [];
  jobTitles: JobTitleLookup[] = [];
  departments: Department[] = [];
  pickerInitial: DesignatedReviewer[] = [];
  readonlyDesignatedReviewers: DesignatedReviewer[] = [];
  private _pickerPayload: DesignatedReviewerPayload[] = [];
  private _suppressedSteps: number[] = [];

  readonly typeLabel   = LEAVE_TYPE_LABELS;
  readonly typeClass   = LEAVE_TYPE_CLASSES;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  ngOnInit() {
    const mode = this.route.snapshot.data['mode'] as string | undefined;
    const id   = Number(this.route.snapshot.paramMap.get('id'));

    if (mode === 'new') {
      this.leaveRequestId = id;
      this.loadRevocableDates();
    } else {
      this.isEdit = true;
      this.revocationId = id;
      this.isReadOnly = mode === 'view';
      this.loadRevocation();
    }

    // 銷假沿用請假的簽核流程設定（後端 ResolveApprovalItemIdAsync 亦傳 "leave"）
    this.approvalSvc.getActiveByType('leave').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;
      if (this.hasDesignatedStep) {
        this.userSvc.getLookup().subscribe({next: users => { this.allUsers = users; this.cdr.markForCheck(); }});
        this.jobTitleSvc.getLookup().subscribe({next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); }});
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().subscribe({next: d => { this.departments = d; this.cdr.markForCheck(); }});
        }
      }
      this.cdr.markForCheck();
    });
  }

  // ── Load ─────────────────────────────────────────────

  private loadRevocableDates(preselect?: LeaveRevocationDate[]) {
    this.service.getRevocableDates(this.leaveRequestId, this.revocationId || undefined).subscribe({
      next: (r: RevocableDatesResult) => {
        this.leaveInfo.set({
          leaveType: r.leaveType, startDate: r.startDate, endDate: r.endDate,
          hours: r.hours, reason: r.reason,
        });
        this.dayChips.set(r.dates.map(d => this.toChip(d)));
        if (preselect?.length) {
          this.selected.set(new Set(preselect.map(d => this.dateKey(d.date))));
        }
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '無法載入可銷假日期。'),
    });
  }

  private loadRevocation() {
    this.service.getById(this.revocationId).subscribe({
      next: (r: LeaveRevocation) => {
        this.approvalStatus = r.approvalStatus;
        this.leaveRequestId = r.leaveRequestId;
        this.isReturned = r.approvalStatus === 'returned';
        // 非 draft / returned 一律唯讀（檢視模式亦然）
        if (r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned') this.isReadOnly = true;

        this.form.patchValue({reason: r.reason});
        if (this.isReadOnly) {
          this.form.disable();
          this.leaveInfo.set({
            leaveType: r.leaveType!, startDate: r.leaveStartDate!, endDate: r.leaveEndDate!,
            hours: r.leaveOriginalHours ?? r.leaveHours ?? 0, reason: '',
          });
          this.dayChips.set((r.dates ?? []).map(d => this.toChip(d)));
          this.selected.set(new Set((r.dates ?? []).map(d => this.dateKey(d.date))));
        } else {
          // 可編輯：重新取可銷清單（排除自己），並回填已勾選的日子
          this.loadRevocableDates(r.dates);
        }

        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }

        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.revocationId, 'leave_revocation').subscribe({
            next: task => {
              this.approvalFlow = task.flow ?? null;
              this.approvalRecords = task.approvalRecords ?? [];
              this.taskCurrentStepOrder = task.currentStepOrder;
              this.taskStatus = task.status;
              this.pendingReviewers = task.currentStepReviewers ?? [];
              this.cdr.markForCheck();
            },
          });
        }
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '無法載入銷假申請。'),
    });
  }

  // ── 逐日勾選 ─────────────────────────────────────────

  private dateKey(iso: string): string { return iso.slice(0, 10); }

  private toChip(d: LeaveRevocationDate): DayChip {
    const key = this.dateKey(d.date);
    const dt  = new Date(`${key}T00:00:00`);
    return {date: key, hours: d.hours, label: `${key.slice(5).replace('-', '/')} (${WEEKDAYS[dt.getDay()]})`};
  }

  isSelected(date: string): boolean { return this.selected().has(date); }

  toggleDate(date: string) {
    if (this.isReadOnly) return;
    this.selected.update(s => {
      const next = new Set(s);
      if (next.has(date)) next.delete(date); else next.add(date);
      return next;
    });
  }

  toggleAll() {
    if (this.isReadOnly) return;
    this.selected.set(this.allSelected() ? new Set() : new Set(this.dayChips().map(c => c.date)));
  }

  // ── 指定審核者 ───────────────────────────────────────

  onPickerChange(payload: DesignatedReviewerPayload[]) { this._pickerPayload = payload; }
  onSuppressedSteps(steps: number[]) { this._suppressedSteps = steps; }
  getUserName(id: string): string { return this.allUsers.find(u => u.id === id)?.name ?? id; }

  // ── Save / Submit ────────────────────────────────────

  get canSave(): boolean {
    return !this.isReadOnly && !this.saving() && this.form.valid && this.selectedDays().length > 0;
  }

  formatDuration(leaveType: LeaveType | undefined, hours: number): string {
    return leaveType ? formatLeaveDuration(leaveType, hours) : `${hours} 小時`;
  }

  private buildPayload() {
    return {
      dates: this.selectedDays().map(c => c.date),
      reason: this.form.value.reason!,
      designatedReviewers: this._pickerPayload.length > 0 ? this._pickerPayload : undefined,
    };
  }

  /**
   * 表單內按 Enter 不送出（textarea 換行不受影響）。
   * 否則任一 input 的 Enter 都會觸發 ngSubmit，直接建草稿並跳回列表。
   */
  onEnterKey(event: Event) {
    const tag = (event.target as HTMLElement)?.tagName;
    if (tag !== 'TEXTAREA') event.preventDefault();
  }

  /** 儲存草稿（新增或編輯退回件） */
  save() {
    if (!this.canSave) return;
    this.errorMsg.set('');
    this.saving.set(true);
    const payload = this.buildPayload();
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const save$ = this.revocationId
      ? this.service.update(this.revocationId, payload)
      : this.service.create(this.leaveRequestId, payload);
    save$.subscribe({
      next: () => this.router.navigate(['/admin/leave-requests']),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出銷假申請（儲存後送簽，重跑原本的請假簽核流程） */
  submitForApproval() {
    if (!this.canSave) return;
    // 流程含「申請人指定審核」步驟時，每個 designated step 至少需要 1 位指定審核者（被抑制者除外）
    if (this.hasDesignatedStep) {
      for (const step of this.designatedSteps) {
        if (this._suppressedSteps.includes(step.stepOrder)) continue;
        if (!this._pickerPayload.some(p => p.approvalStepOrder === step.stepOrder)) {
          this.errorMsg.set(`此簽核流程的步驟 ${step.stepOrder} 包含申請人指定審核，請新增至少 1 位審核者。`);
          return;
        }
      }
    }
    const days = this.selectedDays().length;
    if (!confirm(`確定要銷假 ${days} 天（共 ${this.selectedHours()} 小時）嗎？送出後將重新進入簽核流程。`)) return;

    this.errorMsg.set('');
    this.saving.set(true);
    const payload = this.buildPayload();
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const save$ = this.revocationId
      ? this.service.update(this.revocationId, payload)
      : this.service.create(this.leaveRequestId, payload);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update，避免同一次銷假被建成兩張單
        this.revocationId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => this.router.navigate(['/admin/leave-requests']),
          error: (err: HttpErrorResponse) => {
            this.saving.set(false);
            this.errorMsg.set(
              (err.error?.message || '送出失敗，請稍後再試。') + '（草稿已保留，修正後可直接再送出）');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
