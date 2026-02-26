import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {map, tap} from 'rxjs/operators';
import {ApprovalTask, TaskStatus} from '../models/approval-task.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ApprovalTaskService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<ApprovalTask[]>([]);

  pendingCount$ = this.items$.pipe(map(tasks => tasks.filter(t => t.status === 'pending').length));

  getAll(): Observable<ApprovalTask[]> {
    return this.http.get<ApprovalTask[]>(`${environment.apiUrl}/approval-tasks`).pipe(
      tap(items => this.items$.next(items)),
    );
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<ApprovalTask>> {
    return this.http.get<PagedResult<ApprovalTask>>(`${environment.apiUrl}/approval-tasks`, {params: {page, pageSize}});
  }

  getById(id: number, applicationType?: string): Observable<ApprovalTask> {
    const path = applicationType
      ? `${environment.apiUrl}/approval-tasks/${applicationType}/${id}`
      : `${environment.apiUrl}/approval-tasks/${id}`;
    return this.http.get<ApprovalTask>(path);
  }

  review(id: number, applicationType: string, action: TaskStatus, reviewNote: string): Observable<ApprovalTask> {
    return this.http.patch<ApprovalTask>(
      `${environment.apiUrl}/approval-tasks/${applicationType}/${id}/review`,
      {action, reviewNote, applicationType},
    ).pipe(
      tap(updated => this.items$.next(
        this.items$.getValue().map(t =>
          t.id === id && t.applicationType === applicationType ? updated : t
        )
      )),
    );
  }
}
