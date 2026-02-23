import {Component, inject} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {AuthService} from '@core/auth/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="row justify-content-center">
      <div class="col-11 col-md-8 col-lg-6 col-xl-4">
        <div class="login-card p-4 p-md-6 bg-dark bg-opacity-50 translucent-dark rounded-4">
          <h2 class="text-center mb-4">登入</h2>
          <p class="text-center text-white opacity-50 mb-4">歡迎回來，請登入您的帳號</p>
          <form [formGroup]="form" (ngSubmit)="submit()">
            @if (errorMsg) {
              <div class="alert alert-danger py-2 small">{{ errorMsg }}</div>
            }
            <div class="mb-3">
              <label for="email" class="form-label">電子郵件</label>
              <input type="email" class="form-control form-control-lg text-white bg-dark border-light border-opacity-25 bg-opacity-25"
                     id="email" formControlName="email" required>
            </div>
            <div class="mb-3">
              <label for="password" class="form-label">密碼</label>
              <input type="password" class="form-control form-control-lg text-white bg-dark border-light border-opacity-25 bg-opacity-25"
                     id="password" formControlName="password" required>
            </div>
            <div class="d-grid mb-3">
              <button type="submit" class="btn btn-primary btn-lg waves-effect waves-light"
                      [disabled]="form.invalid">登入</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: ``
})
export class Login {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  errorMsg = '';

  form = this.fb.group({
    email:    ['alice@example.com', [Validators.required, Validators.email]],
    password: ['password', Validators.required],
  });

  submit() {
    if (this.form.invalid) return;
    const {email, password} = this.form.value;
    this.authService.login(email!, password!).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.errorMsg = '電子郵件或密碼錯誤。';
      },
    });
  }
}
