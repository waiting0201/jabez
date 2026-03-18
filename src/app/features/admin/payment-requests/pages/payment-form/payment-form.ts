import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {PaymentRequestService} from '../../services/payment-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {PaymentType, ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/payment-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {User} from '../../../users/models/user.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-payment-form',
  templateUrl: './payment-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, FilePreviewModal, ApprovalTimeline],
})
export class PaymentForm implements OnInit {
  private fb           = inject(FormBuilder);
  private service      = inject(PaymentRequestService);
  private projects$    = inject(ProjectService);
  private jobTitleSvc  = inject(JobTitleService);
  private userSvc      = inject(UserService);
  private approvalSvc  = inject(ApprovalService);
  private taskSvc      = inject(ApprovalTaskService);
  private route        = inject(ActivatedRoute);
  private router       = inject(Router);
  private cdr          = inject(ChangeDetectorRef);
  private sanitizer    = inject(DomSanitizer);
  private modal        = inject(NgbModal);

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

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  jobTitles: JobTitle[] = [];
  allUsers: User[] = [];
  filteredUsers: User[] = [];
  selectedJobTitleId: number | null = null;

  /** 取得已選審核者姓名（用於檢視模式顯示） */
  get designatedReviewerName(): string {
    const id = this.form.get('designatedReviewerId')?.value;
    if (!id) return '—';
    return this.allUsers.find(u => u.id === id)?.name ?? id;
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
    type:                  ['vendor', Validators.required],
    projectId:             [null as number | null, Validators.required],
    designatedReviewerId:  [null as string | null],
    invoices:              this.fb.array([]),
  });

  get invoiceArray(): FormArray { return this.form.get('invoices') as FormArray; }
  get invoiceControls(): AbstractControl[] { return this.invoiceArray.controls; }
  get totalAmount(): number {
    return this.invoiceArray.controls.reduce((sum, c) => sum + (+(c.get('amount')?.value) || 0), 0);
  }

  loadingProjects = true;

  ngOnInit() {
    // 檢查簽核流程是否有「申請人指定審核」步驟
    this.approvalSvc.getAll().subscribe(items => {
      this.hasDesignatedStep = items
        .filter(i => i.isActive && i.applicationType === 'payment_request')
        .some(i => i.steps.some(s => s.useApplicantDesignated));
      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getAll().subscribe({ next: jts => { this.jobTitles = jts; } });
        this.userSvc.getAll().subscribe({ next: users => { this.allUsers = users; } });
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
        this.isReadOnly     = r.approvalStatus !== 'draft';
        this.projectCode    = r.projectCode ?? '';
        this.projectName    = r.projectName ?? '';
        this.estimatedPaymentDate = r.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
        this.paidAt               = r.paidAt?.toString().slice(0, 10) ?? '';
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({type: r.type, projectId: r.projectId, designatedReviewerId: r.designatedReviewerId ?? null});
        // 回填已選審核者的職稱
        if (r.designatedReviewerId) {
          this._prefillDesignatedJobTitle(r.designatedReviewerId);
        }
        r.invoices.forEach(inv => this.invoiceArray.push(
          this._invoiceGroup(String(inv.id), inv.fileName, inv.invoiceNo, inv.amount, inv.fileUrl ?? '', inv.fileUrl ?? '', inv.itemName ?? '', inv.note ?? '')
        ));
        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'payment_request').subscribe({
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

  /** 根據 designatedReviewerId 回填職稱下拉，並篩選人員清單 */
  private _prefillDesignatedJobTitle(userId: string) {
    // 等待 allUsers 載入後再處理
    const tryPrefill = () => {
      const user = this.allUsers.find(u => u.id === userId);
      if (user?.jobTitleId) {
        this.selectedJobTitleId = user.jobTitleId;
        this.filteredUsers = this.allUsers.filter(u => u.jobTitleId === user.jobTitleId && u.status === 'active');
      }
      this.cdr.markForCheck();
    };
    if (this.allUsers.length > 0) {
      tryPrefill();
    } else {
      // allUsers 尚未載入，稍後重試
      const sub = this.userSvc.getAll().subscribe(users => {
        this.allUsers = users;
        tryPrefill();
        sub.unsubscribe();
      });
    }
  }

  /** 職稱選擇變更，篩選可選人員並清除已選審核者 */
  onDesignatedJobTitleChange() {
    if (this.selectedJobTitleId == null) {
      this.filteredUsers = [];
    } else {
      this.filteredUsers = this.allUsers.filter(
        u => u.jobTitleId === this.selectedJobTitleId && u.status === 'active'
      );
    }
    this.form.get('designatedReviewerId')?.setValue(null);
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
          invoiceNo: result.invoiceNo ?? '',
          amount:    result.amount ?? 0,
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

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid) return;
    if (this.invoiceArray.length === 0) {this.showInvoiceError = true; return;}
    this.showInvoiceError = false;
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
    fd.append('type', this.form.get('type')!.value!);
    fd.append('projectId', String(this.form.get('projectId')!.value));
    const designatedReviewerId = this.form.get('designatedReviewerId')?.value;
    if (designatedReviewerId) fd.append('designatedReviewerId', designatedReviewerId);

    const invoicesMeta: any[] = [];
    let fileIndex = 0;

    for (const ctrl of this.invoiceArray.controls) {
      const id = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      const meta = {
        fileName:  ctrl.get('fileName')?.value,
        invoiceNo: ctrl.get('invoiceNo')?.value,
        amount:    +(ctrl.get('amount')?.value || 0),
        itemName:  ctrl.get('itemName')?.value || null,
        note:      ctrl.get('note')?.value || null,
        fileUrl:   ctrl.get('fileUrl')?.value || null,
        fileIndex: file ? fileIndex : -1,
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

  private _invoiceGroup(id: string, fileName: string, invoiceNo: string, amount: number, previewUrl = '', fileUrl = '', itemName = '', note = '') {
    return this.fb.group({
      id:         [id],
      fileName:   [fileName],
      invoiceNo:  [invoiceNo, Validators.required],
      amount:     [amount, [Validators.required, Validators.min(0)]],
      itemName:   [itemName],
      note:       [note],
      previewUrl: [previewUrl],
      fileUrl:    [fileUrl],
    });
  }

}
