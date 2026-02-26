import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {tap} from 'rxjs/operators';
import {Permission} from '../models/permission.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class PermissionService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<Permission[]>([]);

  getAll(): Observable<Permission[]> {
    return this.http.get<Permission[]>(`${environment.apiUrl}/permissions`).pipe(
      tap(permissions => this.items$.next(permissions)),
    );
  }

  getById(id: string): Observable<Permission> {
    return this.http.get<Permission>(`${environment.apiUrl}/permissions/${id}`);
  }

  /** 從本地快取取得模組清單（需先呼叫 getAll） */
  getModules(): string[] {
    return [...new Set(this.items$.getValue().map(p => p.module))];
  }

  create(data: Omit<Permission, 'id'>): Observable<Permission> {
    return this.http.post<Permission>(`${environment.apiUrl}/permissions`, data);
  }

  update(id: string, changes: Partial<Permission>): Observable<Permission> {
    return this.http.patch<Permission>(`${environment.apiUrl}/permissions/${id}`, changes);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/permissions/${id}`);
  }
}
