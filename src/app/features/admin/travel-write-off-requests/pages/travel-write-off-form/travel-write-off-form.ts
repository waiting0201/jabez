import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {TravelWriteOffRequestService} from '../../services/travel-write-off-request.service';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';
import {TravelSummary, ITEM_CATEGORIES} from '../../models/travel-write-off-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';

@Component({
  selector: 'app-travel-write-off-form',
  templateUrl: './travel-write-off-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, FilePreviewModal],
})
export class TravelWriteOffForm implements OnInit {
  private fb             = inject(FormBuilder);
  private service        = inject(TravelWriteOffRequestService);
  private paymentService = inject(PaymentRequestService);
  private jobTitleSvc    = inject(JobTitleService);
  private userSvc        = inject(UserService);
  private approvalSvc    = inject(ApprovalService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private sanitizer      = inject(DomSanitizer);

  /** undefined = 新增模式；數值 = 編輯模式（出差沖銷申請 ID） */
  editId: number | null = null;
  isEdit = false;

  /** 選擇的出差申請 ID（新增模式中由使用者選擇） */
  selectedTravelId: number | null = null;

  /** 已核准的出差申請清單（供新增模式下拉選擇） */
  travelRequests = signal<TravelSummary[]>([]);

  /** 選中的出差申請摘要（供右側顯示金額資訊） */
  get selectedTravel(): TravelSummary | null {
    return this.travelRequests().find(a => a.id === this.selectedTravelId) ?? null;
  }
  loadingTravels = true;

  /** 編輯模式時顯示的出差申請資訊（唯讀） */
  editModeTravelNo = '';
  editModeDestination = '';
  editModeDateRange = '';
  editModeTravelGrandTotal = 0;
  editModeTravelWrittenOffTotal = 0;

  errorMsg = signal('');
  categories = ITEM_CATEGORIES;

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

  /** invoice id → File 物件（新上傳的檔案） */
  fileMap = new Map<string, File>();

  /** IDs of rows currently being OCR-processed */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** File preview modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  form = this.fb.group({
    note:  [''],
    items: this.fb.array([]),
  });

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }

  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  ngOnInit() {
    // 檢查簽核流程是否有「申請人指定審核」步驟
    this.approvalSvc.getAll().subscribe(items => {
      this.hasDesignatedStep = items
        .filter(i => i.isActive && i.applicationType === 'travel_write_off')
        .some(i => i.steps.some(s => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; } });
        this.userSvc.getLookup().subscribe({
          next: users => {
            this.allUsers = users;
            this.designatedEntries.forEach(e => {
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

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      // 編輯模式：載入現有出差沖銷申請
      this.isEdit = true;
      this.editId = +idParam;
      this.service.getById(this.editId).subscribe(r => {
        this.editModeTravelNo    = r.travelRequestNo;
        this.editModeDestination = r.destination;
        this.editModeDateRange   = `${r.startDate?.slice(0, 10) ?? ''} ~ ${r.endDate?.slice(0, 10) ?? ''}`;
        this.selectedTravelId    = r.travelRequestId;
        this.editModeTravelGrandTotal      = r.travelGrandTotal;
        this.editModeTravelWrittenOffTotal  = r.travelWrittenOffTotal;
        this.form.patchValue({note: r.note ?? ''});
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
        // 回填明細行（保留既有檔案 URL）
        r.items.forEach((item, idx) => {
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
            '',           // 無本地 blob URL
            item.fileUrl ?? '',
          ));
          const ctrl = this.itemArray.at(idx);
          ctrl.patchValue({invoiceNo: item.invoiceNo ?? '', invoiceDate: item.invoiceDate ?? '', category: item.category});
        });
        this.cdr.markForCheck();
      });
    } else {
      // 新增模式：載入已核准的出差申請清單
      this.service.getAvailableTravels().subscribe({
        next: list => {
          this.travelRequests.set(list);
          this.loadingTravels = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.loadingTravels = false;
          this.errorMsg.set('載入出差申請清單失敗。');
          this.cdr.markForCheck();
        },
      });
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
        const result = await firstValueFrom(this.paymentService.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        if (idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo:   result.invoiceNo ?? '',
            invoiceDate: result.invoiceDate ?? '',
            unitPrice:   result.amount ?? 0,
            totalPrice:  result.amount ?? 0,
            quantity:    '1式',
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

  calcTotal(ctrl: AbstractControl) {
    const unitPrice = +(ctrl.get('unitPrice')?.value) || 0;
    const qtyStr = (ctrl.get('quantity')?.value ?? '').toString();
    const qtyNum = parseFloat(qtyStr) || 0;
    const total = Math.round(unitPrice * qtyNum);
    ctrl.get('totalPrice')?.setValue(total, {emitEvent: false});
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.itemArray.length === 0) return;
    if (!this.isEdit && !this.selectedTravelId) {
      this.errorMsg.set('請選擇出差單。');
      return;
    }
    const fd = this._buildFormData();
    this.errorMsg.set('');
    const obs = this.isEdit
      ? this.service.update(this.editId!, fd)
      : this.service.create(fd);
    obs.subscribe({
      next: () => this.router.navigate(['/admin/travel-write-off-requests']),
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。'),
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.itemArray.length === 0) return;
    if (!this.isEdit && !this.selectedTravelId) {
      this.errorMsg.set('請選擇出差單。');
      return;
    }
    const fd = this._buildFormData();
    this.errorMsg.set('');
    const save$ = this.isEdit
      ? this.service.update(this.editId!, fd)
      : this.service.create(fd);
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => this.router.navigate(['/admin/travel-write-off-requests']),
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
    fd.append('note', this.form.get('note')?.value || '');

    // 新增模式需帶入 travelRequestId
    if (!this.isEdit && this.selectedTravelId) {
      fd.append('travelRequestId', String(this.selectedTravelId));
    }

    // 指定審核者
    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({ reviewerId: e.selectedUserId!, stepOrder: e.stepOrder }));
    if (reviewers.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(reviewers));
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
    id: string, fileName: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number,
    note: string, sortOrder: number, previewUrl = '', fileUrl = ''
  ) {
    return this.fb.group({
      id:          [id || `${Date.now()}-${Math.random().toString(36).slice(2)}`],
      fileName:    [fileName],
      invoiceNo:   [''],
      invoiceDate: [''],
      category:    [''],
      seqNo:       [seqNo],
      itemName:    [itemName],
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
