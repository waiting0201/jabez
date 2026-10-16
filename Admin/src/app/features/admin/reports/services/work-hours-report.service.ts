import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '@/environments/environment';

/** 工時總表一列：(員工, 專案) 彙總；projectId 為 null ＝ 未分配 */
export interface WorkHoursRow {
  departmentName: string | null;
  employeeId: string;
  employeeName: string;
  projectId: number | null;
  projectCode: string | null;
  projectName: string | null;
  days: number;
  hours: number;
}

/** 逐日明細一列（Excel 第二張工作表）；percentage 為 null ＝ 未分配 */
export interface WorkHoursDailyRow {
  date: string;
  departmentName: string | null;
  employeeId: string;
  employeeName: string;
  projectId: number | null;
  projectCode: string | null;
  projectName: string | null;
  percentage: number | null;
  hours: number;
  isAutoClockOut: boolean;
}

export interface WorkHoursQuery {
  dateFrom: string;
  dateTo: string;
  departmentId?: number | null;
  projectId?: number | null;
}

@Injectable({providedIn: 'root'})
export class WorkHoursReportService {
  private http = inject(HttpClient);

  private toParams(q: WorkHoursQuery): Record<string, string> {
    const params: Record<string, string> = {dateFrom: q.dateFrom, dateTo: q.dateTo};
    if (q.departmentId) params['departmentId'] = String(q.departmentId);
    if (q.projectId) params['projectId'] = String(q.projectId);
    return params;
  }

  getSummary(q: WorkHoursQuery): Observable<WorkHoursRow[]> {
    return this.http.get<WorkHoursRow[]>(`${environment.apiUrl}/reports/work-hours`, {params: this.toParams(q)});
  }

  /** 逐日明細：只在匯出 Excel 時才取 */
  getDaily(q: WorkHoursQuery): Observable<WorkHoursDailyRow[]> {
    return this.http.get<WorkHoursDailyRow[]>(`${environment.apiUrl}/reports/work-hours/daily`, {params: this.toParams(q)});
  }
}
