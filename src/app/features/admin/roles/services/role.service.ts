import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {Role} from '../models/role.model';

const MOCK_ROLES: Role[] = [
  {
    id: 'admin',
    name: 'Administrator',
    description: 'Full system access',
    permissionCodes: [
      'admin:access',
      'users:read', 'users:write', 'users:delete',
      'roles:read', 'roles:write', 'roles:delete',
      'permissions:read', 'permissions:write', 'permissions:delete',
      'departments:read', 'departments:write', 'departments:delete',
      'job-titles:read', 'job-titles:write', 'job-titles:delete',
      'approvals:read', 'approvals:write', 'approvals:delete',
      'projects:read', 'projects:write', 'projects:delete',
      'payment-requests:read', 'payment-requests:write', 'payment-requests:delete',
      'approval-tasks:read', 'approval-tasks:write',
    ],
    createdAt: new Date('2024-01-01'),
  },
  {
    id: 'manager',
    name: 'Manager',
    description: 'Can manage users and view reports',
    permissionCodes: ['admin:access', 'users:read', 'users:write', 'roles:read'],
    createdAt: new Date('2024-01-15'),
  },
  {
    id: 'viewer',
    name: 'Viewer',
    description: 'Read-only access',
    permissionCodes: ['users:read', 'roles:read', 'permissions:read'],
    createdAt: new Date('2024-02-01'),
  },
];

@Injectable({providedIn: 'root'})
export class RoleService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<Role[]>(MOCK_ROLES);

  getAll(): Observable<Role[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Role[]>('/api/roles').pipe(
    //   tap(roles => this.items$.next(roles)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    return this.items$.asObservable();
  }

  getById(id: string): Observable<Role | undefined> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Role>(`/api/roles/${id}`);

    // ─── Mock（前端開發用）──────────────────────────────────
    return of(this.items$.getValue().find(r => r.id === id));
  }

  create(data: Omit<Role, 'id' | 'createdAt'>): Observable<Role> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.post<Role>('/api/roles', data).pipe(
    //   tap(role => this.items$.next([...this.items$.getValue(), role])),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const item: Role = {...data, id: Date.now().toString(), createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: string, changes: Partial<Role>): Observable<Role> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.patch<Role>(`/api/roles/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(r => r.id === id ? updated : r);
    //     this.items$.next(list);
    //   }),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const updated = this.items$.getValue().map(r => r.id === id ? {...r, ...changes} : r);
    this.items$.next(updated);
    return of(updated.find(r => r.id === id)!);
  }

  delete(id: string): Observable<void> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.delete<void>(`/api/roles/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(r => r.id !== id))),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    this.items$.next(this.items$.getValue().filter(r => r.id !== id));
    return of(undefined);
  }
}
