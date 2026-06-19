import {Injectable, computed, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, Subscription, of, timer} from 'rxjs';
import {tap} from 'rxjs/operators';
import {ToastrService} from 'ngx-toastr';
import {ApplicationType} from '@features/admin/approvals/models/approval.model';
import {AuthService} from '@core/auth/services/auth.service';
import {environment} from '@/environments/environment';

/** 最近被核准的「我的單」：前端用 approvedAt 與上次已提示時間比對後跳 toast */
export interface RecentApproval {
  type:       ApplicationType;
  id:         number;
  approvedAt: string;
}

export interface NotificationCounts {
  approvals:        Record<ApplicationType, number>;
  myRequests:       Record<ApplicationType, number>;
  recentApprovals:  RecentApproval[];
}

/** 輪詢間隔（毫秒）：60 秒；簽核通知不需秒級即時 */
const POLL_INTERVAL_MS = 60_000;
/** localStorage key：記錄最後一次已 toast 的核准時間，避免重開頁面重複提示 */
const LAST_SEEN_APPROVED_KEY = 'notif:lastSeenApprovedAt';

/**
 * 鈴噹通知件數聚合 Service。
 * - approvals  ：待我簽核（依申請類型分組）
 * - myRequests ：我送出的進行中申請（pending / returned）
 *
 * Refresh 時機：登入後（main-layout 啟動輪詢）+ 每 60 秒輪詢 + 開 dropdown 時 + 簽核 / 送單後。
 * 輪詢更新 signal 時，鈴鐺紅點與 dropdown 明細自動同步（畫面不刷新）；
 * 偵測到「待我簽核增加」或「我的單被核准」時主動跳 toast。
 */
@Injectable({providedIn: 'root'})
export class NotificationService {
  private http    = inject(HttpClient);
  private auth    = inject(AuthService);
  private toastr  = inject(ToastrService);

  readonly approvalCounts  = signal<Record<string, number>>({});
  readonly myRequestCounts = signal<Record<string, number>>({});

  readonly totalCount = computed(() => {
    const sum = (m: Record<string, number>) =>
      Object.values(m).reduce((a, b) => a + (b ?? 0), 0);
    return sum(this.approvalCounts()) + sum(this.myRequestCounts());
  });

  /** toast 比對基準：首次 refresh 只設基準不跳 toast */
  private initialized = false;
  private prevApprovalTotal = 0;
  private lastSeenApprovedAt = localStorage.getItem(LAST_SEEN_APPROVED_KEY) ?? '';

  private pollSub?: Subscription;
  private readonly onVisibilityChange = () => {
    // 由背景切回前景時立即補抓一次（補上暫停期間的更新）
    if (!document.hidden) this.refresh().subscribe();
  };

  refresh(): Observable<NotificationCounts | null> {
    if (!this.auth.currentUser()) return of(null);

    return this.http.get<NotificationCounts>(`${environment.apiUrl}/me/notification-counts`).pipe(
      tap(data => {
        if (!data) return;
        this.approvalCounts.set(data.approvals);
        this.myRequestCounts.set(data.myRequests);
        this.processToasts(data);
      }),
    );
  }

  /** 啟動 60 秒輪詢；分頁切到背景（document.hidden）時略過發送以省請求 */
  startPolling(): void {
    if (this.pollSub) return;
    this.pollSub = timer(0, POLL_INTERVAL_MS).subscribe(() => {
      if (document.hidden) return;
      this.refresh().subscribe();
    });
    document.addEventListener('visibilitychange', this.onVisibilityChange);
  }

  /** 停止輪詢（登出 / 離開登入區） */
  stopPolling(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
    this.initialized = false;
  }

  /** 比對基準後決定是否跳 toast；首次只設基準不跳 */
  private processToasts(data: NotificationCounts): void {
    const approvalTotal = Object.values(data.approvals).reduce((a, b) => a + (b ?? 0), 0);
    const maxApprovedAt = data.recentApprovals.reduce(
      (max, r) => (r.approvedAt > max ? r.approvedAt : max), this.lastSeenApprovedAt);

    if (!this.initialized) {
      this.prevApprovalTotal  = approvalTotal;
      this.lastSeenApprovedAt = maxApprovedAt;
      this.initialized = true;
      return;
    }

    // 待我簽核增加 → 跳 toast
    const delta = approvalTotal - this.prevApprovalTotal;
    if (delta > 0) {
      this.toastr.info(`您有 ${delta} 件新的待簽核`, '簽核通知');
    }
    this.prevApprovalTotal = approvalTotal;

    // 我的單被核准（approvedAt 比上次已提示時間新）→ 跳 toast
    const fresh = data.recentApprovals.filter(r => r.approvedAt > this.lastSeenApprovedAt);
    if (fresh.length > 0) {
      this.toastr.info(`您有 ${fresh.length} 件申請已核准`, '核准通知');
      this.lastSeenApprovedAt = maxApprovedAt;
      localStorage.setItem(LAST_SEEN_APPROVED_KEY, this.lastSeenApprovedAt);
    }
  }
}
