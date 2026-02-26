import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {OvertimeRequestService} from '../../services/overtime-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/overtime-request.model';

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
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');
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
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = !['draft', 'pending', 'returned'].includes(r.approvalStatus);
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

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.isReadOnly) return;
    const payload = this._buildPayload();
    const obs = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/overtime-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid || this.isReadOnly) return;
    const payload = this._buildPayload();
    const save$ = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    this.errorMsg.set('');
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => this.router.navigate(['/admin/overtime-requests']),
          error: (err: HttpErrorResponse) => {
            this.errorMsg.set(err.error?.message || '送出失敗，請稍後再試。');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  private _buildPayload() {
    const v = this.form.value;
    const ids = Array.from(this.selectedProjectIds);
    const codes = ids.map(id => this.projects.find(p => p.id === id)?.code).filter(Boolean) as string[];
    return {
      overtimeDate:   new Date(v.overtimeDate!),
      projectIds:     ids.length > 0 ? ids : undefined,
      projectCodes:   codes.length > 0 ? codes : undefined,
      estimatedHours: +v.estimatedHours!,
      reason:         v.reason!,
    };
  }
}
