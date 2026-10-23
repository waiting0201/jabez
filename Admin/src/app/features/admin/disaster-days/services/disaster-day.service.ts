import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '@/environments/environment';
import {
  DisasterAffected, DisasterDayCreateRequest, DisasterDayOff, DisasterDayUpdateRequest,
  DisasterFetchLog, MyDisasterDayOff,
} from '../models/disaster-day.model';

@Injectable({providedIn: 'root'})
export class DisasterDayService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/disaster-days`;

  list(from: string, to: string): Observable<DisasterDayOff[]> {
    return this.http.get<DisasterDayOff[]>(this.base, {params: {from, to}});
  }

  create(data: DisasterDayCreateRequest): Observable<DisasterDayOff[]> {
    return this.http.post<DisasterDayOff[]>(this.base, data);
  }

  update(id: number, data: DisasterDayUpdateRequest): Observable<DisasterDayOff> {
    return this.http.patch<DisasterDayOff>(`${this.base}/${id}`, data);
  }

  cancel(id: number): Observable<DisasterDayOff> {
    return this.http.post<DisasterDayOff>(`${this.base}/${id}/cancel`, null);
  }

  affected(id: number): Observable<DisasterAffected> {
    return this.http.get<DisasterAffected>(`${this.base}/${id}/affected`);
  }

  fetchLogs(limit = 20): Observable<DisasterFetchLog[]> {
    return this.http.get<DisasterFetchLog[]>(`${this.base}/fetch-logs`, {params: {limit}});
  }

  /** 手動觸發抓取（僅 Superadmin） */
  fetchNow(): Observable<DisasterFetchLog> {
    return this.http.post<DisasterFetchLog>(`${this.base}/fetch`, null);
  }

  /** 本人今日是否停班（任何登入者；沒有則 null） */
  mine(): Observable<MyDisasterDayOff | null> {
    return this.http.get<MyDisasterDayOff | null>(`${environment.apiUrl}/me/disaster-day-off`);
  }
}
