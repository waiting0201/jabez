import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AdvanceRequestService} from '../../services/advance-request.service';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';
import {AdvanceRequest, ITEM_CATEGORIES} from '../../models/advance-request.model';

@Component({
  selector: 'app-write-off-form',
  templateUrl: './write-off-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, FilePreviewModal],
})
export class WriteOffForm implements OnInit {
  private fb      = inject(FormBuilder);
  private service = inject(AdvanceRequestService);
  private paymentService = inject(PaymentRequestService);
  private route   = inject(ActivatedRoute);
  private router  = inject(Router);
  private cdr     = inject(ChangeDetectorRef);
  private sanitizer = inject(DomSanitizer);

  requestId = 0;
  request = signal<AdvanceRequest | null>(null);
  errorMsg = signal('');
  categories = ITEM_CATEGORIES;

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

  get cashTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('cashAmount')?.value) || 0), 0);
  }
  get checkTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('checkAmount')?.value) || 0), 0);
  }
  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  get existingTotal(): number {
    const r = this.request();
    return r?.writeOffs?.reduce((s, w) => s + w.grandTotal, 0) ?? 0;
  }

  get remainingBalance(): number {
    return (this.request()?.grandTotal ?? 0) - this.existingTotal;
  }

  ngOnInit() {
    this.requestId = +this.route.snapshot.paramMap.get('id')!;
    this.service.getById(this.requestId).subscribe(r => {
      this.request.set(r);
      this.cdr.markForCheck();
    });
  }

  addItem() {
    this.itemArray.push(this._itemGroup('', '', 0, '', 0, '', 0, 0, 0, '', this.itemArray.length));
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
      this.itemArray.push(this._itemGroup(id, file.name, 0, '', 0, '', 0, 0, 0, '', this.itemArray.length, previewUrl));
      return {id, file};
    });

    // OCR 辨識（並行）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const result = await firstValueFrom(this.paymentService.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        if (idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo: result.invoiceNo ?? '',
            unitPrice: result.amount ?? 0,
            totalPrice: result.amount ?? 0,
            cashAmount: result.amount ?? 0,
            quantity: '1式',
          });
        }
      } catch {
        // OCR failed — leave fields empty
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
    ctrl.get('cashAmount')?.setValue(total, {emitEvent: false});
  }

  submit() {
    if (this.itemArray.length === 0) return;
    const fd = this._buildFormData();
    this.errorMsg.set('');
    this.service.createWriteOff(this.requestId, fd).subscribe({
      next: () => this.router.navigate(['/admin/advance-requests', this.requestId]),
      error: (err: HttpErrorResponse) => this.errorMsg.set(err.error?.message || '沖銷失敗。'),
    });
  }

  private _buildFormData(): FormData {
    const fd = new FormData();
    fd.append('note', this.form.get('note')?.value || '');

    const itemsMeta: any[] = [];
    let fileIndex = 0;
    let sortIdx = 0;

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
        note:        ctrl.get('note')?.value || null,
        invoiceNo:   ctrl.get('invoiceNo')?.value || null,
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
    quantity: string, totalPrice: number, cashAmount: number, checkAmount: number,
    note: string, sortOrder: number, previewUrl = '', fileUrl = ''
  ) {
    return this.fb.group({
      id:          [id || `${Date.now()}-${Math.random().toString(36).slice(2)}`],
      fileName:    [fileName],
      invoiceNo:   [''],
      category:    [''],
      seqNo:       [seqNo],
      itemName:    [itemName],
      unitPrice:   [unitPrice, [Validators.min(0)]],
      quantity:    [quantity],
      totalPrice:  [totalPrice],
      cashAmount:  [cashAmount],
      checkAmount: [checkAmount],
      note:        [note],
      previewUrl:  [previewUrl],
      fileUrl:     [fileUrl],
      sortOrder:   [sortOrder],
    });
  }
}
