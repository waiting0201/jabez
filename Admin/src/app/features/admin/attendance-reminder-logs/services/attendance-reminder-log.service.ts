import {Injectable, inject} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '@/environments/environment';
import {
  AttendanceReminderLog,
  AttendanceReminderLogQuery,
  AttendanceReminderLogStats,
  PagedResult,
} from '../models/attendance-reminder-log.model';

@Injectable({providedIn: 'root'})
export class AttendanceReminderLogService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/admin/attendance-reminder-logs`;

  /** 列表（分頁 + 篩選） */
  getPaged(query: AttendanceReminderLogQuery): Observable<PagedResult<AttendanceReminderLog>> {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== null && v !== undefined && v !== '') {
        params = params.set(k, String(v));
      }
    }
    return this.http.get<PagedResult<AttendanceReminderLog>>(this.base, {params});
  }

  /** 統計卡（今日推播 / 失敗 / 批次 + 最近 7 天趨勢） */
  getStats(): Observable<AttendanceReminderLogStats> {
    return this.http.get<AttendanceReminderLogStats>(`${this.base}/stats`);
  }

  /** 同一次 tick 全部紀錄 */
  getByBatchId(batchId: string): Observable<AttendanceReminderLog[]> {
    return this.http.get<AttendanceReminderLog[]>(`${this.base}/batches/${batchId}`);
  }

  /** 單筆詳情 */
  getById(id: number): Observable<AttendanceReminderLog> {
    return this.http.get<AttendanceReminderLog>(`${this.base}/${id}`);
  }
}
