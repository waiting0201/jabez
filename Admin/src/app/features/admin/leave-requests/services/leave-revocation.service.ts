import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {CreateLeaveRevocationRequest, LeaveRevocation, RevocableDatesResult} from '../models/leave-revocation.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class LeaveRevocationService {
  private http = inject(HttpClient);

  /**
   * 可銷假日期清單（已排除已銷、進行中的銷假單佔用、今天以前的日期）。
   * 編輯既有草稿時傳 excludeRevocationId，否則自己已勾的日子會從可選清單消失。
   */
  getRevocableDates(leaveRequestId: number, excludeRevocationId?: number): Observable<RevocableDatesResult> {
    const params: Record<string, number> = {};
    if (excludeRevocationId) params['excludeRevocationId'] = excludeRevocationId;
    return this.http.get<RevocableDatesResult>(`${environment.apiUrl}/leave-requests/${leaveRequestId}/revocable-dates`, {params});
  }

  create(leaveRequestId: number, data: CreateLeaveRevocationRequest): Observable<LeaveRevocation> {
    return this.http.post<LeaveRevocation>(`${environment.apiUrl}/leave-requests/${leaveRequestId}/revocations`, data);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<LeaveRevocation>> {
    return this.http.get<PagedResult<LeaveRevocation>>(`${environment.apiUrl}/leave-revocations`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<LeaveRevocation> {
    return this.http.get<LeaveRevocation>(`${environment.apiUrl}/leave-revocations/${id}`);
  }

  update(id: number, changes: Partial<CreateLeaveRevocationRequest>): Observable<LeaveRevocation> {
    return this.http.patch<LeaveRevocation>(`${environment.apiUrl}/leave-revocations/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/leave-revocations/${id}`);
  }

  /** 送出銷假申請（draft/returned → pending，重跑原本的請假簽核流程） */
  submit(id: number): Observable<LeaveRevocation> {
    return this.http.patch<LeaveRevocation>(`${environment.apiUrl}/leave-revocations/${id}/submit`, {});
  }
}
