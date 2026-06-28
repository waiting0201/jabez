import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-error-500',
  imports: [RouterLink],
  template: `
    <div class="error-root">

      <!-- Left panel — deep red-brown -->
      <div class="error-left">
        <svg class="deco deco-tl" viewBox="0 0 200 200" fill="none">
          <path d="M10 190 Q30 140 80 120 Q120 100 160 60 Q180 40 190 10"
                stroke="rgba(255,255,255,0.12)" stroke-width="1.5" fill="none"/>
          <path d="M10 160 Q50 130 90 100 Q130 70 150 20"
                stroke="rgba(255,255,255,0.08)" stroke-width="1" fill="none"/>
          <circle cx="80" cy="120" r="3" fill="rgba(255,255,255,0.15)"/>
          <path d="M80 120 Q95 105 100 85" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
        </svg>
        <svg class="deco deco-br" viewBox="0 0 200 200" fill="none">
          <path d="M190 10 Q170 60 120 80 Q80 100 40 140 Q20 160 10 190"
                stroke="rgba(255,255,255,0.12)" stroke-width="1.5" fill="none"/>
          <circle cx="120" cy="80" r="3" fill="rgba(255,255,255,0.15)"/>
          <path d="M120 80 Q135 72 150 78" stroke="rgba(255,255,255,0.1)" stroke-width="1" fill="none"/>
        </svg>

        <div class="left-content">
          <div class="left-code">500</div>
          <div class="left-divider"></div>
          <p class="left-text">系統維護中...</p>
        </div>
      </div>

      <!-- Right panel — linen -->
      <div class="error-right">
        <div class="right-wrap" style="animation: fadeUp 0.5s ease both;">

          <div class="error-icon">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                 stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
              <line x1="12" y1="9" x2="12" y2="13"/>
              <line x1="12" y1="17" x2="12.01" y2="17"/>
            </svg>
          </div>

          <h1 class="right-title">伺服器錯誤</h1>
          <p class="right-desc">
            抱歉，伺服器發生了非預期的錯誤。<br>
            我們正在處理中，請稍後再試。
          </p>

          <div class="right-actions">
            <a routerLink="/" class="btn-forest">回到首頁</a>
            <a routerLink="/auth/login" class="btn-outline">重新登入</a>
          </div>

        </div>
      </div>

    </div>
  `,
  styles: `
    .error-root { display: flex; min-height: 100vh; min-height: 100dvh; }

    /* ── Left panel ── */
    .error-left {
      display: none;
      position: relative;
      width: 45%;
      background: #3a2020;
      color: white;
      overflow: hidden;
      flex-direction: column;
      justify-content: center;
      align-items: center;
    }
    @media (min-width: 768px) {
      .error-left { display: flex; }
      .error-right { width: 55%; }
    }

    .deco {
      position: absolute;
      width: 200px;
      height: 200px;
      pointer-events: none;
    }
    .deco-tl { top: 0; left: 0; }
    .deco-br { bottom: 0; right: 0; }

    .left-content {
      position: relative;
      z-index: 1;
      text-align: center;
    }
    .left-code {
      font-family: var(--font-display);
      font-size: 8rem;
      font-weight: 700;
      line-height: 1;
      color: white;
      opacity: 0.9;
    }
    .left-divider {
      width: 4rem;
      height: 2px;
      background: rgba(255,255,255,0.3);
      margin: 1.5rem auto;
    }
    .left-text {
      font-family: var(--font-body);
      font-size: 1rem;
      color: rgba(255,255,255,0.7);
      margin: 0;
    }

    /* ── Right panel ── */
    .error-right {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-surface);
      padding: 2rem 1.5rem;
    }
    .right-wrap {
      width: 100%;
      max-width: 24rem;
      text-align: center;
    }

    .error-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 5.5rem;
      height: 5.5rem;
      border-radius: 50%;
      background: rgba(160, 64, 64, 0.08);
      color: var(--red);
      border: 1.5px solid rgba(160, 64, 64, 0.2);
      margin-bottom: 1.5rem;
    }

    .right-title {
      font-family: var(--font-ui);
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 0.5rem;
    }
    .right-desc {
      font-family: var(--font-body);
      font-size: 0.9rem;
      color: var(--text-secondary);
      line-height: 1.7;
      margin: 0 0 2rem;
    }

    .right-actions {
      display: flex;
      gap: 0.75rem;
      justify-content: center;
      flex-wrap: wrap;
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

    .btn-outline {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1.5rem;
      font-family: var(--font-ui);
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--text-secondary);
      background: transparent;
      border: 1px solid var(--border);
      border-radius: var(--border-radius-uniform);
      text-decoration: none;
      transition: border-color 0.15s, color 0.15s;
    }
    .btn-outline:hover {
      border-color: var(--accent);
      color: var(--text-primary);
    }

    @keyframes fadeUp {
      from { opacity: 0; transform: translateY(16px); }
      to   { opacity: 1; transform: translateY(0); }
    }
  `
})
export class Error500 {}
