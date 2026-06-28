import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {PermissionService} from '../../services/permission.service';

@Component({
  selector: 'app-permission-form',
  templateUrl: './permission-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class PermissionForm implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(PermissionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEdit = false;
  permId = '';
  errorMsg = signal('');

  form = this.fb.group({
    code:        ['', Validators.required],
    name:        ['', Validators.required],
    module:      ['', Validators.required],
    description: [''],
  });

  ngOnInit() {
    this.permId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.permId) {
      this.isEdit = true;
      this.service.getById(this.permId).subscribe(p => {
        if (p) this.form.patchValue({...p});
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.service.update(this.permId, value)
      : this.service.create(value);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/permissions']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
