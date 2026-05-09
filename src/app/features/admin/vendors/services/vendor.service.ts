import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Vendor, VendorLookup} from '../models/vendor.model';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class VendorService {
  private http = inject(HttpClient);

  getAll(): Observable<Vendor[]> {
    return this.http.get<Vendor[]>(`${environment.apiUrl}/vendors`);
  }

  /** 輕量級廠商清單（不需 vendors:read 權限，供下拉選單用；僅回 IsActive=true） */
  getLookup(): Observable<VendorLookup[]> {
    return this.http.get<VendorLookup[]>(`${environment.apiUrl}/vendors/lookup`);
  }

  getById(id: number): Observable<Vendor> {
    return this.http.get<Vendor>(`${environment.apiUrl}/vendors/${id}`);
  }

  create(data: Omit<Vendor, 'id' | 'createdAt' | 'usageCount'>): Observable<Vendor> {
    return this.http.post<Vendor>(`${environment.apiUrl}/vendors`, data);
  }

  update(id: number, changes: Partial<Vendor>): Observable<Vendor> {
    return this.http.patch<Vendor>(`${environment.apiUrl}/vendors/${id}`, changes);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/vendors/${id}`);
  }
}
