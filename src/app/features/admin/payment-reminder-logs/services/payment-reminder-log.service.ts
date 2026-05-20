import {Injectable, inject} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '@/environments/environment';
import {PaymentReminderLogPagedResult, PaymentReminderRunResult} from '../models/payment-reminder-log.model';

@Injectable({providedIn: 'root'})
export class PaymentReminderLogService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/admin`;

  getPaged(query: {
    from?: string; to?: string; status?: string; triggerSource?: string; financeUserId?: string;
    page: number; pageSize: number;
  }): Observable<PaymentReminderLogPagedResult> {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== null && v !== undefined && v !== '') params = params.set(k, String(v));
    }
    return this.http.get<PaymentReminderLogPagedResult>(`${this.base}/payment-reminder-logs`, {params});
  }

  /** Superadmin 手動觸發撥款提醒（除錯用）*/
  manualRun(): Observable<PaymentReminderRunResult> {
    return this.http.post<PaymentReminderRunResult>(`${this.base}/payment-reminder/run`, {});
  }
}
