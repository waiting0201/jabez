import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Role} from '../models/role.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class RoleService {
  private http = inject(HttpClient);

  getAll(): Observable<Role[]> {
    return this.http.get<Role[]>(`${environment.apiUrl}/roles`);
  }

  getById(id: string): Observable<Role> {
    return this.http.get<Role>(`${environment.apiUrl}/roles/${id}`);
  }

  create(data: Omit<Role, 'id' | 'createdAt'>): Observable<Role> {
    return this.http.post<Role>(`${environment.apiUrl}/roles`, data);
  }

  update(id: string, changes: Partial<Role>): Observable<Role> {
    return this.http.patch<Role>(`${environment.apiUrl}/roles/${id}`, changes);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/roles/${id}`);
  }
}
