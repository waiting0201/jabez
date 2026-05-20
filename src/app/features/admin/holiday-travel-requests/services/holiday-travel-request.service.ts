import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {HolidayTravelRequest} from '../models/holiday-travel-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class HolidayTravelRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<HolidayTravelRequest[]> {
    return this.http.get<HolidayTravelRequest[]>(`${environment.apiUrl}/holiday-travel-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<HolidayTravelRequest>> {
    return this.http.get<PagedResult<HolidayTravelRequest>>(`${environment.apiUrl}/holiday-travel-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<HolidayTravelRequest> {
    return this.http.get<HolidayTravelRequest>(`${environment.apiUrl}/holiday-travel-requests/${id}`);
  }

  /**
   * 新增假日執行活動申請（使用 FormData 以支援發票附件上傳）
   */
  create(data: FormData): Observable<HolidayTravelRequest> {
    return this.http.post<HolidayTravelRequest>(`${environment.apiUrl}/holiday-travel-requests`, data);
  }

  /**
   * 更新假日執行活動申請（使用 FormData 以支援發票附件上傳）
   */
  update(id: number, data: FormData): Observable<HolidayTravelRequest> {
    return this.http.patch<HolidayTravelRequest>(`${environment.apiUrl}/holiday-travel-requests/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/holiday-travel-requests/${id}`);
  }

  /** 查詢日期範圍內的假日天數（依行事曆資料） */
  countHolidays(startDate: string, endDate: string): Observable<{holidayDays: number | null; hasCalendarData: boolean}> {
    return this.http.get<{holidayDays: number | null; hasCalendarData: boolean}>(
      `${environment.apiUrl}/holiday-travel-requests/count-holidays`, {params: {startDate, endDate}});
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<HolidayTravelRequest> {
    return this.http.patch<HolidayTravelRequest>(`${environment.apiUrl}/holiday-travel-requests/${id}/submit`, {});
  }
}
