import {Component, inject, OnInit} from '@angular/core';
import {NgbDropdownModule} from '@ng-bootstrap/ng-bootstrap';
import {Router, RouterLink} from '@angular/router';
import {AuthService} from '@core/auth/services/auth.service';
import {LineService} from '@core/auth/services/line.service';
import {environment} from '@/environments/environment';

@Component({
  selector: 'app-profile-dropdown',
  imports: [NgbDropdownModule, RouterLink],
  template: `
    <div ngbDropdown>
      <button type="button" ngbDropdownToggle [title]="user()?.email ?? ''"
              class="btn-system no-arrow bg-transparent flex shrink-0 items-center justify-center"
              aria-label="Open Profile Dropdown">
        <span class="profile-image profile-image-md rounded-circle inline-block overflow-hidden">
          <img [src]="avatarUrl() ?? '/assets/img/demo/avatars/avatar-admin.png'"
               class="w-full h-full object-cover"
               [style.objectPosition]="avatarPosX() + '% ' + avatarPosY() + '%'"
               [style.transform]="avatarScale() === 1 ? null : ('scale(' + avatarScale() + ')')"
               style="transform-origin: center"
               [alt]="user()?.name ?? ''">
        </span>
      </button>

      <div ngbDropdownMenu class="dropdown-menu dropdown-menu-end dropdown-menu-animated">
        <!-- User info header -->
        <div class="flex items-center gap-3 px-4 py-3 border-b border-[--border]">
          <span class="block w-10 h-10 rounded-full overflow-hidden shrink-0">
            <img [src]="avatarUrl() ?? '/assets/img/demo/avatars/avatar-admin.png'"
                 class="w-full h-full object-cover"
                 [style.objectPosition]="avatarPosX() + '% ' + avatarPosY() + '%'"
                 [style.transform]="avatarScale() === 1 ? null : ('scale(' + avatarScale() + ')')"
                 style="transform-origin: center"
                 [alt]="user()?.name ?? ''">
          </span>
          <div class="min-w-0">
            <div class="text-sm font-semibold text-[--text-primary] truncate">{{ user()?.name }}</div>
            @if (departmentName() || jobTitleName()) {
              <div class="text-xs text-[--text-secondary] truncate">{{ departmentName() }}{{ departmentName() && jobTitleName() ? '・' : '' }}{{ jobTitleName() }}</div>
            }
            <div class="text-xs text-[--text-muted] truncate">{{ user()?.email }}</div>
          </div>
        </div>

        <a class="dropdown-item" routerLink="/account/change-password">
          <svg class="sa-icon" style="width:1rem;height:1rem;stroke:currentColor">
            <use href="/assets/icons/sprite.svg#lock"></use>
          </svg>
          <span class="font-medium">修改密碼</span>
        </a>

        <!-- LINE 綁定（三態：未綁定 / 已綁定未加好友 / 已綁定為好友） -->
        @if (!isLineBound()) {
          <a class="dropdown-item" href="javascript:void(0)" (click)="bindLine()">
            <svg style="width:1rem;height:1rem" viewBox="0 0 24 24" fill="#06C755">
              <path d="M19.365 9.863c.349 0 .63.285.63.631 0 .345-.281.63-.63.63H17.61v1.125h1.755c.349 0 .63.283.63.63 0 .344-.281.629-.63.629h-2.386a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 .63-.63h2.386c.349 0 .63.285.63.63 0 .349-.281.63-.63.63H17.61v1.125h1.755zm-3.855 3.016a.63.63 0 0 1-.63.629.626.626 0 0 1-.51-.262l-2.397-3.274v2.906a.63.63 0 0 1-.629.63.63.63 0 0 1-.631-.63V8.108a.63.63 0 0 1 .631-.63c.2 0 .386.096.504.259l2.403 3.274V8.108a.63.63 0 0 1 .629-.63.63.63 0 0 1 .63.63v4.771zm-5.741 0a.63.63 0 0 1-1.26 0V8.108a.63.63 0 0 1 1.26 0v4.771zm-2.451.629H4.932a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 1.261 0v4.141h1.756c.348 0 .629.283.629.63 0 .344-.281.629-.629.629M24 10.314C24 4.943 18.615.572 12 .572S0 4.943 0 10.314c0 4.811 4.27 8.842 10.035 9.608.391.082.923.258 1.058.59.12.301.079.766.038 1.08l-.164 1.02c-.045.301-.24 1.186 1.049.645 1.291-.539 6.916-4.078 9.436-6.975C23.176 14.393 24 12.458 24 10.314"/>
            </svg>
            <span class="font-medium">綁定 LINE</span>
          </a>
        } @else if (isBotFriend()) {
          <div class="dropdown-item flex items-center justify-between" style="cursor:default">
            <div class="flex items-center gap-2">
              <svg style="width:1rem;height:1rem" viewBox="0 0 24 24" fill="#06C755">
                <path d="M19.365 9.863c.349 0 .63.285.63.631 0 .345-.281.63-.63.63H17.61v1.125h1.755c.349 0 .63.283.63.63 0 .344-.281.629-.63.629h-2.386a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 .63-.63h2.386c.349 0 .63.285.63.63 0 .349-.281.63-.63.63H17.61v1.125h1.755zm-3.855 3.016a.63.63 0 0 1-.63.629.626.626 0 0 1-.51-.262l-2.397-3.274v2.906a.63.63 0 0 1-.629.63.63.63 0 0 1-.631-.63V8.108a.63.63 0 0 1 .631-.63c.2 0 .386.096.504.259l2.403 3.274V8.108a.63.63 0 0 1 .629-.63.63.63 0 0 1 .63.63v4.771zm-5.741 0a.63.63 0 0 1-1.26 0V8.108a.63.63 0 0 1 1.26 0v4.771zm-2.451.629H4.932a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 1.261 0v4.141h1.756c.348 0 .629.283.629.63 0 .344-.281.629-.629.629M24 10.314C24 4.943 18.615.572 12 .572S0 4.943 0 10.314c0 4.811 4.27 8.842 10.035 9.608.391.082.923.258 1.058.59.12.301.079.766.038 1.08l-.164 1.02c-.045.301-.24 1.186 1.049.645 1.291-.539 6.916-4.078 9.436-6.975C23.176 14.393 24 12.458 24 10.314"/>
              </svg>
              <span class="font-medium text-[--green]">LINE 已綁定</span>
            </div>
            <button class="text-xs text-[--text-muted] hover:text-[--red] px-1" (click)="unbindLine($event)">解除</button>
          </div>
        } @else {
          <!-- 已綁定但未加 OA 好友 — 未加好友則推播一律失敗，需引導加好友 -->
          <div class="px-4 py-3 border-t border-[--border]" style="background: rgba(184, 137, 42, 0.06)">
            <div class="flex items-start gap-2 mb-2">
              <svg class="sa-icon shrink-0" style="width:1rem;height:1rem;stroke:var(--yellow);margin-top:2px">
                <use href="/assets/icons/sprite.svg#alert-triangle"></use>
              </svg>
              <div class="min-w-0">
                <div class="text-sm font-semibold text-[--yellow]">尚未加入 LINE 官方帳號</div>
                <div class="text-xs text-[--text-secondary] mt-1 leading-relaxed">
                  LINE 帳號已綁定，但請加入官方帳號為好友後才能收到通知推播。
                </div>
              </div>
            </div>
            <div class="flex items-center gap-2 mt-2">
              <a [href]="lineOaFriendUrl" target="_blank" rel="noopener"
                 (click)="$event.stopPropagation()"
                 class="btn btn-sm btn-primary flex items-center gap-1"
                 [class.pointer-events-none]="!lineOaFriendUrl"
                 [class.opacity-50]="!lineOaFriendUrl">
                <svg style="width:0.875rem;height:0.875rem" viewBox="0 0 24 24" fill="#fff">
                  <path d="M19.365 9.863c.349 0 .63.285.63.631 0 .345-.281.63-.63.63H17.61v1.125h1.755c.349 0 .63.283.63.63 0 .344-.281.629-.63.629h-2.386a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 .63-.63h2.386c.349 0 .63.285.63.63 0 .349-.281.63-.63.63H17.61v1.125h1.755zm-3.855 3.016a.63.63 0 0 1-.63.629.626.626 0 0 1-.51-.262l-2.397-3.274v2.906a.63.63 0 0 1-.629.63.63.63 0 0 1-.631-.63V8.108a.63.63 0 0 1 .631-.63c.2 0 .386.096.504.259l2.403 3.274V8.108a.63.63 0 0 1 .629-.63.63.63 0 0 1 .63.63v4.771zm-5.741 0a.63.63 0 0 1-1.26 0V8.108a.63.63 0 0 1 1.26 0v4.771zm-2.451.629H4.932a.63.63 0 0 1-.63-.629V8.108a.63.63 0 0 1 1.261 0v4.141h1.756c.348 0 .629.283.629.63 0 .344-.281.629-.629.629M24 10.314C24 4.943 18.615.572 12 .572S0 4.943 0 10.314c0 4.811 4.27 8.842 10.035 9.608.391.082.923.258 1.058.59.12.301.079.766.038 1.08l-.164 1.02c-.045.301-.24 1.186 1.049.645 1.291-.539 6.916-4.078 9.436-6.975C23.176 14.393 24 12.458 24 10.314"/>
                </svg>
                加入好友
              </a>
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="refreshLineStatus($event)">重新檢查</button>
              <button type="button" class="text-xs text-[--text-muted] hover:text-[--red] ms-auto px-1"
                      (click)="unbindLine($event)">解除</button>
            </div>
          </div>
        }

        <a class="dropdown-item" href="javascript:void(0)" (click)="logout()">
          <svg class="sa-icon sa-icon-danger" style="width:1rem;height:1rem">
            <use href="/assets/icons/sprite.svg#log-out"></use>
          </svg>
          <span class="text-[--red] font-medium">登出</span>
        </a>
      </div>
    </div>
  `,
  styles: ``
})
export class ProfileDropdown implements OnInit {
  private auth = inject(AuthService);
  private router = inject(Router);
  private lineService = inject(LineService);

  user = this.auth.currentUser;
  avatarUrl = this.auth.avatarUrl;
  avatarPosX = this.auth.avatarPositionX;
  avatarPosY = this.auth.avatarPositionY;
  avatarScale = this.auth.avatarScale;
  departmentName = this.auth.departmentName;
  jobTitleName = this.auth.jobTitleName;

  isLineBound = this.lineService.isBound;
  isBotFriend = this.lineService.isBotFriend;
  lineOaFriendUrl = environment.lineOaFriendUrl;

  ngOnInit() {
    this.lineService.refreshStatus();
  }

  bindLine() {
    this.lineService.getBindUrl().subscribe({
      next: (data) => {
        sessionStorage.setItem('line_bind_state', data.state);
        window.location.href = data.url;
      },
      error: (err) => {
        console.error('[LINE] getBindUrl failed:', err);
      },
    });
  }

  unbindLine(event: Event) {
    event.stopPropagation();
    this.lineService.unbind().subscribe();
  }

  refreshLineStatus(event: Event) {
    event.stopPropagation();
    this.lineService.refreshStatus();
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
