import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@/environments/environment';
import { EmployeeProfileDetail, EmployeeProfileUpsertRequest } from '../models/employee-profile.model';

export interface ProfileFileOptions {
  idCardFront?: File | null;
  idCardBack?: File | null;
  removeIdCardFront?: boolean;
  removeIdCardBack?: boolean;
  highestEducationProof?: File | null;
  removeHighestEducationProof?: boolean;
  bankBookImage?: File | null;
  removeBankBook?: boolean;
  bankBookImage2?: File | null;
  removeBankBook2?: boolean;
}

@Injectable({ providedIn: 'root' })
export class EmployeeProfileService {
  private http = inject(HttpClient);

  /** 取得員工人事資料（含 9 個子表）。若尚未建立，後端回傳空預設值。 */
  getByUserId(userId: string): Observable<EmployeeProfileDetail> {
    return this.http.get<EmployeeProfileDetail>(`${environment.apiUrl}/users/${userId}/profile`);
  }

  /**
   * 新增或更新員工人事資料（整批替換子表）。
   * 使用 multipart/form-data：
   *   - text part `payload`：完整 HR JSON
   *   - file parts `idCardFront` / `idCardBack` / `highestEducationProof` / `bankBookImage` / `bankBookImage2`（optional）
   *   - text parts `removeIdCardFront` / `removeIdCardBack` / `removeHighestEducationProof` / `removeBankBook` / `removeBankBook2`（boolean string）
   */
  upsert(
    userId: string,
    payload: EmployeeProfileUpsertRequest,
    files?: ProfileFileOptions,
  ): Observable<EmployeeProfileDetail> {
    const fd = new FormData();
    fd.append('payload', JSON.stringify(payload));
    if (files?.idCardFront)                fd.append('idCardFront', files.idCardFront);
    if (files?.idCardBack)                 fd.append('idCardBack', files.idCardBack);
    if (files?.removeIdCardFront)          fd.append('removeIdCardFront', 'true');
    if (files?.removeIdCardBack)           fd.append('removeIdCardBack', 'true');
    if (files?.highestEducationProof)      fd.append('highestEducationProof', files.highestEducationProof);
    if (files?.removeHighestEducationProof) fd.append('removeHighestEducationProof', 'true');
    if (files?.bankBookImage)              fd.append('bankBookImage', files.bankBookImage);
    if (files?.removeBankBook)             fd.append('removeBankBook', 'true');
    if (files?.bankBookImage2)             fd.append('bankBookImage2', files.bankBookImage2);
    if (files?.removeBankBook2)            fd.append('removeBankBook2', 'true');
    return this.http.put<EmployeeProfileDetail>(`${environment.apiUrl}/users/${userId}/profile`, fd);
  }
}
