import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {JobTitle} from '../models/job-title.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class JobTitleService {
  private http = inject(HttpClient);

  getAll(): Observable<JobTitle[]> {
    return this.http.get<JobTitle[]>(`${environment.apiUrl}/job-titles`);
  }

  getById(id: number): Observable<JobTitle> {
    return this.http.get<JobTitle>(`${environment.apiUrl}/job-titles/${id}`);
  }

  create(data: Omit<JobTitle, 'id' | 'createdAt' | 'employeeCount'>): Observable<JobTitle> {
    return this.http.post<JobTitle>(`${environment.apiUrl}/job-titles`, data);
  }

  update(id: number, changes: Partial<JobTitle>): Observable<JobTitle> {
    return this.http.patch<JobTitle>(`${environment.apiUrl}/job-titles/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/job-titles/${id}`);
  }
}
