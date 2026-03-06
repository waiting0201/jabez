import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {map, switchMap, tap} from 'rxjs/operators';
import {ApprovalTask, TaskStatus} from '../models/approval-task.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ApprovalTaskService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<ApprovalTask[]>([]);

  pendingCount$ = this.items$.pipe(map(tasks => tasks.length));

  /** 拉取所有待審核任務（解包 PagedResult），更新 items$ 供 pendingCount$ 使用 */
  getAll(): Observable<ApprovalTask[]> {
    return this.http.get<PagedResult<ApprovalTask>>(`${environment.apiUrl}/approval-tasks`, {
      params: {page: 1, pageSize: 100, status: 'pending'},
    }).pipe(
      map(result => result.items ?? []),
      tap(items => this.items$.next(items)),
    );
  }

  getPaged(page: number, pageSize: number, status?: string): Observable<PagedResult<ApprovalTask>> {
    const params: Record<string, any> = {page, pageSize};
    if (status) params['status'] = status;
    return this.http.get<PagedResult<ApprovalTask>>(`${environment.apiUrl}/approval-tasks`, {params});
  }

  getById(id: number, applicationType?: string): Observable<ApprovalTask> {
    const path = applicationType
      ? `${environment.apiUrl}/approval-tasks/${applicationType}/${id}`
      : `${environment.apiUrl}/approval-tasks/${id}`;
    return this.http.get<ApprovalTask>(path);
  }

  review(id: number, applicationType: string, action: TaskStatus, reviewNote: string, estimatedPaymentDate?: string, paidAt?: string): Observable<ApprovalTask> {
    return this.http.patch<ApprovalTask>(
      `${environment.apiUrl}/approval-tasks/${applicationType}/${id}/review`,
      {action, reviewNote, applicationType, estimatedPaymentDate, paidAt},
    ).pipe(
      switchMap(updated => this.getAll().pipe(map(() => updated))),
    );
  }
}
