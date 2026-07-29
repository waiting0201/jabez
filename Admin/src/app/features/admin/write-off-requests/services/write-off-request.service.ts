import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {WriteOffRequest, AdvanceSummary, AdvanceWriteOffOverview, UpdateCheckPaymentsRequest} from '../models/write-off-request.model';
import {UpsertInstallmentsRequest} from '../../approval-tasks/models/approval-task.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class WriteOffRequestService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/write-off-requests`;

  getPaged(page: number, pageSize: number): Observable<PagedResult<WriteOffRequest>> {
    return this.http.get<PagedResult<WriteOffRequest>>(this.base, {params: {page, pageSize}});
  }

  getById(id: number): Observable<WriteOffRequest> {
    return this.http.get<WriteOffRequest>(`${this.base}/${id}`);
  }

  /** 依預支單彙總：預支單完整資訊 + 該單底下全部沖銷單 */
  getByAdvance(advanceRequestId: number): Observable<AdvanceWriteOffOverview> {
    return this.http.get<AdvanceWriteOffOverview>(`${this.base}/by-advance/${advanceRequestId}`);
  }

  create(formData: FormData): Observable<WriteOffRequest> {
    return this.http.post<WriteOffRequest>(this.base, formData);
  }

  update(id: number, formData: FormData): Observable<WriteOffRequest> {
    return this.http.patch<WriteOffRequest>(`${this.base}/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  submit(id: number): Observable<WriteOffRequest> {
    return this.http.patch<WriteOffRequest>(`${this.base}/${id}/submit`, {});
  }

  /** 取得已撥款（paidAt 不為空）的預支申請清單，供沖銷申請選擇 */
  getAvailableAdvances(): Observable<AdvanceSummary[]> {
    return this.http.get<AdvanceSummary[]>(`${this.base}/available-advances`);
  }

  /** 差額撥款分期 upsert（僅已核准；財務體系 / Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<unknown> {
    return this.http.patch(`${this.base}/${id}/installments`, body);
  }

  /** 沖銷明細的「支票金額已支付」註記（財務體系 / Superadmin）*/
  updateCheckPayments(id: number, body: UpdateCheckPaymentsRequest): Observable<unknown> {
    return this.http.patch(`${this.base}/${id}/check-payments`, body);
  }
}
