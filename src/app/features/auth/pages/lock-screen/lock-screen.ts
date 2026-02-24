import { Component } from '@angular/core';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-lock-screen',
  imports: [RouterLink],
  template: `
    <div class="auth-page">
      <div class="login-card text-center">
        <div class="mb-4">
          <div class="inline-flex items-center justify-center w-[4.5rem] h-[4.5rem] rounded-full bg-[--bg-elevated] border border-[--border] overflow-hidden">
            <svg width="40" height="40" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="8" r="4" stroke="var(--text-muted)" stroke-width="1.5"/>
              <path d="M4 20c0-4 3.582-7 8-7s8 3 8 7" stroke="var(--text-muted)" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
          </div>
        </div>
        <h4 class="mb-1">工作階段已鎖定</h4>
        <p class="mb-6 text-sm">請輸入密碼或驗證碼以解鎖</p>
        <form class="flex flex-col gap-4 text-left">
          <div>
            <label for="password" class="form-label">密碼</label>
            <input type="password" class="form-control" id="password" placeholder="••••••••" required>
          </div>
          <div class="text-center text-sm text-[--text-muted]">— 或 —</div>
          <div>
            <label for="otp" class="form-label">驗證碼</label>
            <input type="text" class="form-control text-center tracking-widest" id="otp" placeholder="123456" maxlength="6">
          </div>
          <button type="submit" class="btn btn-primary">解鎖</button>
          <p class="text-center text-sm text-[--text-muted] mb-0">
            不是您？<a routerLink="/auth/login" class="text-[--accent]">切換帳號</a>
          </p>
        </form>
      </div>
    </div>
  `,
  styles: ``
})
export class LockScreen {}
