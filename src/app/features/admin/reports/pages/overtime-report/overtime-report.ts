import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';

export interface OvertimeReportRow {
  id: number;
  employeeName: string;
  overtimeDate: string;
  projectCodes: string;
  estimatedHours: string;
  actualHours: string | null;
  reason: string;
}

@Component({
  selector: 'app-overtime-report',
  templateUrl: './overtime-report.html',
  imports: [CommonModule, FormsModule],
})
export class OvertimeReport implements OnInit {
  private http = inject(HttpClient);

  /** 篩選條件 */
  selectedEmployeeId = signal('');
  selectedProjectId = signal('');
  selectedYear = signal('');
  selectedMonth = signal('');

  /** 員工清單 */
  employees = signal<{id: string; code: string; name: string}[]>([]);

  /** 專案清單 */
  projects = signal<{id: number; code: string}[]>([]);

  /** 年份選項 */
  years = signal<number[]>([]);

  /** 月份選項 */
  months = Array.from({length: 12}, (_, i) => i + 1);

  /** 紀錄 */
  records = signal<OvertimeReportRow[]>([]);
  loading = signal(false);

  /** 分頁 */
  currentPage = signal(1);
  totalCount = signal(0);
  totalPages = signal(1);
  private pageSize = 20;

  ngOnInit() {
    const now = new Date();
    const currentYear = now.getFullYear();
    this.years.set([currentYear - 1, currentYear]);
    this.selectedYear.set(String(currentYear));
    this.selectedMonth.set(String(now.getMonth() + 1));
    this.loadEmployees();
    this.loadProjects();
    this.search();
  }

  loadEmployees() {
    this.http.get<any>(`${environment.apiUrl}/users`).subscribe({
      next: (res) => {
        const items = res?.items ?? res ?? [];
        this.employees.set(
          items.map((u: any) => ({id: u.id, code: u.employeeCode ?? '', name: u.name}))
        );
      },
    });
  }

  loadProjects() {
    this.http.get<any>(`${environment.apiUrl}/projects`).subscribe({
      next: (res) => {
        const items = res?.data?.items ?? res?.items ?? res ?? [];
        this.projects.set(
          items.map((p: any) => ({id: p.id, code: p.code}))
        );
      },
    });
  }

  search() {
    this.currentPage.set(1);
    this.fetchData();
  }

  goToPage(page: number) {
    this.currentPage.set(page);
    this.fetchData();
  }

  private fetchData() {
    this.loading.set(true);

    const params: any = {page: this.currentPage(), pageSize: this.pageSize};
    if (this.selectedEmployeeId()) params.employeeId = this.selectedEmployeeId();
    if (this.selectedProjectId()) params.projectId = this.selectedProjectId();
    if (this.selectedYear()) params.year = this.selectedYear();
    if (this.selectedMonth()) params.month = this.selectedMonth();

    this.http.get<any>(`${environment.apiUrl}/reports/overtime`, {params}).subscribe({
      next: (res) => {
        const data = res?.data ?? res ?? {};
        const items = data?.items ?? [];
        this.totalCount.set(data?.totalCount ?? 0);
        this.totalPages.set(data?.totalPages ?? 1);

        this.records.set(
          items.map((r: any) => ({
            id: r.id,
            employeeName: r.employeeName ?? '—',
            overtimeDate: r.overtimeDate ? new Date(r.overtimeDate).toLocaleDateString('zh-TW') : '',
            projectCodes: r.projectCodes?.join(', ') ?? '',
            estimatedHours: Number(r.estimatedHours).toFixed(1),
            actualHours: r.actualHours != null ? Number(r.actualHours).toFixed(1) : null,
            reason: r.reason ?? '',
          }))
        );
        this.loading.set(false);
      },
      error: () => {
        this.records.set([]);
        this.loading.set(false);
      },
    });
  }
}
