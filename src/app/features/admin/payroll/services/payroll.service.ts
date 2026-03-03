import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {MonthlyPayroll} from '../models/payroll.model';
import {environment} from '../../../../../environments/environment';

@Injectable({providedIn: 'root'})
export class PayrollService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/payroll`;

  getMonthly(year: number, month: number): Observable<MonthlyPayroll> {
    return this.http.get<MonthlyPayroll>(this.base, {params: {year, month}});
  }
}
