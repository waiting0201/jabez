import {Component, computed, inject, Input, OnInit, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {ToastrService} from 'ngx-toastr';
import {VendorService, VendorFormPayload, VendorFileOptions} from '../../services/vendor.service';
import {Vendor, VendorLookup} from '../../models/vendor.model';
import {ImageCompressionService} from '../../../../../shared/services/image-compression.service';

const MAX_FILE_BYTES = 1 * 1024 * 1024;

type IdentifierType = 'taxId' | 'idNumber';

@Component({
  selector: 'app-vendor-quick-add-modal',
  templateUrl: './vendor-quick-add-modal.html',
  imports: [ReactiveFormsModule],
})
export class VendorQuickAddModal implements OnInit {
  private fb               = inject(FormBuilder);
  private vendorService    = inject(VendorService);
  private toastr           = inject(ToastrService);
  private imageCompression = inject(ImageCompressionService);
  activeModal = inject(NgbActiveModal);

  /** 由父元件傳入：在下拉沒找到時把使用者輸入的字帶進來 */
  @Input() prefillName?: string;

  saving   = signal(false);
  looking  = signal(false);
  errorMsg = signal('');

  identifierType = signal<IdentifierType>('taxId');

  bankBookImageFile     = signal<File | null>(null);
  bankBookImageFileName = signal<string | null>(null);
  hasBankBook = computed(() => !!this.bankBookImageFile());

  idCardFrontFile     = signal<File | null>(null);
  idCardFrontFileName = signal<string | null>(null);
  hasIdCardFront = computed(() => !!this.idCardFrontFile());

  idCardBackFile     = signal<File | null>(null);
  idCardBackFileName = signal<string | null>(null);
  hasIdCardBack = computed(() => !!this.idCardBackFile());

  form = this.fb.group({
    identifier:    ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
    name:          ['', Validators.required],
    phone:         [''],
    contactPerson: [''],
    address:       [''],
    bankAccount:   [''],
    note:          [''],
  });

  ngOnInit() {
    if (this.prefillName) this.form.patchValue({name: this.prefillName});
  }

  setIdentifierType(type: IdentifierType) {
    this.identifierType.set(type);
    const ctrl = this.form.controls.identifier;
    ctrl.setValidators(type === 'taxId'
      ? [Validators.required, Validators.pattern(/^\d{8}$/)]
      : [Validators.required, Validators.pattern(/^[A-Za-z][0-9]{9}$/)]);
    ctrl.updateValueAndValidity();
  }

  onTaxIdBlur() {
    if (this.identifierType() !== 'taxId') return;
    const ctrl  = this.form.controls.identifier;
    const taxId = (ctrl.value ?? '').trim();
    if (!taxId || ctrl.invalid) return;
    if (this.looking()) return;

    this.looking.set(true);
    this.vendorService.lookupByTaxId(taxId).subscribe({
      next: result => {
        this.looking.set(false);
        const patch: Partial<VendorFormPayload> = {};
        if (!this.form.controls.name.value)          patch.name          = result.name;
        if (!this.form.controls.address.value
            && result.address)                       patch.address       = result.address;
        if (!this.form.controls.contactPerson.value
            && result.contactPerson)                 patch.contactPerson = result.contactPerson;

        if (Object.keys(patch).length === 0) {
          this.toastr.info('已查到公司資料，但欄位皆已填寫，未覆寫。');
        } else {
          this.form.patchValue(patch);
          this.toastr.success('已自動帶入廠商資料');
        }
      },
      error: (err: HttpErrorResponse) => {
        this.looking.set(false);
        if (err.status === 404) {
          this.toastr.info('查無此統編資料，請手動填寫廠商名稱');
        } else {
          this.toastr.error(err.error?.message || '查詢失敗，請稍後再試');
        }
      },
    });
  }

  onBankBookImageSelected(event: Event) {
    this.pickInto(event, this.bankBookImageFile, this.bankBookImageFileName);
  }
  onRemoveBankBookImage() {
    this.bankBookImageFile.set(null);
    this.bankBookImageFileName.set(null);
  }

  onIdCardFrontSelected(event: Event) {
    this.pickInto(event, this.idCardFrontFile, this.idCardFrontFileName);
  }
  onRemoveIdCardFront() {
    this.idCardFrontFile.set(null);
    this.idCardFrontFileName.set(null);
  }

  onIdCardBackSelected(event: Event) {
    this.pickInto(event, this.idCardBackFile, this.idCardBackFileName);
  }
  onRemoveIdCardBack() {
    this.idCardBackFile.set(null);
    this.idCardBackFileName.set(null);
  }

  private async pickInto(
    event: Event,
    fileSig: ReturnType<typeof signal<File | null>>,
    nameSig: ReturnType<typeof signal<string | null>>,
  ) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, {maxSize: 1600, quality: 0.85});
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      fileSig.set(compressed);
      nameSig.set(file.name);
    } catch (err) {
      console.error('[VendorQuickAddModal] 檔案處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  submit() {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const type  = this.identifierType();
    const idVal = (this.form.controls.identifier.value ?? '').trim();

    // 存摺封面為必填
    if (!this.bankBookImageFile()) {
      this.toastr.error('請上傳存摺封面。');
      return;
    }

    // 個人工作室須備齊身分證正反面
    if (type === 'idNumber' && (!this.idCardFrontFile() || !this.idCardBackFile())) {
      this.toastr.error('請上傳身分證正反面。');
      return;
    }

    const base = this.form.value;
    const value: VendorFormPayload = {
      name:          base.name!.trim(),
      taxId:         type === 'taxId'    ? idVal : null,
      idNumber:      type === 'idNumber' ? idVal.toUpperCase() : null,
      phone:         base.phone ?? null,
      contactPerson: base.contactPerson ?? null,
      address:       base.address ?? null,
      bankAccount:   base.bankAccount ?? null,
      note:          base.note ?? null,
      isActive:      true,
    };
    const files: VendorFileOptions = {
      bankBookImage: this.bankBookImageFile() ?? undefined,
      idCardFront:   this.idCardFrontFile() ?? undefined,
      idCardBack:    this.idCardBackFile() ?? undefined,
    };

    this.errorMsg.set('');
    this.saving.set(true);
    this.vendorService.create(value, files).subscribe({
      next: (v: Vendor) => {
        const lookup: VendorLookup = {id: v.id, name: v.name, taxId: v.taxId, idNumber: v.idNumber};
        this.activeModal.close(lookup);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '新增廠商失敗，請稍後再試。');
      },
    });
  }

  cancel() {
    this.activeModal.dismiss();
  }
}
