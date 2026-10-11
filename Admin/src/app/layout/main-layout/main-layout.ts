import {Component, OnDestroy, OnInit, inject} from '@angular/core';
import {RouterOutlet} from "@angular/router";
import {Footer} from '../components/footer/footer';
import {Topbar} from '@layouts/components/topbar/topbar';
import {Sidenav} from '@layouts/components/sidenav/sidenav';
import {NotificationService} from '@features/admin/notifications/services/notification.service';
import {AuthService} from '@core/auth/services/auth.service';

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
export class MainLayout implements OnInit, OnDestroy {
  private notification = inject(NotificationService);
  private auth = inject(AuthService);

  ngOnInit() {
    // 強制改密碼期間後端只放行改密碼端點，輪詢只會每分鐘吃一次 403，故不啟動
    if (this.auth.mustChangePassword()) return;
    // 啟動鈴鐺 60 秒輪詢（內部首次即 refresh）；分頁切到背景時自動暫停發送
    this.notification.startPolling();
  }

  ngOnDestroy() {
    this.notification.stopPolling();
  }
}
