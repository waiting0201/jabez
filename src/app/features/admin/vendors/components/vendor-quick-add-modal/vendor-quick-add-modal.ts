import {Component, inject, Input, OnInit, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {VendorService} from '../../services/vendor.service';
import {Vendor, VendorLookup} from '../../models/vendor.model';

@Component({
  selector: 'app-vendor-quick-add-modal',
  templateUrl: './vendor-quick-add-modal.html',
  imports: [ReactiveFormsModule],
})
export class VendorQuickAddModal implements OnInit {
  private fb = inject(FormBuilder);
  private vendorService = inject(VendorService);
  activeModal = inject(NgbActiveModal);

  /** 由父元件傳入：在下拉沒找到時把使用者輸入的字帶進來 */
  @Input() prefillName?: string;

  saving = signal(false);
  errorMsg = signal('');

  form = this.fb.group({
    name:          ['', Validators.required],
    taxId:         [''],
    phone:         [''],
    contactPerson: [''],
    address:       [''],
    bankAccount:   [''],
    note:          [''],
  });

  ngOnInit() {
    if (this.prefillName) this.form.patchValue({name: this.prefillName});
  }

  submit() {
    if (this.form.invalid || this.saving()) return;
    const value = this.form.value as any;
    this.errorMsg.set('');
    this.saving.set(true);
    this.vendorService.create({...value, isActive: true}).subscribe({
      next: (v: Vendor) => {
        const lookup: VendorLookup = {id: v.id, name: v.name, taxId: v.taxId};
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
