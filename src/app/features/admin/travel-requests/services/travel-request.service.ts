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

  create(data: Omit<TravelRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<TravelRequest> {
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

  /** 更新撥款日期（核准後財務部操作） */
  updatePaymentDate(id: number, estimatedPaymentDate?: string, paidAt?: string, estimatedRefundDate?: string, refundedAt?: string, refundedAmount?: number | null): Observable<any> {
    return this.http.patch(`${environment.apiUrl}/travel-requests/${id}/payment-date`, {
      estimatedPaymentDate: estimatedPaymentDate || null,
      paidAt: paidAt || null,
      estimatedRefundDate: estimatedRefundDate || null,
      refundedAt: refundedAt || null,
      refundedAmount: refundedAmount ?? null,
    });
  }

  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<{id: number; estimatedPaymentDate?: string; paidAt?: string; installmentCount: number}> {
    return this.http.patch<{id: number; estimatedPaymentDate?: string; paidAt?: string; installmentCount: number}>(
      `${environment.apiUrl}/travel-requests/${id}/installments`,
      body,
    );
  }
}
