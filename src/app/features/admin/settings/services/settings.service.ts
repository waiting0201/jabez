import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {SystemSettings} from '../models/settings.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);

  get(): Observable<SystemSettings> {
    return this.http.get<SystemSettings>(`${environment.apiUrl}/settings`);
  }

  save(changes: Partial<SystemSettings>): Observable<SystemSettings> {
    return this.http.patch<SystemSettings>(`${environment.apiUrl}/settings`, changes);
  }
}
