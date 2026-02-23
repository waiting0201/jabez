import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {ApprovalItem, ApprovalStep} from '../models/approval.model';

const MOCK_ITEMS: ApprovalItem[] = [
  {
    id: 1,
    name: '請假申請',
    code: 'leave_request',
    description: '員工請假需經主管審核',
    isActive: true,
    applicationType: 'leave',
    steps: [
      {id: 1, stepOrder: 1, departmentId: 1, departmentName: '管理部', jobTitleId: 4, jobTitleName: '部門主管', note: '直屬主管核可'},
      {id: 2, stepOrder: 2, departmentId: 1, departmentName: '管理部', jobTitleId: 4, jobTitleName: '部門主管', note: '人事部門確認'},
    ],
    createdAt: new Date('2024-01-01'),
  },
  {
    id: 2,
    name: '採購申請',
    code: 'purchase_request',
    description: '物品採購需逐級審核',
    isActive: true,
    applicationType: 'payment_request',
    steps: [
      {id: 3, stepOrder: 1, jobTitleId: 4, jobTitleName: '部門主管', note: '部門主管審核'},
      {id: 4, stepOrder: 2, departmentId: 1, departmentName: '管理部', jobTitleId: 4, jobTitleName: '部門主管', note: '管理部核准'},
    ],
    createdAt: new Date('2024-01-01'),
  },
];

@Injectable({providedIn: 'root'})
export class ApprovalService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<ApprovalItem[]>(MOCK_ITEMS);

  getAll(): Observable<ApprovalItem[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<ApprovalItem[]>('/api/approval-items').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getById(id: number): Observable<ApprovalItem | undefined> {
    // return this.http.get<ApprovalItem>(`/api/approval-items/${id}`);
    return of(this.items$.getValue().find(a => a.id === id));
  }

  create(data: Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType'>): Observable<ApprovalItem> {
    // return this.http.post<ApprovalItem>('/api/approval-items', data).pipe(
    //   tap(item => this.items$.next([...this.items$.getValue(), item])),
    // );
    const item: ApprovalItem = {...data, id: Date.now(), steps: [], createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<Pick<ApprovalItem, 'name' | 'code' | 'description' | 'isActive' | 'applicationType'>>): Observable<ApprovalItem> {
    // return this.http.patch<ApprovalItem>(`/api/approval-items/${id}`, changes).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(a => a.id === id ? updated : a))),
    // );
    const updated = this.items$.getValue().map(a => a.id === id ? {...a, ...changes} : a);
    this.items$.next(updated);
    return of(updated.find(a => a.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/approval-items/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(a => a.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(a => a.id !== id));
    return of(undefined);
  }

  // ── Steps ──────────────────────────────────────────────────────────────

  addStep(itemId: number, step: Omit<ApprovalStep, 'id'>): Observable<ApprovalItem> {
    // return this.http.post<ApprovalItem>(`/api/approval-items/${itemId}/steps`, step).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(a => a.id === itemId ? updated : a))),
    // );
    const newStep: ApprovalStep = {...step, id: Date.now()};
    const updated = this.items$.getValue().map(a =>
      a.id === itemId ? {...a, steps: [...a.steps, newStep].sort((x, y) => x.stepOrder - y.stepOrder)} : a
    );
    this.items$.next(updated);
    return of(updated.find(a => a.id === itemId)!);
  }

  updateStep(itemId: number, stepId: number, changes: Partial<Omit<ApprovalStep, 'id'>>): Observable<ApprovalItem> {
    // return this.http.patch<ApprovalItem>(`/api/approval-items/${itemId}/steps/${stepId}`, changes).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(a => a.id === itemId ? updated : a))),
    // );
    const updated = this.items$.getValue().map(a => {
      if (a.id !== itemId) return a;
      const steps = a.steps.map(s => s.id === stepId ? {...s, ...changes} : s)
                           .sort((x, y) => x.stepOrder - y.stepOrder);
      return {...a, steps};
    });
    this.items$.next(updated);
    return of(updated.find(a => a.id === itemId)!);
  }

  deleteStep(itemId: number, stepId: number): Observable<ApprovalItem> {
    // return this.http.delete<ApprovalItem>(`/api/approval-items/${itemId}/steps/${stepId}`).pipe(
    //   tap(updated => this.items$.next(this.items$.getValue().map(a => a.id === itemId ? updated : a))),
    // );
    const updated = this.items$.getValue().map(a =>
      a.id === itemId ? {...a, steps: a.steps.filter(s => s.id !== stepId)} : a
    );
    this.items$.next(updated);
    return of(updated.find(a => a.id === itemId)!);
  }
}
