import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {tap} from 'rxjs/operators';
import {SystemSettings} from '../models/settings.model';

const MOCK_SETTINGS: SystemSettings = {
  workStartTime: '09:00',
  workEndTime: '18:00',
  monthlyOvertimeLimit: 46,
};

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);
  private settings$ = new BehaviorSubject<SystemSettings>(MOCK_SETTINGS);

  get(): Observable<SystemSettings> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<SystemSettings>('/api/settings').pipe(
    //   tap(settings => this.settings$.next(settings)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    return this.settings$.asObservable();
  }

  save(changes: Partial<SystemSettings>): Observable<SystemSettings> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.patch<SystemSettings>('/api/settings', changes).pipe(
    //   tap(updated => this.settings$.next(updated)),
    // );

    // ─── Mock（前端開發用）──────────────────────────────────
    const updated = {...this.settings$.getValue(), ...changes};
    this.settings$.next(updated);
    return of(updated);
  }
}
