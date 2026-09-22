import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {ActivityDay, SaveActivityDayRequest, SaveActivityDayResult} from '../models/activity-day.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ActivityDayService {
  private http = inject(HttpClient);

  getByMonth(year: number, month: number, departmentId?: number): Observable<ActivityDay[]> {
    const params: Record<string, string | number> = {year, month};
    if (departmentId) params['departmentId'] = departmentId;
    return this.http.get<ActivityDay[]>(`${environment.apiUrl}/activity-days`, {params});
  }

  create(data: SaveActivityDayRequest): Observable<SaveActivityDayResult> {
    return this.http.post<SaveActivityDayResult>(`${environment.apiUrl}/activity-days`, data);
  }

  update(id: number, data: SaveActivityDayRequest): Observable<SaveActivityDayResult> {
    return this.http.put<SaveActivityDayResult>(`${environment.apiUrl}/activity-days/${id}`, data);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/activity-days/${id}`);
  }
}
