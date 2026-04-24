import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  LeaveRequest, AnnualQuota, CompensatoryHours, CeremonialQuota,
  MarriageQuota, MaternityStatus, BereavementQuota, SeniorExecutiveEligibility,
} from '../models/leave-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class LeaveRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(`${environment.apiUrl}/leave-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<LeaveRequest>> {
    return this.http.get<PagedResult<LeaveRequest>>(`${environment.apiUrl}/leave-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<LeaveRequest> {
    return this.http.get<LeaveRequest>(`${environment.apiUrl}/leave-requests/${id}`);
  }

  create(data: Omit<LeaveRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${environment.apiUrl}/leave-requests`, data);
  }

  update(id: number, changes: Partial<LeaveRequest>): Observable<LeaveRequest> {
    return this.http.patch<LeaveRequest>(`${environment.apiUrl}/leave-requests/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/leave-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<LeaveRequest> {
    return this.http.patch<LeaveRequest>(`${environment.apiUrl}/leave-requests/${id}/submit`, {});
  }

  /** 查詢當前使用者的可補休時數 */
  getCompensatoryHours(): Observable<CompensatoryHours> {
    return this.http.get<CompensatoryHours>(`${environment.apiUrl}/leave-requests/compensatory-hours`);
  }

  /** 查詢當前使用者的年假額度 */
  getAnnualQuota(): Observable<AnnualQuota> {
    return this.http.get<AnnualQuota>(`${environment.apiUrl}/leave-requests/annual-quota`);
  }

  /** 查詢當前使用者的歲時祭儀假額度（僅原住民） */
  getCeremonialQuota(): Observable<CeremonialQuota> {
    return this.http.get<CeremonialQuota>(`${environment.apiUrl}/leave-requests/ceremonial-quota`);
  }

  /** 查詢當前使用者的婚假配額（上限 8 天） */
  getMarriageQuota(): Observable<MarriageQuota> {
    return this.http.get<MarriageQuota>(`${environment.apiUrl}/leave-requests/marriage-quota`);
  }

  /** 查詢當前使用者的產假狀態（檢查是否已有活躍申請） */
  getMaternityStatus(): Observable<MaternityStatus> {
    return this.http.get<MaternityStatus>(`${environment.apiUrl}/leave-requests/maternity-status`);
  }

  /** 查詢當前使用者的喪假配額（依親屬關係） */
  getBereavementQuota(relationship: string): Observable<BereavementQuota> {
    return this.http.get<BereavementQuota>(`${environment.apiUrl}/leave-requests/bereavement-quota`, {
      params: {relationship},
    });
  }

  /** 查詢當前使用者高階主管假適用性（JobTitle.Level ≤ 3） */
  getSeniorExecutiveEligibility(): Observable<SeniorExecutiveEligibility> {
    return this.http.get<SeniorExecutiveEligibility>(`${environment.apiUrl}/leave-requests/senior-executive-eligibility`);
  }
}
