import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-error-404',
  imports: [RouterLink],
  template: `
    <div class="error-page">
      <!-- Botanical decoration -->
      <svg class="deco deco-tl" viewBox="0 0 200 200" fill="none">
        <path d="M10 190 Q30 140 80 120 Q120 100 160 60 Q180 40 190 10"
              stroke="var(--accent-border)" stroke-width="1.5" fill="none"/>
        <path d="M10 160 Q50 130 90 100 Q130 70 150 20"
              stroke="var(--border)" stroke-width="1" fill="none"/>
        <circle cx="80" cy="120" r="3" fill="var(--accent-dim)"/>
      </svg>
      <svg class="deco deco-br" viewBox="0 0 200 200" fill="none">
        <path d="M190 10 Q170 60 120 80 Q80 100 40 140 Q20 160 10 190"
              stroke="var(--accent-border)" stroke-width="1.5" fill="none"/>
        <circle cx="120" cy="80" r="3" fill="var(--accent-dim)"/>
      </svg>

      <div class="error-content">
        <!-- Compass icon -->
        <div class="error-icon icon-sage">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor"
               stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <circle cx="12" cy="12" r="10"/>
            <polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76"/>
          </svg>
        </div>

        <h1 class="error-code">404</h1>
        <h2 class="error-title">找不到此頁面</h2>
        <p class="error-desc">
          您所尋找的頁面不存在或已被移除。<br>
          請檢查網址是否正確，或返回主頁重新導航。
        </p>

        <div class="error-actions">
          <a routerLink="/dashboard" class="btn-forest">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
              <polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
            回到主頁
          </a>
        </div>
      </div>
    </div>
  `,
  styles: `
    .error-page {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: calc(100vh - var(--app-header-height) - 3rem);
      padding: 2rem;
      overflow: hidden;
    }

    .deco {
      position: absolute;
      width: 180px;
      height: 180px;
      pointer-events: none;
      opacity: 0.6;
    }
    .deco-tl { top: 0; left: 0; }
    .deco-br { bottom: 0; right: 0; }

    .error-content {
      position: relative;
      z-index: 1;
      text-align: center;
      max-width: 28rem;
      animation: fadeUp 0.5s ease both;
    }

    .error-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 5rem;
      height: 5rem;
      border-radius: 50%;
      margin-bottom: 1.5rem;
    }
    .icon-sage {
      background: rgba(74, 107, 58, 0.08);
      color: var(--forest-mid);
      border: 1.5px solid rgba(74, 107, 58, 0.18);
    }

    .error-code {
      font-family: var(--font-display);
      font-size: 5rem;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin: 0 0 0.5rem;
      letter-spacing: -0.02em;
    }

    .error-title {
      font-family: var(--font-ui);
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 0.75rem;
    }

    .error-desc {
      font-family: var(--font-body);
      font-size: 0.9rem;
      color: var(--text-secondary);
      line-height: 1.7;
      margin: 0 0 2rem;
    }

    .error-actions {
      display: flex;
      gap: 0.75rem;
      justify-content: center;
    }

    .btn-forest {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1.5rem;
      font-family: var(--font-ui);
      font-size: 0.875rem;
      font-weight: 500;
      color: white;
      background: var(--forest);
      border-radius: var(--border-radius-uniform);
      text-decoration: none;
      transition: background 0.15s;
    }
    .btn-forest:hover { background: var(--forest-mid); }

    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(16px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `
})
export class Error404 {}
