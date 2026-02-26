import {Component, inject, OnInit} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {RouterLink} from '@angular/router';
import {AppLogo} from '@app/components/app-logo';
import {ToggleSidenav} from '@layouts/components/topbar/components/toggle-sidenav';
import {ToggleMobileMenu} from '@layouts/components/topbar/components/toggle-mobile-menu';
import {ProfileDropdown} from '@layouts/components/topbar/components/profile-dropdown';
import {ApprovalTaskService} from '@features/admin/approval-tasks/services/approval-task.service';
import {AuthService} from '@core/auth/services/auth.service';

@Component({
  selector: 'app-topbar',
  imports: [
    AppLogo,
    ToggleSidenav,
    ToggleMobileMenu,
    ProfileDropdown,
    AsyncPipe,
    RouterLink,
  ],
  templateUrl: './topbar.html',
  styles: ``
})
export class Topbar implements OnInit {
  private approvalTaskService = inject(ApprovalTaskService);
  private authService = inject(AuthService);

  readonly pendingCount$ = this.approvalTaskService.pendingCount$;
  readonly hasApprovalPermission = this.authService.hasPermission('approval-tasks:read');

  ngOnInit() {
    if (this.hasApprovalPermission) {
      this.approvalTaskService.getAll().subscribe();
    }
  }
}
