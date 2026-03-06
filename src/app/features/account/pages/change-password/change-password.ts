import {Component, inject, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {AuthService} from '@core/auth/services/auth.service';
import {ToastrService} from 'ngx-toastr';

@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule],
  templateUrl: './change-password.html',
})
export class ChangePassword {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toastr = inject(ToastrService);

  isForced = this.route.snapshot.queryParamMap.get('forced') === '1';
  submitting = signal(false);
  showCurrentPassword = signal(false);
  showNewPassword = signal(false);
  showConfirmPassword = signal(false);

  form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]],
  });

  submit() {
    if (this.form.invalid) return;

    const {currentPassword, newPassword, confirmPassword} = this.form.getRawValue();

    if (newPassword !== confirmPassword) {
      this.toastr.error('新密碼與確認密碼不一致。');
      return;
    }

    if (currentPassword === newPassword) {
      this.toastr.error('新密碼不可與舊密碼相同。');
      return;
    }

    this.submitting.set(true);
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.toastr.success('密碼修改成功，請重新登入。');
        this.auth.logout();
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.submitting.set(false);
        const msg = err?.error?.message || err?.message || '密碼修改失敗。';
        this.toastr.error(msg);
      },
    });
  }
}
