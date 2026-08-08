import {ChangeDetectorRef, Component, inject, OnInit, signal, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom, Observable, OperatorFunction} from 'rxjs';
import {debounceTime, distinctUntilChanged, map, take} from 'rxjs/operators';
import heic2any from 'heic2any';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {AttachmentsUpload} from '../../../../../shared/components/attachments-upload';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {AttachmentItem} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, ApprovalTask, InstallmentDto, PaymentInstallmentStatus} from '../../../approval-tasks/models/approval-task.model';
import {PaymentRequestService, OcrItem} from '../../services/payment-request.service';
import {validateInvoiceBuyer} from '../../../../../shared/utils/invoice-buyer-validator';
import {PaymentPdfService} from '../../services/payment-pdf.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {PaymentType, ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, DesignatedReviewer} from '../../models/payment-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';
import {Department} from '../../../departments/models/department.model';
import {VendorService} from '../../../vendors/services/vendor.service';
import {VendorLookup} from '../../../vendors/models/vendor.model';
import {VendorQuickAddModal} from '../../../vendors/components/vendor-quick-add-modal/vendor-quick-add-modal';
import {NgbModal, NgbTypeahead, NgbTypeaheadSelectItemEvent} from '@ng-bootstrap/ng-bootstrap';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-payment-form',
  templateUrl: './payment-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, FilePreviewModal, ApprovalTimeline, NgbTypeahead, InstallmentsTable, AttachmentsUpload, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class PaymentForm implements OnInit {
  private fb           = inject(FormBuilder);
  private service      = inject(PaymentRequestService);
  private projects$    = inject(ProjectService);
  private jobTitleSvc  = inject(JobTitleService);
  private userSvc      = inject(UserService);
  private approvalSvc  = inject(ApprovalService);
  private deptSvc      = inject(DepartmentService);
  private taskSvc      = inject(ApprovalTaskService);
  private vendorSvc    = inject(VendorService);
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private cdr          = inject(ChangeDetectorRef);
  private sanitizer    = inject(DomSanitizer);
  private modal        = inject(NgbModal);
  pdfService           = inject(PaymentPdfService);

  attachmentsUpload = viewChild(AttachmentsUpload);

  /** 編輯模式回填的既有附件（僅一般請款） */
  loadedAttachments: AttachmentItem[] = [];

  projects: Project[] = [];
  isEdit     = false;
  isReturned = false;
  isReadOnly = false;
  requestId  = 0;
  showInvoiceError = false;
  errorMsg = signal('');
  approvalStatus: ApprovalStatus = 'draft';
  isDraft    = true;
  /** 檢視模式時顯示的專案編號與名稱 */
  projectCode = '';
  projectName = '';
  /** 編輯/檢視時顯示的單號（PR-yyyyMMdd-NNN） */
  requestNo = '';

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  approvalTask: ApprovalTask | null = null;
  // 分期撥款（read-only 顯示用，財務排定後申請人可查看）
  installments: InstallmentDto[] | null = null;
  paymentStatus: PaymentInstallmentStatus | null = null;
  loadedTotalAmount = 0;

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

  /** 廠商下拉清單（type=vendor 時顯示，僅含 IsActive 廠商） */
  vendors = signal<VendorLookup[]>([]);

  /** 廠商 typeahead 雙向綁定值（選定後為 VendorLookup，輸入過程為 string） */
  vendorTypeaheadModel: VendorLookup | string | null = null;

  /** 廠商 autocomplete 搜尋（依名稱 / 統編 / 身分證字號，最多 10 筆） */
  vendorSearch: OperatorFunction<string, readonly VendorLookup[]> = (text$: Observable<string>) =>
    text$.pipe(
      debounceTime(150),
      distinctUntilChanged(),
      map(term => {
        const t = (term ?? '').toString().toLowerCase().trim();
        const list = this.vendors();
        const filtered = t.length === 0
          ? list
          : list.filter(v =>
              v.name.toLowerCase().includes(t) ||
              (v.taxId ?? '').toLowerCase().includes(t) ||
              (v.idNumber ?? '').toLowerCase().includes(t));
        return filtered.slice(0, 10);
      })
    );

  /** 下拉項目顯示格式：「名稱（統編或身分證字號）」 */
  vendorFormatter = (v: VendorLookup) => {
    const code = v.taxId ?? v.idNumber;
    return v.name + (code ? `（${code}）` : '');
  };
  /** 選中後 input 顯示格式：只顯示名稱 */
  vendorInputFormatter = (v: VendorLookup) => v.name;

  onVendorSelect(event: NgbTypeaheadSelectItemEvent) {
    const v = event.item as VendorLookup;
    this.form.get('vendorId')!.setValue(v.id);
  }

  /** 使用者編輯輸入框時，若文字與選中的廠商名稱不符則清空 vendorId 強迫重選 */
  onVendorInput(event: Event) {
    const inputVal = (event.target as HTMLInputElement).value;
    const selectedId = this.form.get('vendorId')?.value;
    if (!selectedId) return;
    const selectedVendor = this.vendors().find(v => v.id === selectedId);
    if (selectedVendor && inputVal !== this.vendorInputFormatter(selectedVendor)) {
      this.form.get('vendorId')!.setValue(null);
    }
  }

  /** 廠商輸入框失焦：未選定廠商（含 typeahead editable=false 自動清空文字）時立即標記 touched，
   * 讓「請從清單中選擇廠商」提示馬上顯示，不必等到點送出才發現 */
  onVendorBlur() {
    if (!this.form.get('vendorId')?.value) this.form.get('vendorId')!.markAsTouched();
  }

  /** 開啟「快速新增廠商」Modal；建立成功後將新廠商加入下拉並自動選取 */
  openQuickAddVendor() {
    const ref = this.modal.open(VendorQuickAddModal, {
      centered: true,
      backdrop: 'static',
      keyboard: false,
      size: 'lg',
    });
    ref.closed.subscribe((newVendor: VendorLookup | undefined) => {
      if (!newVendor) return;
      this.vendors.update(list =>
        [...list, newVendor].sort((a, b) => a.name.localeCompare(b.name, 'zh-Hant'))
      );
      this.form.get('vendorId')!.setValue(newVendor.id);
      this.vendorTypeaheadModel = newVendor;
      this.cdr.markForCheck();
    });
  }

  /** invoice id → File 物件（新上傳的檔案） */
  fileMap = new Map<string, File>();

  /** IDs of invoice rows currently being OCR-processed */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** 發票買方抬頭/統編驗證警告（key = 列 id，value = 警告訊息）；僅供顯示，不阻擋送出 */
  invoiceWarnings = new Map<string, string>();

  /** OCR 填值後驗證買方抬頭/統編（僅統一發票）；不符則記錄該列警告 */
  private _checkBuyer(rowId: string, item: OcrItem) {
    if (item.docType !== 'invoice') { this.invoiceWarnings.delete(rowId); return; }
    const r = validateInvoiceBuyer(item.buyerName ?? '', item.buyerTaxId ?? '');
    if (r.level === 'warn') this.invoiceWarnings.set(rowId, r.message!);
    else this.invoiceWarnings.delete(rowId);
  }

  /** File preview modal state */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    type:      ['vendor', Validators.required],
    projectId: [null as number | null, Validators.required],
    vendorId:  [null as number | null, Validators.required],   // 預設 type=vendor 故必填
    reason:    ['', Validators.required],
    invoices:  this.fb.array([]),
  });

  get invoiceArray(): FormArray { return this.form.get('invoices') as FormArray; }
  get invoiceControls(): AbstractControl[] { return this.invoiceArray.controls; }
  get totalAmount(): number {
    return this.invoiceArray.controls.reduce((sum, c) => sum + (+(c.get('amount')?.value) || 0), 0);
  }

  loadingProjects = true;

  ngOnInit() {
    // 載入廠商下拉清單（lookup 端點，免 vendors:read 權限）
    this.vendorSvc.getLookup().subscribe(v => { this.vendors.set(v); this.cdr.markForCheck(); });

    // type 變化時動態切換 vendorId 的 required validator
    this.form.get('type')!.valueChanges.subscribe(t => {
      const c = this.form.get('vendorId')!;
      if (t === 'vendor') {
        c.setValidators(Validators.required);
      } else {
        c.clearValidators();
        c.setValue(null);
        this.vendorTypeaheadModel = null;
      }
      c.updateValueAndValidity();
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('payment_request').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        // 載入職稱與使用者（所有 designated step 共用）
        this.jobTitleSvc.getLookup().pipe(take(1)).subscribe(jts => { this.jobTitles = jts; });
        this.userSvc.getLookup().subscribe({
          next: users => { this.allUsers = users; this.cdr.markForCheck(); },
        });
        // 若有任何 step 需選部門，則載入部門清單
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().pipe(take(1)).subscribe(d => {
            this.departments = d;
            this.cdr.markForCheck();
          });
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
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isDraft        = r.approvalStatus === 'draft';
        this.isReturned     = r.approvalStatus === 'returned';
        this.isReadOnly     = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.projectCode    = r.projectCode ?? '';
        this.projectName    = r.projectName ?? '';
        this.requestNo      = r.requestNo ?? '';
        this.installments         = r.installments ?? null;
        this.paymentStatus        = r.paymentStatus ?? null;
        this.loadedTotalAmount    = r.totalAmount ?? 0;
        this.loadedAttachments    = r.attachments ?? [];
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({type: r.type, projectId: r.projectId, reason: r.reason ?? '', vendorId: r.vendorId ?? null});

        // 若請款單帶有 vendorId 但下拉中沒有（例如已停用），補回該選項以保留當前值
        if (r.vendorId && r.vendorName && !this.vendors().some(v => v.id === r.vendorId)) {
          this.vendors.update(list => [...list, {id: r.vendorId!, name: r.vendorName!, taxId: r.vendorTaxId}]);
        }
        // 同步 typeahead 顯示值
        if (r.vendorId) {
          const found = this.vendors().find(v => v.id === r.vendorId);
          if (found) this.vendorTypeaheadModel = found;
        }

        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.readonlyDesignatedReviewers = r.designatedReviewers;
          this.pickerInitial = r.designatedReviewers;
        }

        r.invoices.forEach(inv => this.invoiceArray.push(
          this._invoiceGroup(String(inv.id), inv.fileName, inv.invoiceNo, inv.amount, inv.fileUrl ?? '', inv.fileUrl ?? '', inv.itemName ?? '', inv.note ?? '', inv.invoiceDate?.toString().slice(0, 10) ?? '')
        ));
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'payment_request').subscribe({
            next: task => {
              this.approvalTask = task;
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
    }
  }

  /** picker change 事件：每次使用者操作時更新最新 payload */
  onPickerChange(payload: DesignatedReviewerPayload[]) {
    this._pickerPayload = payload;
  }

  /** picker 回報被抑制（部門最高層級 → 自動略過）的指定步驟 */
  onSuppressedSteps(stepOrders: number[]) {
    this._suppressedSteps = stepOrders;
  }

  /** 取得審核者的顯示名稱（唯讀模式用） */
  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const rawFiles = Array.from(input.files);
    input.value = '';
    this.showInvoiceError = false;

    // HEIC/HEIF → JPEG 轉換（iPhone 預設拍照格式）
    const files = await Promise.all(rawFiles.map(f => this._convertHeicIfNeeded(f)));

    // Add all rows immediately as "loading" placeholders; create blob URL for preview
    const entries = files.map(file => {
      const id         = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.fileMap.set(id, file);
      this.invoiceArray.push(this._invoiceGroup(id, file.name, '', 0, previewUrl));
      return {id, file};
    });

    // 使用後端 Gemini API 辨識發票（並行處理所有檔案；一張圖可辨識出多筆 → 展開多列）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const results = await firstValueFrom(this.service.ocrInvoice(file));
        const idx = this.invoiceArray.controls.findIndex(c => c.get('id')?.value === id);
        // 第 1 筆填入 placeholder 列；第 2..N 筆各新增一列（共用同一檔案，各存一份複本）
        if (results.length >= 1 && idx >= 0) {
          this.invoiceArray.controls[idx].patchValue({
            invoiceNo:   results[0].invoiceNo ?? '',
            amount:      results[0].amount ?? 0,
            invoiceDate: results[0].invoiceDate ?? '',
            ...(results[0].docType === 'ticket' ? { note: '票號' } : {}),
          });
          this._checkBuyer(id, results[0]);
        }
        for (const item of results.slice(1)) {
          const newId      = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
          const previewUrl = URL.createObjectURL(file);
          this.fileMap.set(newId, file);
          this.invoiceArray.push(this._invoiceGroup(
            newId, file.name, item.invoiceNo ?? '', item.amount ?? 0, previewUrl, '', '',
            item.docType === 'ticket' ? '票號' : '', item.invoiceDate ?? '',
          ));
          this._checkBuyer(newId, item);
          this.invoiceArray.at(this.invoiceArray.length - 1).markAllAsTouched();
        }
      } catch {
        // OCR failed — leave fields empty for manual entry
      } finally {
        // OCR 辨識完成（無論成功或失敗）立即標記該列 touched，讓漏填的必填欄位（如發票號碼）馬上顯示紅框，
        // 避免使用者不知道表單無效、送出按鈕鎖住卻找不到原因
        this.invoiceArray.controls.find(c => c.get('id')?.value === id)?.markAllAsTouched();
        this.ocrLoadingIds.delete(id);
        this.cdr.markForCheck();
      }
    }));
  }

  /** HEIC/HEIF 圖片轉換為 JPEG（iPhone 預設格式瀏覽器無法顯示） */
  private async _convertHeicIfNeeded(file: File): Promise<File> {
    const name = file.name.toLowerCase();
    if (!name.endsWith('.heic') && !name.endsWith('.heif')) return file;
    try {
      const blob = await heic2any({blob: file, toType: 'image/jpeg', quality: 0.85}) as Blob;
      const jpegName = file.name.replace(/\.heic$/i, '.jpg').replace(/\.heif$/i, '.jpg');
      return new File([blob], jpegName, {type: 'image/jpeg'});
    } catch {
      return file; // 轉換失敗則使用原檔
    }
  }

  removeInvoice(i: number) {
    const ctrl = this.invoiceArray.at(i);
    const id  = ctrl.get('id')?.value as string;
    const url = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    this.invoiceWarnings.delete(id);
    this.invoiceArray.removeAt(i);
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再儲存。');
      return;
    }
    if (this.invoiceArray.length === 0) {this.showInvoiceError = true; return;}
    this.showInvoiceError = false;
    const fd = this._buildFormData();
    const obs = this.isEdit
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/payment-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 列印請款單 PDF */
  printPayment() {
    if (this.approvalTask) this.pdfService.printPaymentRequest(this.approvalTask);
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再送出。');
      return;
    }
    if (this.invoiceArray.length === 0) {this.showInvoiceError = true; return;}
    this.showInvoiceError = false;
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
    const save$ = this.isEdit
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    this.errorMsg.set('');
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => this._onSubmitted(['/admin/payment-requests']),
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

  private _onSubmitted(target: unknown[]) {
    const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
    ref.componentInstance.formType = 'payment_request';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildFormData(): FormData {
    const fd = new FormData();
    const type = this.form.get('type')!.value!;
    fd.append('type', type);
    fd.append('projectId', String(this.form.get('projectId')!.value));
    fd.append('reason', this.form.get('reason')?.value || '');
    // vendorId：永遠帶入（含 type=vendor 必填、其他類型回傳空字串讓後端強制清空）
    const vendorId = this.form.get('vendorId')?.value;
    fd.append('vendorId', type === 'vendor' && vendorId ? String(vendorId) : '');

    // 指定審核者清單（從 picker payload 組成，含 approvalStepOrder 與 selectedDepartmentId）
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    const invoicesMeta: any[] = [];
    let fileIndex = 0;

    for (const ctrl of this.invoiceArray.controls) {
      const id = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      const meta = {
        fileName:    ctrl.get('fileName')?.value,
        invoiceNo:   ctrl.get('invoiceNo')?.value,
        invoiceDate: ctrl.get('invoiceDate')?.value || null,
        amount:      +(ctrl.get('amount')?.value || 0),
        itemName:    ctrl.get('itemName')?.value || null,
        note:        ctrl.get('note')?.value || null,
        fileUrl:     ctrl.get('fileUrl')?.value || null,
        fileIndex:   file ? fileIndex : -1,
      };
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
      invoicesMeta.push(meta);
    }

    fd.append('invoices', JSON.stringify(invoicesMeta));

    // 整單批次附件：廠商請款 / 一般請款皆帶入
    const att = this.attachmentsUpload();
    const attMeta = att ? att.getMeta() : [];
    fd.append('attachments', JSON.stringify(attMeta));
    if (att) {
      att.getNewFiles().forEach(f => fd.append('attachmentFiles', f, f.name));
    }
    return fd;
  }

  private _invoiceGroup(id: string, fileName: string, invoiceNo: string, amount: number, previewUrl = '', fileUrl = '', itemName = '', note = '', invoiceDate = '') {
    return this.fb.group({
      id:          [id],
      fileName:    [fileName],
      invoiceNo:   [invoiceNo, Validators.required],
      invoiceDate: [invoiceDate],
      amount:      [amount, [Validators.required, Validators.min(0)]],
      itemName:    [itemName],
      note:        [note],
      previewUrl:  [previewUrl],
      fileUrl:     [fileUrl],
    });
  }

}
