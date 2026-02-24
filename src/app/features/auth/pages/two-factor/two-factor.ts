import { Component } from '@angular/core';

@Component({
  selector: 'app-two-factor',
  imports: [],
  template: `
    <div class="auth-page">
      <div class="login-card">
        <h2 class="text-center mb-2">雙重驗證</h2>
        <p class="text-center mb-6 text-sm">請輸入傳送至您電子郵件或手機的 6 位數驗證碼</p>
        <form class="flex flex-col gap-4">
          <div>
            <label for="otp" class="form-label">驗證碼</label>
            <input type="text" class="form-control text-center tracking-widest text-lg" id="otp" placeholder="123456" maxlength="6" required>
          </div>
          <button type="submit" class="btn btn-primary">驗證</button>
          <p class="text-center text-sm text-[--text-muted] mb-0">
            沒有收到驗證碼？<a href="#!" class="text-[--accent]">重新發送</a>
          </p>
        </form>
      </div>
    </div>
  `,
  styles: ``
})
export class TwoFactor {}
