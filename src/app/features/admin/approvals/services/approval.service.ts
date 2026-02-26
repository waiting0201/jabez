import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {ApprovalItem, ApprovalStep} from '../models/approval.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ApprovalService {
  private http = inject(HttpClient);

  getAll(): Observable<ApprovalItem[]> {
    return this.http.get<ApprovalItem[]>(`${environment.apiUrl}/approval-items`);
  }

  getById(id: number): Observable<ApprovalItem> {
    return this.http.get<ApprovalItem>(`${environment.apiUrl}/approval-items/${id}`);
  }

  create(data: Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType'>): Observable<ApprovalItem> {
    return this.http.post<ApprovalItem>(`${environment.apiUrl}/approval-items`, data);
  }

  update(id: number, changes: Partial<Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType'>>): Observable<ApprovalItem> {
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
