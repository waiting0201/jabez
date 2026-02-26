import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {LeaveRequest} from '../models/leave-request.model';
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
}
