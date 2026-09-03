import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES, DesignatedReviewer, AdvanceRound, roundLabel} from '../../models/advance-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, InstallmentDto, PaymentInstallmentStatus, PendingReviewer} from '../../../approval-tasks/models/approval-task.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import heic2any from 'heic2any';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

/** 明細列的三個連動金額欄（總價 = 現金(預支) + 支票(月結)） */
type AmountField = 'total' | 'cash' | 'check';

@Component({
  selector: 'app-advance-form',
  templateUrl: './advance-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class AdvanceForm implements OnInit {
  private fb             = inject(FormBuilder);
  private service        = inject(AdvanceRequestService);
  private projectService = inject(ProjectService);
  private jobTitleSvc    = inject(JobTitleService);
  private userSvc        = inject(UserService);
  private approvalSvc    = inject(ApprovalService);
  private deptSvc        = inject(DepartmentService);
  private taskSvc        = inject(ApprovalTaskService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private modal          = inject(NgbModal);
  private sanitizer      = inject(DomSanitizer);

  projects: Project[] = [];
  loadingProjects = true;
  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  isReadOnly = false;
  isReturned = false;
  /** 後端已存在的申請單 ID（編輯 / 追加模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免 multipart 上傳期間連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);

  /** 追加預支模式：掛在已核准預支單上新增 / 編輯一個追加批次 */
  isSupplement    = false;
  /** 追加批次號；0 = 新增追加（尚未建立），> 0 = 編輯已退回的該批次 */
  supplementRound = 0;
  /** 原預支單既有批次（唯讀對照用） */
  parentRounds: AdvanceRound[] = [];
  parentGrandTotal = 0;
  readonly roundLabel = roundLabel;
  errorMsg   = signal('');
  approvalStatus: ApprovalStatus = 'draft';
  projectCode = '';
  projectName = '';
  categories = ITEM_CATEGORIES;

  /** 檔案上傳相關 */
  fileMap = new Map<string, File>();
  previewFile: PreviewFileData | null = null;

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  /** 目前關卡的待簽核者（後端解析；空陣列＝查無可簽核人員）*/
  pendingReviewers: PendingReviewer[] = [];

  /** 分期撥款（read-only 顯示用，財務排定後申請人可查看）*/
  installments: InstallmentDto[] | null = null;
  paymentStatus: PaymentInstallmentStatus | null = null;
  loadedGrandTotal = 0;

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
  departments: Department[] = [];
  /** 編輯回填給 picker 的 initial（含 approvalStepOrder / selectedDepartmentId） */
  pickerInitial: DesignatedReviewer[] = [];
  /** picker 每次 change 後存放最新 payload，送出時使用 */
  private _pickerPayload: DesignatedReviewerPayload[] = [];
  /** 被抑制（部門最高層級 → 自動略過）的指定步驟 stepOrder，驗證時排除 */
  private _suppressedSteps: number[] = [];
  /** 唯讀模式下顯示的已指定審核者（從 DTO 取得） */
  readonlyDesignatedReviewers: DesignatedReviewer[] = [];

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
    projectId:      [null as number | null, Validators.required],
    activityName:   ['', Validators.required],
    activityPeriod: ['', Validators.required],
    advanceDate:    ['', Validators.required],
    advanceNeededDate: ['', Validators.required],
    reason:         [''],   // 僅追加模式使用
    items:          this.fb.array([]),
  });

  /** 追加模式沿用原單的指定審核者，不重新挑人 */
  get showDesignatedPicker(): boolean { return this.hasDesignatedStep && !this.isSupplement; }

  /** 本次追加金額（明細加總）與追加後總額 */
  get supplementAfterTotal(): number { return this.parentGrandTotal + this.grandTotal; }

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }

  get cashTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('cashAmount')?.value) || 0), 0);
  }
  get checkTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('checkAmount')?.value) || 0), 0);
  }
  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  ngOnInit() {
    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('advance').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); } });
        this.userSvc.getLookup().subscribe({
          next: users => { this.allUsers = users; this.cdr.markForCheck(); },
        });
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().subscribe({ next: d => { this.departments = d; this.cdr.markForCheck(); } });
        }
      }
      this.cdr.markForCheck();
    });

    this.projectService.getActive().subscribe({
      next: p => { this.projects = p; this.loadingProjects = false; this.cdr.markForCheck(); },
      error: () => { this.loadingProjects = false; this.errorMsg.set('載入專案資料失敗。'); },
    });
    this.isSupplement = this.route.snapshot.data['mode'] === 'supplement';
    const roundParam = this.route.snapshot.paramMap.get('round');
    this.supplementRound = roundParam ? +roundParam : 0;

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isReturned = r.approvalStatus === 'returned';
        // 追加模式編輯的是追加批次，父單狀態不決定唯讀
        this.isReadOnly = !this.isSupplement
          && r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.projectCode = r.projectCode ?? '';
        this.projectName = r.projectName ?? '';
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({
          projectId:      r.projectId,
          activityName:   r.activityName,
          activityPeriod: r.activityPeriod,
          advanceDate:    r.advanceDate?.toString().slice(0, 10),
          advanceNeededDate: r.advanceNeededDate?.toString().slice(0, 10) ?? '',
        });
        this.installments    = r.installments ?? null;
        this.paymentStatus   = r.paymentStatus ?? null;
        this.loadedGrandTotal = r.grandTotal ?? 0;
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }

        if (this.isSupplement) {
          this._initSupplement(r.rounds ?? [], r.grandTotal ?? 0);
          // 只載入本批次明細；新增追加時為空白
          r.items
            .filter(item => item.roundNo === this.supplementRound)
            .forEach((item, idx) => this.itemArray.push(this._itemGroup(
              item.category, item.seqNo, item.itemName, item.unitPrice,
              item.quantity, item.totalPrice, item.cashAmount, item.checkAmount,
              item.note ?? '', idx, item.fileName ?? '', item.fileUrl ?? ''
            )));
          if (this.itemArray.length === 0) this.addItem();
          this.cdr.markForCheck();
          return;
        }

        r.items.forEach((item, idx) => this.itemArray.push(this._itemGroup(
          item.category, item.seqNo, item.itemName, item.unitPrice,
          item.quantity, item.totalPrice, item.cashAmount, item.checkAmount,
          item.note ?? '', idx, item.fileName ?? '', item.fileUrl ?? ''
        )));
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'advance').subscribe({
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
      });
    }
  }

  /**
   * 追加模式初始化：專案 / 活動欄位沿用原單且不可改，
   * advanceDate 改為「本批次的預支日期」，並算出扣除本批次後的原總額。
   */
  private _initSupplement(rounds: AdvanceRound[], grandTotal: number) {
    this.form.get('projectId')!.disable();
    this.form.get('activityName')!.disable();
    this.form.get('activityPeriod')!.disable();

    this.parentRounds = rounds;

    if (this.supplementRound > 0) {
      // 編輯已退回的批次：父單總額已含本批次，須扣除後才是「原預支總額」
      const cur = rounds.find(x => x.roundNo === this.supplementRound);
      this.parentGrandTotal = grandTotal - (cur?.grandTotal ?? 0);
      this.form.patchValue({
        advanceDate:       cur?.advanceDate?.toString().slice(0, 10) ?? '',
        advanceNeededDate: cur?.advanceNeededDate?.toString().slice(0, 10) ?? '',
        reason:            cur?.reason ?? '',
      });
    } else {
      this.parentGrandTotal = grandTotal;
      this.supplementRound = (rounds.at(-1)?.roundNo ?? 1) + 1;
      this.form.patchValue({advanceDate: '', advanceNeededDate: '', reason: ''});
    }
  }

  addItem() {
    this.itemArray.push(this._itemGroup('', 0, '', 0, '', 0, 0, 0, '', this.itemArray.length));
  }

  removeItem(i: number) {
    const ctrl = this.itemArray.at(i);
    const id  = ctrl.get('id')?.value as string;
    const url = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    this.amountWarnings.delete(id);
    this._pinnedTotals.delete(id);
    this.itemArray.removeAt(i);
  }

  /** 單列檔案選取 */
  async onFileSelected(event: Event, rowIndex: number) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    let file = input.files[0];
    input.value = '';
    file = await this._convertHeicIfNeeded(file);

    const ctrl = this.itemArray.at(rowIndex);
    const id = ctrl.get('id')?.value as string;

    // 清理舊的 blob URL
    const oldUrl = ctrl.get('previewUrl')?.value as string;
    if (oldUrl?.startsWith('blob:')) URL.revokeObjectURL(oldUrl);

    const previewUrl = URL.createObjectURL(file);
    this.fileMap.set(id, file);
    ctrl.patchValue({ fileName: file.name, previewUrl, fileUrl: '' });
    this.cdr.markForCheck();
  }

  removeFile(rowIndex: number) {
    const ctrl = this.itemArray.at(rowIndex);
    const id  = ctrl.get('id')?.value as string;
    const url = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    ctrl.patchValue({ fileName: '', previewUrl: '', fileUrl: '' });
  }

  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  /** HEIC/HEIF → JPEG 轉換 */
  private async _convertHeicIfNeeded(file: File): Promise<File> {
    const name = file.name.toLowerCase();
    if (!name.endsWith('.heic') && !name.endsWith('.heif')) return file;
    try {
      const blob = await heic2any({blob: file, toType: 'image/jpeg', quality: 0.85}) as Blob;
      const jpegName = file.name.replace(/\.heic$/i, '.jpg').replace(/\.heif$/i, '.jpg');
      return new File([blob], jpegName, {type: 'image/jpeg'});
    } catch {
      return file;
    }
  }

  /** 單價 × 數量（嘗試解析數量前面的數字） */
  calcTotal(ctrl: AbstractControl) {
    const unitPrice = +(ctrl.get('unitPrice')?.value) || 0;
    const qtyStr = (ctrl.get('quantity')?.value ?? '').toString();
    const qtyNum = parseFloat(qtyStr) || 0;
    const total = Math.round(unitPrice * qtyNum);
    // 單價 / 數量 算出的總價視為已確立；支票金額保留，由現金吸收差額（支票為 0 時即等於總價，同舊行為）
    this.setTotal(ctrl, total);
  }

  /**
   * 金額三欄連動：**總價 = 現金(預支) + 支票(月結)**，輸入其中兩欄自動算出第三欄。
   *
   * 推算哪一欄取決於總價是否「已確立」（單價×數量 / 手動輸入 / 編輯載入）：
   * - 已確立 → 改現金推支票、改支票推現金（總價不被反推變動，維持與單價×數量一致）
   * - 未確立（「新增項目」的空白列）→ 現金 + 支票 反推總價
   *
   * 存放已確立總價的列 id。與預支沖銷申請（write-off-form）同一套規則。
   */
  private _pinnedTotals = new Set<string>();

  /** 總價 ≠ 現金 + 支票 時的提示（key = 列 id）；僅顯示，不阻擋送出 */
  amountWarnings = new Map<string, string>();

  /** 總價由外部（單價×數量 / 編輯載入）寫入：標記已確立並讓現金吸收差額 */
  setTotal(ctrl: AbstractControl, total: number) {
    ctrl.get('totalPrice')?.setValue(total, {emitEvent: false});
    this._pinnedTotals.add(ctrl.get('id')?.value);
    this.onAmountInput(ctrl, 'total');
  }

  onAmountInput(ctrl: AbstractControl, field: AmountField) {
    const id  = ctrl.get('id')?.value as string;
    const val = (name: string) => Math.max(0, +(ctrl.get(name)?.value) || 0);
    const set = (name: string, v: number) => ctrl.get(name)?.setValue(v, {emitEvent: false});

    if (field === 'total') this._pinnedTotals.add(id);

    if (!this._pinnedTotals.has(id)) {
      set('totalPrice', val('cashAmount') + val('checkAmount'));
    } else if (field === 'cash') {
      set('checkAmount', Math.max(0, val('totalPrice') - val('cashAmount')));
    } else {
      set('cashAmount', Math.max(0, val('totalPrice') - val('checkAmount')));
    }

    // 推算欄被 0 截斷時（如支票金額大於總價）三欄會對不起來，出提示讓使用者自行修正
    const sum = val('cashAmount') + val('checkAmount');
    if (sum !== val('totalPrice')) {
      this.amountWarnings.set(id, `現金(預支) + 支票(月結)（${sum.toLocaleString()}）與總價（${val('totalPrice').toLocaleString()}）不符，請確認。`);
    } else {
      this.amountWarnings.delete(id);
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

  save() {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再儲存。');
      return;
    }
    if (this.itemArray.length === 0) {
      this.errorMsg.set('請至少新增一筆明細。');
      return;
    }
    this.errorMsg.set('');
    // 追加模式：只有「編輯已退回批次」可儲存不送簽（新增追加一律建立即送簽）
    if (this.isSupplement) {
      this.saving.set(true);
      this.service.updateSupplement(this.requestId, this.supplementRound, this._buildSupplementFormData()).subscribe({
        next: () => this.router.navigate(['/admin/advance-requests', this.requestId]),
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
        },
      });
      return;
    }
    const fd = this._buildFormData();
    this.saving.set(true);
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const obs = this.requestId
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    obs.subscribe({
      next: saved => {
        this.requestId = saved.id;
        this.router.navigate(['/admin/advance-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  submitForApproval() {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再送出。');
      return;
    }
    if (this.itemArray.length === 0) {
      this.errorMsg.set('請至少新增一筆明細。');
      return;
    }
    this.errorMsg.set('');
    if (this.isSupplement) {
      this._submitSupplement();
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
    const fd = this._buildFormData();
    this.saving.set(true);
    const save$ = this.requestId
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update，避免同一筆申請被建成兩張單
        this.requestId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => {
            this.saving.set(false);
            this._onSubmitted(['/admin/advance-requests']);
          },
          error: (err: HttpErrorResponse) => {
            this.saving.set(false);
            this.errorMsg.set((err.error?.message || '送出失敗。') + '（草稿已保留，修正後可直接再送出）');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗。');
      },
    });
  }

  /**
   * 送出追加批次。
   * 新增：POST supplements 一步建立並送簽（後端同交易併入總額 + 重跑簽核流程）。
   * 編輯（已退回）：先更新批次明細，再走既有 submit 重送。
   */
  private _submitSupplement() {
    const fd = this._buildSupplementFormData();
    const done = () => {
      this.saving.set(false);
      this._onSubmitted(['/admin/advance-requests', this.requestId]);
    };
    const fail = (msg: string) => (err: HttpErrorResponse) => {
      this.saving.set(false);
      this.errorMsg.set(err.error?.message || msg);
    };

    // 新增追加是「建立即送簽」的單一 POST，連按就會建出兩個批次，故此處同樣受 saving 鎖保護
    this.saving.set(true);

    if (this.supplementRound > 0 && this.isReturned) {
      this.service.updateSupplement(this.requestId, this.supplementRound, fd).subscribe({
        next: () => this.service.submit(this.requestId).subscribe({
          next: done,
          error: fail('送出失敗。'),
        }),
        error: fail('儲存失敗。'),
      });
      return;
    }

    this.service.createSupplement(this.requestId, fd).subscribe({
      next: done,
      error: fail('送出失敗。'),
    });
  }

  private _onSubmitted(target: unknown[]) {
    const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
    ref.componentInstance.formType = 'advance';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildFormData(): FormData {
    const fd = new FormData();
    fd.append('projectId', String(this.form.get('projectId')!.value));
    fd.append('activityName', this.form.get('activityName')?.value || '');
    fd.append('activityPeriod', this.form.get('activityPeriod')?.value || '');
    fd.append('advanceDate', this.form.get('advanceDate')?.value || '');
    fd.append('advanceNeededDate', this.form.get('advanceNeededDate')?.value || '');

    // 指定審核者清單（從 picker payload 組成，含 approvalStepOrder 與 selectedDepartmentId）
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    this._appendItems(fd);
    return fd;
  }

  /** 追加批次 payload：只帶本批次的預支日期 / 原因 / 明細（專案與活動沿用原單） */
  private _buildSupplementFormData(): FormData {
    const fd = new FormData();
    fd.append('advanceDate', this.form.get('advanceDate')?.value || '');
    fd.append('advanceNeededDate', this.form.get('advanceNeededDate')?.value || '');
    fd.append('reason', this.form.get('reason')?.value || '');
    this._appendItems(fd);
    return fd;
  }

  /** 將明細列與檔案寫入 FormData（一般申請與追加批次共用） */
  private _appendItems(fd: FormData) {
    const itemsMeta: any[] = [];
    let fileIndex = 0;

    for (const ctrl of this.itemArray.controls) {
      const id = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      const meta = {
        category:    ctrl.get('category')?.value || '',
        seqNo:       +(ctrl.get('seqNo')?.value) || 0,
        itemName:    ctrl.get('itemName')?.value || '',
        unitPrice:   +(ctrl.get('unitPrice')?.value) || 0,
        quantity:    ctrl.get('quantity')?.value || '',
        totalPrice:  +(ctrl.get('totalPrice')?.value) || 0,
        cashAmount:  +(ctrl.get('cashAmount')?.value) || 0,
        checkAmount: +(ctrl.get('checkAmount')?.value) || 0,
        note:        ctrl.get('note')?.value || '',
        sortOrder:   itemsMeta.length,
        fileName:    file ? file.name : (ctrl.get('fileName')?.value || null),
        fileUrl:     ctrl.get('fileUrl')?.value || null,
        fileIndex:   file ? fileIndex : -1,
      };
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
      itemsMeta.push(meta);
    }
    fd.append('items', JSON.stringify(itemsMeta));
  }

  private _itemGroup(
    category: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, cashAmount: number, checkAmount: number,
    note: string, sortOrder: number, fileName = '', fileUrl = ''
  ) {
    const rowId = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    // 帶總價進來的列（編輯 / 追加模式回填）總價視為已確立，改現金 / 支票時不反推總價
    if (totalPrice > 0) this._pinnedTotals.add(rowId);
    return this.fb.group({
      id:          [rowId],
      category:    [category, Validators.required],
      seqNo:       [seqNo],
      itemName:    [itemName, Validators.required],
      unitPrice:   [unitPrice, [Validators.required, Validators.min(0)]],
      quantity:    [quantity, Validators.required],
      totalPrice:  [totalPrice],
      cashAmount:  [cashAmount],
      checkAmount: [checkAmount],
      note:        [note],
      sortOrder:   [sortOrder],
      fileName:    [fileName],
      fileUrl:     [fileUrl],
      previewUrl:  [fileUrl],  // 既有檔案用 fileUrl 作為預覽
    });
  }
}
