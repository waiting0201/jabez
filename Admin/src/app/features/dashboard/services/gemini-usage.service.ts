import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {GeminiUsage} from '../models/gemini-usage.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class GeminiUsageService {
  private http = inject(HttpClient);

  getUsage(): Observable<GeminiUsage> {
    return this.http.get<GeminiUsage>(`${environment.apiUrl}/gemini/usage`);
  }
}
