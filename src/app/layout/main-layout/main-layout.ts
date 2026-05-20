import {Component, OnInit, inject} from '@angular/core';
import {RouterOutlet} from "@angular/router";
import {Footer} from '../components/footer/footer';
import {Topbar} from '@layouts/components/topbar/topbar';
import {Sidenav} from '@layouts/components/sidenav/sidenav';
import {NotificationService} from '@features/admin/notifications/services/notification.service';

@Component({
  selector: 'app-main-layout',
  imports: [
    RouterOutlet,
    Footer,
    Topbar,
    Sidenav,
  ],
  templateUrl: './main-layout.html',
  styles: ``
})
export class MainLayout implements OnInit {
  private notification = inject(NotificationService);

  ngOnInit() {
    this.notification.refresh().subscribe();
  }
}
