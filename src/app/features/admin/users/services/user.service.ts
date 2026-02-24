import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {User} from '../models/user.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';

const MOCK_USERS: User[] = [
  {
    id: '1',
    name: 'Alice Chen',
    email: 'alice@example.com',
    roleIds: ['admin'],
    status: 'active',
    departmentId: 1, departmentName: '管理部',
    jobTitleId: 4,   jobTitleName: '部門主管',
    hireDate: new Date('2022-03-01'),
    baseSalary: 80000,
    createdAt: new Date('2024-01-01'),
  },
  {
    id: '2',
    name: 'Bob Wang',
    email: 'bob@example.com',
    roleIds: ['manager'],
    status: 'active',
    departmentId: 2, departmentName: '資訊部',
    jobTitleId: 2,   jobTitleName: '資深工程師',
    hireDate: new Date('2023-06-15'),
    baseSalary: 60000,
    agentUserId: '1', agentName: 'Alice Chen',
    createdAt: new Date('2024-02-10'),
  },
  {
    id: '3',
    name: 'Carol Liu',
    email: 'carol@example.com',
    roleIds: ['viewer'],
    status: 'inactive',
    departmentId: 3, departmentName: '業務部',
    jobTitleId: 1,   jobTitleName: '工程師',
    hireDate: new Date('2023-09-01'),
    resignDate: new Date('2025-01-31'),
    baseSalary: 50000,
    createdAt: new Date('2024-03-05'),
  },
];

@Injectable({providedIn: 'root'})
export class UserService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<User[]>(MOCK_USERS);

  getAll(): Observable<User[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<User[]>('/api/users').pipe(
    //   tap(users => this.items$.next(users)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    return this.items$.asObservable();
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<User>> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<{success: boolean; data: PagedResult<User>}>(
    //   '/api/users', {params: {page, pageSize}}
    // ).pipe(map(r => r.data!));

    // ─── Mock（前端開發用）──────────────────────────────────
    const all = this.items$.getValue();
    const start = (page - 1) * pageSize;
    const totalPages = Math.max(1, Math.ceil(all.length / pageSize));
    return of({items: all.slice(start, start + pageSize), totalCount: all.length, page, pageSize, totalPages});
  }

  getById(id: string): Observable<User | undefined> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<User>(`/api/users/${id}`);

    // ─── Mock（前端開發用）──────────────────────────────────
    return of(this.items$.getValue().find(u => u.id === id));
  }

  create(data: Omit<User, 'id' | 'createdAt'>): Observable<User> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.post<User>('/api/users', data).pipe(
    //   tap(user => this.items$.next([...this.items$.getValue(), user])),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const item: User = {...data, id: Date.now().toString(), createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: string, changes: Partial<User>): Observable<User> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.patch<User>(`/api/users/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(u => u.id === id ? updated : u);
    //     this.items$.next(list);
    //   }),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const updated = this.items$.getValue().map(u => u.id === id ? {...u, ...changes} : u);
    this.items$.next(updated);
    return of(updated.find(u => u.id === id)!);
  }

  delete(id: string): Observable<void> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.delete<void>(`/api/users/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(u => u.id !== id))),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    this.items$.next(this.items$.getValue().filter(u => u.id !== id));
    return of(undefined);
  }
}
