import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES, DesignatedReviewer} from '../../models/advance-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';

@Component({
  selector: 'app-advance-form',
  templateUrl: './advance-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ApprovalTimeline],
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
  successModal = viewChild<TemplateRef<any>>('successModal');

  projects: Project[] = [];
  loadingProjects = true;
  isEdit     = false;
  isReadOnly = false;
  requestId  = 0;
  errorMsg   = signal('');
  approvalStatus: ApprovalStatus = 'draft';
  projectCode = '';
  projectName = '';
  categories = ITEM_CATEGORIES;

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
    // 檢查簽核流程是否有「申請人指定審核」步驟
    this.approvalSvc.getAll().subscribe(items => {
      this.hasDesignatedStep = items
        .filter(i => i.isActive && i.applicationType === 'advance')
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
          item.note ?? '', idx
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
    this.itemArray.removeAt(i);
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
    const body = this._buildBody();
    const obs = this.isEdit
      ? this.service.update(this.requestId, body)
      : this.service.create(body);
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
    const body = this._buildBody();
    const save$ = this.isEdit
      ? this.service.update(this.requestId, body)
      : this.service.create(body);
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

  private _buildBody() {
    const f = this.form.value;
    const reviewers = this.designatedEntries
      .filter(e => e.selectedUserId)
      .map(e => ({ reviewerId: e.selectedUserId!, stepOrder: e.stepOrder }));
    return {
      projectId:            f.projectId,
      activityName:         f.activityName,
      activityPeriod:       f.activityPeriod,
      advanceDate:          f.advanceDate,
      designatedReviewers:  reviewers.length > 0 ? reviewers : undefined,
      items: this.itemArray.controls.map((c, idx) => ({
        category:    c.get('category')?.value || '',
        seqNo:       +(c.get('seqNo')?.value) || 0,
        itemName:    c.get('itemName')?.value || '',
        unitPrice:   +(c.get('unitPrice')?.value) || 0,
        quantity:    c.get('quantity')?.value || '',
        totalPrice:  +(c.get('totalPrice')?.value) || 0,
        cashAmount:  +(c.get('cashAmount')?.value) || 0,
        checkAmount: +(c.get('checkAmount')?.value) || 0,
        note:        c.get('note')?.value || '',
        sortOrder:   idx,
      })),
    };
  }

  private _itemGroup(
    category: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, cashAmount: number, checkAmount: number,
    note: string, sortOrder: number
  ) {
    return this.fb.group({
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
    });
  }
}
