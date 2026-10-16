import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ToastrService} from 'ngx-toastr';
import * as XLSX from 'xlsx';
import {firstValueFrom} from 'rxjs';
import {DepartmentLookup, DepartmentService} from '@features/admin/departments/services/department.service';
import {ProjectService} from '@features/admin/projects/services/project.service';
import {ProjectOption, ProjectTypeahead} from '@shared/components/project-typeahead';
import {monthToRange} from '../../utils/date-range';
import {WorkHoursQuery, WorkHoursReportService, WorkHoursRow} from '../../services/work-hours-report.service';

interface ProjectGroup {
  key: string;
  projectId: number | null;
  label: string;
  rows: WorkHoursRow[];
  hours: number;
}

const round2 = (n: number) => Math.round(n * 100) / 100;

/**
 * 工時總表：專案 × 員工，工時 = 每天 8 小時 × 佔比（後端計算）。
 * 依專案分組、每組小計、最後總計；「未分配」（系統補卡 / 忘記填）排最後並灰字。
 */
@Component({
  selector: 'app-work-hours-report',
  templateUrl: './work-hours-report.html',
  imports: [DecimalPipe, FormsModule, ProjectTypeahead],
})
export class WorkHoursReport implements OnInit {
  private reportService = inject(WorkHoursReportService);
  private departmentService = inject(DepartmentService);
  private projectService = inject(ProjectService);
  private toastr = inject(ToastrService);

  dateFrom = signal('');
  dateTo = signal('');
  departmentId = signal<number | null>(null);
  projectId = signal<number | null>(null);

  departments = signal<DepartmentLookup[]>([]);
  projects = signal<ProjectOption[]>([]);

  rows = signal<WorkHoursRow[]>([]);
  loading = signal(false);
  exporting = signal(false);

  /** 依專案分組（後端已排序：專案自然排序、未分配最後；此處只依序歸組） */
  groups = computed<ProjectGroup[]>(() => {
    const map = new Map<string, ProjectGroup>();
    for (const r of this.rows()) {
      const key = r.projectId == null ? 'none' : String(r.projectId);
      let g = map.get(key);
      if (!g) {
        g = {
          key,
          projectId: r.projectId,
          label: r.projectId == null ? '未分配' : `${r.projectCode ?? ''} ${r.projectName ?? ''}`.trim(),
          rows: [], hours: 0,
        };
        map.set(key, g);
      }
      g.rows.push(r);
      g.hours = round2(g.hours + r.hours);
    }
    return [...map.values()];
  });

  grandTotal = computed(() => {
    const gs = this.groups();
    return {hours: round2(gs.reduce((a, g) => a + g.hours, 0))};
  });

  ngOnInit(): void {
    const now = new Date();
    const r = monthToRange(now.getFullYear(), now.getMonth() + 1);
    this.dateFrom.set(r.dateFrom);
    this.dateTo.set(r.dateTo);

    this.departmentService.getLookup().subscribe({next: d => this.departments.set(d ?? []), error: () => {}});
    // 篩選用，允許跨部門未結案專案
    this.projectService.getActiveAll().subscribe({
      next: list => this.projects.set(list.map(p => ({id: p.id, code: p.code, name: p.name, departmentName: p.departmentName}))),
      error: () => {},
    });
    this.load();
  }

  private query(): WorkHoursQuery {
    return {
      dateFrom: this.dateFrom(), dateTo: this.dateTo(),
      departmentId: this.departmentId(), projectId: this.projectId(),
    };
  }

  private validRange(): boolean {
    if (!this.dateFrom() || !this.dateTo()) {
      this.toastr.warning('請選擇日期區間');
      return false;
    }
    if (this.dateFrom() > this.dateTo()) {
      this.toastr.warning('起日不可晚於迄日');
      return false;
    }
    return true;
  }

  load(): void {
    if (!this.validRange()) return;
    this.loading.set(true);
    this.reportService.getSummary(this.query()).subscribe({
      next: rows => { this.rows.set(rows ?? []); this.loading.set(false); },
      error: () => { this.rows.set([]); this.loading.set(false); },
    });
  }

  async exportExcel(): Promise<void> {
    if (this.exporting() || this.rows().length === 0 || !this.validRange()) return;
    this.exporting.set(true);
    try {
      const q = this.query();
      const daily = await firstValueFrom(this.reportService.getDaily(q));

      // 工作表 1：工時總表（含小計與總計）
      const aoa: (string | number)[][] = [['專案', '部門', '員工', '工時(小時)']];
      for (const g of this.groups()) {
        for (const r of g.rows) aoa.push([g.label, r.departmentName ?? '', r.employeeName, r.hours]);
        aoa.push([`${g.label} 小計`, '', '', g.hours]);
      }
      const t = this.grandTotal();
      aoa.push(['總計', '', '', t.hours]);
      const ws1 = XLSX.utils.aoa_to_sheet(aoa);
      ws1['!cols'] = [{wch: 36}, {wch: 16}, {wch: 14}, {wch: 12}];

      // 工作表 2：逐日明細
      const ws2 = XLSX.utils.json_to_sheet((daily ?? []).map(d => ({
        '日期': (d.date ?? '').slice(0, 10),
        '部門': d.departmentName ?? '',
        '員工': d.employeeName,
        '專案': d.projectId == null ? '未分配' : `${d.projectCode ?? ''} ${d.projectName ?? ''}`.trim(),
        '佔比(%)': d.percentage ?? '',
        '工時(小時)': d.hours,
        '系統補下班卡': d.isAutoClockOut ? '是' : '',
      })));

      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws1, '工時總表');
      XLSX.utils.book_append_sheet(wb, ws2, '逐日明細');
      XLSX.writeFile(wb, `工時總表_${q.dateFrom}_${q.dateTo}.xlsx`);
    } catch {
      this.toastr.error('匯出失敗，請稍後重試');
    } finally {
      this.exporting.set(false);
    }
  }
}
