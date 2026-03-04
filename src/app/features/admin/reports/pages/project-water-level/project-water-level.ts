import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {HttpClient} from '@angular/common/http';
import {environment} from '@/environments/environment';

export interface ProjectWaterLevelRow {
  projectId: number;
  projectCode: string;
  status: string;
  departmentName: string;
  businessAmount: number | null;
  paymentAmount: number;
  paidAmount: number;
  percentage: number | null;
}

@Component({
  selector: 'app-project-water-level',
  templateUrl: './project-water-level.html',
  imports: [CommonModule],
})
export class ProjectWaterLevel implements OnInit {
  private http = inject(HttpClient);

  records = signal<ProjectWaterLevelRow[]>([]);
  loading = signal(false);

  ngOnInit() {
    this.fetchData();
  }

  fetchData() {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/reports/project-water-level`).subscribe({
      next: (res) => {
        const items = res?.data ?? res ?? [];
        this.records.set(
          (Array.isArray(items) ? items : []).map((r: any) => ({
            projectId: r.projectId,
            projectCode: r.projectCode ?? '—',
            status: r.status ?? 'active',
            departmentName: r.departmentName ?? '—',
            businessAmount: r.businessAmount,
            paymentAmount: r.paymentAmount ?? 0,
            paidAmount: r.paidAmount ?? 0,
            percentage: r.percentage,
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

  getBarColor(pct: number | null): string {
    if (pct === null) return 'var(--text-muted)';
    if (pct >= 90) return 'var(--red)';
    if (pct >= 70) return 'var(--yellow)';
    return 'var(--forest)';
  }

  getBarWidth(pct: number | null): string {
    if (pct === null) return '0%';
    return Math.min(pct, 100) + '%';
  }
}
