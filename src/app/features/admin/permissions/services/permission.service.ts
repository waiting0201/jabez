import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {Permission} from '../models/permission.model';

const MOCK_PERMISSIONS: Permission[] = [
  {id: '1', code: 'admin:access',       name: 'Admin Access',          module: 'Admin'},
  {id: '2', code: 'users:read',         name: 'View Users',            module: 'Users'},
  {id: '3', code: 'users:write',        name: 'Create/Edit Users',     module: 'Users'},
  {id: '4', code: 'users:delete',       name: 'Delete Users',          module: 'Users'},
  {id: '5', code: 'roles:read',         name: 'View Roles',            module: 'Roles'},
  {id: '6', code: 'roles:write',        name: 'Create/Edit Roles',     module: 'Roles'},
  {id: '7', code: 'roles:delete',       name: 'Delete Roles',          module: 'Roles'},
  {id: '8', code: 'permissions:read',   name: 'View Permissions',      module: 'Permissions'},
  {id: '9', code: 'permissions:write',  name: 'Create/Edit Permissions', module: 'Permissions'},
  {id: '10', code: 'permissions:delete', name: 'Delete Permissions',   module: 'Permissions'},
  {id: '11', code: 'departments:read',  name: 'View Departments',      module: 'Departments'},
  {id: '12', code: 'departments:write', name: 'Create/Edit Departments', module: 'Departments'},
  {id: '13', code: 'departments:delete', name: 'Delete Departments',   module: 'Departments'},
  {id: '14', code: 'job-titles:read',   name: 'View Job Titles',       module: 'JobTitles'},
  {id: '15', code: 'job-titles:write',  name: 'Create/Edit Job Titles', module: 'JobTitles'},
  {id: '16', code: 'job-titles:delete', name: 'Delete Job Titles',     module: 'JobTitles'},
  {id: '17', code: 'approvals:read',    name: 'View Approvals',        module: 'Approvals'},
  {id: '18', code: 'approvals:write',   name: 'Create/Edit Approvals', module: 'Approvals'},
  {id: '19', code: 'approvals:delete',  name: 'Delete Approvals',      module: 'Approvals'},
  {id: '20', code: 'projects:read',    name: 'View Projects',         module: 'Projects'},
  {id: '21', code: 'projects:write',   name: 'Create/Edit Projects',  module: 'Projects'},
  {id: '22', code: 'projects:delete',  name: 'Delete Projects',       module: 'Projects'},
  {id: '23', code: 'payment-requests:read',   name: 'View Payment Requests',        module: 'PaymentRequests'},
  {id: '24', code: 'payment-requests:write',  name: 'Create/Edit Payment Requests', module: 'PaymentRequests'},
  {id: '25', code: 'payment-requests:delete', name: 'Delete Payment Requests',      module: 'PaymentRequests'},
  {id: '26', code: 'approval-tasks:read',   name: 'View Approval Tasks',        module: 'ApprovalTasks'},
  {id: '27', code: 'approval-tasks:write',  name: 'Review Approval Tasks',      module: 'ApprovalTasks'},
  {id: '28', code: 'leave-requests:read',   name: 'View Leave Requests',        module: 'LeaveRequests'},
  {id: '29', code: 'leave-requests:write',  name: 'Create/Edit Leave Requests', module: 'LeaveRequests'},
  {id: '30', code: 'leave-requests:delete', name: 'Delete Leave Requests',      module: 'LeaveRequests'},
  {id: '31', code: 'travel-requests:read',   name: 'View Travel Requests',        module: 'TravelRequests'},
  {id: '32', code: 'travel-requests:write',  name: 'Create/Edit Travel Requests', module: 'TravelRequests'},
  {id: '33', code: 'travel-requests:delete', name: 'Delete Travel Requests',      module: 'TravelRequests'},
];

@Injectable({providedIn: 'root'})
export class PermissionService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<Permission[]>(MOCK_PERMISSIONS);

  getAll(): Observable<Permission[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Permission[]>('/api/permissions').pipe(
    //   tap(permissions => this.items$.next(permissions)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    return this.items$.asObservable();
  }

  getById(id: string): Observable<Permission | undefined> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Permission>(`/api/permissions/${id}`);

    // ─── Mock（前端開發用）──────────────────────────────────
    return of(this.items$.getValue().find(p => p.id === id));
  }

  getModules(): string[] {
    return [...new Set(this.items$.getValue().map(p => p.module))];
  }

  create(data: Omit<Permission, 'id'>): Observable<Permission> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.post<Permission>('/api/permissions', data).pipe(
    //   tap(perm => this.items$.next([...this.items$.getValue(), perm])),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const item: Permission = {...data, id: Date.now().toString()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: string, changes: Partial<Permission>): Observable<Permission> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.patch<Permission>(`/api/permissions/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(p => p.id === id ? updated : p);
    //     this.items$.next(list);
    //   }),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const updated = this.items$.getValue().map(p => p.id === id ? {...p, ...changes} : p);
    this.items$.next(updated);
    return of(updated.find(p => p.id === id)!);
  }

  delete(id: string): Observable<void> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.delete<void>(`/api/permissions/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(p => p.id !== id))),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    this.items$.next(this.items$.getValue().filter(p => p.id !== id));
    return of(undefined);
  }
}
