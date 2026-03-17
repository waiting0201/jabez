import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES, ITEM_CATEGORIES} from '../../models/advance-request.model';

@Component({
  selector: 'app-advance-form',
  templateUrl: './advance-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
})
export class AdvanceForm implements OnInit {
  private fb      = inject(FormBuilder);
  private service = inject(AdvanceRequestService);
  private projectService = inject(ProjectService);
  private route   = inject(ActivatedRoute);
  private router  = inject(Router);
  private cdr     = inject(ChangeDetectorRef);

  projects: Project[] = [];
  loadingProjects = true;
  isEdit     = false;
  isReadOnly = false;
  requestId  = 0;
  errorMsg   = signal('');
  approvalStatus: ApprovalStatus = 'draft';
  projectCode = '';
  categories = ITEM_CATEGORIES;

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
        if (this.isReadOnly) this.form.disable();
        this.form.patchValue({
          projectId:      r.projectId,
          activityName:   r.activityName,
          activityPeriod: r.activityPeriod,
          advanceDate:    r.advanceDate?.toString().slice(0, 10),
        });
        r.items.forEach((item, idx) => this.itemArray.push(this._itemGroup(
          item.category, item.seqNo, item.itemName, item.unitPrice,
          item.quantity, item.totalPrice, item.cashAmount, item.checkAmount,
          item.note ?? '', idx
        )));
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
          next: () => this.router.navigate(['/admin/advance-requests']),
          error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '送出失敗。'),
        });
      },
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '儲存失敗。'),
    });
  }

  private _buildBody() {
    const f = this.form.value;
    return {
      projectId:      f.projectId,
      activityName:   f.activityName,
      activityPeriod: f.activityPeriod,
      advanceDate:    f.advanceDate,
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
