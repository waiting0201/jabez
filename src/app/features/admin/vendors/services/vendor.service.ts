import {Injectable, inject} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {Vendor, VendorLookup, VendorTaxIdLookup} from '../models/vendor.model';
import {environment} from '@/environments/environment';

export interface VendorFormPayload {
  name: string;
  taxId?: string | null;
  phone?: string | null;
  contactPerson?: string | null;
  address?: string | null;
  bankAccount?: string | null;
  note?: string | null;
  isActive?: boolean;
}

export interface VendorFileOptions {
  bankBookImage?: File | null;
  removeBankBookImage?: boolean;
}

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

  /** 以統編查詢 GCIS 公司登記資料（任何登入者可用） */
  lookupByTaxId(taxId: string): Observable<VendorTaxIdLookup> {
    const params = new HttpParams().set('taxId', taxId);
    return this.http.get<VendorTaxIdLookup>(`${environment.apiUrl}/vendors/lookup-by-tax-id`, {params});
  }

  getById(id: number): Observable<Vendor> {
    return this.http.get<Vendor>(`${environment.apiUrl}/vendors/${id}`);
  }

  create(payload: VendorFormPayload, files?: VendorFileOptions): Observable<Vendor> {
    const fd = this.buildFormData(payload, files);
    return this.http.post<Vendor>(`${environment.apiUrl}/vendors`, fd);
  }

  update(id: number, payload: Partial<VendorFormPayload>, files?: VendorFileOptions): Observable<Vendor> {
    const fd = this.buildFormData(payload, files);
    return this.http.patch<Vendor>(`${environment.apiUrl}/vendors/${id}`, fd);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/vendors/${id}`);
  }

  /** 透過授權代理讀取存摺封面（需 JWT） */
  getBankBookImage(fileName: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/files/vendor-passbooks/${fileName}`, {responseType: 'blob'});
  }

  private buildFormData(payload: object, files?: VendorFileOptions): FormData {
    const fd = new FormData();
    fd.append('payload', JSON.stringify(payload));
    if (files?.bankBookImage)       fd.append('bankBookImage', files.bankBookImage);
    if (files?.removeBankBookImage) fd.append('removeBankBookImage', 'true');
    return fd;
  }
}
