import {Component, inject} from '@angular/core';
import {AsyncPipe} from '@angular/common';
import {RouterLink} from '@angular/router';
import {AppLogo} from '@app/components/app-logo';
import {ToggleSidenav} from '@layouts/components/topbar/components/toggle-sidenav';
import {ToggleMobileMenu} from '@layouts/components/topbar/components/toggle-mobile-menu';
import {ProfileDropdown} from '@layouts/components/topbar/components/profile-dropdown';
import {ApprovalTaskService} from '@features/admin/approval-tasks/services/approval-task.service';

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
export class Topbar {
  readonly pendingCount$ = inject(ApprovalTaskService).pendingCount$;
}
