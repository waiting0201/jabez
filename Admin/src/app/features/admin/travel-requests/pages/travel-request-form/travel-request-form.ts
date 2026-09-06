import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {TravelRequestService} from '../../services/travel-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES, DesignatedReviewer, TravelRequest} from '../../models/travel-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, InstallmentDto, PaymentInstallmentStatus, StepReviewers} from '../../../approval-tasks/models/approval-task.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';
import {MAX_REQUEST_DATE, MIN_REQUEST_DATE} from '@shared/utils/date-bounds';

@Component({
  selector: 'app-travel-request-form',
  templateUrl: './travel-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, InstallmentsTable, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class TravelRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(TravelRequestService);
  private projects$   = inject(ProjectService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private deptSvc     = inject(DepartmentService);
  private taskSvc     = inject(ApprovalTaskService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);
  private modal       = inject(NgbModal);

  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  /** 後端已存在的申請單 ID（編輯模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);
  /** 日期欄位合理範圍：擋民國年誤植（見 shared/utils/date-bounds.ts） */
  readonly minDate = MIN_REQUEST_DATE;
  readonly maxDate = MAX_REQUEST_DATE;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  /** 編輯模式下的完整出差申請資料（用於顯示結案退款資訊） */
  existingRequest: TravelRequest | null = null;
  errorMsg = signal('');
  projects: Project[] = [];
  loadingProjects = true;
  categories = ITEM_CATEGORIES;

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  /** 各關卡的可簽核者（後端解析；某關為空陣列＝該關查無可簽核人員）*/
  stepReviewers: StepReviewers[] = [];

  /** 分期撥款（read-only 顯示用，財務排定後申請人可查看）*/
  installments: InstallmentDto[] | null = null;
  paymentStatus: PaymentInstallmentStatus | null = null;
  loadedGrandTotal = 0;

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  departments: Department[] = [];
  /** 編輯回填給 picker 的 initial（含 approvalStepOrder / selectedDepartmentId） */
  pickerInitial: DesignatedReviewer[] = [];
  /** 唯讀模式下顯示的已指定審核者（從 DTO 取得） */
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
    destination:     ['', Validators.required],
    startDate:       ['', Validators.required],
    endDate:         ['', Validators.required],
    advanceNeededDate: ['', Validators.required],
    purpose:         ['', Validators.required],
    projectId:       [null as number | null],
    items:           this.fb.array([]),
  });

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }

  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  ngOnInit() {
    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('travel').subscribe(flow => {
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

    this.projects$.getActive().subscribe({
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
        this.existingRequest = r;
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.installments    = r.installments ?? null;
        this.paymentStatus   = r.paymentStatus ?? null;
        this.loadedGrandTotal = r.grandTotal ?? 0;
        this.form.patchValue({
          destination:     r.destination,
          // 後端回傳 "2026-03-24T00:00:00"，<input type="date"> 只接受 yyyy-MM-dd；
          // 用字串切割而非 toISOString()，避免台北 +8 轉 UTC 造成日期少一天
          startDate: r.startDate?.toString().slice(0, 10) ?? '',
          endDate:   r.endDate?.toString().slice(0, 10) ?? '',
          advanceNeededDate: r.advanceNeededDate?.toString().slice(0, 10) ?? '',
          purpose:         r.purpose,
          projectId:       r.projectId ?? null,
        });
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }
        // 回填費用明細
        (r.items ?? []).forEach((item, idx) => this.itemArray.push(this._itemGroup(
          item.category, item.seqNo, item.itemName, item.unitPrice,
          item.quantity, item.totalPrice, item.note ?? '', idx
        )));
        if (this.isReadOnly) this.form.disable();
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'travel').subscribe({
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
    }
  }

  addItem() {
    this.itemArray.push(this._itemGroup('', 0, '', 0, '', 0, '', this.itemArray.length));
  }

  removeItem(i: number) {
    this.itemArray.removeAt(i);
  }

  /** 單價 × 數量（嘗試解析數量前面的數字） */
  calcTotal(ctrl: AbstractControl) {
    const unitPrice = +(ctrl.get('unitPrice')?.value) || 0;
    const qtyStr = (ctrl.get('quantity')?.value ?? '').toString();
    const qtyNum = parseFloat(qtyStr) || 0;
    const total = Math.round(unitPrice * qtyNum);
    ctrl.get('totalPrice')?.setValue(total, {emitEvent: false});
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
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly) return;
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
        this.router.navigate(['/admin/travel-requests']);
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
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly) return;
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
          next: () => {
            this.saving.set(false);
            this._onSubmitted(['/admin/travel-requests']);
          },
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

  private _onSubmitted(target: unknown[]) {
    const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
    ref.componentInstance.formType = 'travel';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildPayload() {
    const v = this.form.value;
    const project = this.projects.find(p => p.id === v.projectId);
    const reviewers = this._pickerPayload.map(p => ({
      reviewerId: p.reviewerId,
      stepOrder: p.stepOrder,
      approvalStepOrder: p.approvalStepOrder,
      selectedDepartmentId: p.selectedDepartmentId,
    }));
    const items = this.itemArray.controls.map((c, idx) => ({
      category:   c.get('category')?.value || '',
      seqNo:      +(c.get('seqNo')?.value) || 0,
      itemName:   c.get('itemName')?.value || '',
      unitPrice:  +(c.get('unitPrice')?.value) || 0,
      quantity:   c.get('quantity')?.value || '',
      totalPrice: +(c.get('totalPrice')?.value) || 0,
      note:       c.get('note')?.value || '',
      sortOrder:  idx,
    }));
    const grandTotal = items.reduce((s, i) => s + i.totalPrice, 0);
    return {
      destination:         v.destination!,
      // 直接送表單的 yyyy-MM-dd 字串，不包成 Date：後端 DateTime 可解析純日期，
      // 也避免 Date → JSON 的 UTC 轉換讓日期位移
      startDate:           v.startDate!,
      endDate:             v.endDate!,
      advanceNeededDate:   v.advanceNeededDate!,
      purpose:             v.purpose!,
      projectId:           v.projectId ?? undefined,
      projectCode:         project?.code,
      grandTotal,
      designatedReviewers: reviewers.length > 0 ? reviewers : undefined,
      items,
    };
  }

  private _itemGroup(
    category: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, note: string, sortOrder: number
  ) {
    return this.fb.group({
      category:   [category, Validators.required],
      seqNo:      [seqNo],
      itemName:   [itemName, Validators.required],
      unitPrice:  [unitPrice, [Validators.required, Validators.min(0)]],
      quantity:   [quantity, Validators.required],
      totalPrice: [totalPrice],
      note:       [note],
      sortOrder:  [sortOrder],
    });
  }
}
