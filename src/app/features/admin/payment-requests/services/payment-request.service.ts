import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {PaymentRequest} from '../models/payment-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class PaymentRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<PaymentRequest[]> {
    return this.http.get<PaymentRequest[]>(`${environment.apiUrl}/payment-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<PaymentRequest>> {
    return this.http.get<PagedResult<PaymentRequest>>(`${environment.apiUrl}/payment-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<PaymentRequest> {
    return this.http.get<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}`);
  }

  createWithFiles(formData: FormData): Observable<PaymentRequest> {
    return this.http.post<PaymentRequest>(`${environment.apiUrl}/payment-requests`, formData);
  }

  updateWithFiles(id: number, formData: FormData): Observable<PaymentRequest> {
    return this.http.patch<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/payment-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<PaymentRequest> {
    return this.http.patch<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}/submit`, {});
  }
}
