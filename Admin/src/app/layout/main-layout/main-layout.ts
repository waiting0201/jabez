import {Component, OnDestroy, OnInit, inject} from '@angular/core';
import {RouterOutlet} from "@angular/router";
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {Footer} from '../components/footer/footer';
import {Topbar} from '@layouts/components/topbar/topbar';
import {Sidenav} from '@layouts/components/sidenav/sidenav';
import {NotificationService} from '@features/admin/notifications/services/notification.service';
import {AuthService} from '@core/auth/services/auth.service';
import {MissingAllocationPromptService} from '@features/dashboard/services/missing-allocation-prompt.service';

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
  private missingAllocationPrompt = inject(MissingAllocationPromptService);
  private modal = inject(NgbModal);

  ngOnInit() {
    // 強制改密碼期間後端只放行改密碼端點，輪詢只會每分鐘吃一次 403，故不啟動
    if (this.auth.mustChangePassword()) return;
    // 啟動鈴鐺 60 秒輪詢（內部首次即 refresh）；分頁切到背景時自動暫停發送
    this.notification.startPolling();
    // 系統補下班卡的日子：強制逐日補填專案工時佔比（無 attendances:write 或查詢失敗即放行）
    void this.missingAllocationPrompt.check();
  }

  ngOnDestroy() {
    this.notification.stopPolling();
    // 登出 / token 失效導回登入頁時，強制補填視窗不得殘留在登入頁上
    this.modal.dismissAll();
  }
}
