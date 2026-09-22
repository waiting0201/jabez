import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {ShiftScheduleMonth, SaveShiftScheduleRequest} from '../models/shift-schedule.model';
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
}
