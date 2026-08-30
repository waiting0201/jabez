import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, timeout} from 'rxjs';
import {PaymentRequest} from '../models/payment-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';
import {UpsertInstallmentsRequest} from '../../approval-tasks/models/approval-task.model';
import {environment} from '@/environments/environment';

/** 單筆 OCR 辨識結果（發票 / 收據 / 交通票根）；一張圖可辨識出多筆 */
export interface OcrItem {
  invoiceNo: string;
  amount: number;
  invoiceDate: string;
  docType: 'invoice' | 'ticket';
  /** 買方抬頭（統一發票買受人）；交通票根為空字串 */
  buyerName: string;
  /** 買方統編（統一發票買受人）；交通票根為空字串 */
  buyerTaxId: string;
  /** 賣方統編（發票專用章）；供交叉比對排除 OCR 抓錯欄位，交通票根為空字串 */
  sellerTaxId: string;
}

@Injectable({providedIn: 'root'})
export class PaymentRequestService {
  private http = inject(HttpClient);

  getAll(): Observable<PaymentRequest[]> {
    return this.http.get<PaymentRequest[]>(`${environment.apiUrl}/payment-requests`);
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<PaymentRequest>> {
    return this.http.get<PagedResult<PaymentRequest>>(`${environment.apiUrl}/payment-requests`, {params: {page, pageSize}});
  }

  getById(id: number): Observable<PaymentRequest> {
    return this.http.get<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}`);
  }

  createWithFiles(formData: FormData): Observable<PaymentRequest> {
    return this.http.post<PaymentRequest>(`${environment.apiUrl}/payment-requests`, formData);
  }

  updateWithFiles(id: number, formData: FormData): Observable<PaymentRequest> {
    return this.http.patch<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}`, formData);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/payment-requests/${id}`);
  }

  /** 送出申請（draft → pending） */
  submit(id: number): Observable<PaymentRequest> {
    return this.http.patch<PaymentRequest>(`${environment.apiUrl}/payment-requests/${id}/submit`, {});
  }

  /**
   * 發票 / 交通票根 OCR 辨識（後端透過 Google Gemini API）；一張圖可辨識出多筆，回傳陣列。
   * 加 45 秒前端逾時（後端 Gemini 呼叫上限 30 秒）作為安全網：避免請求卡住時上傳列的
   * OCR pending 狀態無法解除、連帶把 `isAnyOcrPending` 控制的「送出 / 儲存」按鈕永久鎖住。
   */
  ocrInvoice(file: File): Observable<OcrItem[]> {
    const fd = new FormData();
    fd.append('file', file, file.name);
    return this.http.post<OcrItem[]>(`${environment.apiUrl}/invoice-ocr`, fd)
      .pipe(timeout(45000));
  }

  /** 新增/更新分期撥款明細（4 種申請類型共用語意；僅財務部/Superadmin）*/
  upsertInstallments(id: number, body: UpsertInstallmentsRequest): Observable<{id: number; installmentCount: number}> {
    return this.http.patch<{id: number; installmentCount: number}>(
      `${environment.apiUrl}/payment-requests/${id}/installments`,
      body,
    );
  }
}
