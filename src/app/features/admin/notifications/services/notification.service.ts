import {Injectable, computed, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {ApplicationType} from '@features/admin/approvals/models/approval.model';
import {AuthService} from '@core/auth/services/auth.service';
import {environment} from '@/environments/environment';

export interface NotificationCounts {
  approvals:  Record<ApplicationType, number>;
  myRequests: Record<ApplicationType, number>;
}

/**
 * 鈴噹通知件數聚合 Service。
 * - approvals  ：待我簽核（依申請類型分組）
 * - myRequests ：我送出的進行中申請（pending / returned）
 *
 * Refresh 時機：登入後（main-layout init）+ 開 dropdown 時 + 簽核 / 送單後。
 */
@Injectable({providedIn: 'root'})
export class NotificationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  readonly approvalCounts  = signal<Record<string, number>>({});
  readonly myRequestCounts = signal<Record<string, number>>({});

  readonly totalCount = computed(() => {
    const sum = (m: Record<string, number>) =>
      Object.values(m).reduce((a, b) => a + (b ?? 0), 0);
    return sum(this.approvalCounts()) + sum(this.myRequestCounts());
  });

  refresh(): Observable<NotificationCounts | null> {
    if (!this.auth.currentUser()) return of(null);

    return this.http.get<NotificationCounts>(`${environment.apiUrl}/me/notification-counts`).pipe(
      tap(data => {
        if (data) {
          this.approvalCounts.set(data.approvals);
          this.myRequestCounts.set(data.myRequests);
        }
      }),
    );
  }
}
