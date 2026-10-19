import {ChangeDetectorRef, Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {of} from 'rxjs';
import {catchError, debounceTime, distinctUntilChanged, map, switchMap} from 'rxjs/operators';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {OvertimeRequestService} from '../../services/overtime-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, COMPENSATION_TYPE_LABELS, DesignatedReviewer, OvertimeCompensationType, OvertimePayEstimate, OvertimeProject, OvertimeRequestKind, OvertimeRequestPayload, PostHocContext} from '../../models/overtime-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, StepReviewers} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';
import {ProjectTypeahead} from '@shared/components/project-typeahead';
import {MAX_REQUEST_DATE, MIN_REQUEST_DATE} from '@shared/utils/date-bounds';
import {ShiftDayType} from '../../../shift-schedules/models/shift-schedule.model';

@Component({
  selector: 'app-overtime-request-form',
  templateUrl: './overtime-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DatePipe, DecimalPipe, ApprovalTimeline, DesignatedReviewersPicker, ScrollIntoViewDirective, ProjectTypeahead],
})
export class OvertimeRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(OvertimeRequestService);
  private projects$   = inject(ProjectService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private deptSvc     = inject(DepartmentService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);
  private destroyRef  = inject(DestroyRef);

  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  /** 後端已存在的申請單 ID（編輯模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  /** 載入的單是否為事後申請（唯讀顯示 badge 用；新增時以表單 requestKind 為準） */
  isPostHoc = false;
  requestNo = '';   // OT-yyyyMMdd-NNN；草稿尚未取號故為空
  /**
   * 台北時區今天（yyyy-MM-dd）與事後申請可選的最早日期。事後申請期限＝加班日後一個工作日（遇六日 / 國定假日順延），
   * 前端不知道行事曆，故日期選擇器只放寬到 14 天前（涵蓋春節等長假），實際可否申請由 post-hoc-context 判定並顯示原因。
   */
  readonly todayStr = OvertimeRequestForm.taipeiDate(0);
  readonly postHocMinStr = OvertimeRequestForm.taipeiDate(-14);
  private static taipeiDate(offsetDays: number): string {
    const d = new Date(Date.now() + offsetDays * 86400000);
    return new Intl.DateTimeFormat('en-CA', {timeZone: 'Asia/Taipei'}).format(d);
  }

  /** 事後申請資格（post-hoc-context）；null＝尚未查詢 */
  postHocCtx = signal<PostHocContext | null>(null);
  postHocLoading = signal(false);
  /** 編輯載入時的起訖不被預填覆蓋（僅第一次查詢） */
  private _keepLoadedTimes = false;

  get isPostHocKind(): boolean {
    return this.form.get('requestKind')?.value === 'post_hoc';
  }

  /** 日期 input 的 min / max：事後申請限昨天～今天 */
  get dateMin(): string { return this.isPostHocKind ? this.postHocMinStr : this.minDate; }
  get dateMax(): string { return this.isPostHocKind ? this.todayStr : this.maxDate; }

  /** 事後申請不符資格 / 起訖不完整 / 時數不合 → 擋送出 */
  get isPostHocBlocked(): boolean {
    if (!this.isPostHocKind) return false;
    const ctx = this.postHocCtx();
    if (!ctx || !ctx.eligible) return true;
    return !this.composeActual() || this.isActualEndInFuture;
  }

  /** 實際加班結束時間晚於現在（台北時間）→ 尚未發生，事後申請不得送出（後端 EnsurePostHocAsync 同步擋） */
  get isActualEndInFuture(): boolean {
    const c = this.composeActual();
    if (!c) return false;
    const now = new Intl.DateTimeFormat('sv-SE', {
      timeZone: 'Asia/Taipei', year: 'numeric', month: '2-digit', day: '2-digit',
      hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
    }).format(new Date()).replace(' ', 'T');
    return c.end > now;
  }

  /** 實際加班時長（小時）；起訖不完整或訖 ≤ 起回 null */
  get actualDurationHours(): number | null {
    const c = this.composeActual();
    if (!c) return null;
    const diff = (new Date(c.end).getTime() - new Date(c.start).getTime()) / 3600000;
    return diff > 0 ? Math.round(diff * 10) / 10 : null;
  }

  /** 組出 "yyyy-MM-ddTHH:mm:00"；任一未填回 null */
  private composeActual(): {start: string; end: string} | null {
    const v = this.form.getRawValue();
    const date = v.overtimeDate;
    if (!date || !v.actualStartTime || !v.actualEndTime) return null;
    const endDate = v.endNextDay ? this.addDays(date, 1) : date;
    return {start: `${date}T${v.actualStartTime}:00`, end: `${endDate}T${v.actualEndTime}:00`};
  }

  private addDays(date: string, n: number): string {
    const [y, m, d] = date.split('-').map(Number);
    const dt = new Date(Date.UTC(y, m - 1, d + n));
    return dt.toISOString().slice(0, 10);
  }

  onKindChange() {
    if (this.isReadOnly) return;
    if (this.isPostHocKind) {
      const d = this.form.get('overtimeDate')!.value as string;
      if (!d || d < this.postHocMinStr || d > this.todayStr) this.form.patchValue({overtimeDate: this.todayStr}, {emitEvent: false});
      this.loadPostHocContext();
    } else {
      this.postHocCtx.set(null);
    }
  }

  onDateChange() {
    if (this.isReadOnly || !this.isPostHocKind) return;
    this.loadPostHocContext();
  }

  /** 查詢事後申請資格並預填起訖（起＝應下班時間、訖＝下班卡時間，皆可改） */
  private loadPostHocContext() {
    const date = (this.form.get('overtimeDate')!.value as string | null)?.slice(0, 10);
    if (!date) { this.postHocCtx.set(null); return; }
    this.postHocLoading.set(true);
    this.service.getPostHocContext(date).subscribe({
      next: ctx => {
        // 回應已過期（使用者又換了日期 / 類別）則丟棄
        if (!this.isPostHocKind || this.form.get('overtimeDate')!.value?.slice(0, 10) !== date) return;
        this.postHocCtx.set(ctx);
        this.postHocLoading.set(false);
        if (ctx.eligible && !this._keepLoadedTimes) {
          const startT = ctx.regularEnd?.slice(11, 16) ?? '';
          const endT = ctx.clockOutTime?.slice(11, 16) ?? '';
          this.form.patchValue({
            actualStartTime: startT,
            actualEndTime: endT,
            endNextDay: !!ctx.clockOutTime && ctx.clockOutTime.slice(0, 10) > date,
          });
        }
        this._keepLoadedTimes = false;
        this.cdr.markForCheck();
      },
      error: (err: HttpErrorResponse) => {
        this.postHocLoading.set(false);
        this.postHocCtx.set({eligible: false, reason: err.error?.message || '無法查詢事後申請資格，請稍後再試。',
          clockOutTime: null, isClockOutAuto: false, regularEnd: null, hasOvertimePunch: false});
        this.cdr.markForCheck();
      },
    });
  }

  errorMsg = signal('');
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);
  /** 日期欄位合理範圍：擋民國年誤植（見 shared/utils/date-bounds.ts） */
  readonly minDate = MIN_REQUEST_DATE;
  readonly maxDate = MAX_REQUEST_DATE;
  projects: Project[] = [];

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  /** 各關卡的可簽核者（後端解析；某關為空陣列＝該關查無可簽核人員）*/
  stepReviewers: StepReviewers[] = [];

  /** 檢視模式時顯示的關聯專案明細 */
  readonlyProjects: OvertimeProject[] = [];
  /** 預估總時數（明細加總；表單唯讀顯示） */
  totalHours = signal(0);

  /** 加班費試算結果（僅 compensationType='pay' 時查詢） */
  estimate = signal<OvertimePayEstimate | null>(null);
  estimateLoading = signal(false);
  readonly compensationLabel = COMPENSATION_TYPE_LABELS;

  /** 唯讀模式顯示的加班費快照（核准當下寫入，不重新試算） */
  snapshotPayAmount: number | null = null;
  snapshotPayableHours: number | null = null;
  snapshotDayType: ShiftDayType | null = null;

  /**
   * 加班日別 badge 的文字與配色。三值語意（2026-09 四週彈性工時）：
   * 上班日＝平日級距、休假日／例假日＝假日級距、國定假日＝平日級距 ＋ 薪資端另加發 1 日日薪。
   * 文案刻意寫「加班」而非日別本身，因為使用者在意的是「我這張單算哪一種」。
   */
  overtimeDayLabel(t: ShiftDayType | null | undefined): string {
    switch (t) {
      case 'public_holiday': return '國定假日加班';
      case 'rest_day':
      case 'statutory_off':  return '假日加班';
      default:               return '平日加班';
    }
  }

  overtimeDayBadgeClass(t: ShiftDayType | null | undefined): string {
    switch (t) {
      case 'public_holiday': return 'bg-danger-subtle text-danger-emphasis';
      case 'rest_day':
      case 'statutory_off':  return 'bg-warning-subtle text-warning-emphasis';
      default:               return 'bg-secondary-subtle text-secondary';
    }
  }

  /** 目前是否選擇「加班費」（模板用，避免在 template 重複取值） */
  get isPayMode(): boolean {
    return this.form.get('compensationType')?.value === 'pay';
  }

  /**
   * 送出 / 儲存是否應被擋下——超出加班上限。
   * **與補償方式無關**（2026-09 起兩種補償方式一律擋，見後端 GuardOvertimeHoursAsync），
   * 故試算改為兩種模式都查詢（見下方 valueChanges pipeline），不再只在 isPayMode 時才打 API。
   */
  get isBlocked(): boolean {
    return !!this.estimate()?.blockMessage;
  }

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

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    requestKind:      ['normal' as OvertimeRequestKind, Validators.required],
    overtimeDate:     ['', Validators.required],
    // 事後申請的實際起訖（HH:mm）；訖可勾「隔日」跨午夜
    actualStartTime:  [''],
    actualEndTime:    [''],
    endNextDay:       [false],
    // 補償方式二擇一；預設補休（與後端 Normalize 的安全側一致）
    compensationType: ['compensatory' as OvertimeCompensationType, Validators.required],
    reason:           ['', Validators.required],
    projects:         this.fb.array<FormGroup>([]),
  });

  get projectsArray(): FormArray<FormGroup> { return this.form.get('projects') as FormArray<FormGroup>; }
  get projectControls(): FormGroup[] { return this.projectsArray.controls as FormGroup[]; }

  loadingProjects = true;

  private buildProjectGroup(p?: Partial<OvertimeProject>): FormGroup {
    return this.fb.group({
      rowId:          [typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : this.fallbackUuid()],
      projectId:      [p?.projectId ?? null, Validators.required],
      estimatedHours: [p?.estimatedHours ?? 1, [Validators.required, Validators.min(0.5)]],
    });
  }

  /** 舊瀏覽器或非 HTTPS 情境下的簡易 UUID fallback */
  private fallbackUuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  addProject() {
    if (this.isReadOnly) return;
    this.projectsArray.push(this.buildProjectGroup());
  }

  removeProject(index: number) {
    if (this.isReadOnly) return;
    this.projectsArray.removeAt(index);
  }

  /** 第 index 列的專案 autocomplete 要排除的 id：其他列已選過的（後端亦擋重複專案） */
  excludedProjectIds(index: number): number[] {
    return this.projectControls
        .filter((_, i) => i !== index)
        .map(g => g.get('projectId')!.value as number | null)
        .filter((v): v is number => v != null);
  }

  /** 重算預估總時數（對齊後端 decimal(5,1)） */
  private recomputeTotalHours() {
    const sum = this.projectControls.reduce((acc, g) => acc + (+(g.get('estimatedHours')!.value ?? 0) || 0), 0);
    this.totalHours.set(Math.round(sum * 10) / 10);
  }

  ngOnInit() {
    // 這條訂閱必須維持在最前面註冊：下方試算 pipeline 讀 totalHours()，
    // 同一 tick 內若尚未重算會拿到舊值。
    this.projectsArray.valueChanges.subscribe(() => this.recomputeTotalHours());

    // 申請類別 / 加班日期變動 → 事後申請資格查詢（用 valueChanges 而非模板 (change)，避免事件順序讀到舊值）
    this.form.get('requestKind')!.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.onKindChange());
    this.form.get('overtimeDate')!.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.onDateChange());

    // 加班時數試算（**兩種補償方式都查**，2026-09 改）：超出上限
    // 這兩種擋件與補償方式無關，補休模式若只在切到「加班費」才查，使用者選補休送出時
    // 完全看不到擋件訊息，只能等後端 400 才發現。金額 / 分段明細仍只在 isPayMode 顯示（見 template）。
    // 範式比照 user-form 的底薪 → 勞健保級距 lookup。
    this.form.valueChanges.pipe(
      takeUntilDestroyed(this.destroyRef),
      map(() => ({
        date:  this.form.get('overtimeDate')!.value as string,
        type:  this.form.get('compensationType')!.value as OvertimeCompensationType,
        hours: this.totalHours(),
      })),
      debounceTime(300),
      distinctUntilChanged((a, b) => a.date === b.date && a.type === b.type && a.hours === b.hours),
      switchMap(v => {
        if (this.isReadOnly || !v.date || v.hours <= 0) return of(null);
        this.estimateLoading.set(true);
        return this.service.estimatePay(v.date, v.hours).pipe(catchError(() => of(null)));
      }),
    ).subscribe(e => {
      this.estimate.set(e);
      this.estimateLoading.set(false);
      this.cdr.markForCheck();
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('overtime').subscribe(flow => {
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

    this.projects$.getActiveAll().subscribe({
      next: p => {
        this.projects = p;
        this.loadingProjects = false;
        this.cdr.markForCheck();
      },
      error: () => { this.loadingProjects = false; this.errorMsg.set('載入專案資料失敗。'); },
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.requestNo      = r.requestNo ?? '';
        this.isPostHoc      = r.requestKind === 'post_hoc';
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.form.patchValue({
          // 字串切割而非 toISOString()，避免台北 +8 轉 UTC 造成日期少一天
          overtimeDate:     r.overtimeDate?.toString().slice(0, 10) ?? '',
          compensationType: r.compensationType ?? 'compensatory',
          reason: r.reason,
        });
        // requestKind 不觸發 valueChanges：否則 onKindChange 會多發一次資格查詢，
        // 後到的回應把已存的起訖蓋成預填值（應下班 / 下班卡時間）。資格改由下方明確查一次。
        this.form.patchValue({requestKind: r.requestKind ?? 'normal'}, {emitEvent: false});
        if (this.isPostHoc && r.actualStartTime && r.actualEndTime) {
          const d = r.overtimeDate.toString().slice(0, 10);
          this.form.patchValue({
            actualStartTime: r.actualStartTime.slice(11, 16),
            actualEndTime:   r.actualEndTime.slice(11, 16),
            endNextDay:      r.actualEndTime.slice(0, 10) > d,
          });
          this._keepLoadedTimes = true;
        }
        if (this.isPostHoc && !this.isReadOnly) this.loadPostHocContext();
        // 唯讀模式顯示核准當下寫入的快照，不重新試算（避免調薪後金額與已發放的不符）
        this.snapshotPayAmount    = r.overtimePayAmount ?? null;
        this.snapshotPayableHours = r.payableHours ?? null;
        this.snapshotDayType      = r.overtimeDayType ?? null;
        this.readonlyProjects = r.projects ?? [];
        this.projectsArray.clear();
        this.readonlyProjects.forEach(p => this.projectsArray.push(this.buildProjectGroup(p)));
        // 舊單無關聯專案（migration 未回填）→ 可編輯時補 1 列空白，讓使用者依必填規則補齊
        if (this.projectsArray.length === 0 && !this.isReadOnly) this.projectsArray.push(this.buildProjectGroup());
        this.recomputeTotalHours();
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }
        if (this.isReadOnly) this.form.disable();
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'overtime').subscribe({
            next: task => {
              this.approvalFlow = task.flow ?? null;
              this.approvalRecords = task.approvalRecords ?? [];
              this.taskCurrentStepOrder = task.currentStepOrder;
              this.taskStatus = task.status;
              this.stepReviewers = task.stepReviewers ?? [];
              this.cdr.markForCheck();
            },
          });
        }
        this.cdr.markForCheck();
      });
    } else {
      // 新增模式：預設一列空白明細（關聯專案必填）
      this.projectsArray.push(this.buildProjectGroup());
    }
  }

  /**
   * 表單內按 Enter 不送出（textarea 換行不受影響）。
   * 否則任一 input 的 Enter 都會觸發 ngSubmit，直接建草稿並跳回列表。
   */
  onEnterKey(event: Event) {
    const tag = (event.target as HTMLElement)?.tagName;
    if (tag !== 'TEXTAREA') event.preventDefault();
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.saving()) return;
    if (this.form.invalid || this.isReadOnly || this.isBlocked || this.isPostHocBlocked) return;
    if (this.projectsArray.length === 0) {
      this.errorMsg.set('請至少新增一筆關聯專案。');
      return;
    }
    const payload = this._buildPayload();
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const obs = this.requestId
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    this.saving.set(true);
    obs.subscribe({
      next: saved => {
        this.requestId = saved.id;
        this.router.navigate(['/admin/overtime-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.saving()) return;
    if (this.form.invalid || this.isReadOnly || this.isBlocked || this.isPostHocBlocked) return;
    if (this.projectsArray.length === 0) {
      this.errorMsg.set('請至少新增一筆關聯專案。');
      return;
    }
    // 流程含「申請人指定審核」步驟時，至少需要 1 位指定審核者（fail-fast，避免送出後才被後端擋下）
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
    const save$ = this.requestId
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    this.saving.set(true);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update，避免同一筆申請被建成兩張單
        this.requestId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => this.router.navigate(['/admin/overtime-requests']),
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

  private _buildPayload(): OvertimeRequestPayload {
    const v = this.form.value;
    const reviewers = this._pickerPayload;
    const actual = v.requestKind === 'post_hoc' ? this.composeActual() : null;
    return {
      requestKind:         v.requestKind!,
      actualStartTime:     actual?.start ?? null,
      actualEndTime:       actual?.end ?? null,
      overtimeDate:        new Date(v.overtimeDate!),
      projects:            this.projectControls.map(g => ({
        projectId:      +g.get('projectId')!.value,
        estimatedHours: +g.get('estimatedHours')!.value,
      })),
      reason:              v.reason!,
      compensationType:    v.compensationType!,
      designatedReviewers: reviewers.length > 0 ? reviewers : undefined,
    };
  }
}
