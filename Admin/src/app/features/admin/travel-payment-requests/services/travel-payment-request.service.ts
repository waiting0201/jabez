import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {TravelPaymentRequest} from '../models/travel-payment-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {UpsertInstallmentsRequest} from '../../approval-tasks/models/approval-task.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class TravelPaymentRequestService {
  private http = inject(HttpClient);

  getPaged(page: number, pageSize: number): Observable<PagedResult<TravelPaymentRequest>> {
    return this.http.get<PagedResult<TravelPaymentRequest>>(`${environment.apiUrl}/travel-payment-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<TravelPaymentRequest> {
    return this.http.get<TravelPaymentRequest>(`${environment.apiUrl}/travel-payment-requests/${id}`);
  }

  create(formData: FormData): Observable<TravelPaymentRequest> {
    return this.http.post<TravelPaymentRequest>(`${environment.apiUrl}/travel-payment-requests`, formData);
  }

  update(id: number, formData: FormData): Observable<TravelPaymentRequest> {
    return this.http.patch<TravelPaymentRequest>(`${environment.apiUrl}/travel-payment-requests/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/travel-payment-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<TravelPaymentRequest> {
    return this.http.patch<TravelPaymentRequest>(`${environment.apiUrl}/travel-payment-requests/${id}/submit`, {});
  }

  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<{id: number; installmentCount: number}> {
    return this.http.patch<{id: number; installmentCount: number}>(
      `${environment.apiUrl}/travel-payment-requests/${id}/installments`,
      body,
    );
  }
}
