import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Project} from '../models/project.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class ProjectService {
  private http = inject(HttpClient);

  getAll(): Observable<Project[]> {
    return this.http.get<Project[]>(`${environment.apiUrl}/projects`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<Project>> {
    return this.http.get<PagedResult<Project>>(`${environment.apiUrl}/projects`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<Project> {
    return this.http.get<Project>(`${environment.apiUrl}/projects/${id}`);
  }

  create(data: Omit<Project, 'id' | 'createdAt'>): Observable<Project> {
    return this.http.post<Project>(`${environment.apiUrl}/projects`, data);
  }

  update(id: number, changes: Partial<Project>): Observable<Project> {
    return this.http.patch<Project>(`${environment.apiUrl}/projects/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/projects/${id}`);
  }
}
