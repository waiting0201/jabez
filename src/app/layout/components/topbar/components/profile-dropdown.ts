import {Component, inject} from '@angular/core';
import {NgbDropdownModule} from '@ng-bootstrap/ng-bootstrap';
import {Router} from '@angular/router';
import {AuthService} from '@core/auth/services/auth.service';

@Component({
  selector: 'app-profile-dropdown',
  imports: [NgbDropdownModule],
  template: `
    <div ngbDropdown>
      <button type="button" ngbDropdownToggle [title]="user()?.email ?? ''"
              class="btn-system no-arrow bg-transparent flex shrink-0 items-center justify-center"
              aria-label="Open Profile Dropdown">
        <img src="/assets/img/demo/avatars/avatar-admin.png"
             class="profile-image profile-image-md rounded-circle"
             [alt]="user()?.name ?? ''">
      </button>

      <div ngbDropdownMenu class="dropdown-menu dropdown-menu-end dropdown-menu-animated">
        <!-- User info header -->
        <div class="flex items-center gap-3 px-4 py-3 border-b border-[--border]">
          <img src="/assets/img/demo/avatars/avatar-admin.png"
               class="w-10 h-10 rounded-full object-cover shrink-0"
               [alt]="user()?.name ?? ''">
          <div class="min-w-0">
            <div class="text-sm font-semibold text-[--text-primary] truncate">{{ user()?.name }}</div>
            <div class="text-xs text-[--text-muted] truncate">{{ user()?.email }}</div>
          </div>
        </div>

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
export class ProfileDropdown {
  private auth = inject(AuthService);
  private router = inject(Router);

  user = this.auth.currentUser;

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }

}
