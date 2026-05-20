import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {AdvanceRequest, WriteOffRecord} from '../models/advance-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {UpsertInstallmentsRequest} from '../../approval-tasks/models/approval-task.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class AdvanceRequestService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/advance-requests`;

  getPaged(page: number, pageSize: number): Observable<PagedResult<AdvanceRequest>> {
    return this.http.get<PagedResult<AdvanceRequest>>(this.base, {params: {page, pageSize}});
  }

  getById(id: number): Observable<AdvanceRequest> {
    return this.http.get<AdvanceRequest>(`${this.base}/${id}`);
  }

  createWithFiles(formData: FormData): Observable<AdvanceRequest> {
    return this.http.post<AdvanceRequest>(this.base, formData);
  }

  updateWithFiles(id: number, formData: FormData): Observable<AdvanceRequest> {
    return this.http.patch<AdvanceRequest>(`${this.base}/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  submit(id: number): Observable<AdvanceRequest> {
    return this.http.patch<AdvanceRequest>(`${this.base}/${id}/submit`, {});
  }

  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<{id: number; installmentCount: number}> {
    return this.http.patch<{id: number; installmentCount: number}>(
      `${this.base}/${id}/installments`,
      body,
    );
  }

  // ── 沖銷 ──────────────────────────────────────────────────────────────────

  getWriteOffs(id: number): Observable<WriteOffRecord[]> {
    return this.http.get<WriteOffRecord[]>(`${this.base}/${id}/write-offs`);
  }

  createWriteOff(id: number, formData: FormData): Observable<WriteOffRecord> {
    return this.http.post<WriteOffRecord>(`${this.base}/${id}/write-offs`, formData);
  }

  getWriteOffById(id: number, writeOffId: number): Observable<WriteOffRecord> {
    return this.http.get<WriteOffRecord>(`${this.base}/${id}/write-offs/${writeOffId}`);
  }

  deleteWriteOff(id: number, writeOffId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/write-offs/${writeOffId}`);
  }

}
