import { Component } from '@angular/core';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [RouterLink],
  template: `
    <div class="auth-page">
      <div class="login-card">
        <h2 class="text-center mb-2">申請試用</h2>
        <p class="text-center mb-6">填寫以下資料以申請帳號</p>
        <form class="flex flex-col gap-4">
          <div>
            <label for="email" class="form-label">電子郵件</label>
            <input type="email" class="form-control" id="email" placeholder="your@email.com" required>
          </div>
          <div>
            <label for="password" class="form-label">密碼</label>
            <input type="password" class="form-control" id="password" placeholder="••••••••" required>
          </div>
          <div>
            <label for="password-confirm" class="form-label">確認密碼</label>
            <input type="password" class="form-control" id="password-confirm" placeholder="••••••••" required>
          </div>
          <div class="form-check">
            <input class="form-check-input" type="checkbox" id="terms" required>
            <label class="form-check-label" for="terms">
              我同意 <a [routerLink]="[]" class="text-[--accent] underline">服務條款</a> 與 <a [routerLink]="[]" class="text-[--accent] underline">隱私政策</a>
            </label>
          </div>
          <button type="submit" class="btn btn-primary">申請帳號</button>
          <p class="text-center text-sm text-[--text-muted] mb-0">
            已有帳號？<a routerLink="/auth/login" class="text-[--accent] font-medium">立即登入</a>
          </p>
        </form>
      </div>
    </div>
  `,
  styles: ``
})
export class Register {}
