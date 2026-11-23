import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  CalendarDay, CalendarDayCreateDto, CalendarDayUpdateDto, CalendarImportResult,
  DoublePayDay, DoublePayDayCreateDto,
} from '../models/calendar-day.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class CalendarDayService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/calendar-days`;

  /** 查詢指定年份所有日曆資料 */
  getByYear(year: number): Observable<CalendarDay[]> {
    return this.http.get<CalendarDay[]>(this.base, {params: {year}});
  }

  /** 從政府 API 匯入指定年份行事曆 */
  importYear(year: number): Observable<CalendarImportResult> {
    return this.http.post<CalendarImportResult>(`${this.base}/import`, null, {params: {year}});
  }

  /** 手動新增單筆日曆資料 */
  create(data: CalendarDayCreateDto): Observable<CalendarDay> {
    return this.http.post<CalendarDay>(this.base, data);
  }

  /** 更新單筆日曆資料 */
  update(id: number, changes: CalendarDayUpdateDto): Observable<CalendarDay> {
    return this.http.put<CalendarDay>(`${this.base}/${id}`, changes);
  }

  /** 刪除單筆日曆資料 */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /** 查詢指定年份的加倍工資日 */
  getDoublePayDays(year: number): Observable<DoublePayDay[]> {
    return this.http.get<DoublePayDay[]>(`${this.base}/double-pay`, {params: {year}});
  }

  /** 新增加倍工資日 */
  createDoublePayDay(data: DoublePayDayCreateDto): Observable<DoublePayDay> {
    return this.http.post<DoublePayDay>(`${this.base}/double-pay`, data);
  }

  /** 刪除加倍工資日 */
  deleteDoublePayDay(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/double-pay/${id}`);
  }
}
