import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {User} from '../models/user.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class UserService {
  private http = inject(HttpClient);

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(`${environment.apiUrl}/users`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<User>> {
    return this.http.get<PagedResult<User>>(`${environment.apiUrl}/users`, {params: {page, pageSize}});
  }

  getById(id: string): Observable<User> {
    return this.http.get<User>(`${environment.apiUrl}/users/${id}`);
  }

  create(data: Omit<User, 'id' | 'createdAt'>): Observable<User> {
    return this.http.post<User>(`${environment.apiUrl}/users`, data);
  }

  update(id: string, changes: Partial<User>): Observable<User> {
    return this.http.patch<User>(`${environment.apiUrl}/users/${id}`, changes);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/users/${id}`);
  }
}
