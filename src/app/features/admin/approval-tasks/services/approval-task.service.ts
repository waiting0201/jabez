import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {map, switchMap, tap} from 'rxjs/operators';
import {ApprovalTask, TaskStatus} from '../models/approval-task.model';
import {ApplicationType} from '../../approvals/models/approval.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

/** 批次核准後需補填撥款/退款日的提醒 */
export interface BatchApprovePending {
  applicationType: ApplicationType;
  id: number;
  requestNo: string;
  kind: 'payment' | 'refund';
}

export interface BatchApproveFailure {
  applicationType: ApplicationType;
  id: number;
  reason: string;
}

export interface BatchApproveResult {
  succeeded: number;
  failed: BatchApproveFailure[];
  pendingPayment: BatchApprovePending[];
}

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

  getPaged(page: number, pageSize: number, status?: string, paymentStatus?: string, applicationType?: string): Observable<PagedResult<ApprovalTask>> {
    const params: Record<string, any> = {page, pageSize};
    if (status) params['status'] = status;
    if (paymentStatus) params['paymentStatus'] = paymentStatus;
    if (applicationType) params['applicationType'] = applicationType;
    return this.http.get<PagedResult<ApprovalTask>>(`${environment.apiUrl}/approval-tasks`, {params});
  }

  getById(id: number, applicationType?: string): Observable<ApprovalTask> {
    const path = applicationType
      ? `${environment.apiUrl}/approval-tasks/${applicationType}/${id}`
      : `${environment.apiUrl}/approval-tasks/${id}`;
    return this.http.get<ApprovalTask>(path);
  }

  review(id: number, applicationType: string, action: TaskStatus, reviewNote: string, estimatedPaymentDate?: string, paidAt?: string, closeAdvance?: boolean): Observable<ApprovalTask> {
    return this.http.patch<ApprovalTask>(
      `${environment.apiUrl}/approval-tasks/${applicationType}/${id}/review`,
      {action, reviewNote, applicationType, estimatedPaymentDate, paidAt, closeAdvance},
    ).pipe(
      switchMap(updated => this.getAll().pipe(map(() => updated))),
    );
  }

  closeCase(id: number, applicationType: 'write_off' | 'travel_write_off'): Observable<ApprovalTask> {
    return this.http.patch<ApprovalTask>(
      `${environment.apiUrl}/approval-tasks/${applicationType}/${id}/close`, {},
    );
  }

  /**
   * 批次核准多筆待審申請。僅支援 approved 動作，撥款類不會自動填撥款日。
   * 每筆獨立驗證權限，失敗者回報於 failed；最終 approved 且需補填撥款/退款日者列於 pendingPayment。
   */
  batchApprove(items: { applicationType: string; id: number }[]): Observable<BatchApproveResult> {
    return this.http.post<BatchApproveResult>(
      `${environment.apiUrl}/approval-tasks/batch-approve`,
      { items },
    ).pipe(
      switchMap(result => this.getAll().pipe(map(() => result))),
    );
  }
}
