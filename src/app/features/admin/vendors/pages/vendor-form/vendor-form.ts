import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {VendorService} from '../../services/vendor.service';

@Component({
  selector: 'app-vendor-form',
  templateUrl: './vendor-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class VendorForm implements OnInit {
  private fb = inject(FormBuilder);
  private vendorService = inject(VendorService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEdit = false;
  vendorId = 0;
  errorMsg = signal('');

  form = this.fb.group({
    name:          ['', Validators.required],
    taxId:         [''],
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
        if (v) this.form.patchValue(v);
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.vendorService.update(this.vendorId, value)
      : this.vendorService.create(value);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/vendors']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
