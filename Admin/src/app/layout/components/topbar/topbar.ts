import {Component} from '@angular/core';
import {AppLogo} from '@app/components/app-logo';
import {ToggleSidenav} from '@layouts/components/topbar/components/toggle-sidenav';
import {ToggleMobileMenu} from '@layouts/components/topbar/components/toggle-mobile-menu';
import {ProfileDropdown} from '@layouts/components/topbar/components/profile-dropdown';
import {NotificationDropdown} from '@layouts/components/topbar/components/notification-dropdown';

@Component({
  selector: 'app-topbar',
  imports: [
    AppLogo,
    ToggleSidenav,
    ToggleMobileMenu,
    NotificationDropdown,
    ProfileDropdown,
  ],
  templateUrl: './topbar.html',
  styles: ``
})
export class Topbar {
}
