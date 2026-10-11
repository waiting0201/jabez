import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {ShiftScheduleAdjustment, ShiftScheduleMonth, SaveShiftScheduleRequest} from '../models/shift-schedule.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ShiftScheduleService {
  private http = inject(HttpClient);

  /** 某人某月的排班月曆。不帶 userId ＝ 自己。 */
  getMonth(year: number, month: number, userId?: string): Observable<ShiftScheduleMonth> {
    const params: Record<string, string | number> = {year, month};
    if (userId) params['userId'] = userId;
    return this.http.get<ShiftScheduleMonth>(`${environment.apiUrl}/shift-schedules`, {params});
  }

  /** 整月整批替換。只需送非上班日的格子；國定假日格送了會被後端丟棄。 */
  save(data: SaveShiftScheduleRequest): Observable<ShiftScheduleMonth> {
    return this.http.put<ShiftScheduleMonth>(`${environment.apiUrl}/shift-schedules`, data);
  }

  /** 本人未確認的「活動日覆蓋班表」通知。 */
  getAdjustments(): Observable<ShiftScheduleAdjustment[]> {
    return this.http.get<ShiftScheduleAdjustment[]>(`${environment.apiUrl}/shift-schedules/adjustments`);
  }

  /** 「我知道了」：把本人所有未確認的通知標為已讀。 */
  acknowledgeAdjustments(): Observable<{count: number}> {
    return this.http.post<{count: number}>(`${environment.apiUrl}/shift-schedules/adjustments/ack`, {});
  }
}
