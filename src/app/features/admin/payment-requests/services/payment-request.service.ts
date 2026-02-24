import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {BehaviorSubject, Observable, of} from 'rxjs';
import {PaymentRequest} from '../models/payment-request.model';
import {PagedResult} from '../../../../shared/models/paged-result.model';

const MOCK_REQUESTS: PaymentRequest[] = [
  {
    id: 1, type: 'vendor', projectId: 1, projectCode: 'P2024-001',
    invoices: [
      {id: 'i1', fileName: 'invoice_001.jpg', invoiceNo: 'AB-12345678', amount: 15000},
      {id: 'i2', fileName: 'invoice_002.jpg', invoiceNo: 'CD-87654321', amount: 8500},
    ],
    totalAmount: 23500, approvalStatus: 'approved',
    createdAt: new Date('2024-04-10'),
  },
  {
    id: 2, type: 'travel', projectId: 2, projectCode: 'P2024-002',
    invoices: [
      {id: 'i3', fileName: 'receipt_hotel.jpg', invoiceNo: 'EF-11223344', amount: 4200},
      {id: 'i4', fileName: 'receipt_train.jpg', invoiceNo: 'GH-55667788', amount: 980},
    ],
    totalAmount: 5180, approvalStatus: 'pending',
    createdAt: new Date('2024-07-05'),
  },
  {
    id: 3, type: 'advance', projectId: 3, projectCode: 'P2025-001',
    invoices: [
      {id: 'i5', fileName: 'advance_001.jpg', invoiceNo: 'IJ-99887766', amount: 20000},
    ],
    totalAmount: 20000, approvalStatus: 'rejected',
    createdAt: new Date('2025-02-01'),
  },
];

@Injectable({providedIn: 'root'})
export class PaymentRequestService {
  private http = inject(HttpClient);
  private items$ = new BehaviorSubject<PaymentRequest[]>(MOCK_REQUESTS);

  getAll(): Observable<PaymentRequest[]> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<PaymentRequest[]>('/api/payment-requests').pipe(
    //   tap(items => this.items$.next(items)),
    // );
    return this.items$.asObservable();
  }

  getPaged(page: number, pageSize: number): Observable<PagedResult<PaymentRequest>> {
    // ─── 真實 API（後端就緒時啟用）─────────────────────────
    // return this.http.get<{success: boolean; data: PagedResult<PaymentRequest>}>(
    //   '/api/payment-requests', {params: {page, pageSize}}
    // ).pipe(map(r => r.data!));

    // ─── Mock（前端開發用）──────────────────────────────────
    const all = this.items$.getValue();
    const start = (page - 1) * pageSize;
    const totalPages = Math.max(1, Math.ceil(all.length / pageSize));
    return of({items: all.slice(start, start + pageSize), totalCount: all.length, page, pageSize, totalPages});
  }

  getById(id: number): Observable<PaymentRequest | undefined> {
    return of(this.items$.getValue().find(r => r.id === id));
  }

  create(data: Omit<PaymentRequest, 'id' | 'createdAt'>): Observable<PaymentRequest> {
    const item: PaymentRequest = {...data, id: Date.now(), createdAt: new Date()};
    this.items$.next([...this.items$.getValue(), item]);
    return of(item);
  }

  update(id: number, changes: Partial<PaymentRequest>): Observable<PaymentRequest> {
    const updated = this.items$.getValue().map(r => r.id === id ? {...r, ...changes} : r);
    this.items$.next(updated);
    return of(updated.find(r => r.id === id)!);
  }

  delete(id: number): Observable<void> {
    this.items$.next(this.items$.getValue().filter(r => r.id !== id));
    return of(undefined);
  }
}
