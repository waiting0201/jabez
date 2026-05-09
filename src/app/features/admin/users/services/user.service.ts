import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {User, UserLookup} from '../models/user.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

export interface UserFileOptions {
  signatureFile?: File | null;
  avatarFile?: File | null;
  indigenousProofFile?: File | null;
  lowIncomeProofFile?: File | null;
  disabledProofFile?: File | null;
}

export interface UserUpdateFileOptions extends UserFileOptions {
  removeSignature?: boolean;
  removeAvatar?: boolean;
  removeIndigenousProof?: boolean;
  removeLowIncomeProof?: boolean;
  removeDisabledProof?: boolean;
}

@Injectable({providedIn: 'root'})
export class UserService {
  private http = inject(HttpClient);

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(`${environment.apiUrl}/users`);
  }

  /** 輕量級使用者清單（不需 users:read 權限，供指定審核者下拉選單用） */
  getLookup(): Observable<UserLookup[]> {
    return this.http.get<UserLookup[]>(`${environment.apiUrl}/users/lookup`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<User>> {
    return this.http.get<PagedResult<User>>(`${environment.apiUrl}/users`, {params: {page, pageSize}});
  }

  getById(id: string): Observable<User> {
    return this.http.get<User>(`${environment.apiUrl}/users/${id}`);
  }

  create(data: Record<string, any>, files?: UserFileOptions): Observable<User> {
    const formData = this.buildFormData(data, files);
    return this.http.post<User>(`${environment.apiUrl}/users`, formData);
  }

  update(id: string, data: Record<string, any>, files?: UserUpdateFileOptions): Observable<User> {
    const formData = this.buildFormData(data, files);
    if (files?.removeSignature)        formData.append('removeSignature', 'true');
    if (files?.removeAvatar)           formData.append('removeAvatar', 'true');
    if (files?.removeIndigenousProof)  formData.append('removeIndigenousProof', 'true');
    if (files?.removeLowIncomeProof)   formData.append('removeLowIncomeProof', 'true');
    if (files?.removeDisabledProof)    formData.append('removeDisabledProof', 'true');
    return this.http.patch<User>(`${environment.apiUrl}/users/${id}`, formData);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/users/${id}`);
  }

  sendCredentials(id: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/users/${id}/send-credentials`, {});
  }

  /** 以 JWT 取得原住民證明檔（HR 權限保護，回傳 Blob 供前端開啟） */
  getIndigenousProof(fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/files/indigenous-proofs/${fileName}`, {responseType: 'blob'});
  }

  /** 以 JWT 取得低收入戶證明檔（HR 權限保護） */
  getLowIncomeProof(fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/files/low-income-proofs/${fileName}`, {responseType: 'blob'});
  }

  /** 以 JWT 取得殘障證明檔（HR 權限保護） */
  getDisabledProof(fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/files/disabled-proofs/${fileName}`, {responseType: 'blob'});
  }

  /** 以 JWT 取得身分證影本（HR 權限保護） */
  getIdCard(fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/files/id-cards/${fileName}`, {responseType: 'blob'});
  }

  private buildFormData(data: Record<string, any>, files?: UserFileOptions): FormData {
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
    if (files?.signatureFile)       fd.append('signature', files.signatureFile);
    if (files?.avatarFile)          fd.append('avatar', files.avatarFile);
    if (files?.indigenousProofFile) fd.append('indigenousProof', files.indigenousProofFile);
    if (files?.lowIncomeProofFile)  fd.append('lowIncomeProof', files.lowIncomeProofFile);
    if (files?.disabledProofFile)   fd.append('disabledProof', files.disabledProofFile);
    return fd;
  }
}
