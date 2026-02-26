import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {OvertimeRequest} from '../models/overtime-request.model';
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

  create(data: Omit<OvertimeRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<OvertimeRequest> {
    return this.http.post<OvertimeRequest>(`${environment.apiUrl}/overtime-requests`, data);
  }

  update(id: number, changes: Partial<OvertimeRequest>): Observable<OvertimeRequest> {
    return this.http.patch<OvertimeRequest>(`${environment.apiUrl}/overtime-requests/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/overtime-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<OvertimeRequest> {
    return this.http.patch<OvertimeRequest>(`${environment.apiUrl}/overtime-requests/${id}/submit`, {});
  }

  /** 取得今日已核准的加班申請（打卡頁用） */
  getApprovedForToday(): Observable<OvertimeRequest[]> {
    return this.http.get<OvertimeRequest[]>(`${environment.apiUrl}/overtime-requests`, {
      params: {status: 'approved', today: 'true'},
    });
  }
}
