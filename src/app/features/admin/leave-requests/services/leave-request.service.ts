import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {LeaveRequest} from '../models/leave-request.model';

const MOCK_REQUESTS: LeaveRequest[] = [
  {
    id: 1,
    leaveType: 'annual',
    startDate: new Date('2026-03-01'),
    endDate:   new Date('2026-03-05'),
    days:      5,
    reason:    '個人旅遊',
    approvalStatus: 'pending',
    createdAt: new Date('2026-02-10'),
  },
  {
    id: 2,
    leaveType: 'sick',
    startDate: new Date('2026-02-20'),
    endDate:   new Date('2026-02-21'),
    days:      2,
    reason:    '身體不適就醫',
    approvalStatus: 'approved',
    createdAt:  new Date('2026-02-20'),
    reviewedAt: new Date('2026-02-20'),
    reviewNote: '核准',
  },
  {
    id: 3,
    leaveType: 'compensatory',
    startDate: new Date('2026-02-15'),
    endDate:   new Date('2026-02-15'),
    days:      1,
    reason:    '補休加班時數',
    approvalStatus: 'rejected',
    createdAt:  new Date('2026-02-13'),
    reviewedAt: new Date('2026-02-14'),
    reviewNote: '補休時數不足，請確認後重新申請',
  },
];

@Injectable({providedIn: 'root'})
export class LeaveRequestService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<LeaveRequest[]>(MOCK_REQUESTS);

  getAll(): Observable<LeaveRequest[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<LeaveRequest[]>('/api/leave-requests').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getById(id: number): Observable<LeaveRequest | undefined> {
    // return this.http.get<LeaveRequest>(`/api/leave-requests/${id}`);
    return of(this.items$.getValue().find(r => r.id === id));
  }

  create(data: Omit<LeaveRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<LeaveRequest> {
    // return this.http.post<LeaveRequest>('/api/leave-requests', data).pipe(
    //   tap(item => this.items$.next([...this.items$.getValue(), item])),
    // );
    const item: LeaveRequest = {...data, id: Date.now(), approvalStatus: 'pending', createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<LeaveRequest>): Observable<LeaveRequest> {
    // return this.http.patch<LeaveRequest>(`/api/leave-requests/${id}`, changes).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(r => r.id === id ? updated : r))),
    // );
    const updated = this.items$.getValue().map(r => r.id === id ? {...r, ...changes} : r);
    this.items$.next(updated);
    return of(updated.find(r => r.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/leave-requests/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(r => r.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(r => r.id !== id));
    return of(undefined);
  }
}
