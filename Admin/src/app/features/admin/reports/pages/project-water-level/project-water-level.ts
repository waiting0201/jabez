import {Component, inject, OnInit, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient, HttpParams} from '@angular/common/http';
import {toSignal} from '@angular/core/rxjs-interop';
import {environment} from '@/environments/environment';
import {AuthService} from '@/app/core/auth/services/auth.service';
import {ProjectService} from '../../../projects/services/project.service';
import {PROJECT_STATUS_LABELS} from '../../../projects/models/project.model';

export interface ProjectWaterLevelRow {
  projectId: number;
  projectCode: string;
  projectName: string;
  status: string;
  departmentName: string;
  contractAmount: number | null;
  businessAmount: number | null;
  disbursedAmount: number;
  percentage: number | null;
  totalPercentage: number | null;
}

@Component({
  selector: 'app-project-water-level',
  templateUrl: './project-water-level.html',
  imports: [CommonModule, FormsModule],
})
export class ProjectWaterLevel implements OnInit {
  private http = inject(HttpClient);
  private projectService = inject(ProjectService);

  /**
   * 「總專案水位」欄（分母＝契約金額，含公司保留 40%）為管理層資訊，需獨立權限。
   * 用 component 欄位而非 *appHasPermission —— 空資料列的 colspan 也要跟著變，共用同一個真相。
   * 後端 ProjectWaterLevelHandler 亦會對無此權限者回傳 totalPercentage = null（縱深防禦）。
   */
  readonly canSeeTotal = inject(AuthService).hasPermission('reports-project-water-level:total');

  records = signal<ProjectWaterLevelRow[]>([]);
  loading = signal(false);

  selectedYearInput: number | undefined;
  selectedStatusInput: string | undefined;
  yearOptions = toSignal(this.projectService.getYears(), {initialValue: [] as number[]});
  readonly statusLabel = PROJECT_STATUS_LABELS;

  ngOnInit() {
    this.fetchData();
  }

  doSearch() {
    this.fetchData();
  }

  fetchData() {
    this.loading.set(true);
    let params = new HttpParams();
    if (this.selectedYearInput != null) params = params.set('year', String(this.selectedYearInput));
    if (this.selectedStatusInput) params = params.set('status', this.selectedStatusInput);

    this.http.get<any>(`${environment.apiUrl}/reports/project-water-level`, {params}).subscribe({
      next: (res) => {
        const items = res?.data ?? res ?? [];
        this.records.set(
          (Array.isArray(items) ? items : []).map((r: any) => ({
            projectId: r.projectId,
            projectCode: r.projectCode ?? '—',
            projectName: r.projectName ?? '',
            status: r.status ?? 'active',
            departmentName: r.departmentName ?? '—',
            contractAmount: r.contractAmount,
            businessAmount: r.businessAmount,
            disbursedAmount: r.disbursedAmount ?? 0,
            percentage: r.percentage,
            totalPercentage: r.totalPercentage,
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
