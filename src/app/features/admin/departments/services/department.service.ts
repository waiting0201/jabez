import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {Department} from '../models/department.model';

const MOCK_DEPARTMENTS: Department[] = [
  {id: 1, name: '管理部', code: 'MGT', sortOrder: 1, employeeCount: 1, createdAt: new Date('2024-01-01')},
  {id: 2, name: '資訊部', code: 'IT',  sortOrder: 2, employeeCount: 1, createdAt: new Date('2024-01-01')},
  {id: 3, name: '業務部', code: 'SLS', sortOrder: 3, employeeCount: 1, createdAt: new Date('2024-01-01')},
];

@Injectable({providedIn: 'root'})
export class DepartmentService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<Department[]>(MOCK_DEPARTMENTS);

  getAll(): Observable<Department[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<Department[]>('/api/departments').pipe(
    //   tap(depts => this.items$.next(depts)),
    // );
    return this.items$.asObservable();
  }

  getById(id: number): Observable<Department | undefined> {
    // return this.http.get<Department>(`/api/departments/${id}`);
    return of(this.items$.getValue().find(d => d.id === id));
  }

  create(data: Omit<Department, 'id' | 'createdAt' | 'employeeCount' | 'parentName'>): Observable<Department> {
    // return this.http.post<Department>('/api/departments', data).pipe(
    //   tap(d => this.items$.next([...this.items$.getValue(), d])),
    // );
    const item: Department = {...data, id: Date.now(), employeeCount: 0, createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<Department>): Observable<Department> {
    // return this.http.patch<Department>(`/api/departments/${id}`, changes).pipe(
    //   tap(updated => {
    //     const list = this.items$.getValue().map(d => d.id === id ? updated : d);
    //     this.items$.next(list);
    //   }),
    // );
    const updated = this.items$.getValue().map(d => d.id === id ? {...d, ...changes} : d);
    this.items$.next(updated);
    return of(updated.find(d => d.id === id)!);
  }

  delete(id: number): Observable<void> {
    // return this.http.delete<void>(`/api/departments/${id}`).pipe(
    //   tap(() => this.items$.next(this.items$.getValue().filter(d => d.id !== id))),
    // );
    this.items$.next(this.items$.getValue().filter(d => d.id !== id));
    return of(undefined);
  }
}
