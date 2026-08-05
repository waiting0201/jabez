import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LineService } from '@core/auth/services/line.service';
import { environment } from '@/environments/environment';

@Component({
  selector: 'app-line-bind-callback',
  imports: [],
  template: `
    <div class="flex items-center justify-center" style="min-height: 60vh;">
      <div class="text-center">
        @if (isLoading()) {
          <div class="spinner-border text-primary mb-3" role="status"></div>
          <p class="text-[--text-secondary]">正在綁定 LINE 帳號...</p>
        } @else if (errorMsg()) {
          <div class="text-[--red] mb-3">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"/><path d="M15 9l-6 6M9 9l6 6"/>
            </svg>
          </div>
          <p class="text-[--red] fw-600 mb-2">綁定失敗</p>
          <p class="text-[--text-secondary] text-sm mb-4">{{ errorMsg() }}</p>
          <button class="btn btn-outline-secondary btn-sm" (click)="goBack()">返回</button>
        }
      </div>
    </div>
  `,
})
export class LineBindCallback implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private lineService = inject(LineService);
  private toastr = inject(ToastrService);

  isLoading = signal(true);
  errorMsg = signal('');

  ngOnInit() {
    const code = this.route.snapshot.queryParamMap.get('code');
    const state = this.route.snapshot.queryParamMap.get('state');
    const savedState = sessionStorage.getItem('line_bind_state');

    // 驗證 state 防 CSRF
    if (!code || !state || state !== savedState) {
      this.isLoading.set(false);
      this.errorMsg.set('驗證失敗，請重新操作。');
      sessionStorage.removeItem('line_bind_state');
      return;
    }

    sessionStorage.removeItem('line_bind_state');

    this.lineService.bind(code, environment.lineCallbackUrl).subscribe({
      next: () => {
        this.toastr.success('LINE 帳號綁定成功！');
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMsg.set(err?.error?.message || '綁定失敗，請重試。');
      },
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }
}
