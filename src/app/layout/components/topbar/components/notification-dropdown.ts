import {Component, computed, inject} from '@angular/core';
import {Router} from '@angular/router';
import {NgbDropdownModule} from '@ng-bootstrap/ng-bootstrap';
import {NotificationService} from '@features/admin/notifications/services/notification.service';
import {AuthService} from '@core/auth/services/auth.service';
import {ApplicationType, APPLICATION_TYPE_LABELS} from '@features/admin/approvals/models/approval.model';

/** 申請類型 → 列表頁路由 */
const TYPE_ROUTES: Record<ApplicationType, string> = {
  payment_request:  '/admin/payment-requests',
  leave:            '/admin/leave-requests',
  travel:           '/admin/travel-requests',
  overtime:         '/admin/overtime-requests',
  advance:          '/admin/advance-requests',
  write_off:        '/admin/write-off-requests',
  travel_write_off: '/admin/travel-write-off-requests',
  holiday_travel:   '/admin/holiday-travel-requests',
  travel_payment:   '/admin/travel-payment-requests',
};

/** 申請類型 → 對應的 read 權限代碼（同 admin.routes.ts 內的 permission） */
const TYPE_PERMISSIONS: Record<ApplicationType, string> = {
  payment_request:  'payment-requests:read',
  leave:            'leave-requests:read',
  travel:           'travel-requests:read',
  overtime:         'overtime-requests:read',
  advance:          'advance-requests:read',
  write_off:        'write-off-requests:read',
  travel_write_off: 'travel-write-off-requests:read',
  holiday_travel:   'holiday-travel-requests:read',
  travel_payment:   'travel-payment-requests:read',
};

/** 固定排列順序（與設計討論一致） */
const TYPE_ORDER: ApplicationType[] = [
  'payment_request',
  'advance',
  'write_off',
  'travel',
  'travel_payment',
  'travel_write_off',
  'holiday_travel',
  'leave',
  'overtime',
];

@Component({
  selector: 'app-notification-dropdown',
  imports: [NgbDropdownModule],
  template: `
    <div ngbDropdown (openChange)="onOpenChange($event)">
      <button type="button" ngbDropdownToggle
              class="btn btn-system position-relative no-arrow"
              aria-label="通知"
              [title]="totalCount() > 0 ? ('共 ' + totalCount() + ' 件待辦') : '沒有待辦事項'">
        @if (totalCount() > 0) {
          <span class="badge badge-icon pos-top pos-end bg-danger">{{ totalCount() }}</span>
        }
        <svg class="sa-icon sa-icon-2x">
          <use href="/assets/icons/sprite.svg#bell"></use>
        </svg>
      </button>

      <div ngbDropdownMenu class="dropdown-menu dropdown-menu-end dropdown-menu-animated" style="min-width: 260px">
        @let approvalSum = approvalTotal();
        @let myRequestTypes = visibleMyRequestTypes();
        @let showApprovalSection = hasApprovalPermission() && approvalSum > 0;

        @if (showApprovalSection) {
          <div class="dropdown-header">待我簽核</div>
          <a class="dropdown-item"
             style="justify-content: space-between"
             (click)="navigateApproval($event)">
            <span>簽核作業</span>
            <span class="badge bg-danger">{{ approvalSum }}</span>
          </a>
        }

        @if (showApprovalSection && myRequestTypes.length > 0) {
          <div class="dropdown-divider"></div>
        }

        @if (myRequestTypes.length > 0) {
          <div class="dropdown-header">我的申請</div>
          @for (type of myRequestTypes; track type) {
            <a class="dropdown-item"
               [class.disabled]="!canAccess(type)"
               style="justify-content: space-between"
               (click)="navigate(type, $event)">
              <span>{{ labels[type] }}</span>
              <span class="badge bg-warning">{{ myRequestCount(type) }}</span>
            </a>
          }
        }

        @if (!showApprovalSection && myRequestTypes.length === 0) {
          <div class="px-4 py-3 text-sm" style="color: var(--text-muted)">
            目前沒有待辦事項
          </div>
        }
      </div>
    </div>
  `,
  styles: ``,
})
export class NotificationDropdown {
  private notification = inject(NotificationService);
  private auth         = inject(AuthService);
  private router       = inject(Router);

  readonly types  = TYPE_ORDER;
  readonly labels = APPLICATION_TYPE_LABELS;

  readonly totalCount = this.notification.totalCount;
  readonly hasApprovalPermission = computed(() => this.auth.hasPermission('approval-tasks:read'));

  /** 待我簽核：彙總所有類型件數成單一「簽核作業」項目 */
  readonly approvalTotal = computed(() => {
    const counts = this.notification.approvalCounts();
    return Object.values(counts).reduce((a, b) => a + (b ?? 0), 0);
  });

  /** 只列出有件數的類型；無件數類型不顯示 */
  readonly visibleMyRequestTypes = computed<ApplicationType[]>(() => {
    const counts = this.notification.myRequestCounts();
    return TYPE_ORDER.filter(t => (counts[t] ?? 0) > 0);
  });

  myRequestCount(type: ApplicationType): number {
    return this.notification.myRequestCounts()[type] ?? 0;
  }

  canAccess(type: ApplicationType): boolean {
    return this.auth.hasPermission(TYPE_PERMISSIONS[type]);
  }

  onOpenChange(open: boolean) {
    if (open) {
      this.notification.refresh().subscribe();
    }
  }

  navigate(type: ApplicationType, event: Event) {
    event.preventDefault();
    if (!this.canAccess(type)) return;
    this.router.navigateByUrl(TYPE_ROUTES[type]);
  }

  navigateApproval(event: Event) {
    event.preventDefault();
    this.router.navigateByUrl('/admin/approval-tasks');
  }
}
