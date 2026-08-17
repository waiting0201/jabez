import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {OvertimeRequestService} from '../../services/overtime-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, DesignatedReviewer, OvertimeProject, OvertimeRequestPayload} from '../../models/overtime-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-overtime-request-form',
  templateUrl: './overtime-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline, DesignatedReviewersPicker, ScrollIntoViewDirective],
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

  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  /** 後端已存在的申請單 ID（編輯模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);
  projects: Project[] = [];

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';

  /** 檢視模式時顯示的關聯專案明細 */
  readonlyProjects: OvertimeProject[] = [];
  /** 預估總時數（明細加總；表單唯讀顯示） */
  totalHours = signal(0);

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
    overtimeDate: ['', Validators.required],
    reason:       ['', Validators.required],
    projects:     this.fb.array<FormGroup>([]),
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

  /** 第 index 列可選的專案：排除其他列已選過的（後端亦擋重複專案） */
  availableProjects(index: number): Project[] {
    const taken = new Set<number>(
      this.projectControls
          .filter((_, i) => i !== index)
          .map(g => g.get('projectId')!.value as number | null)
          .filter((v): v is number => v != null));
    return this.projects.filter(p => !taken.has(p.id));
  }

  /** 重算預估總時數（對齊後端 decimal(5,1)） */
  private recomputeTotalHours() {
    const sum = this.projectControls.reduce((acc, g) => acc + (+(g.get('estimatedHours')!.value ?? 0) || 0), 0);
    this.totalHours.set(Math.round(sum * 10) / 10);
  }

  ngOnInit() {
    this.projectsArray.valueChanges.subscribe(() => this.recomputeTotalHours());

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
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.form.patchValue({
          // 字串切割而非 toISOString()，避免台北 +8 轉 UTC 造成日期少一天
          overtimeDate: r.overtimeDate?.toString().slice(0, 10) ?? '',
          reason: r.reason,
        });
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
    if (this.form.invalid || this.isReadOnly) return;
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
    if (this.form.invalid || this.isReadOnly) return;
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
    return {
      overtimeDate:        new Date(v.overtimeDate!),
      projects:            this.projectControls.map(g => ({
        projectId:      +g.get('projectId')!.value,
        estimatedHours: +g.get('estimatedHours')!.value,
      })),
      reason:              v.reason!,
      designatedReviewers: reviewers.length > 0 ? reviewers : undefined,
    };
  }
}
