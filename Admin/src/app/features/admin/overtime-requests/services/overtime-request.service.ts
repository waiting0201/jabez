import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {OvertimePayEstimate, OvertimeRequest, OvertimeRequestPayload} from '../models/overtime-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class OvertimeRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<OvertimeRequest[]> {
    return this.http.get<OvertimeRequest[]>(`${environment.apiUrl}/overtime-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<OvertimeRequest>> {
    return this.http.get<PagedResult<OvertimeRequest>>(`${environment.apiUrl}/overtime-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<OvertimeRequest> {
    return this.http.get<OvertimeRequest>(`${environment.apiUrl}/overtime-requests/${id}`);
  }

  create(data: OvertimeRequestPayload): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(`${environment.apiUrl}/overtime-requests`, data);
  }

  update(id: number, changes: OvertimeRequestPayload): Observable<OvertimeRequest> {
    return this.http.patch<OvertimeRequest>(`${environment.apiUrl}/overtime-requests/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/overtime-requests/${id}`);
  }

  /**
   * 加班費即時試算（表單用）。對象一律為登入者本人，端點不接受 employeeId。
   * @param date  加班日期 yyyy-MM-dd
   * @param hours 預估總時數
   */
  estimatePay(date: string, hours: number): Observable<OvertimePayEstimate> {
    return this.http.get<OvertimePayEstimate>(`${environment.apiUrl}/overtime-requests/estimate`, {
      params: {date, hours},
    });
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<OvertimeRequest> {
    return this.http.patch<OvertimeRequest>(`${environment.apiUrl}/overtime-requests/${id}/submit`, {});
  }

  /**
   * 取得今日已核准的加班申請（打卡頁用）。
   * 日期須以「本地日期」組字串 —— toISOString() 是 UTC，台北 00:00–08:00 會取到前一天而查不到加班單。
   */
  getApprovedForToday(): Observable<OvertimeRequest[]> {
    const d = new Date();
    const today = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    return this.http.get<OvertimeRequest[]>(`${environment.apiUrl}/overtime-requests`, {
      params: {status: 'approved', date: today},
    });
  }
}
