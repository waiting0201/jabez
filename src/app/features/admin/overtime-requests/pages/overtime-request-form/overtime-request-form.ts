import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {OvertimeRequestService} from '../../services/overtime-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/overtime-request.model';

@Component({
  selector: 'app-overtime-request-form',
  templateUrl: './overtime-request-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class OvertimeRequestForm implements OnInit {
  private fb        = inject(FormBuilder);
  private service   = inject(OvertimeRequestService);
  private projects$ = inject(ProjectService);
  private route     = inject(ActivatedRoute);
  private router    = inject(Router);

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  projects: Project[] = [];

  /** 已勾選的專案 ID 集合 */
  selectedProjectIds = new Set<number>();
  /** 檢視模式時顯示的專案編號 */
  displayProjectCodes: string[] = [];

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    overtimeDate:   ['', Validators.required],
    estimatedHours: [1, [Validators.required, Validators.min(0.5)]],
    reason:         ['', Validators.required],
  });

  ngOnInit() {
    this.projects$.getAll().subscribe(p => this.projects = p.filter(x => x.status === 'active'));
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'pending' && r.approvalStatus !== 'returned';
        this.form.patchValue({
          overtimeDate: r.overtimeDate instanceof Date
            ? r.overtimeDate.toISOString().split('T')[0]
            : String(r.overtimeDate),
          estimatedHours: r.estimatedHours,
          reason:         r.reason,
        });
        if (r.projectIds) {
          r.projectIds.forEach(id => this.selectedProjectIds.add(id));
        }
        if (r.projectCodes) {
          this.displayProjectCodes = r.projectCodes;
        }
        if (this.isReadOnly) this.form.disable();
      });
    }
  }

  toggleProject(projectId: number) {
    if (this.isReadOnly) return;
    if (this.selectedProjectIds.has(projectId)) {
      this.selectedProjectIds.delete(projectId);
    } else {
      this.selectedProjectIds.add(projectId);
    }
  }

  submit() {
    if (this.form.invalid || this.isReadOnly) return;
    const v = this.form.value;
    const ids = Array.from(this.selectedProjectIds);
    const codes = ids.map(id => this.projects.find(p => p.id === id)?.code).filter(Boolean) as string[];
    const payload = {
      overtimeDate:   new Date(v.overtimeDate!),
      projectIds:     ids.length > 0 ? ids : undefined,
      projectCodes:   codes.length > 0 ? codes : undefined,
      estimatedHours: +v.estimatedHours!,
      reason:         v.reason!,
    };
    const obs = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(['/admin/overtime-requests']));
  }
}
