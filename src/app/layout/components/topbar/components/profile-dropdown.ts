import {Component, inject} from '@angular/core';
import {NgbDropdownModule} from '@ng-bootstrap/ng-bootstrap';
import {Router} from '@angular/router';
import {AuthService} from '@core/auth/services/auth.service';

@Component({
  selector: 'app-profile-dropdown',
  imports: [NgbDropdownModule],
  template: `
    <div ngbDropdown>
      <button type="button" ngbDropdownToggle title="drlantern@getwebora.com" data-bs-toggle="dropdown"
              class="btn-system no-arrow bg-transparent d-flex flex-shrink-0 align-items-center justify-content-center"
              aria-label="Open Profile Dropdown">
        <img src="/assets/img/demo/avatars/avatar-admin.png" class="profile-image profile-image-md rounded-circle"
             alt="Sunny A.">
      </button>

      <div ngbDropdownMenu class="dropdown-menu dropdown-menu-animated">
        <div class="notification-header rounded-top mb-2">
          <div class="d-flex flex-row align-items-center mt-1 mb-1 color-white">
                <span class="status status-success d-inline-block me-2">
                    <img src="/assets/img/demo/avatars/avatar-admin.png" class="profile-image rounded-circle"
                         alt="Sunny A.">
                </span>
            <div class="info-card-text">
              <div class="fs-lg text-truncate text-truncate-lg">Sunny A.</div>
              <span class="text-truncate text-truncate-md opacity-80 fs-sm">sunnya&#64;sadim.com</span>
            </div>
          </div>
        </div>

        <div class="dropdown-divider m-0"></div>

        <a class="dropdown-item py-3 fw-500 d-flex justify-content-between" href="javascript:void(0)" (click)="logout()">
          <span class="text-danger" data-i18n="drpdwn.page-logout">登出</span>
        </a>
      </div>
    </div>
  `,
  styles: ``
})
export class ProfileDropdown {
  private auth = inject(AuthService);
  private router = inject(Router);

  logout() {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }

}
