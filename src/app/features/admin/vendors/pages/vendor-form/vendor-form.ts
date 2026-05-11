import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import {VendorService, VendorFormPayload} from '../../services/vendor.service';
import {ImageCompressionService} from '../../../../../shared/services/image-compression.service';

const MAX_FILE_BYTES = 1 * 1024 * 1024; // 1 MB

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

  form = this.fb.group({
    taxId:         ['', [Validators.required, Validators.pattern(/^\d{8}$/)]],
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
        this.form.patchValue(v);
        this.bankBookImageUrl.set(v.bankBookImageUrl ?? null);
      });
    }
  }

  /** 統編失焦自動查詢公司資料（GCIS Open Data） */
  onTaxIdBlur() {
    const taxIdCtrl = this.form.controls.taxId;
    const taxId     = (taxIdCtrl.value ?? '').trim();
    if (!taxId || taxIdCtrl.invalid) return;
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

  async onBankBookImageSelected(event: Event) {
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
      this.bankBookImageFile.set(compressed);
      this.bankBookImageFileName.set(file.name);
      this.removeBankBookImage.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.bankBookImagePreview.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.bankBookImagePreview.set(null);
      }
    } catch (err) {
      console.error('[VendorForm] 存摺封面處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveBankBookImage() {
    this.bankBookImageFile.set(null);
    this.bankBookImagePreview.set(null);
    this.bankBookImageFileName.set(null);
    this.removeBankBookImage.set(true);
  }

  viewBankBookImage() {
    const url = this.bankBookImageUrl();
    if (!url) return;
    const match    = url.match(/\/vendor-passbooks\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.vendorService.getBankBookImage(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入存摺封面。', '載入失敗'),
    });
  }

  submit() {
    if (this.form.invalid || this.saving()) return;
    const value = this.form.value as VendorFormPayload;
    const files = {
      bankBookImage:       this.bankBookImageFile() ?? undefined,
      removeBankBookImage: this.removeBankBookImage(),
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
