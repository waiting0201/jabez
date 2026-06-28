import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {ApplicationType, ApprovalFlowSummary, ApprovalItem, ApprovalStep} from '../models/approval.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ApprovalService {
  private http = inject(HttpClient);

  getAll(): Observable<ApprovalItem[]> {
    return this.http.get<ApprovalItem[]>(`${environment.apiUrl}/approval-items`);
  }

  /**
   * 取得指定 ApplicationType 的啟用中流程摘要（精簡版）。
   * 不需 approvals:read 權限（登入即可），供申請表單判斷是否需顯示「指定審核者」欄位。
   * 回傳 null 代表該類型尚未設定啟用中的流程。
   */
  getActiveByType(type: ApplicationType): Observable<ApprovalFlowSummary | null> {
    return this.http.get<ApprovalFlowSummary | null>(
      `${environment.apiUrl}/approval-items/active`,
      {params: {type}},
    );
  }

  getById(id: number): Observable<ApprovalItem> {
    return this.http.get<ApprovalItem>(`${environment.apiUrl}/approval-items/${id}`);
  }

  create(data: Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType' | 'departmentId'>): Observable<ApprovalItem> {
    return this.http.post<ApprovalItem>(`${environment.apiUrl}/approval-items`, data);
  }

  update(id: number, changes: Partial<Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType' | 'departmentId'>>): Observable<ApprovalItem> {
    return this.http.patch<ApprovalItem>(`${environment.apiUrl}/approval-items/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/approval-items/${id}`);
  }

  // ── Steps ──────────────────────────────────────────────────────────────

  addStep(itemId: number, step: Omit<ApprovalStep, 'id'>): Observable<ApprovalItem> {
    return this.http.post<ApprovalItem>(`${environment.apiUrl}/approval-items/${itemId}/steps`, step);
  }

  updateStep(itemId: number, stepId: number, changes: Partial<Omit<ApprovalStep, 'id'>>): Observable<ApprovalItem> {
    return this.http.patch<ApprovalItem>(`${environment.apiUrl}/approval-items/${itemId}/steps/${stepId}`, changes);
  }

  deleteStep(itemId: number, stepId: number): Observable<ApprovalItem> {
    return this.http.delete<ApprovalItem>(`${environment.apiUrl}/approval-items/${itemId}/steps/${stepId}`);
  }
}
