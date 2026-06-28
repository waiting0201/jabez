import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {MonthlyPayroll, PayrollAdjustment, PayrollAdjustmentRequest} from '../models/payroll.model';
import {environment} from '../../../../../environments/environment';

@Injectable({providedIn: 'root'})
export class PayrollService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/payroll`;

  getMonthly(year: number, month: number): Observable<MonthlyPayroll> {
    return this.http.get<MonthlyPayroll>(this.base, {params: {year, month}});
  }

  getAdjustment(employeeId: string, year: number, month: number): Observable<PayrollAdjustment | null> {
    return this.http.get<PayrollAdjustment | null>(`${this.base}/${employeeId}/adjustment`, {params: {year, month}});
  }

  upsertAdjustment(employeeId: string, year: number, month: number, body: PayrollAdjustmentRequest): Observable<PayrollAdjustment> {
    return this.http.put<PayrollAdjustment>(`${this.base}/${employeeId}/adjustment`, body, {params: {year, month}});
  }

  sendSlips(year: number, month: number): Observable<{sent: number; total: number; errors: string[]}> {
    return this.http.post<{sent: number; total: number; errors: string[]}>(`${this.base}/send-slips`, null, {params: {year, month}});
  }
}
