import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {OvertimeRequest} from '../models/overtime-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {AuthService} from '@core/auth/services/auth.service';

function daysAgo(n: number): Date { const d = new Date(); d.setDate(d.getDate() - n); return d; }
function daysFromNow(n: number): Date { const d = new Date(); d.setDate(d.getDate() + n); return d; }

const MOCK_REQUESTS: OvertimeRequest[] = [
  {
    id: 1,
    employeeId: '1',
    overtimeDate:    new Date(),          // 今天
    projectIds:      [1, 3],
    projectCodes:    ['P2024-001', 'P2024-003'],
    estimatedHours:  3,
    reason:          '專案趕工，需加班完成模組開發與系統測試',
    approvalStatus:  'approved',
    createdAt:       daysAgo(1),
    reviewedAt:      daysAgo(1),
    reviewNote:      '核准',
  },
  {
    id: 2,
    employeeId: '2',
    overtimeDate:    daysFromNow(1),      // 明天
    projectIds:      [2],
    projectCodes:    ['P2024-002'],
    estimatedHours:  2,
    reason:          '客戶報告截止日前需完成',
    approvalStatus:  'pending',
    createdAt:       new Date(),
  },
];

@Injectable({providedIn: 'root'})
export class OvertimeRequestService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private items$ = new BehaviorSubject<OvertimeRequest[]>(MOCK_REQUESTS);

  getAll(): Observable<OvertimeRequest[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<OvertimeRequest[]>('/api/overtime-requests').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<OvertimeRequest>> {
    // ─── 真實 API（後端就緒時啟用，後端依 JWT 自動過濾）────
    // return this.http.get<{success: boolean; data: PagedResult<OvertimeRequest>}>(
    //   '/api/overtime-requests', {params: {page, pageSize}}
    // ).pipe(map(r => r.data!));

    // ─── Mock（前端開發用，模擬只能看自己的資料）─────────────
    const uid = this.auth.currentUser()?.id;
    const all = this.items$.getValue().filter(r => r.employeeId === uid);
    const start = (page - 1) * pageSize;
    const totalPages = Math.max(1, Math.ceil(all.length / pageSize));
    return of({items: all.slice(start, start + pageSize), totalCount: all.length, page, pageSize, totalPages});
  }

  getById(id: number): Observable<OvertimeRequest | undefined> {
    // return this.http.get<OvertimeRequest>(`/api/overtime-requests/${id}`);
    return of(this.items$.getValue().find(r => r.id === id));
  }

  create(data: Omit<OvertimeRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<OvertimeRequest> {
    // return this.http.post<OvertimeRequest>('/api/overtime-requests', data).pipe(
    //   tap(item => this.items$.next([...this.items$.getValue(), item])),
    // );
    const uid = this.auth.currentUser()?.id;
    const item: OvertimeRequest = {...data, id: Date.now(), employeeId: uid, approvalStatus: 'pending', createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<OvertimeRequest>): Observable<OvertimeRequest> {
    // return this.http.patch<OvertimeRequest>(`/api/overtime-requests/${id}`, changes).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(r => r.id === id ? updated : r))),
    // );
    const updated = this.items$.getValue().map(r => r.id === id ? {...r, ...changes} : r);
    this.items$.next(updated);
    return of(updated.find(r => r.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/overtime-requests/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(r => r.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(r => r.id !== id));
    return of(undefined);
  }

  /** 取得今日已核准的加班申請（打卡頁用） */
  getApprovedForToday(): Observable<OvertimeRequest[]> {
    // ─── 真實 API 可加 query param filter ─────────────────
    const uid = this.auth.currentUser()?.id;
    const today = new Date().toISOString().split('T')[0];
    const approved = this.items$.getValue().filter(
      r => r.employeeId === uid &&
           r.approvalStatus === 'approved' &&
           new Date(r.overtimeDate).toISOString().split('T')[0] === today
    );
    return of(approved);
  }
}
