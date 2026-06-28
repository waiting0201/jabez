import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@/environments/environment';
import { User } from '../../admin/users/models/user.model';
import { EmployeeProfileDetail } from '../../admin/users/models/employee-profile.model';

@Injectable({ providedIn: 'root' })
export class MyProfileService {
  private http = inject(HttpClient);

  /** 取得自己的基本資料（與 GET /users/{id} 同型別） */
  getMyUser(): Observable<User> {
    return this.http.get<User>(`${environment.apiUrl}/me/user`);
  }

  /** 取得自己的人事資料（與 GET /users/{id}/profile 同型別） */
  getMyProfile(): Observable<EmployeeProfileDetail> {
    return this.http.get<EmployeeProfileDetail>(`${environment.apiUrl}/me/profile`);
  }

  /** 下載 PII 檔案（需 Bearer token），回傳 Blob 供前端建立 Object URL */
  downloadFile(container: string, fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/me/files/${container}/${fileName}`, { responseType: 'blob' });
  }
}
