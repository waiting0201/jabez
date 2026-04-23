import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {HolidayTravelRequestService} from '../../services/holiday-travel-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {
  ApprovalStatus,
  APPROVAL_STATUS_LABELS,
  APPROVAL_STATUS_CLASSES,
  ITEM_CATEGORIES,
  DesignatedReviewer,
  HolidayTravelRequest,
  TravelParticipant,
} from '../../models/holiday-travel-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';

@Component({
  selector: 'app-holiday-travel-request-form',
  templateUrl: './holiday-travel-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline, FilePreviewModal],
})
export class HolidayTravelRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(HolidayTravelRequestService);
  private projects$   = inject(ProjectService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private paymentSvc  = inject(PaymentRequestService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);
  private modal       = inject(NgbModal);
  private sanitizer   = inject(DomSanitizer);

  successModal = viewChild<TemplateRef<any>>('successModal');

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  existingRequest: HolidayTravelRequest | null = null;
  errorMsg = signal('');
  projects: Project[] = [];
  loadingProjects = true;
  categories = ITEM_CATEGORIES;

  /** 假日天數（從行事曆 API 查詢） */
  holidayDays = signal<number | null>(null);
  holidayDaysLoading = signal(false);
  holidayDaysNoCalendar = signal(false);

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];

  /** 指定審核者條目清單（多人） */
  designatedEntries: {
    stepOrder: number;
    selectedJobTitleId: number | null;
    selectedUserId: string | null;
    filteredUsers: UserLookup[];
  }[] = [];

  /** 參與執行人員清單 */
  participantEntries: {
    sortOrder: number;
    selectedUserId: string | null;
  }[] = [];

  /** 發票檔案 id → File 物件（新上傳） */
  fileMap = new Map<string, File>();

  /** 正在 OCR 處理中的 row ids */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** 檔案預覽 modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    destination: ['', Validators.required],
    startDate:   ['', Validators.required],
    endDate:     ['', Validators.required],
    purpose:     ['', Validators.required],
    projectId:   [null as number | null],
    items:       this.fb.array([]),
  });

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }

  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  /** 按鈕 disabled 時的提示訊息，null 表示可提交 */
  get disabledReason(): string | null {
    if (this.isAnyOcrPending) return '發票辨識中，請稍候…';
    if (this.itemArray.length === 0) return '請新增至少一筆費用明細。';
    if (this.form.invalid) {
      const fields: [string, string][] = [
        ['destination', '執行活動地點'], ['startDate', '開始日期'],
        ['endDate', '結束日期'], ['purpose', '活動主旨及內容'],
      ];
      for (const [key, label] of fields) {
        if (this.form.get(key)?.invalid) return `請填寫「${label}」。`;
      }
      const idx = this.itemControls.findIndex(c => c.get('itemName')?.invalid);
      if (idx >= 0) return `第 ${idx + 1} 筆費用明細的「項目說明」未填寫。`;
      return '表單資料不完整，請檢查必填欄位。';
    }
    return null;
  }

  /** 日期變更時查詢假日天數 */
  onDateChange() {
    const v = this.form.value;
    if (!v.startDate || !v.endDate) {
      this.holidayDays.set(null);
      return;
    }
    this.holidayDaysLoading.set(true);
    this.holidayDaysNoCalendar.set(false);
    this.service.countHolidays(v.startDate, v.endDate).subscribe({
      next: res => {
        this.holidayDays.set(res.holidayDays);
        this.holidayDaysNoCalendar.set(!res.hasCalendarData);
        this.holidayDaysLoading.set(false);
      },
      error: () => {
        this.holidayDays.set(null);
        this.holidayDaysLoading.set(false);
      },
    });
  }

  // ── 指定審核者操作 ──

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

  // ── 參與執行人員操作 ──

  addParticipant() {
    const nextOrder = this.participantEntries.length + 1;
    this.participantEntries.push({sortOrder: nextOrder, selectedUserId: null});
  }

  removeParticipant(i: number) {
    this.participantEntries.splice(i, 1);
    this.participantEntries.forEach((e, idx) => e.sortOrder = idx + 1);
  }

  // ── 發票上傳 / OCR ──

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
      this.itemArray.push(this._itemGroup(id, file.name, 0, '', 0, '', 0, '', this.itemArray.length, previewUrl));
      return {id, file};
    });

    // OCR 辨識（並行）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const result = await firstValueFrom(this.paymentSvc.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        if (idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo:   result.invoiceNo ?? '',
            invoiceDate: result.invoiceDate ?? '',
            unitPrice:   result.amount ?? 0,
            totalPrice:  result.amount ?? 0,
            quantity:    '1式',
            itemName:    result.invoiceNo ? `發票 ${result.invoiceNo}` : file.name,
            ...(result.docType === 'ticket' ? { note: '票號' } : {}),
          });
        }
      } catch {
        // OCR 失敗 — 保留空白欄位
      } finally {
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

  addItem() {
    this.itemArray.push(this._itemGroup('', '', 0, '', 0, '', 0, '', this.itemArray.length));
  }

  removeItem(i: number) {
    const ctrl = this.itemArray.at(i);
    const id  = ctrl.get('id')?.value as string;
    const url = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
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

  ngOnInit() {
    // 載入使用者清單（用於指定審核者與參與執行人員）
    this.userSvc.getLookup().subscribe({
      next: users => {
        this.allUsers = users;
        this.cdr.markForCheck();
      },
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟
    this.approvalSvc.getAll().subscribe(items => {
      this.hasDesignatedStep = items
        .filter(i => i.isActive && i.applicationType === 'holiday_travel')
        .some(i => i.steps.some(s => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; } });
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
        this.form.patchValue({
          destination: r.destination,
          startDate: r.startDate instanceof Date
            ? r.startDate.toISOString().split('T')[0]
            : String(r.startDate),
          endDate: r.endDate instanceof Date
            ? r.endDate.toISOString().split('T')[0]
            : String(r.endDate),
          purpose:   r.purpose,
          projectId: r.projectId ?? null,
        });

        // 回填日期後查詢假日天數
        this.onDateChange();

        // 回填參與執行人員
        if (r.participants?.length) {
          this.participantEntries = r.participants
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map(p => ({sortOrder: p.sortOrder, selectedUserId: p.userId}));
        }

        // 回填指定審核者清單
        if (r.designatedReviewers?.length) {
          this.designatedEntries = r.designatedReviewers.map(dr => ({
            stepOrder: dr.stepOrder,
            selectedJobTitleId: this.allUsers.find(u => u.id === dr.reviewerId)?.jobTitleId ?? null,
            selectedUserId: dr.reviewerId,
            filteredUsers: [],
          }));
          if (this.allUsers.length > 0) {
            this.designatedEntries.forEach(e => {
              if (e.selectedJobTitleId) {
                e.filteredUsers = this.allUsers.filter(u => u.jobTitleId === e.selectedJobTitleId && u.status === 'active');
              }
            });
          }
        }

        // 回填費用明細（含發票資訊）
        (r.items ?? []).forEach((item, idx) => {
          this.itemArray.push(this._itemGroup(
            `existing-${item.id}`,
            item.fileName ?? '',
            item.seqNo,
            item.itemName,
            item.unitPrice,
            item.quantity,
            item.totalPrice,
            item.note ?? '',
            idx,
            '',
            item.fileUrl ?? '',
          ));
          const ctrl = this.itemArray.at(idx);
          ctrl.patchValue({
            invoiceNo:  item.invoiceNo ?? '',
            invoiceDate: item.invoiceDate ?? '',
            category:   item.category,
          });
        });

        if (this.isReadOnly) this.form.disable();

        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'holiday_travel').subscribe({
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
    }
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly) return;
    const fd = this._buildFormData();
    const obs = this.isEdit
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/holiday-travel-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid || this.itemArray.length === 0 || this.isReadOnly) return;
    const fd = this._buildFormData();
    const save$ = this.isEdit
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    this.errorMsg.set('');
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => {
            const tpl = this.successModal();
            if (tpl) {
              const ref = this.modal.open(tpl, { centered: true, backdrop: 'static', keyboard: false });
              ref.result
                .then(() => this.router.navigate(['/admin/holiday-travel-requests']))
                .catch(() => this.router.navigate(['/admin/holiday-travel-requests']));
            } else {
              this.router.navigate(['/admin/holiday-travel-requests']);
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
    const v = this.form.value;
    const project = this.projects.find(p => p.id === v.projectId);
    const fd = new FormData();

    fd.append('destination',   v.destination!);
    fd.append('startDate',     v.startDate!);
    fd.append('endDate',       v.endDate!);
    fd.append('purpose',       v.purpose!);
    if (v.projectId) {
      fd.append('projectId',   String(v.projectId));
      if (project?.code) fd.append('projectCode', project.code);
    }

    // 參與執行人員
    const participants = this.participantEntries
      .filter(e => e.selectedUserId)
      .map(e => ({userId: e.selectedUserId!, sortOrder: e.sortOrder}));
    if (participants.length > 0) {
      fd.append('participants', JSON.stringify(participants));
    }

    // 指定審核者
    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({reviewerId: e.selectedUserId!, stepOrder: e.stepOrder}));
    if (reviewers.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(reviewers));
    }

    // 費用明細（含發票附件）
    const itemsMeta: object[] = [];
    let fileIndex = 0;

    for (let i = 0; i < this.itemArray.controls.length; i++) {
      const ctrl = this.itemArray.at(i);
      const rowId  = ctrl.get('id')?.value as string;
      const file   = this.fileMap.get(rowId);
      itemsMeta.push({
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
        sortOrder:   i,
      });
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
    }

    const grandTotal = this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
    fd.append('grandTotal', String(grandTotal));
    fd.append('items', JSON.stringify(itemsMeta));

    return fd;
  }

  private _itemGroup(
    id: string, fileName: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, note: string, sortOrder: number,
    previewUrl = '', fileUrl = ''
  ) {
    return this.fb.group({
      id:          [id || `${Date.now()}-${Math.random().toString(36).slice(2)}`],
      fileName:    [fileName],
      invoiceNo:   [''],
      invoiceDate: [''],
      category:    [''],
      seqNo:       [seqNo],
      itemName:    [itemName, Validators.required],
      unitPrice:   [unitPrice, [Validators.min(0)]],
      quantity:    [quantity],
      totalPrice:  [totalPrice],
      note:        [note],
      previewUrl:  [previewUrl],
      fileUrl:     [fileUrl],
      sortOrder:   [sortOrder],
    });
  }
}
