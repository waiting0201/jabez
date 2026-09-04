import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {switchMap, map} from 'rxjs/operators';
import {ApprovalTask, ApprovalTaskApplicant, TaskStatus, InstallmentInput} from '../models/approval-task.model';
import {ApplicationType} from '../../approvals/models/approval.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {NotificationService} from '../../notifications/services/notification.service';
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
  private notification = inject(NotificationService);

  getPaged(page: number, pageSize: number, status?: string, paymentStatus?: string, applicationType?: string, submittedByUserId?: string, scope?: string, dateFrom?: string, dateTo?: string): Observable<PagedResult<ApprovalTask>> {
    const params: Record<string, any> = {page, pageSize};
    if (status) params['status'] = status;
    if (paymentStatus) params['paymentStatus'] = paymentStatus;
    if (applicationType) params['applicationType'] = applicationType;
    if (submittedByUserId) params['submittedByUserId'] = submittedByUserId;
    // scope=director：總監室簽核頁籤（與 status 四態組合），僅財務管理部 / 會計室 / Superadmin 可用
    if (scope) params['scope'] = scope;
    // 申請日期（送簽日）區間篩選，dateTo 含當日
    if (dateFrom) params['dateFrom'] = dateFrom;
    if (dateTo) params['dateTo'] = dateTo;
    return this.http.get<PagedResult<ApprovalTask>>(`${environment.apiUrl}/approval-tasks`, {params});
  }

  /** 申請人下拉選項（僅財務體系部門可呼叫，其他人後端回 403） */
  getApplicants(): Observable<ApprovalTaskApplicant[]> {
    return this.http.get<ApprovalTaskApplicant[]>(`${environment.apiUrl}/approval-tasks/applicants`);
  }

  getById(id: number, applicationType?: string): Observable<ApprovalTask> {
    const path = applicationType
      ? `${environment.apiUrl}/approval-tasks/${applicationType}/${id}`
      : `${environment.apiUrl}/approval-tasks/${id}`;
    return this.http.get<ApprovalTask>(path);
  }

  review(id: number, applicationType: string, action: TaskStatus, reviewNote: string, estimatedRefundDate?: string, refundedAt?: string, closeAdvance?: boolean, installments?: InstallmentInput[]): Observable<ApprovalTask> {
    return this.http.patch<ApprovalTask>(
      `${environment.apiUrl}/approval-tasks/${applicationType}/${id}/review`,
      {action, reviewNote, applicationType, estimatedRefundDate, refundedAt, closeAdvance, installments},
    ).pipe(
      switchMap(updated => this.notification.refresh().pipe(map(() => updated))),
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
      switchMap(result => this.notification.refresh().pipe(map(() => result))),
    );
  }
}
