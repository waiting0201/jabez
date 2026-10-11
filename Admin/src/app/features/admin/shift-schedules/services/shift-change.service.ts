import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {
  ChangeableShiftDates,
  PreviewShiftChangeRequest,
  SaveShiftChangeRequest,
  ShiftChangeMonthView,
  ShiftChangeRequest,
} from '../models/shift-change.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ShiftChangeService {
  private http = inject(HttpClient);

  /** 可申請改班的日期（逐日現況 + 配額）。已排除國定假日與過去日期 */
  getChangeableDates(year: number, month: number): Observable<ChangeableShiftDates> {
    return this.http.get<ChangeableShiftDates>(
      `${environment.apiUrl}/shift-changes/changeable-dates`, {params: {year, month}});
  }

  /** 試算：把調整疊到現行班表上，回傳整月月曆 + 檢核（不寫入）。表單每點一格呼叫一次 */
  preview(data: PreviewShiftChangeRequest): Observable<ShiftChangeMonthView> {
    return this.http.post<ShiftChangeMonthView>(`${environment.apiUrl}/shift-changes/preview`, data);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<ShiftChangeRequest>> {
    return this.http.get<PagedResult<ShiftChangeRequest>>(
      `${environment.apiUrl}/shift-changes`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<ShiftChangeRequest> {
    return this.http.get<ShiftChangeRequest>(`${environment.apiUrl}/shift-changes/${id}`);
  }

  create(data: SaveShiftChangeRequest): Observable<ShiftChangeRequest> {
    return this.http.post<ShiftChangeRequest>(`${environment.apiUrl}/shift-changes`, data);
  }

  update(id: number, data: Partial<SaveShiftChangeRequest>): Observable<ShiftChangeRequest> {
    return this.http.patch<ShiftChangeRequest>(`${environment.apiUrl}/shift-changes/${id}`, data);
  }

  submit(id: number): Observable<ShiftChangeRequest> {
    return this.http.patch<ShiftChangeRequest>(`${environment.apiUrl}/shift-changes/${id}/submit`, {});
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/shift-changes/${id}`);
  }
}
