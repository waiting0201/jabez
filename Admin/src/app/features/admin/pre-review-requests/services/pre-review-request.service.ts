import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {PreReviewRequest} from '../models/pre-review-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {environment} from '@/environments/environment';

/** 單筆 Quote OCR 辨識結果（報價單品項）；一張圖可辨識出多筆 */
export interface QuoteOcrItem {
  itemName: string;
  amount: number;
  note: string;
}

@Injectable({providedIn: 'root'})
export class PreReviewRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<PreReviewRequest[]> {
    return this.http.get<PreReviewRequest[]>(`${environment.apiUrl}/pre-review-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<PreReviewRequest>> {
    return this.http.get<PagedResult<PreReviewRequest>>(`${environment.apiUrl}/pre-review-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<PreReviewRequest> {
    return this.http.get<PreReviewRequest>(`${environment.apiUrl}/pre-review-requests/${id}`);
  }

  createWithFiles(formData: FormData): Observable<PreReviewRequest> {
    return this.http.post<PreReviewRequest>(`${environment.apiUrl}/pre-review-requests`, formData);
  }

  updateWithFiles(id: number, formData: FormData): Observable<PreReviewRequest> {
    return this.http.patch<PreReviewRequest>(`${environment.apiUrl}/pre-review-requests/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/pre-review-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<PreReviewRequest> {
    return this.http.patch<PreReviewRequest>(`${environment.apiUrl}/pre-review-requests/${id}/submit`, {});
  }

  /** 報價單 OCR 辨識（後端透過 Google Gemini API）；一張圖可辨識出多筆品項，回傳陣列 */
  quoteOcr(file: File): Observable<QuoteOcrItem[]> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<QuoteOcrItem[]>(`${environment.apiUrl}/quote-ocr`, fd);
  }
}
