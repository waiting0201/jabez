import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {TravelRequest} from '../models/travel-request.model';

const MOCK_REQUESTS: TravelRequest[] = [
  {
    id: 1,
    destination:   '台南',
    startDate:     new Date('2026-03-10'),
    endDate:       new Date('2026-03-12'),
    estimatedCost: 3000,
    purpose:       '客戶現場拜訪與需求確認',
    projectId:     1,
    projectCode:   'P2024-001',
    approvalStatus: 'pending',
    createdAt:     new Date('2026-03-01'),
  },
  {
    id: 2,
    destination:   '台中',
    startDate:     new Date('2026-02-25'),
    endDate:       new Date('2026-02-26'),
    estimatedCost: 1500,
    purpose:       '供應商工廠參訪',
    projectId:     2,
    projectCode:   'P2024-002',
    approvalStatus: 'approved',
    createdAt:     new Date('2026-02-20'),
    reviewedAt:    new Date('2026-02-24'),
    reviewNote:    '核准',
  },
];

@Injectable({providedIn: 'root'})
export class TravelRequestService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<TravelRequest[]>(MOCK_REQUESTS);

  getAll(): Observable<TravelRequest[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<TravelRequest[]>('/api/travel-requests').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getById(id: number): Observable<TravelRequest | undefined> {
    // return this.http.get<TravelRequest>(`/api/travel-requests/${id}`);
    return of(this.items$.getValue().find(r => r.id === id));
  }

  create(data: Omit<TravelRequest, 'id' | 'createdAt' | 'approvalStatus'>): Observable<TravelRequest> {
    // return this.http.post<TravelRequest>('/api/travel-requests', data).pipe(
    //   tap(item => this.items$.next([...this.items$.getValue(), item])),
    // );
    const item: TravelRequest = {...data, id: Date.now(), approvalStatus: 'pending', createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<TravelRequest>): Observable<TravelRequest> {
    // return this.http.patch<TravelRequest>(`/api/travel-requests/${id}`, changes).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(r => r.id === id ? updated : r))),
    // );
    const updated = this.items$.getValue().map(r => r.id === id ? {...r, ...changes} : r);
    this.items$.next(updated);
    return of(updated.find(r => r.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/travel-requests/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(r => r.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(r => r.id !== id));
    return of(undefined);
  }
}
