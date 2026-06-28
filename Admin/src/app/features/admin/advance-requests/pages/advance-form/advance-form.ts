import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES, DesignatedReviewer} from '../../models/advance-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord, InstallmentDto, PaymentInstallmentStatus} from '../../../approval-tasks/models/approval-task.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import heic2any from 'heic2any';

@Component({
  selector: 'app-advance-form',
  templateUrl: './advance-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline, FilePreviewModal, InstallmentsTable],
})
export class AdvanceForm implements OnInit {
  private fb             = inject(FormBuilder);
  private service        = inject(AdvanceRequestService);
  private projectService = inject(ProjectService);
  private jobTitleSvc    = inject(JobTitleService);
  private userSvc        = inject(UserService);
  private approvalSvc    = inject(ApprovalService);
  private taskSvc        = inject(ApprovalTaskService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private modal          = inject(NgbModal);
  private sanitizer      = inject(DomSanitizer);
  successModal = viewChild<TemplateRef<any>>('successModal');

  projects: Project[] = [];
  loadingProjects = true;
  isEdit     = false;
  isReadOnly = false;
  isReturned = false;
  requestId  = 0;
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

  /** 分期撥款（read-only 顯示用，財務排定後申請人可查看）*/
  installments: InstallmentDto[] | null = null;
  paymentStatus: PaymentInstallmentStatus | null = null;
  loadedGrandTotal = 0;

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

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    projectId:      [null as number | null, Validators.required],
    activityName:   ['', Validators.required],
    activityPeriod: ['', Validators.required],
    advanceDate:    ['', Validators.required],
    items:          this.fb.array([]),
  });

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
      this.hasDesignatedStep = flow?.steps.some(s => s.useApplicantDesignated) ?? false;
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; } });
        this.userSvc.getLookup().subscribe({
          next: users => {
            this.allUsers = users;
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

    this.projectService.getActive().subscribe({
      next: p => { this.projects = p; this.loadingProjects = false; this.cdr.markForCheck(); },
      error: () => { this.loadingProjects = false; this.errorMsg.set('載入專案資料失敗。'); },
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.projectCode = r.projectCode ?? '';
        this.projectName = r.projectName ?? '';
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({
          projectId:      r.projectId,
          activityName:   r.activityName,
          activityPeriod: r.activityPeriod,
          advanceDate:    r.advanceDate?.toString().slice(0, 10),
        });
        this.installments    = r.installments ?? null;
        this.paymentStatus   = r.paymentStatus ?? null;
        this.loadedGrandTotal = r.grandTotal ?? 0;
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
              this.cdr.markForCheck();
            },
          });
        }
        this.cdr.markForCheck();
      });
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
    ctrl.get('totalPrice')?.setValue(total, {emitEvent: false});
    ctrl.get('cashAmount')?.setValue(total, {emitEvent: false});
  }

  save() {
    if (this.form.invalid || this.itemArray.length === 0) return;
    const fd = this._buildFormData();
    const obs = this.isEdit
      ? this.service.updateWithFiles(this.requestId, fd)
      : this.service.createWithFiles(fd);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/advance-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  submitForApproval() {
    if (this.form.invalid || this.itemArray.length === 0) return;
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
              ref.result.then(() => this.router.navigate(['/admin/advance-requests']))
                        .catch(() => this.router.navigate(['/admin/advance-requests']));
            } else {
              this.router.navigate(['/admin/advance-requests']);
            }
          },
          error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '送出失敗。'),
        });
      },
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '儲存失敗。'),
    });
  }

  private _buildFormData(): FormData {
    const fd = new FormData();
    fd.append('projectId', String(this.form.get('projectId')!.value));
    fd.append('activityName', this.form.get('activityName')?.value || '');
    fd.append('activityPeriod', this.form.get('activityPeriod')?.value || '');
    fd.append('advanceDate', this.form.get('advanceDate')?.value || '');

    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({ reviewerId: e.selectedUserId!, stepOrder: e.stepOrder }));
    if (reviewers.length > 0) fd.append('designatedReviewers', JSON.stringify(reviewers));

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
    return fd;
  }

  private _itemGroup(
    category: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, cashAmount: number, checkAmount: number,
    note: string, sortOrder: number, fileName = '', fileUrl = ''
  ) {
    return this.fb.group({
      id:          [`${Date.now()}-${Math.random().toString(36).slice(2)}`],
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
