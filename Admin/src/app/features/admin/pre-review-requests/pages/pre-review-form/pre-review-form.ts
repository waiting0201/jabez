import {ChangeDetectorRef, Component, inject, OnInit, signal, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom, Observable, OperatorFunction} from 'rxjs';
import {debounceTime, distinctUntilChanged, map} from 'rxjs/operators';
import heic2any from 'heic2any';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {AttachmentsUpload} from '../../../../../shared/components/attachments-upload';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {AttachmentItem, PendingReviewer} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {PreReviewRequestService, QuoteOcrItem} from '../../services/pre-review-request.service';
import {PreReviewPdfService} from '../../services/pre-review-pdf.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {PaymentType, ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, DesignatedReviewer, ITEM_CATEGORIES, ItemCategory} from '../../models/pre-review-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {VendorService} from '../../../vendors/services/vendor.service';
import {VendorLookup} from '../../../vendors/models/vendor.model';
import {VendorQuickAddModal} from '../../../vendors/components/vendor-quick-add-modal/vendor-quick-add-modal';
import {NgbModal, NgbTypeahead, NgbTypeaheadSelectItemEvent} from '@ng-bootstrap/ng-bootstrap';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-pre-review-form',
  templateUrl: './pre-review-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, FilePreviewModal, ApprovalTimeline, NgbTypeahead, AttachmentsUpload, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class PreReviewForm implements OnInit {
  private fb           = inject(FormBuilder);
  private service      = inject(PreReviewRequestService);
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
  pdfService           = inject(PreReviewPdfService);

  attachmentsUpload = viewChild(AttachmentsUpload);

  /** 編輯模式回填的既有附件 */
  loadedAttachments: AttachmentItem[] = [];

  projects: Project[] = [];
  /** 路由模式旗標（僅影響版面呈現），create 成功後不改動 */
  isEdit     = false;
  isReturned = false;
  isReadOnly = false;
  /** 後端已存在的申請單 ID（編輯模式進場即有；新增模式 create 成功後填入）；> 0 即代表要走 update */
  requestId  = 0;
  showItemsError = false;
  errorMsg = signal('');
  /** 儲存 / 送出進行中：鎖按鈕 + spinner，避免 multipart 上傳期間連按建出多張單（見 docs/frontend-design.md §8.4.1） */
  saving = signal(false);
  approvalStatus: ApprovalStatus = 'draft';
  isDraft    = true;
  projectCode = '';
  projectName = '';
  requestNo   = '';

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  /** 目前關卡的待簽核者（後端解析；空陣列＝查無可簽核人員）*/
  pendingReviewers: PendingReviewer[] = [];
  approvalTask: ApprovalTask | null = null;

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

  /** 廠商下拉清單 */
  vendors = signal<VendorLookup[]>([]);
  vendorTypeaheadModel: VendorLookup | string | null = null;

  /** 品項類別常數 */
  readonly itemCategories: ItemCategory[] = [...ITEM_CATEGORIES];

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

  vendorFormatter = (v: VendorLookup) => {
    const code = v.taxId ?? v.idNumber;
    return v.name + (code ? `（${code}）` : '');
  };
  vendorInputFormatter = (v: VendorLookup) => v.name;

  onVendorSelect(event: NgbTypeaheadSelectItemEvent) {
    const v = event.item as VendorLookup;
    this.form.get('vendorId')!.setValue(v.id);
  }

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

  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

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

  /** item id → File 物件（新上傳的檔案） */
  fileMap = new Map<string, File>();

  /** IDs of item rows currently being OCR-processed */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

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
    vendorId:  [null as number | null, Validators.required],
    reason:    ['', Validators.required],
    taxAmount: [0],
    items:     this.fb.array([]),
  });

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }
  get totalAmount(): number {
    return this.itemArray.controls.reduce((sum, c) => sum + (+(c.get('amount')?.value) || 0), 0);
  }
  get grandTotal(): number {
    const tax = this.form.get('taxAmount');
    return this.totalAmount + (tax ? (+(tax.value ?? 0) || 0) : 0);
  }

  loadingProjects = true;

  ngOnInit() {
    this.vendorSvc.getLookup().subscribe(v => { this.vendors.set(v); this.cdr.markForCheck(); });

    // 協力廠商 / 設計師 皆須選擇廠商，vendorId 永遠必填（初始化已設定），切換類型不清空

    this.approvalSvc.getActiveByType('pre_review').subscribe(flow => {
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
        this.approvalStatus = r.approvalStatus;
        this.isDraft        = r.approvalStatus === 'draft';
        this.isReturned     = r.approvalStatus === 'returned';
        this.isReadOnly     = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.projectCode    = r.projectCode ?? '';
        this.projectName    = r.projectName ?? '';
        this.requestNo      = r.requestNo ?? '';
        this.loadedAttachments = r.attachments ?? [];
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({type: r.type, projectId: r.projectId, reason: r.reason ?? '', vendorId: r.vendorId ?? null, taxAmount: r.taxAmount ?? 0});

        if (r.vendorId && r.vendorName && !this.vendors().some(v => v.id === r.vendorId)) {
          this.vendors.update(list => [...list, {id: r.vendorId!, name: r.vendorName!, taxId: r.vendorTaxId}]);
        }
        if (r.vendorId) {
          const found = this.vendors().find(v => v.id === r.vendorId);
          if (found) this.vendorTypeaheadModel = found;
        }
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }
        // 回填品項列（品項類別需判斷 preset 或 其他）
        r.items.forEach(item => {
          const isPreset = (ITEM_CATEGORIES as readonly string[]).includes(item.itemCategory ?? '');
          const categorySelect = isPreset ? (item.itemCategory ?? '') : '其他';
          const categoryCustom = isPreset ? '' : (item.itemCategory ?? '');
          this.itemArray.push(this._itemGroup(
            String(item.id),
            item.fileName,
            categorySelect,
            categoryCustom,
            item.itemName ?? '',
            item.amount,
            item.fileUrl ?? '',
            item.fileUrl ?? '',
            item.note ?? '',
            item.itemDate?.toString().slice(0, 10) ?? '',
            item.description ?? '',
          ));
        });
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'pre_review').subscribe({
            next: task => {
              this.approvalTask = task;
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

  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const rawFiles = Array.from(input.files);
    input.value = '';
    this.showItemsError = false;

    const files = await Promise.all(rawFiles.map(f => this._convertHeicIfNeeded(f)));

    const entries = files.map(file => {
      const id         = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.fileMap.set(id, file);
      this.itemArray.push(this._itemGroup(id, file.name, '', '', '', 0, previewUrl));
      return {id, file};
    });

    // OCR 識別報價單品項
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const results = await firstValueFrom(this.service.quoteOcr(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        // 第 1 筆填入 placeholder；第 2..N 筆各新增一列（共用同一檔案）
        if (results.length >= 1 && idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            itemName: results[0].itemName ?? '',
            amount:   results[0].amount ?? 0,
            note:     results[0].note ?? '',
          });
        }
        for (const item of results.slice(1)) {
          const newId      = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
          const previewUrl = URL.createObjectURL(file);
          this.fileMap.set(newId, file);
          this.itemArray.push(this._itemGroup(
            newId, file.name, '', '',
            item.itemName ?? '', item.amount ?? 0,
            previewUrl, '', item.note ?? '', '',
          ));
          this.itemArray.at(this.itemArray.length - 1).markAllAsTouched();
        }
      } catch {
        // OCR 失敗 — 保留空列供手動輸入
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

  removeItem(i: number) {
    const ctrl = this.itemArray.at(i);
    const id   = ctrl.get('id')?.value as string;
    const url  = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    this.itemArray.removeAt(i);
  }

  /**
   * 表單內按 Enter 不送出（textarea 換行不受影響）。
   * 否則任一 input 的 Enter 都會觸發 ngSubmit，直接建草稿並跳回列表。
   */
  onEnterKey(event: Event) {
    const tag = (event.target as HTMLElement)?.tagName;
    if (tag !== 'TEXTAREA') event.preventDefault();
  }

  /** 儲存（草稿或更新） */
  save() {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再儲存。');
      return;
    }
    if (this.itemArray.length === 0) {this.showItemsError = true; return;}
    this.showItemsError = false;
    const fd = this._buildFormData();
    // 判斷依據是「後端已有這張單」，不是路由模式：create 成功後重送必須走 update
    const obs = this.requestId
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    this.errorMsg.set('');
    this.saving.set(true);
    obs.subscribe({
      next: saved => {
        this.requestId = saved.id;
        this.router.navigate(['/admin/pre-review-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 列印預審單 PDF */
  printPreReview() {
    if (this.approvalTask) this.pdfService.printPreReviewRequest(this.approvalTask);
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.saving()) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMsg.set('尚有必填欄位未填寫，請檢查紅字標示的欄位後再送出。');
      return;
    }
    if (this.itemArray.length === 0) {this.showItemsError = true; return;}
    this.showItemsError = false;
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
    const save$ = this.requestId
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    this.errorMsg.set('');
    this.saving.set(true);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update，避免同一筆申請被建成兩張單
        this.requestId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => {
            this.saving.set(false);
            this._onSubmitted(['/admin/pre-review-requests']);
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
    const ref = this.modal.open(SubmitSuccessModal, {centered: true, backdrop: 'static', keyboard: false});
    ref.componentInstance.message = '預審申請已送出，等待審核中';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildFormData(): FormData {
    const fd   = new FormData();
    const type = this.form.get('type')!.value!;
    fd.append('type', type);
    fd.append('projectId', String(this.form.get('projectId')!.value));
    fd.append('reason', this.form.get('reason')?.value || '');
    const vendorId = this.form.get('vendorId')?.value;
    fd.append('vendorId', vendorId ? String(vendorId) : '');

    const taxCtrl = this.form.get('taxAmount');
    fd.append('taxAmount', String(taxCtrl ? (+(taxCtrl.value ?? 0) || 0) : 0));

    // 指定審核者清單（從 picker payload 組成，含 approvalStepOrder 與 selectedDepartmentId）
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    const itemsMeta: any[] = [];
    let fileIndex = 0;

    for (const ctrl of this.itemArray.controls) {
      const id   = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      // 解析品項類別：categorySelect === '其他' 時取 categoryCustom
      const categorySelect = ctrl.get('categorySelect')?.value ?? '';
      const categoryCustom = ctrl.get('categoryCustom')?.value ?? '';
      const itemCategory   = categorySelect === '其他' ? categoryCustom : categorySelect;
      const meta = {
        fileName:     ctrl.get('fileName')?.value,
        itemCategory: itemCategory || null,
        itemDate:     ctrl.get('itemDate')?.value || null,
        amount:       +(ctrl.get('amount')?.value || 0),
        itemName:     ctrl.get('itemName')?.value || null,
        description:  ctrl.get('description')?.value || null,
        note:         ctrl.get('note')?.value || null,
        fileUrl:      ctrl.get('fileUrl')?.value || null,
        fileIndex:    file ? fileIndex : -1,
      };
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
      itemsMeta.push(meta);
    }
    fd.append('items', JSON.stringify(itemsMeta));

    const att     = this.attachmentsUpload();
    const attMeta = att ? att.getMeta() : [];
    fd.append('attachments', JSON.stringify(attMeta));
    if (att) {
      att.getNewFiles().forEach(f => fd.append('attachmentFiles', f, f.name));
    }
    return fd;
  }

  private _itemGroup(
    id: string,
    fileName: string,
    categorySelect: string,
    categoryCustom: string,
    itemName: string,
    amount: number,
    previewUrl = '',
    fileUrl = '',
    note = '',
    itemDate = '',
    description = '',
  ) {
    return this.fb.group({
      id:             [id],
      fileName:       [fileName],
      categorySelect: [categorySelect],
      categoryCustom: [categoryCustom],
      itemDate:       [itemDate],
      amount:         [amount, [Validators.required, Validators.min(0)]],
      itemName:       [itemName],
      description:    [description],
      note:           [note],
      previewUrl:     [previewUrl],
      fileUrl:        [fileUrl],
    });
  }
}
