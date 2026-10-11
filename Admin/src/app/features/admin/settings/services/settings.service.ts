import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {SystemSettings} from '../models/settings.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);

  get(): Observable<SystemSettings> {
    return this.http.get<SystemSettings>(`${environment.apiUrl}/settings`);
  }

  save(changes: Partial<SystemSettings>): Observable<SystemSettings> {
    return this.http.patch<SystemSettings>(`${environment.apiUrl}/settings`, changes);
  }

  /**
   * 設定四週彈性工時切換日。後端只收**未來某月的 1 號**（回溯或月中會 400）。
   * 刻意與上面的 `save()` 分開走：這是高後果、極少動的開關，
   * 不該因為有人來改「加班時數限制」順手按了儲存就被一起送出去。
   */
  setFlexibleWorkStartDate(date: string): Observable<SystemSettings> {
    return this.http.patch<SystemSettings>(`${environment.apiUrl}/settings`, {flexibleWorkStartDate: date});
  }

  /** 清空切換日＝退回舊制。`null` 在 PATCH 語意下是「不變更」，故必須另帶旗標。 */
  clearFlexibleWorkStartDate(): Observable<SystemSettings> {
    return this.http.patch<SystemSettings>(`${environment.apiUrl}/settings`, {clearFlexibleWorkStartDate: true});
  }
}
