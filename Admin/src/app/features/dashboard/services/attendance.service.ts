import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable} from 'rxjs';
import {tap} from 'rxjs/operators';
import {TodayAttendance, ClockActionRequest, ClockActionType, ClockChallenge, MissingAllocationDay, ProjectAllocationInput} from '../models/attendance.model';
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

  /** 取得一次性打卡挑戰碼（防機器人打卡）：四個打卡動作送出前都要先取碼 */
  getChallenge(action: ClockActionType): Observable<ClockChallenge> {
    return this.http.post<ClockChallenge>(`${environment.apiUrl}/attendances/clock-challenge`, {action});
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

  /** 修改今日專案工時佔比（僅限自己今天、已打下班卡的紀錄；整批替換） */
  updateTodayAllocations(projectAllocations: ProjectAllocationInput[]): Observable<TodayAttendance> {
    return this.http.put<TodayAttendance>(`${environment.apiUrl}/attendances/today/project-allocations`, {projectAllocations}).pipe(
      tap(record => this.today$.next(record)),
    );
  }

  /** 本人「系統補下班卡、尚未填專案佔比」的日子（上線日之後、今天以前） */
  getMissingAllocations(): Observable<MissingAllocationDay[]> {
    return this.http.get<MissingAllocationDay[]>(`${environment.apiUrl}/attendances/missing-project-allocations`);
  }

  /** 補填系統補卡日的專案佔比（同一份佔比可套多天，只能補一次）；回傳剩餘待補清單 */
  fillMissingAllocations(attendanceIds: number[], projectAllocations: ProjectAllocationInput[]): Observable<MissingAllocationDay[]> {
    return this.http.put<MissingAllocationDay[]>(`${environment.apiUrl}/attendances/missing-project-allocations`, {attendanceIds, projectAllocations});
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

  update(id: number, body: {clockInTime?: string | null; clockOutTime?: string | null; overtimeStartTime?: string | null; overtimeEndTime?: string | null; remark?: string | null}): Observable<any> {
    return this.http.patch(`${environment.apiUrl}/attendances/${id}`, body);
  }
}
