import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Department} from '../models/department.model';
import {environment} from '@/environments/environment';

export interface DepartmentLookup { id: number; name: string; parentId: number | null; }

@Injectable({providedIn: 'root'})
export class DepartmentService {
  private http = inject(HttpClient);

  getAll(): Observable<Department[]> {
    return this.http.get<Department[]>(`${environment.apiUrl}/departments`);
  }

  /** 取得部門精簡清單（免權限，供篩選下拉使用） */
  getLookup(): Observable<DepartmentLookup[]> {
    return this.http.get<DepartmentLookup[]>(`${environment.apiUrl}/departments/lookup`);
  }

  getById(id: number): Observable<Department> {
    return this.http.get<Department>(`${environment.apiUrl}/departments/${id}`);
  }

  create(data: Omit<Department, 'id' | 'createdAt' | 'employeeCount' | 'parentName'>): Observable<Department> {
    return this.http.post<Department>(`${environment.apiUrl}/departments`, data);
  }

  update(id: number, changes: Partial<Department>): Observable<Department> {
    return this.http.patch<Department>(`${environment.apiUrl}/departments/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/departments/${id}`);
  }
}
