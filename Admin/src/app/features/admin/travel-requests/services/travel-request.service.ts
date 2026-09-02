import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {TravelRequest} from '../models/travel-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {UpsertInstallmentsRequest} from '../../approval-tasks/models/approval-task.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class TravelRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<TravelRequest[]> {
    return this.http.get<TravelRequest[]>(`${environment.apiUrl}/travel-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<TravelRequest>> {
    return this.http.get<PagedResult<TravelRequest>>(`${environment.apiUrl}/travel-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<TravelRequest> {
    return this.http.get<TravelRequest>(`${environment.apiUrl}/travel-requests/${id}`);
  }

  create(data: Omit<TravelRequest, 'id' | 'requestNo' | 'createdAt' | 'submittedAt' | 'approvalStatus'>): Observable<TravelRequest> {
    return this.http.post<TravelRequest>(`${environment.apiUrl}/travel-requests`, data);
  }

  update(id: number, changes: Partial<TravelRequest>): Observable<TravelRequest> {
    return this.http.patch<TravelRequest>(`${environment.apiUrl}/travel-requests/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/travel-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<TravelRequest> {
    return this.http.patch<TravelRequest>(`${environment.apiUrl}/travel-requests/${id}/submit`, {});
  }

  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<{id: number; installmentCount: number}> {
    return this.http.patch<{id: number; installmentCount: number}>(
      `${environment.apiUrl}/travel-requests/${id}/installments`,
      body,
    );
  }
}
