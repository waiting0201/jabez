import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import {VendorService, VendorFormPayload, VendorFileOptions} from '../../services/vendor.service';
import {ImageCompressionService} from '../../../../../shared/services/image-compression.service';

const MAX_FILE_BYTES = 1 * 1024 * 1024; // 1 MB

type IdentifierType = 'taxId' | 'idNumber';

@Component({
  selector: 'app-vendor-form',
  templateUrl: './vendor-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class VendorForm implements OnInit {
  private fb               = inject(FormBuilder);
  private vendorService    = inject(VendorService);
  private route            = inject(ActivatedRoute);
  private router           = inject(Router);
  private toastr           = inject(ToastrService);
  private imageCompression = inject(ImageCompressionService);

  isEdit = false;
  vendorId = 0;
  errorMsg = signal('');
  saving   = signal(false);
  looking  = signal(false);

  // 識別碼類型：統編（公司） / 身分證字號（個人工作室）
  identifierType = signal<IdentifierType>('taxId');

  // 存摺封面：5 個 signal（沿用 user-form 模式）
  bankBookImageUrl       = signal<string | null>(null);
  bankBookImageFile      = signal<File | null>(null);
  bankBookImagePreview   = signal<string | null>(null);
  bankBookImageFileName  = signal<string | null>(null);
  removeBankBookImage    = signal(false);

  hasExistingBankBook = computed(() =>
    !!this.bankBookImageUrl() && !this.bankBookImageFile() && !this.removeBankBookImage());

  bankBookDisplayName = computed(() => {
    if (this.bankBookImageFileName()) return this.bankBookImageFileName();
    if (this.hasExistingBankBook()) {
      const url = this.bankBookImageUrl()!;
      const match = url.match(/\/vendor-passbooks\/(.+)$/);
      return match?.[1] ?? '存摺封面';
    }
    return null;
  });

  // 身分證正面
  idCardFrontUrl       = signal<string | null>(null);
  idCardFrontFile      = signal<File | null>(null);
  idCardFrontPreview   = signal<string | null>(null);
  idCardFrontFileName  = signal<string | null>(null);
  removeIdCardFront    = signal(false);

  hasExistingIdCardFront = computed(() =>
    !!this.idCardFrontUrl() && !this.idCardFrontFile() && !this.removeIdCardFront());

  idCardFrontDisplayName = computed(() => {
    if (this.idCardFrontFileName()) return this.idCardFrontFileName();
    if (this.hasExistingIdCardFront()) {
      const match = this.idCardFrontUrl()!.match(/\/vendor-id-cards\/(.+)$/);
      return match?.[1] ?? '身分證正面';
    }
    return null;
  });

  // 身分證反面
  idCardBackUrl       = signal<string | null>(null);
  idCardBackFile      = signal<File | null>(null);
  idCardBackPreview   = signal<string | null>(null);
  idCardBackFileName  = signal<string | null>(null);
  removeIdCardBack    = signal(false);

  hasExistingIdCardBack = computed(() =>
    !!this.idCardBackUrl() && !this.idCardBackFile() && !this.removeIdCardBack());

  idCardBackDisplayName = computed(() => {
    if (this.idCardBackFileName()) return this.idCardBackFileName();
    if (this.hasExistingIdCardBack()) {
      const match = this.idCardBackUrl()!.match(/\/vendor-id-cards\/(.+)$/);
      return match?.[1] ?? '身分證反面';
    }
    return null;
  });

  form = this.fb.group({
    identifier:    ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
    name:          ['', Validators.required],
    phone:         [''],
    contactPerson: [''],
    address:       [''],
    bankAccount:   [''],
    note:          [''],
    isActive:      [true],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.vendorId = +id;
      this.vendorService.getById(this.vendorId).subscribe(v => {
        if (!v) return;
        this.setIdentifierType(v.idNumber ? 'idNumber' : 'taxId');
        this.form.patchValue({
          identifier:    v.idNumber ?? v.taxId ?? '',
          name:          v.name,
          phone:         v.phone ?? '',
          contactPerson: v.contactPerson ?? '',
          address:       v.address ?? '',
          bankAccount:   v.bankAccount ?? '',
          note:          v.note ?? '',
          isActive:      v.isActive,
        });
        this.bankBookImageUrl.set(v.bankBookImageUrl ?? null);
        this.idCardFrontUrl.set(v.idCardFrontUrl ?? null);
        this.idCardBackUrl.set(v.idCardBackUrl ?? null);
      });
    }
  }

  /** 切換識別碼類型並調整驗證規則（統編 8 碼 / 身分證 1 英文字 + 9 數字） */
  setIdentifierType(type: IdentifierType) {
    this.identifierType.set(type);
    const ctrl = this.form.controls.identifier;
    ctrl.setValidators(type === 'taxId'
      ? [Validators.required, Validators.pattern(/^\d{8}$/)]
      : [Validators.required, Validators.pattern(/^[A-Za-z][0-9]{9}$/)]);
    ctrl.updateValueAndValidity();
  }

  /** 統編失焦自動查詢公司資料（GCIS Open Data；僅統編類型） */
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
        // 只填空欄位，避免覆寫使用者已輸入內容
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

  // ── 存摺封面 ──────────────────────────────────────────────
  async onBankBookImageSelected(event: Event) {
    const file = await this.pickCompressed(event);
    if (!file) return;
    this.bankBookImageFile.set(file.compressed);
    this.bankBookImageFileName.set(file.name);
    this.removeBankBookImage.set(false);
    this.bankBookImagePreview.set(file.preview);
  }

  onRemoveBankBookImage() {
    this.bankBookImageFile.set(null);
    this.bankBookImagePreview.set(null);
    this.bankBookImageFileName.set(null);
    this.removeBankBookImage.set(true);
  }

  viewBankBookImage() {
    const fileName = this.bankBookImageUrl()?.match(/\/vendor-passbooks\/(.+)$/)?.[1];
    if (!fileName) return;
    this.openBlob(this.vendorService.getBankBookImage(fileName), '無法載入存摺封面。');
  }

  // ── 身分證正面 ────────────────────────────────────────────
  async onIdCardFrontSelected(event: Event) {
    const file = await this.pickCompressed(event);
    if (!file) return;
    this.idCardFrontFile.set(file.compressed);
    this.idCardFrontFileName.set(file.name);
    this.removeIdCardFront.set(false);
    this.idCardFrontPreview.set(file.preview);
  }

  onRemoveIdCardFront() {
    this.idCardFrontFile.set(null);
    this.idCardFrontPreview.set(null);
    this.idCardFrontFileName.set(null);
    this.removeIdCardFront.set(true);
  }

  viewIdCardFront() {
    const fileName = this.idCardFrontUrl()?.match(/\/vendor-id-cards\/(.+)$/)?.[1];
    if (!fileName) return;
    this.openBlob(this.vendorService.getIdCardImage(fileName), '無法載入身分證正面。');
  }

  // ── 身分證反面 ────────────────────────────────────────────
  async onIdCardBackSelected(event: Event) {
    const file = await this.pickCompressed(event);
    if (!file) return;
    this.idCardBackFile.set(file.compressed);
    this.idCardBackFileName.set(file.name);
    this.removeIdCardBack.set(false);
    this.idCardBackPreview.set(file.preview);
  }

  onRemoveIdCardBack() {
    this.idCardBackFile.set(null);
    this.idCardBackPreview.set(null);
    this.idCardBackFileName.set(null);
    this.removeIdCardBack.set(true);
  }

  viewIdCardBack() {
    const fileName = this.idCardBackUrl()?.match(/\/vendor-id-cards\/(.+)$/)?.[1];
    if (!fileName) return;
    this.openBlob(this.vendorService.getIdCardImage(fileName), '無法載入身分證反面。');
  }

  // ── 共用：壓縮選檔 + 開啟 blob ─────────────────────────────
  private async pickCompressed(event: Event): Promise<{compressed: File; name: string; preview: string | null} | null> {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return null;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, {maxSize: 1600, quality: 0.85});
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return null;
      }
      let preview: string | null = null;
      if (compressed.type.startsWith('image/')) {
        preview = await new Promise<string>(resolve => {
          const reader = new FileReader();
          reader.onload = () => resolve(reader.result as string);
          reader.readAsDataURL(compressed);
        });
      }
      return {compressed, name: file.name, preview};
    } catch (err) {
      console.error('[VendorForm] 檔案處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
      return null;
    }
  }

  private openBlob(obs: ReturnType<VendorService['getBankBookImage']>, errMsg: string) {
    obs.subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || errMsg, '載入失敗'),
    });
  }

  submit() {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const type  = this.identifierType();
    const idVal = (this.form.controls.identifier.value ?? '').trim();

    // 存摺封面為必填
    if (!this.bankBookImageFile() && !this.hasExistingBankBook()) {
      this.toastr.error('請上傳存摺封面。');
      return;
    }

    // 個人工作室須備齊身分證正反面
    if (type === 'idNumber') {
      const frontOk = !!this.idCardFrontFile() || this.hasExistingIdCardFront();
      const backOk  = !!this.idCardBackFile()  || this.hasExistingIdCardBack();
      if (!frontOk || !backOk) {
        this.toastr.error('請上傳身分證正反面。');
        return;
      }
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
      isActive:      base.isActive ?? true,
    };
    const files: VendorFileOptions = {
      bankBookImage:       this.bankBookImageFile() ?? undefined,
      removeBankBookImage: this.removeBankBookImage(),
      idCardFront:         this.idCardFrontFile() ?? undefined,
      removeIdCardFront:   this.removeIdCardFront(),
      idCardBack:          this.idCardBackFile() ?? undefined,
      removeIdCardBack:    this.removeIdCardBack(),
    };

    this.errorMsg.set('');
    this.saving.set(true);
    const obs = this.isEdit
      ? this.vendorService.update(this.vendorId, value, files)
      : this.vendorService.create(value, files);

    obs.subscribe({
      next: () => {
        this.toastr.success(this.isEdit ? '廠商已更新' : '廠商已建立');
        this.router.navigate(['/admin/vendors']);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
