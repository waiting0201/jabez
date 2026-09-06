import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {TravelPaymentRequestService} from '../../services/travel-payment-request.service';
import {PaymentRequestService, OcrItem} from '../../../payment-requests/services/payment-request.service';
import {validateInvoiceBuyer} from '../../../../../shared/utils/invoice-buyer-validator';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES, DesignatedReviewer, TravelPaymentRequest} from '../../models/travel-payment-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, InstallmentDto, PaymentInstallmentStatus, StepReviewers} from '../../../approval-tasks/models/approval-task.model';
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

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';
import {MAX_REQUEST_DATE, MIN_REQUEST_DATE} from '@shared/utils/date-bounds';

@Component({
  selector: 'app-travel-payment-form',
  templateUrl: './travel-payment-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class TravelPaymentForm implements OnInit {
  private fb             = inject(FormBuilder);
  private service        = inject(TravelPaymentRequestService);
  private paymentService = inject(PaymentRequestService);
  private projects$      = inject(ProjectService);
  private jobTitleSvc    = inject(JobTitleService);
  private userSvc        = inject(UserService);
  private approvalSvc    = inject(ApprovalService);
  private taskSvc        = inject(ApprovalTaskService);
  private deptSvc        = inject(DepartmentService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private modal          = inject(NgbModal);
  private sanitizer      = inject(DomSanitizer);

  /** invoice id → File 物件（新上傳的檔案） */
  fileMap = new Map<string, File>();

  /** 正在 OCR 辨識中的列 ID */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** 發票買方抬頭/統編驗證警告（key = 列 id，value = 警告訊息）；僅供顯示，不阻擋送出 */
  invoiceWarnings = new Map<string, string>();

  /**
   * 使用者對買方警告勾選「確認無誤」的列 id。
   * OCR 誤判（抬頭缺字 / 手寫誤讀）時讓使用者自行放行，警告由紅字轉為灰字已確認樣式。
   * 與警告本身一樣**僅供顯示**：不阻擋送出、不寫入 DB、重開草稿不重現。
   */
  invoiceConfirmed = new Set<string>();

  /** 警告列的「確認無誤」勾選切換 */
  toggleInvoiceConfirm(rowId: string, event: Event) {
    (event.target as HTMLInputElement).checked
      ? this.invoiceConfirmed.add(rowId)
      : this.invoiceConfirmed.delete(rowId);
  }

  /** OCR 填值後驗證買方抬頭/統編（僅統一發票）；不符則記錄該列警告 */
  private _checkBuyer(rowId: string, item: OcrItem) {
    this.invoiceConfirmed.delete(rowId); // 同一列重新辨識 → 舊的人工確認失效
    if (item.docType !== 'invoice') { this.invoiceWarnings.delete(rowId); return; }
    const r = validateInvoiceBuyer(item.buyerName ?? '', item.buyerTaxId ?? '', item.sellerTaxId ?? '');
    if (r.level === 'warn') this.invoiceWarnings.set(rowId, r.message!);
    else this.invoiceWarnings.delete(rowId);
  }

  /** 檔案預覽 modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    if (!url) return;
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  /** 後端已存在的申請單 ID（編輯模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免 multipart 上傳期間連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);
  /** 日期欄位合理範圍：擋民國年誤植（見 shared/utils/date-bounds.ts） */
  readonly minDate = MIN_REQUEST_DATE;
  readonly maxDate = MAX_REQUEST_DATE;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  existingRequest: TravelPaymentRequest | null = null;
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
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
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
    this.approvalSvc.getActiveByType('travel_payment').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        // 載入職稱與使用者（所有 designated step 共用）
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); } });
        this.userSvc.getLookup().subscribe({ next: users => { this.allUsers = users; this.cdr.markForCheck(); } });
        // 若有任何 step 需選部門，則載入部門清單
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
          purpose:         r.purpose,
          projectId:       r.projectId ?? null,
        });
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }
        // 回填費用明細（保留既有發票檔案 URL）
        (r.items ?? []).forEach((item, idx) => this.itemArray.push(this._itemGroup(
          `existing-${item.id ?? idx}`, item.fileName ?? '',
          item.category, item.seqNo, item.itemName, item.unitPrice,
          item.quantity, item.totalPrice, item.note ?? '',
          item.invoiceNo ?? '', item.invoiceDate?.toString().slice(0, 10) ?? '', idx,
          '', item.fileUrl ?? '',
        )));
        if (this.isReadOnly) this.form.disable();
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'travel_payment').subscribe({
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
    this.itemArray.push(this._itemGroup(
      '', '', '', 0, '', 0, '', 0, '', '', '', this.itemArray.length,
    ));
  }

  removeItem(i: number) {
    const ctrl = this.itemArray.at(i);
    const id   = ctrl.get('id')?.value as string;
    const url  = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    this.invoiceWarnings.delete(id);
    this.invoiceConfirmed.delete(id);
    this.itemArray.removeAt(i);
  }

  /** 發票檔案上傳 — 自動新增行、OCR 辨識 */
  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const rawFiles = Array.from(input.files);
    input.value = '';

    const files = await Promise.all(rawFiles.map(f => this._convertHeicIfNeeded(f)));

    const entries = files.map(file => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.fileMap.set(id, file);
      this.itemArray.push(this._itemGroup(
        id, file.name, '', 0, '', 0, '', 0, '', '', '',
        this.itemArray.length, previewUrl,
      ));
      return {id, file};
    });

    // OCR 辨識（並行；一張圖可辨識出多筆 → 展開多列）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const results = await firstValueFrom(this.paymentService.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        // 第 1 筆填入 placeholder 列；第 2..N 筆各新增一列（共用同一檔案，各存一份複本）
        if (results.length >= 1 && idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo:   results[0].invoiceNo ?? '',
            invoiceDate: results[0].invoiceDate ?? '',
            unitPrice:   results[0].amount ?? 0,
            totalPrice:  results[0].amount ?? 0,
            quantity:    '1式',
            ...(results[0].docType === 'ticket' ? { note: '票號', category: '交通費' } : {}),
          });
          this._checkBuyer(id, results[0]);
        }
        for (const item of results.slice(1)) {
          const newId      = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
          const previewUrl = URL.createObjectURL(file);
          const amount     = item.amount ?? 0;
          const isTicket   = item.docType === 'ticket';
          this.fileMap.set(newId, file);
          this.itemArray.push(this._itemGroup(
            newId, file.name, isTicket ? '交通費' : '', 0, '', amount, '1式', amount,
            isTicket ? '票號' : '', item.invoiceNo ?? '', item.invoiceDate ?? '',
            this.itemArray.length, previewUrl,
          ));
          this._checkBuyer(newId, item);
          this.itemArray.at(this.itemArray.length - 1).markAllAsTouched();
        }
      } catch {
        // OCR 失敗 — 保留空白欄位
      } finally {
        // OCR 辨識完成（無論成功或失敗）立即標記該列 touched，讓漏填的必填欄位馬上顯示紅框，
        // 避免使用者不知道表單無效、送出按鈕鎖住卻找不到原因
        this.itemArray.controls.find(c => c.get('id')?.value === id)?.markAllAsTouched();
        this.ocrLoadingIds.delete(id);
        this.cdr.markForCheck();
      }
    }));
  }

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
    if (this.isReadOnly || this.isAnyOcrPending) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再儲存。');
      return;
    }
    if (this.itemArray.length === 0) {
      this.errorMsg.set('請至少新增一筆明細。');
      return;
    }
    const fd = this._buildFormData();
    this.errorMsg.set('');
    this.saving.set(true);
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const obs = this.requestId
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    obs.subscribe({
      next: saved => {
        this.requestId = saved.id;
        this.router.navigate(['/admin/travel-payment-requests']);
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
    if (this.isReadOnly || this.isAnyOcrPending) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再送出。');
      return;
    }
    if (this.itemArray.length === 0) {
      this.errorMsg.set('請至少新增一筆明細。');
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
    this.errorMsg.set('');
    this.saving.set(true);
    const save$ = this.requestId
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update，避免同一筆申請被建成兩張單
        this.requestId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => {
            this.saving.set(false);
            this._onSubmitted(['/admin/travel-payment-requests']);
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
    ref.componentInstance.formType = 'travel_payment';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildFormData(): FormData {
    const v = this.form.value;
    const fd = new FormData();
    fd.append('destination', v.destination ?? '');
    fd.append('purpose',     v.purpose ?? '');
    if (v.startDate) fd.append('startDate', v.startDate);
    if (v.endDate)   fd.append('endDate',   v.endDate);
    if (v.projectId != null) fd.append('projectId', String(v.projectId));

    // 指定審核者清單（從 picker payload 組成，含 approvalStepOrder 與 selectedDepartmentId）
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    const itemsMeta: object[] = [];
    let fileIndex = 0;
    let sortIdx = 0;

    for (const ctrl of this.itemArray.controls) {
      const id   = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      const meta = {
        category:    ctrl.get('category')?.value || '',
        seqNo:       +(ctrl.get('seqNo')?.value) || 0,
        itemName:    ctrl.get('itemName')?.value || '',
        unitPrice:   +(ctrl.get('unitPrice')?.value) || 0,
        quantity:    ctrl.get('quantity')?.value || '',
        totalPrice:  +(ctrl.get('totalPrice')?.value) || 0,
        note:        ctrl.get('note')?.value || null,
        invoiceNo:   ctrl.get('invoiceNo')?.value || null,
        invoiceDate: ctrl.get('invoiceDate')?.value || null,
        fileName:    ctrl.get('fileName')?.value || null,
        fileUrl:     ctrl.get('fileUrl')?.value || null,
        fileIndex:   file ? fileIndex : -1,
        sortOrder:   sortIdx++,
      };
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
      itemsMeta.push(meta);
    }

    fd.append('items', JSON.stringify(itemsMeta));
    return fd;
  }

  private _itemGroup(
    id: string, fileName: string,
    category: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, note: string,
    invoiceNo: string, invoiceDate: string, sortOrder: number,
    previewUrl = '', fileUrl = '',
  ) {
    return this.fb.group({
      id:          [id || `${Date.now()}-${Math.random().toString(36).slice(2)}`],
      fileName:    [fileName],
      category:    [category, Validators.required],
      seqNo:       [seqNo],
      itemName:    [itemName, Validators.required],
      unitPrice:   [unitPrice, [Validators.required, Validators.min(0)]],
      quantity:    [quantity, Validators.required],
      totalPrice:  [totalPrice],
      note:        [note],
      invoiceNo:   [invoiceNo],
      invoiceDate: [invoiceDate],
      previewUrl:  [previewUrl],
      fileUrl:     [fileUrl],
      sortOrder:   [sortOrder],
    });
  }
}
