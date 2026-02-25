import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {TodayAttendance, ClockActionRequest} from '../models/attendance.model';

@Injectable({providedIn: 'root'})
export class AttendanceService {
  private http = inject(HttpClient);
  private today$ = new BehaviorSubject<TodayAttendance | null>(null);

  getToday(): Observable<TodayAttendance | null> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<{success: boolean; data: TodayAttendance}>('/api/attendances/today').pipe(
    //   map(r => r.data ?? null),
    //   tap(record => this.today$.next(record)),
    // );
    return this.today$.asObservable();
  }

  clockIn(body: ClockActionRequest): Observable<TodayAttendance> {
    // return this.http.post<{data: TodayAttendance}>('/api/attendances/clock-in', body).pipe(map(r => r.data));
    const now = new Date();
    const record = this._ensureRecord();
    record.clockInTime = now.toISOString();
    record.clockInLatitude = body.latitude;
    record.clockInLongitude = body.longitude;
    this.today$.next({...record});
    return of({...record});
  }

  clockOut(body: ClockActionRequest): Observable<TodayAttendance> {
    // return this.http.post<{data: TodayAttendance}>('/api/attendances/clock-out', body).pipe(map(r => r.data));
    const now = new Date();
    const record = this._ensureRecord();
    record.clockOutTime = now.toISOString();
    record.clockOutLatitude = body.latitude;
    record.clockOutLongitude = body.longitude;
    this.today$.next({...record});
    return of({...record});
  }

  overtimeStart(body: ClockActionRequest): Observable<TodayAttendance> {
    // return this.http.post<{data: TodayAttendance}>('/api/attendances/overtime-start', body).pipe(map(r => r.data));
    const now = new Date();
    const record = this._ensureRecord();
    record.overtimeStartTime = now.toISOString();
    record.overtimeStartLatitude = body.latitude;
    record.overtimeStartLongitude = body.longitude;
    record.overtimeRequestId = body.overtimeRequestId;
    this.today$.next({...record});
    return of({...record});
  }

  overtimeEnd(body: ClockActionRequest): Observable<TodayAttendance> {
    // return this.http.post<{data: TodayAttendance}>('/api/attendances/overtime-end', body).pipe(map(r => r.data));
    const now = new Date();
    const record = this._ensureRecord();
    record.overtimeEndTime = now.toISOString();
    record.overtimeEndLatitude = body.latitude;
    record.overtimeEndLongitude = body.longitude;
    this.today$.next({...record});
    return of({...record});
  }

  private _ensureRecord(): TodayAttendance {
    let r = this.today$.getValue();
    if (!r) {
      r = {
        id: Date.now(),
        userId: '1',
        recordDate: new Date().toISOString().split('T')[0],
      };
    }
    return r;
  }
}
