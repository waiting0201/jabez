import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {LineQuota} from '../models/line-quota.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class LineQuotaService {
  private http = inject(HttpClient);

  getQuota(): Observable<LineQuota> {
    return this.http.get<LineQuota>(`${environment.apiUrl}/line/quota`);
  }
}
