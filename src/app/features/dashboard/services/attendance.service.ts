import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {tap} from 'rxjs/operators';
import {TodayAttendance, ClockActionRequest} from '../models/attendance.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class AttendanceService {
  private http = inject(HttpClient);
  private today$ = new BehaviorSubject<TodayAttendance | null>(null);

  getToday(): Observable<TodayAttendance | null> {
    return this.http.get<TodayAttendance | null>(`${environment.apiUrl}/attendances/today`).pipe(
      tap(record => this.today$.next(record)),
    );
  }

  clockIn(body: ClockActionRequest): Observable<TodayAttendance> {
    return this.http.post<TodayAttendance>(`${environment.apiUrl}/attendances/clock-in`, body).pipe(
      tap(record => this.today$.next(record)),
    );
  }

  clockOut(body: ClockActionRequest): Observable<TodayAttendance> {
    return this.http.post<TodayAttendance>(`${environment.apiUrl}/attendances/clock-out`, body).pipe(
      tap(record => this.today$.next(record)),
    );
  }

  overtimeStart(body: ClockActionRequest): Observable<TodayAttendance> {
    return this.http.post<TodayAttendance>(`${environment.apiUrl}/attendances/overtime-start`, body).pipe(
      tap(record => this.today$.next(record)),
    );
  }

  overtimeEnd(body: ClockActionRequest): Observable<TodayAttendance> {
    return this.http.post<TodayAttendance>(`${environment.apiUrl}/attendances/overtime-end`, body).pipe(
      tap(record => this.today$.next(record)),
    );
  }
}
