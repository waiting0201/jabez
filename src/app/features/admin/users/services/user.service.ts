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

  create(data: Record<string, any>, signatureFile?: File | null): Observable<User> {
    const formData = this.buildFormData(data, signatureFile);
    return this.http.post<User>(`${environment.apiUrl}/users`, formData);
  }

  update(id: string, data: Record<string, any>, signatureFile?: File | null, removeSignature = false): Observable<User> {
    const formData = this.buildFormData(data, signatureFile);
    if (removeSignature) formData.append('removeSignature', 'true');
    return this.http.patch<User>(`${environment.apiUrl}/users/${id}`, formData);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/users/${id}`);
  }

  sendCredentials(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/users/${id}/send-credentials`, {});
  }

  private buildFormData(data: Record<string, any>, signatureFile?: File | null): FormData {
    const fd = new FormData();
    for (const [key, value] of Object.entries(data)) {
      if (value === undefined || value === null) continue;
      if (Array.isArray(value)) {
        value.forEach(v => fd.append(key, String(v)));
      } else if (value instanceof Date) {
        fd.append(key, value.toISOString());
      } else {
        fd.append(key, String(value));
      }
    }
    if (signatureFile) {
      fd.append('signature', signatureFile);
    }
    return fd;
  }
}
