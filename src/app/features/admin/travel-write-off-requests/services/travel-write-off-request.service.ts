import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {TravelWriteOffRequest, TravelSummary} from '../models/travel-write-off-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class TravelWriteOffRequestService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/travel-write-off-requests`;

  getPaged(page: number, pageSize: number): Observable<PagedResult<TravelWriteOffRequest>> {
    return this.http.get<PagedResult<TravelWriteOffRequest>>(this.base, {params: {page, pageSize}});
  }

  getById(id: number): Observable<TravelWriteOffRequest> {
    return this.http.get<TravelWriteOffRequest>(`${this.base}/${id}`);
  }

  create(formData: FormData): Observable<TravelWriteOffRequest> {
    return this.http.post<TravelWriteOffRequest>(this.base, formData);
  }

  update(id: number, formData: FormData): Observable<TravelWriteOffRequest> {
    return this.http.patch<TravelWriteOffRequest>(`${this.base}/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  submit(id: number): Observable<TravelWriteOffRequest> {
    return this.http.patch<TravelWriteOffRequest>(`${this.base}/${id}/submit`, {});
  }

  /** 取得已核准（approvalStatus = approved）的出差申請清單，供沖銷申請選擇 */
  getAvailableTravels(): Observable<TravelSummary[]> {
    return this.http.get<TravelSummary[]>(`${this.base}/available-travels`);
  }
}
