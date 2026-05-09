import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
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
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, ApprovalTask} from '../../../approval-tasks/models/approval-task.model';
import {PaymentRequestService} from '../../services/payment-request.service';
import {PaymentPdfService} from '../../services/payment-pdf.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {PaymentType, ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, DesignatedReviewer} from '../../models/payment-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {VendorService} from '../../../vendors/services/vendor.service';
import {VendorLookup} from '../../../vendors/models/vendor.model';
import {VendorQuickAddModal} from '../../../vendors/components/vendor-quick-add-modal/vendor-quick-add-modal';
import {NgbModal, NgbTypeahead, NgbTypeaheadSelectItemEvent} from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-payment-form',
  templateUrl: './payment-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, FilePreviewModal, ApprovalTimeline, NgbTypeahead],
})
export class PaymentForm implements OnInit {
  private fb           = inject(FormBuilder);
  private service      = inject(PaymentRequestService);
  private projects$    = inject(ProjectService);
  private jobTitleSvc  = inject(JobTitleService);
  private userSvc      = inject(UserService);
  private approvalSvc  = inject(ApprovalService);
  private taskSvc      = inject(ApprovalTaskService);
  private vendorSvc    = inject(VendorService);
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private cdr          = inject(ChangeDetectorRef);
  private sanitizer    = inject(DomSanitizer);
  private modal        = inject(NgbModal);
  pdfService           = inject(PaymentPdfService);

  successModal = viewChild<TemplateRef<any>>('successModal');

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
  estimatedPaymentDate = '';
  paidAt = '';

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';
  approvalTask: ApprovalTask | null = null;

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];

  /** 廠商下拉清單（type=vendor 時顯示，僅含 IsActive 廠商） */
  vendors = signal<VendorLookup[]>([]);

  /** 廠商 typeahead 雙向綁定值（選定後為 VendorLookup，輸入過程為 string） */
  vendorTypeaheadModel: VendorLookup | string | null = null;

  /** 廠商 autocomplete 搜尋（依名稱 / 統編，最多 10 筆） */
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
              (v.taxId ?? '').toLowerCase().includes(t));
        return filtered.slice(0, 10);
      })
    );

  /** 下拉項目顯示格式：「名稱（統編）」 */
  vendorFormatter = (v: VendorLookup) => v.name + (v.taxId ? `（${v.taxId}）` : '');
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

  /** 指定審核者條目清單（多人） */
  designatedEntries: {
    stepOrder: number;
    selectedJobTitleId: number | null;
    selectedUserId: string | null;
    filteredUsers: UserLookup[];
  }[] = [];

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
    // 重新排序 stepOrder
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
    reason:    [''],
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
      this.hasDesignatedStep = flow?.steps.some(s => s.useApplicantDesignated) ?? false;
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; } });
        this.userSvc.getLookup().subscribe({
          next: users => {
            this.allUsers = users;
            // allUsers 載入後補填 selectedJobTitleId 與 filteredUsers
            this.designatedEntries.forEach(e => {
              if (!e.selectedJobTitleId && e.selectedUserId) {
                e.selectedJobTitleId = users.find(u => u.id === e.selectedUserId)?.jobTitleId ?? null;
              }
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
        this.estimatedPaymentDate = r.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
        this.paidAt               = r.paidAt?.toString().slice(0, 10) ?? '';
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
        // 回填指定審核者清單
        if (r.designatedReviewers?.length) {
          this.designatedEntries = r.designatedReviewers.map(dr => ({
            stepOrder: dr.stepOrder,
            selectedJobTitleId: this.allUsers.find(u => u.id === dr.reviewerId)?.jobTitleId ?? null,
            selectedUserId: dr.reviewerId,
            filteredUsers: [],  // allUsers 載入後再補填
          }));
          // 若 allUsers 已載入則立即補填 filteredUsers
          if (this.allUsers.length > 0) {
            this.designatedEntries.forEach(e => {
              if (e.selectedJobTitleId) {
                e.filteredUsers = this.allUsers.filter(u => u.jobTitleId === e.selectedJobTitleId && u.status === 'active');
              }
            });
          }
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

    // 使用後端 Claude Haiku API 辨識發票（並行處理所有檔案）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const result = await firstValueFrom(this.service.ocrInvoice(file));
        const idx = this.invoiceArray.controls.findIndex(c => c.get('id')?.value === id);
        if (idx >= 0) this.invoiceArray.controls[idx].patchValue({
          invoiceNo:   result.invoiceNo ?? '',
          amount:      result.amount ?? 0,
          invoiceDate: result.invoiceDate ?? '',
          ...(result.docType === 'ticket' ? { note: '票號' } : {}),
        });
      } catch {
        // OCR failed — leave fields empty for manual entry
      } finally {
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
    this.invoiceArray.removeAt(i);
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid) return;
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
    if (this.form.invalid) return;
    if (this.invoiceArray.length === 0) {this.showInvoiceError = true; return;}
    this.showInvoiceError = false;
    // 流程含「申請人指定審核」步驟時，至少需要 1 位指定審核者（fail-fast，避免送出後才被後端擋下）
    if (this.hasDesignatedStep) {
      const validEntries = this.designatedEntries.filter(e => e.selectedUserId);
      if (validEntries.length === 0) {
        this.errorMsg.set('此簽核流程包含申請人指定審核步驟，請於下方「指定審核者」區塊新增至少 1 位審核者。');
        return;
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
          next: () => {
            const tpl = this.successModal();
            if (tpl) {
              const ref = this.modal.open(tpl, { centered: true, backdrop: 'static', keyboard: false });
              ref.result.then(() => this.router.navigate(['/admin/payment-requests']))
                        .catch(() => this.router.navigate(['/admin/payment-requests']));
            } else {
              this.router.navigate(['/admin/payment-requests']);
            }
          },
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

  private _buildFormData(): FormData {
    const fd = new FormData();
    const type = this.form.get('type')!.value!;
    fd.append('type', type);
    fd.append('projectId', String(this.form.get('projectId')!.value));
    fd.append('reason', this.form.get('reason')?.value || '');
    // vendorId：永遠帶入（含 type=vendor 必填、其他類型回傳空字串讓後端強制清空）
    const vendorId = this.form.get('vendorId')?.value;
    fd.append('vendorId', type === 'vendor' && vendorId ? String(vendorId) : '');
    // 指定審核者清單
    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({ reviewerId: e.selectedUserId!, stepOrder: e.stepOrder }));
    if (reviewers.length > 0) fd.append('designatedReviewers', JSON.stringify(reviewers));

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
