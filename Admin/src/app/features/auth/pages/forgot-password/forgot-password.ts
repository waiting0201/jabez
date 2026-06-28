import { Component, signal } from '@angular/core';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  imports: [RouterLink],
  template: `
    <div class="auth-page">
      <div class="login-card">
        @if (!sent()) {
          <h2 class="text-center mb-2">忘記密碼</h2>
          <p class="text-center mb-6">請輸入您的電子郵件，系統將發送重設密碼連結</p>
          <form class="flex flex-col gap-4" (submit)="send($event)">
            <div>
              <label for="email" class="form-label">電子郵件</label>
              <input type="email" class="form-control" id="email" placeholder="your@email.com" required>
            </div>
            <button type="submit" class="btn btn-primary">發送重設連結</button>
            <p class="text-center text-sm text-[--text-muted] mb-0">
              <a routerLink="/auth/login" class="text-[--accent]">返回登入</a>
            </p>
          </form>
        } @else {
          <div class="flex flex-col items-center gap-4 text-center">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="11" stroke="var(--green)" stroke-width="1.5"/>
              <path d="M7 12l3.5 3.5L17 8.5" stroke="var(--green)" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            <h2 class="mb-1">已發送郵件</h2>
            <p class="mb-4">請至您的信箱查收重設密碼連結</p>
            <a routerLink="/auth/login" class="btn btn-primary">返回登入</a>
          </div>
        }
      </div>
    </div>
  `,
  styles: ``
})
export class ForgotPassword {
  sent = signal(false);
  send(e: Event) { e.preventDefault(); this.sent.set(true); }
}
