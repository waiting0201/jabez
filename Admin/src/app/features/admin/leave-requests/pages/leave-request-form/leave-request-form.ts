import {ChangeDetectorRef, Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {DatePipe, DecimalPipe} from '@angular/common';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveType, ApprovalStatus, AnnualQuota, CompensatoryHours, CeremonialQuota,
  MarriageQuota, MaternityStatus, BereavementQuota, SeniorExecutiveEligibility,
  SeniorExecutiveQuota, MenstrualQuota, DesignatedReviewer,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
  LEAVE_TYPE_GROUPS, LEAVE_TYPE_LABELS, LEAVE_TYPE_DAYS_LIMIT, LEAVE_TIME_UNIT,
  BEREAVEMENT_GROUPS, BEREAVEMENT_RELATIONSHIP_LABELS, BEREAVEMENT_DAYS,
  BereavementRelationship,
} from '../../models/leave-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';

type HalfDaySlot = 'am' | 'pm';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-leave-request-form',
  templateUrl: './leave-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, ApprovalTimeline, DatePipe, DecimalPipe, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class LeaveRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(LeaveRequestService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private auth        = inject(AuthService);
  private deptSvc     = inject(DepartmentService);
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

  /**
   * 載入既有資料期間的 reentry guard。
   * patchValue 與 form.disable() 都會同步觸發 child controls 的 valueChanges；
   * 若不擋下，leaveType 的 valueChanges → onLeaveTypeChange 會把剛回填的 startDate/endDate 清掉
   * （特別是 disable() 在 patch 之後重新觸發，導致檢視頁日期消失）。
   */
  private isLoadingExisting = false;

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
  departments: Department[] = [];
  /** 編輯回填給 picker 的 initial（含 approvalStepOrder / selectedDepartmentId） */
  pickerInitial: DesignatedReviewer[] = [];
  /** 唯讀模式下顯示的已指定審核者 */
  readonlyDesignatedReviewers: DesignatedReviewer[] = [];
  /** picker 每次 change 後存放最新 payload，送出時使用 */
  private _pickerPayload: DesignatedReviewerPayload[] = [];
  /** 被抑制（部門最高層級 → 自動略過）的指定步驟 stepOrder，驗證時排除 */
  private _suppressedSteps: number[] = [];

  /** 假別常數（供 template 使用） */
  readonly leaveTypeGroups = LEAVE_TYPE_GROUPS;
  readonly leaveTypeLabels = LEAVE_TYPE_LABELS;
  readonly leaveTypeDaysLimit = LEAVE_TYPE_DAYS_LIMIT;
  readonly leaveTimeUnit = LEAVE_TIME_UNIT;
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

  /** 歲時祭儀假額度（從 API 取得） */
  ceremonialQuota = signal<CeremonialQuota | null>(null);
  ceremonialLoading = signal(false);

  /** 婚假額度 */
  marriageQuota = signal<MarriageQuota | null>(null);

  /** 產假狀態 */
  maternityStatus = signal<MaternityStatus | null>(null);

  /** 喪假額度（依當前選擇的親屬關係） */
  bereavementQuota = signal<BereavementQuota | null>(null);

  /** 高階主管假適用性 */
  seniorExecEligibility = signal<SeniorExecutiveEligibility | null>(null);

  /** 高階主管假額度（每年 20 天，曆年歸零） */
  seniorExecQuota = signal<SeniorExecutiveQuota | null>(null);
  seniorExecQuotaLoading = signal(false);

  /** 生理假配額（限女性；亦用於下拉過濾） */
  menstrualQuota = signal<MenstrualQuota | null>(null);

  /**
   * 是否為協理以上（決定高階主管假選項是否顯示）
   * - Superadmin 一律通過
   * - 同時檢查 JWT `job_title_level` claim 與後端 `/leave-requests/senior-executive-eligibility` API，
   *   任一來源為真即顯示，避免舊 JWT 尚無 claim 時被誤過濾。
   */
  readonly isSeniorExecutive = computed<boolean>(() => {
    if (this.auth.isSuperAdmin()) return true;
    if (this.auth.isSeniorExecutive()) return true;
    return this.seniorExecEligibility()?.isEligible === true;
  });

  /** 整點小時選項（0 ~ 23） */
  readonly hourOptions: number[] = Array.from({length: 24}, (_, i) => i);

  form = this.fb.group({
    leaveType:               ['annual' as LeaveType, Validators.required],
    bereavementRelationship: ['' as string],
    // 日期（yyyy-MM-dd）：Hour / HalfDay / Day / Maternity 共用
    startDate:               [''],
    endDate:                 [''],
    // Hour 模式：整點小時下拉（0 ~ 23）
    startHour:               [9 as number],
    endHour:                 [18 as number],
    // HalfDay 模式：時段
    startSlot:               ['am' as HalfDaySlot],
    endSlot:                 ['pm' as HalfDaySlot],
    reason:                  ['', Validators.required],
  });

  /** 將整數小時轉為 HH:00 顯示 */
  formatHour(h: number): string {
    return `${String(h).padStart(2, '0')}:00`;
  }

  /** 當前選擇的假別 */
  get selectedLeaveType(): LeaveType {
    return this.form.get('leaveType')?.value as LeaveType || 'annual';
  }

  /** 當前假別的時間單位 */
  get selectedUnit(): 'hour' | 'half_day' | 'day' {
    return this.leaveTimeUnit[this.selectedLeaveType];
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

  /** 產假自動計算的結束日（start + 55 天） */
  get maternityEndDate(): string | null {
    if (this.selectedLeaveType !== 'maternity') return null;
    const start = this.form.get('startDate')?.value;
    if (!start) return null;
    const d = new Date(start);
    if (isNaN(d.getTime())) return null;
    d.setDate(d.getDate() + 55);
    return this._formatDate(d);
  }

  /** 根據當前表單狀態計算時數 */
  get calculatedHours(): number {
    const type = this.selectedLeaveType;
    const unit = this.selectedUnit;

    if (type === 'maternity') {
      return this.form.get('startDate')?.value ? 448 : 0;
    }

    if (unit === 'hour') {
      const sDate = this.form.get('startDate')?.value;
      const eDate = this.form.get('endDate')?.value;
      const sHour = this.form.get('startHour')?.value;
      const eHour = this.form.get('endHour')?.value;
      if (!sDate || !eDate || sHour === null || sHour === undefined || eHour === null || eHour === undefined) return 0;
      const startD = this._parseDate(sDate);
      const endD = this._parseDate(eDate);
      if (!startD || !endD) return 0;
      const startMs = startD.getTime() + sHour * 3600_000;
      const endMs = endD.getTime() + eHour * 3600_000;
      const diff = endMs - startMs;
      if (diff <= 0) return 0;
      return Math.round(diff / 3600_000);
    }

    if (unit === 'day') {
      const s = this.form.get('startDate')?.value;
      const e = this.form.get('endDate')?.value;
      if (!s || !e) return 0;
      const startD = this._parseDate(s);
      const endD = this._parseDate(e);
      if (!startD || !endD) return 0;
      const days = Math.floor((endD.getTime() - startD.getTime()) / (1000 * 60 * 60 * 24)) + 1;
      return days > 0 ? days * 8 : 0;
    }

    // half_day
    const s = this.form.get('startDate')?.value;
    const e = this.form.get('endDate')?.value;
    if (!s || !e) return 0;
    const startD = this._parseDate(s);
    const endD = this._parseDate(e);
    if (!startD || !endD) return 0;
    const startSlot = this.form.get('startSlot')?.value as HalfDaySlot;
    const endSlot = this.form.get('endSlot')?.value as HalfDaySlot;

    if (startD.getTime() === endD.getTime()) {
      if (startSlot === 'am' && endSlot === 'am') return 4;
      if (startSlot === 'am' && endSlot === 'pm') return 8;
      if (startSlot === 'pm' && endSlot === 'pm') return 4;
      return 0; // pm → am 單日無效
    }
    if (endD.getTime() < startD.getTime()) return 0;

    const startHrs = startSlot === 'am' ? 8 : 4;
    const endHrs = endSlot === 'pm' ? 8 : 4;
    const daysBetween = Math.floor((endD.getTime() - startD.getTime()) / (1000 * 60 * 60 * 24)) - 1;
    return startHrs + Math.max(0, daysBetween) * 8 + endHrs;
  }

  /** 時數顯示（依單位） */
  get calculatedDisplay(): string {
    const h = this.calculatedHours;
    if (h <= 0) return '—';
    const unit = this.selectedUnit;
    if (unit === 'hour') return `${h} 小時`;
    const days = Math.round((h / 8) * 10) / 10;
    return `${days} 天`;
  }

  /** 是否為「結束早於開始」的非法時間範圍（用於顯示錯誤訊息） */
  get isTimeRangeInvalid(): boolean {
    const type = this.selectedLeaveType;
    if (type === 'maternity') return false; // 產假由系統自動計算結束日

    const unit = this.selectedUnit;
    const sDate = this.form.get('startDate')?.value;
    const eDate = this.form.get('endDate')?.value;
    if (!sDate || !eDate) return false; // 尚未輸入完整
    const startD = this._parseDate(sDate);
    const endD = this._parseDate(eDate);
    if (!startD || !endD) return false;

    if (unit === 'hour') {
      const sHour = this.form.get('startHour')?.value ?? 0;
      const eHour = this.form.get('endHour')?.value ?? 0;
      return (endD.getTime() + eHour * 3600_000) <= (startD.getTime() + sHour * 3600_000);
    }
    if (unit === 'day') {
      return endD.getTime() < startD.getTime();
    }
    // half_day
    if (endD.getTime() < startD.getTime()) return true;
    if (endD.getTime() === startD.getTime()) {
      // 同日且 pm → am
      const s = this.form.get('startSlot')?.value as HalfDaySlot;
      const e = this.form.get('endSlot')?.value as HalfDaySlot;
      return s === 'pm' && e === 'am';
    }
    return false;
  }

  // ── 指定審核者 ───────────────────────────────────────

  /** picker change 事件：每次使用者操作時更新最新 payload */
  onPickerChange(payload: DesignatedReviewerPayload[]) {
    this._pickerPayload = payload;
  }

  /** picker 回報被抑制（部門最高層級 → 自動略過）的指定步驟 */
  onSuppressedSteps(stepOrders: number[]) {
    this._suppressedSteps = stepOrders;
  }

  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

  /** 取得簽核狀態標籤（安全索引） */
  getStatusLabel(status: string | undefined | null): string {
    if (!status) return '';
    return (APPROVAL_STATUS_LABELS as Record<string, string>)[status] ?? status;
  }

  /** 取得喪假關係標籤（安全索引） */
  getBereavementLabel(rel: string | undefined | null): string {
    if (!rel) return '';
    return (BEREAVEMENT_RELATIONSHIP_LABELS as Record<string, string>)[rel] ?? rel;
  }

  ngOnInit() {
    this.loadAnnualQuota();
    this.loadSeniorExecEligibility();
    // 預載歲時祭儀假額度以判斷使用者是否為原住民身份（用於下拉過濾）
    this.loadCeremonialQuota();
    // 預載生理假配額以判斷使用者是否為女性身份（用於下拉過濾）
    this.loadMenstrualQuota();

    // 監聽假別變化（載入既有資料期間跳過，避免 patch / disable 觸發的 valueChanges 把日期清掉）
    this.form.get('leaveType')?.valueChanges.subscribe(type => {
      if (this.isLoadingExisting) return;
      this.onLeaveTypeChange(type as LeaveType);
    });
    // 監聽喪假親屬關係變化（同樣跳過載入期間，由 applyLeaveTypeOnLoad 統一處理）
    this.form.get('bereavementRelationship')?.valueChanges.subscribe(rel => {
      if (this.isLoadingExisting) return;
      if (this.selectedLeaveType === 'bereavement' && rel) {
        this.loadBereavementQuota(rel);
      } else {
        this.bereavementQuota.set(null);
      }
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('leave').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); } });
        this.userSvc.getLookup().subscribe({ next: users => { this.allUsers = users; this.cdr.markForCheck(); } });
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().subscribe({ next: d => { this.departments = d; this.cdr.markForCheck(); } });
        }
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
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';

        this.isLoadingExisting = true;
        try {
          // 依單位回填對應欄位
          const unit = LEAVE_TIME_UNIT[r.leaveType];
          const baseValues = {
            leaveType:               r.leaveType,
            bereavementRelationship: r.bereavementRelationship ?? '',
            reason:                  r.reason,
          };
          if (unit === 'hour') {
            const startParts = this._splitDateHour(r.startDate);
            const endParts = this._splitDateHour(r.endDate);
            this.form.patchValue({
              ...baseValues,
              startDate: startParts.date,
              endDate:   endParts.date,
              startHour: startParts.hour,
              endHour:   endParts.hour,
            });
          } else if (r.leaveType === 'maternity') {
            this.form.patchValue({
              ...baseValues,
              startDate: this._toDateString(r.startDate),
            });
          } else if (unit === 'day') {
            this.form.patchValue({
              ...baseValues,
              startDate: this._toDateString(r.startDate),
              endDate:   this._toDateString(r.endDate),
            });
          } else {
            // half_day：從 hours 反推 slots
            const slots = this._inferHalfDaySlots(r.startDate, r.endDate, r.hours);
            this.form.patchValue({
              ...baseValues,
              startDate: this._toDateString(r.startDate),
              endDate:   this._toDateString(r.endDate),
              startSlot: slots.startSlot,
              endSlot:   slots.endSlot,
            });
          }

          // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
          if (r.designatedReviewers?.length) {
            this.pickerInitial = r.designatedReviewers;
            this.readonlyDesignatedReviewers = r.designatedReviewers;
          }
          if (this.isReadOnly) this.form.disable();
        } finally {
          this.isLoadingExisting = false;
        }

        // 取代被 guard 跳過的 valueChanges 副作用：手動套用驗證規則與配額載入
        this.applyLeaveTypeOnLoad(r.leaveType, r.bereavementRelationship);

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
    // 切換時清空共用日期欄位，避免前一模式的殘留值
    this.form.patchValue({startDate: '', endDate: ''}, {emitEvent: false});

    // 喪假：bereavementRelationship 必填
    if (type === 'bereavement') {
      this.form.get('bereavementRelationship')?.setValidators(Validators.required);
    } else {
      this.form.get('bereavementRelationship')?.clearValidators();
      this.form.get('bereavementRelationship')?.setValue('', {emitEvent: false});
      this.bereavementQuota.set(null);
    }
    this.form.get('bereavementRelationship')?.updateValueAndValidity({emitEvent: false});

    // 依假別載入對應配額
    if (type === 'annual') this.loadAnnualQuota();
    if (type === 'compensatory') this.loadCompensatoryHours();
    if (type === 'ceremonial_festival') this.loadCeremonialQuota();
    if (type === 'marriage') this.loadMarriageQuota();
    if (type === 'maternity') this.loadMaternityStatus();
    if (type === 'menstrual') this.loadMenstrualQuota();
    if (type === 'senior_executive') this.loadSeniorExecQuota();
  }

  /** 載入既有資料時手動套用 leaveType 對應的驗證規則與配額載入（取代被 guard 跳過的 valueChanges 副作用） */
  private applyLeaveTypeOnLoad(type: LeaveType, bereavementRel?: string | null) {
    if (type === 'bereavement') {
      this.form.get('bereavementRelationship')?.setValidators(Validators.required);
      this.form.get('bereavementRelationship')?.updateValueAndValidity({emitEvent: false});
      if (bereavementRel) this.loadBereavementQuota(bereavementRel);
    }
    if (type === 'annual') this.loadAnnualQuota();
    if (type === 'compensatory') this.loadCompensatoryHours();
    if (type === 'ceremonial_festival') this.loadCeremonialQuota();
    if (type === 'marriage') this.loadMarriageQuota();
    if (type === 'maternity') this.loadMaternityStatus();
    if (type === 'menstrual') this.loadMenstrualQuota();
    if (type === 'senior_executive') this.loadSeniorExecQuota();
  }

  /** 補休時數是否足夠 */
  get isCompensatoryExceeded(): boolean {
    if (this.selectedLeaveType !== 'compensatory') return false;
    const hours = this.compensatoryHours();
    if (!hours) return false;
    return this.calculatedHours > hours.availableHours;
  }

  /**
   * 申請人是否為原住民身份（用於下拉選單過濾歲時祭儀假選項）
   * - Superadmin 一律通過（可代任何員工建立）
   * - 否則依 ceremonialQuota.isIndigenous 判斷
   * - 尚未載入時預設 false（保守：先不顯示，載入後再揭露）
   */
  readonly isIndigenousUser = computed<boolean>(() => {
    if (this.auth.isSuperAdmin()) return true;
    return this.ceremonialQuota()?.isIndigenous === true;
  });

  /** 歲時祭儀假：申請人非原住民則不可申請 */
  get isCeremonialNotAllowed(): boolean {
    if (this.selectedLeaveType !== 'ceremonial_festival') return false;
    const q = this.ceremonialQuota();
    return q !== null && !q.isIndigenous;
  }

  /**
   * 申請人是否為女性（用於下拉選單過濾生理假選項）
   * - Superadmin 一律通過（可代任何員工建立）
   * - 否則依 menstrualQuota.isFemale 判斷
   * - 尚未載入時預設 false（保守：先不顯示，載入後再揭露）
   */
  readonly isFemaleEmployee = computed<boolean>(() => {
    if (this.auth.isSuperAdmin()) return true;
    return this.menstrualQuota()?.isFemale === true;
  });

  /** 生理假：申請人非女性則不可申請 */
  get isMenstrualNotAllowed(): boolean {
    if (this.selectedLeaveType !== 'menstrual') return false;
    const q = this.menstrualQuota();
    return q !== null && !q.isFemale;
  }

  /** 生理假：是否超過當月上限（1 天）或全年上限（12 天） */
  get isMenstrualExceeded(): boolean {
    if (this.selectedLeaveType !== 'menstrual') return false;
    const q = this.menstrualQuota();
    if (!q || !q.isFemale) return false;
    const requestDays = this.calculatedHours / 8;
    return requestDays > q.monthlyAvailableDays || requestDays > q.annualAvailableDays;
  }

  /** 產假：已有活躍申請則不可再送 */
  get isMaternityBlocked(): boolean {
    if (this.selectedLeaveType !== 'maternity') return false;
    const status = this.maternityStatus();
    if (!status?.hasActiveRequest) return false;
    // 編輯自己的活躍產假不阻擋
    return !this.isEdit || status.activeRequestId !== this.requestId;
  }

  /** 高階主管假：非協理以上不可申請 */
  get isSeniorExecBlocked(): boolean {
    return this.selectedLeaveType === 'senior_executive' && !this.isSeniorExecutive();
  }

  /** 高階主管假：是否超過每年 20 天額度 */
  get isSeniorExecExceeded(): boolean {
    if (this.selectedLeaveType !== 'senior_executive') return false;
    const q = this.seniorExecQuota();
    if (!q || !q.isEligible) return false;
    const requestDays = this.calculatedHours / 8;
    return requestDays > q.availableDays;
  }

  /** 婚假：是否已超過上限 */
  get isMarriageExceeded(): boolean {
    if (this.selectedLeaveType !== 'marriage') return false;
    const q = this.marriageQuota();
    if (!q) return false;
    const requestDays = this.calculatedHours / 8;
    return requestDays > q.remainingDays;
  }

  /** 喪假：是否已超過親屬上限 */
  get isBereavementExceeded(): boolean {
    if (this.selectedLeaveType !== 'bereavement') return false;
    const q = this.bereavementQuota();
    if (!q) return false;
    const requestDays = this.calculatedHours / 8;
    return requestDays > q.remainingDays;
  }

  /** 表單整體是否可送出 */
  get canSubmit(): boolean {
    if (this.form.invalid || this.isReadOnly) return false;
    if (this.isTimeRangeInvalid) return false;
    if (this.calculatedHours <= 0) return false;
    if (this.isCompensatoryExceeded) return false;
    if (this.isCeremonialNotAllowed) return false;
    if (this.isMaternityBlocked) return false;
    if (this.isSeniorExecBlocked) return false;
    if (this.isSeniorExecExceeded) return false;
    if (this.isMarriageExceeded) return false;
    if (this.isBereavementExceeded) return false;
    if (this.isMenstrualNotAllowed) return false;
    if (this.isMenstrualExceeded) return false;
    return true;
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.calculatedHours <= 0 || this.form.invalid || this.isReadOnly) return;
    // 後端 CreateAsync / UpdateAsync 已擋；前端保險擋一次避免空跑
    if (this.isCeremonialNotAllowed) {
      this.errorMsg.set('僅原住民身份之員工可申請歲時祭儀假。');
      return;
    }
    if (this.isMenstrualNotAllowed) {
      this.errorMsg.set('僅女性員工可申請生理假。');
      return;
    }
    const payload = this._buildPayload();
    if (!payload) return;
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
    if (!this.canSubmit) return;
    if (this.isCompensatoryExceeded) {
      const hours = this.compensatoryHours()!;
      this.errorMsg.set(`補休時數不足。申請 ${this.calculatedHours} 小時，可用 ${hours.availableHours} 小時。`);
      return;
    }
    if (this.isCeremonialNotAllowed) {
      this.errorMsg.set('僅原住民身份之員工可申請歲時祭儀假。');
      return;
    }
    if (this.isMaternityBlocked) {
      this.errorMsg.set('已有未完成或進行中的產假申請，產假需一次請完。');
      return;
    }
    if (this.isSeniorExecBlocked) {
      this.errorMsg.set('高階主管假僅限協理（含）以上職級申請。');
      return;
    }
    if (this.isSeniorExecExceeded) {
      const q = this.seniorExecQuota()!;
      this.errorMsg.set(`高階主管假額度不足。每年上限 ${q.totalDays} 天，已使用 ${q.usedDays} 天，可用 ${q.availableDays} 天。`);
      return;
    }
    if (this.isMenstrualNotAllowed) {
      this.errorMsg.set('僅女性員工可申請生理假。');
      return;
    }
    if (this.isMenstrualExceeded) {
      this.errorMsg.set('生理假超過上限（每月 1 天、全年 12 天）。');
      return;
    }
    // 流程含「申請人指定審核」步驟時，每個 designated step 至少需要 1 位指定審核者（被抑制者除外）
    if (this.hasDesignatedStep) {
      for (const step of this.designatedSteps) {
        if (this._suppressedSteps.includes(step.stepOrder)) continue;
        const hasForStep = this._pickerPayload.some(p => p.approvalStepOrder === step.stepOrder);
        if (!hasForStep) {
          this.errorMsg.set(`此簽核流程的步驟 ${step.stepOrder} 包含申請人指定審核，請新增至少 1 位審核者。`);
          return;
        }
      }
    }
    const payload = this._buildPayload();
    if (!payload) return;
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

  // ── Quota loaders ────────────────────────────────────

  private loadCompensatoryHours() {
    this.compensatoryLoading.set(true);
    this.service.getCompensatoryHours().subscribe({
      next: data => { this.compensatoryHours.set(data); this.compensatoryLoading.set(false); },
      error: () => this.compensatoryLoading.set(false),
    });
  }

  private loadAnnualQuota() {
    this.annualQuotaLoading.set(true);
    this.service.getAnnualQuota().subscribe({
      next: data => { this.annualQuota.set(data); this.annualQuotaLoading.set(false); },
      error: () => this.annualQuotaLoading.set(false),
    });
  }

  private loadCeremonialQuota() {
    this.ceremonialLoading.set(true);
    this.service.getCeremonialQuota().subscribe({
      next: data => { this.ceremonialQuota.set(data); this.ceremonialLoading.set(false); },
      error: () => this.ceremonialLoading.set(false),
    });
  }

  private loadMarriageQuota() {
    this.service.getMarriageQuota().subscribe({
      next: data => this.marriageQuota.set(data),
    });
  }

  private loadMaternityStatus() {
    this.service.getMaternityStatus().subscribe({
      next: data => this.maternityStatus.set(data),
    });
  }

  private loadSeniorExecQuota() {
    this.seniorExecQuotaLoading.set(true);
    this.service.getSeniorExecutiveQuota().subscribe({
      next: data => { this.seniorExecQuota.set(data); this.seniorExecQuotaLoading.set(false); },
      error: () => this.seniorExecQuotaLoading.set(false),
    });
  }

  private loadBereavementQuota(relationship: string) {
    this.service.getBereavementQuota(relationship).subscribe({
      next: data => this.bereavementQuota.set(data),
    });
  }

  private loadSeniorExecEligibility() {
    this.service.getSeniorExecutiveEligibility().subscribe({
      next: data => this.seniorExecEligibility.set(data),
    });
  }

  private loadMenstrualQuota() {
    this.service.getMenstrualQuota().subscribe({
      next: data => this.menstrualQuota.set(data),
    });
  }

  // ── Payload builder ──────────────────────────────────

  private _buildPayload() {
    const v = this.form.value;
    const type = v.leaveType as LeaveType;
    const unit = LEAVE_TIME_UNIT[type];
    const reviewers = this._pickerPayload;

    let startDateStr = '';
    let endDateStr = '';
    let hours = this.calculatedHours;

    if (type === 'maternity') {
      // 產假：只送起始日，後端自動填充 56 天
      const s = v.startDate!;
      startDateStr = `${s}T00:00:00`;
      // 前端先計算 end 方便顯示，後端會覆寫
      const endD = this._parseDate(s)!;
      endD.setDate(endD.getDate() + 55);
      endDateStr = `${this._formatDate(endD)}T00:00:00`;
      hours = 448;
    } else if (unit === 'hour') {
      const sh = String(v.startHour ?? 0).padStart(2, '0');
      const eh = String(v.endHour ?? 0).padStart(2, '0');
      startDateStr = `${v.startDate}T${sh}:00:00`;
      endDateStr = `${v.endDate}T${eh}:00:00`;
    } else if (unit === 'day') {
      startDateStr = `${v.startDate}T00:00:00`;
      endDateStr = `${v.endDate}T23:59:00`;
    } else {
      // half_day：將 slot 轉為代表性時間
      const startHour = v.startSlot === 'am' ? '08:00:00' : '13:00:00';
      const endHour = v.endSlot === 'am' ? '12:00:00' : '17:00:00';
      startDateStr = `${v.startDate}T${startHour}`;
      endDateStr = `${v.endDate}T${endHour}`;
    }

    return {
      leaveType:               type,
      bereavementRelationship: type === 'bereavement' ? v.bereavementRelationship || undefined : undefined,
      startDate:               startDateStr,
      endDate:                 endDateStr,
      hours,
      reason:                  v.reason!,
      designatedReviewers:     reviewers.length > 0 ? reviewers : undefined,
    };
  }

  // ── Date helpers ─────────────────────────────────────

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

  /** 將日期轉為 yyyy-MM-dd（台北時區） */
  private _toDateString(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d.getTime())) return '';
    const parts = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Asia/Taipei',
      year: 'numeric', month: '2-digit', day: '2-digit',
    }).formatToParts(d);
    const get = (type: string) => parts.find(p => p.type === type)?.value ?? '00';
    return `${get('year')}-${get('month')}-${get('day')}`;
  }

  /** yyyy-MM-dd → 當天 00:00 Date（本地時區） */
  private _parseDate(s: string): Date | null {
    if (!s) return null;
    const m = s.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (!m) return null;
    return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
  }

  /** Date → yyyy-MM-dd */
  private _formatDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${dd}`;
  }

  /** 將既有記錄的 date / datetime 拆成 (yyyy-MM-dd, hour) 供 Hour 模式回填 */
  private _splitDateHour(date: Date | string): {date: string; hour: number} {
    const d = date instanceof Date ? date : new Date(date);
    if (isNaN(d.getTime())) return {date: '', hour: 0};
    const parts = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Asia/Taipei',
      year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', hour12: false,
    }).formatToParts(d);
    const get = (type: string) => parts.find(p => p.type === type)?.value ?? '00';
    return {
      date: `${get('year')}-${get('month')}-${get('day')}`,
      hour: Number(get('hour')),
    };
  }

  /** 根據後端存的 startDate / endDate / hours 反推半天模式的 slots */
  private _inferHalfDaySlots(startDate: Date | string, endDate: Date | string, hours: number)
    : {startSlot: HalfDaySlot; endSlot: HalfDaySlot} {
    const startHour = new Date(startDate).getHours();
    const endHour = new Date(endDate).getHours();
    const startSlot: HalfDaySlot = startHour >= 12 ? 'pm' : 'am';
    const endSlot: HalfDaySlot = endHour < 13 ? 'am' : 'pm';
    return {startSlot, endSlot};
  }
}
