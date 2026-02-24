import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '@core/auth/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-root">

      <!-- ══ LEFT PANEL — Forest green brand ══ -->
      <div class="login-left">

        <!-- Botanical decorative SVGs -->
        <svg class="deco deco-tl" viewBox="0 0 200 200" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M10 190 Q30 140 80 120 Q120 100 160 60 Q180 40 190 10" stroke="rgba(255,255,255,0.12)" stroke-width="1.5" fill="none"/>
          <path d="M10 160 Q50 130 90 100 Q130 70 150 20" stroke="rgba(255,255,255,0.08)" stroke-width="1" fill="none"/>
          <circle cx="80" cy="120" r="3" fill="rgba(255,255,255,0.15)"/>
          <circle cx="130" cy="80" r="2" fill="rgba(255,255,255,0.12)"/>
          <path d="M80 120 Q95 105 100 85" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
          <path d="M80 120 Q65 110 50 115" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
        </svg>
        <svg class="deco deco-br" viewBox="0 0 200 200" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M190 10 Q170 60 120 80 Q80 100 40 140 Q20 160 10 190" stroke="rgba(255,255,255,0.12)" stroke-width="1.5" fill="none"/>
          <path d="M190 40 Q150 70 110 100 Q70 130 50 180" stroke="rgba(255,255,255,0.07)" stroke-width="1" fill="none"/>
          <circle cx="120" cy="80" r="3" fill="rgba(255,255,255,0.15)"/>
          <path d="M120 80 Q105 68 100 50" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
          <path d="M120 80 Q135 72 150 78" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
        </svg>

        <!-- Brand content -->
        <div class="left-content">
          <div class="left-logo">
            <img src="/assets/img/jabez-logo.png" alt="Jabez" class="left-logo-img" />
          </div>

          <h2 class="left-heading">紮根成長，<br>一切從這裡開始。</h2>
          <p class="left-sub">Jabez 管理平台讓您輕鬆掌握每一個環節，<br>讓品牌在土壤中穩健蔓延。</p>

          <div class="left-features">
            <div class="left-feature">
              <span class="lf-dot"></span>
              <span>費用請款流程簽核管理</span>
            </div>
            <div class="left-feature">
              <span class="lf-dot"></span>
              <span>請假與出差申請追蹤</span>
            </div>
            <div class="left-feature">
              <span class="lf-dot"></span>
              <span>角色權限細粒度控管</span>
            </div>
          </div>
        </div>

      </div>

      <!-- ══ RIGHT PANEL — Linen form area ══ -->
      <div class="login-right">
        <div class="form-wrap" style="animation: fadeUp 0.5s ease both;">

          <div class="right-logo">
            <img src="/assets/img/jabez-logo.png" alt="Jabez" class="right-logo-img" />
          </div>

          <div class="form-header">
            <h1 class="form-title">歡迎回來</h1>
            <p class="form-subtitle">請輸入您的帳號與密碼以登入管理系統</p>
          </div>

          <form class="form-body" [formGroup]="form" (ngSubmit)="submit()">

            <!-- Email -->
            <div class="field-group">
              <label class="field-label" for="email">電子郵件</label>
              <input
                id="email"
                type="email"
                class="field-input"
                placeholder="輸入您的電子郵件"
                autocomplete="email"
                formControlName="email"
              />
            </div>

            <!-- Password -->
            <div class="field-group">
              <label class="field-label" for="password">密碼</label>
              <div class="field-wrap">
                <input
                  id="password"
                  [type]="showPassword() ? 'text' : 'password'"
                  class="field-input"
                  placeholder="輸入您的密碼"
                  autocomplete="current-password"
                  formControlName="password"
                />
                <button type="button" class="eye-btn" (click)="togglePassword()">
                  @if (!showPassword()) {
                    <svg width="18" height="18" viewBox="0 0 16 16" fill="none">
                      <path d="M1 8C1 8 4 3 8 3s7 5 7 5-3 5-7 5S1 8 1 8z" stroke="currentColor" stroke-width="1.3"/>
                      <circle cx="8" cy="8" r="2" stroke="currentColor" stroke-width="1.3"/>
                    </svg>
                  } @else {
                    <svg width="18" height="18" viewBox="0 0 16 16" fill="none">
                      <path d="M2 2l12 12M7 4.1C7.3 4 7.6 4 8 4c4 0 7 4 7 4s-.8 1.3-2 2.5M4.5 5.5C2.8 6.7 1 8 1 8s3 5 7 5c1.5 0 2.8-.5 3.9-1.2" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/>
                    </svg>
                  }
                </button>
              </div>
            </div>

            <!-- Remember + Forgot -->
            <div class="remember-row">
              <label class="remember-label">
                <input type="checkbox" class="remember-checkbox" />
                <span>記住我</span>
              </label>
              <a routerLink="/auth/forgot-password" class="forgot-link">忘記密碼？</a>
            </div>

            <!-- Error -->
            @if (errorMsg) {
              <div class="error-bar">
                <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                  <circle cx="7" cy="7" r="6" stroke="currentColor" stroke-width="1.3"/>
                  <path d="M7 4v3M7 9.5v.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/>
                </svg>
                {{ errorMsg }}
              </div>
            }

            <!-- Submit -->
            <button type="submit" class="submit-btn" [disabled]="form.invalid || isLoading()">
              @if (isLoading()) {
                <span class="spinner"></span>
                <span>驗證中...</span>
              } @else {
                登入系統
              }
            </button>

          </form>


        </div>
      </div>

    </div>
  `,
  styles: `
    .login-root { display: flex; min-height: 100vh; min-height: 100dvh; }

    /* ── Left panel ── */
    .login-left {
      display: none; position: relative; width: 45%;
      background: var(--forest); color: white; overflow: hidden;
      padding: 3rem; flex-direction: column; justify-content: center; align-items: center;
    }
    @media (min-width: 768px) {
      .login-left { display: flex; }
      .login-right { width: 55%; }
    }
    .deco { position: absolute; width: 200px; height: 200px; pointer-events: none; }
    .deco-tl { top: 0; left: 0; }
    .deco-br { bottom: 0; right: 0; }
    .left-content { position: relative; z-index: 1; width: 100%; text-align: center; }
    .left-logo { margin-bottom: 2.5rem; display: flex; justify-content: center; }
    .left-logo-img { height: 5rem; width: auto; border-radius: 0.75rem; }
    .left-heading {
      font-family: var(--font-display); font-size: 2rem; font-weight: 700;
      line-height: 1.3; color: white; margin-bottom: 1rem;
    }
    .left-sub { font-size: 0.9rem; color: rgba(255,255,255,0.7); line-height: 1.7; margin-bottom: 2rem; }
    .left-features { display: flex; flex-direction: column; gap: 0.75rem; align-items: center; }
    .left-feature { display: flex; align-items: center; gap: 0.75rem; font-size: 0.875rem; color: rgba(255,255,255,0.85); }
    .lf-dot { flex-shrink: 0; width: 0.4rem; height: 0.4rem; border-radius: 50%; background: rgba(255,255,255,0.5); }

    /* ── Right panel ── */
    .login-right {
      flex: 1; display: flex; align-items: center; justify-content: center;
      background: var(--bg-surface); padding: 2rem 1.5rem; overflow-y: auto;
    }
    .form-wrap { width: 100%; max-width: 24rem; }
    .right-logo { display: flex; justify-content: center; margin-bottom: 2rem; }
    .right-logo-img { height: 4rem; width: auto; border-radius: 0.75rem; }
    @media (min-width: 768px) { .right-logo { display: none; } }
    .form-header { margin-bottom: 1.75rem; }
    .form-title { font-size: 1.5rem; font-weight: 700; color: var(--text-primary); margin-bottom: 0.25rem; }
    .form-subtitle { font-size: 0.875rem; color: var(--text-muted); margin: 0; }
    .form-body { display: flex; flex-direction: column; gap: 1.25rem; }
    .field-group { display: flex; flex-direction: column; gap: 0.375rem; }
    .field-label { font-size: 0.8125rem; font-weight: 500; color: var(--text-secondary); }
    .field-wrap { position: relative; }
    .field-input {
      width: 100%; padding: 0.625rem 0.875rem; font-size: 0.9rem;
      font-family: var(--font-ui); color: var(--text-primary);
      background: var(--bg-base); border: 1px solid var(--border);
      border-radius: var(--border-radius-uniform); outline: none;
      transition: border-color 0.15s, box-shadow 0.15s; -webkit-appearance: none;
    }
    .field-wrap .field-input { padding-right: 2.75rem; }
    .field-input:focus { border-color: var(--accent); box-shadow: 0 0 0 3px var(--accent-dim); }
    .eye-btn {
      position: absolute; right: 0; top: 0; height: 100%; width: 2.75rem;
      display: flex; align-items: center; justify-content: center;
      background: none; border: none; cursor: pointer; color: var(--text-muted); padding: 0;
    }
    .eye-btn:hover { color: var(--text-secondary); }
    .remember-row { display: flex; align-items: center; justify-content: space-between; }
    .remember-label { display: flex; align-items: center; gap: 0.5rem; font-size: 0.8125rem; color: var(--text-secondary); cursor: pointer; }
    .remember-checkbox { width: 1rem; height: 1rem; accent-color: var(--accent); }
    .forgot-link { font-size: 0.8125rem; color: var(--accent); text-decoration: none; }
    .forgot-link:hover { text-decoration: underline; }
    .error-bar {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.625rem 0.875rem;
      background: rgba(160,64,64,0.08); border: 1px solid rgba(160,64,64,0.22);
      border-radius: var(--border-radius-uniform); color: var(--red); font-size: 0.8125rem;
    }
    .submit-btn {
      display: flex; align-items: center; justify-content: center; gap: 0.5rem;
      width: 100%; padding: 0.7rem 1.25rem;
      font-size: 0.9375rem; font-weight: 600; font-family: var(--font-ui);
      color: white; background: var(--forest); border: none;
      border-radius: var(--border-radius-uniform); cursor: pointer;
      transition: background 0.15s; margin-top: 0.25rem;
    }
    .submit-btn:hover:not(:disabled) { background: var(--forest-mid); }
    .submit-btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .spinner {
      display: inline-block; width: 1rem; height: 1rem;
      border: 2px solid rgba(255,255,255,0.3); border-top-color: white;
      border-radius: 50%; animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(12px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `
})
export class Login {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  showPassword = signal(false);
  isLoading = signal(false);
  errorMsg = '';

  form = this.fb.group({
    email:    ['alice@example.com', [Validators.required, Validators.email]],
    password: ['password', Validators.required],
  });

  togglePassword() {
    this.showPassword.update(v => !v);
  }

  submit() {
    if (this.form.invalid) return;
    this.isLoading.set(true);
    this.errorMsg = '';
    const { email, password } = this.form.value;
    this.authService.login(email!, password!).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMsg = '電子郵件或密碼錯誤，請重試。';
      },
    });
  }
}
