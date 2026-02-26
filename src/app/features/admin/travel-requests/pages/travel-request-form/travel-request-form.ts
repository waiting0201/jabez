import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {TravelRequestService} from '../../services/travel-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {ApprovalStatus, APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/travel-request.model';

@Component({
  selector: 'app-travel-request-form',
  templateUrl: './travel-request-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class TravelRequestForm implements OnInit {
  private fb       = inject(FormBuilder);
  private service  = inject(TravelRequestService);
  private projects$ = inject(ProjectService);
  private route    = inject(ActivatedRoute);
  private router   = inject(Router);
  private cdr      = inject(ChangeDetectorRef);

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  errorMsg = signal('');
  projects: Project[] = [];

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    destination:   ['', Validators.required],
    startDate:     ['', Validators.required],
    endDate:       ['', Validators.required],
    estimatedCost: [0, [Validators.required, Validators.min(0)]],
    purpose:       ['', Validators.required],
    projectId:     [null as number | null],
  });

  loadingProjects = true;

  ngOnInit() {
    this.projects$.getAll().subscribe({
      next: p => {
        this.projects = p.filter(x => x.status === 'active');
        this.loadingProjects = false;
        this.cdr.markForCheck();
      },
      error: () => { this.loadingProjects = false; this.errorMsg.set('載入專案資料失敗。'); },
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft';
        this.form.patchValue({
          destination:   r.destination,
          startDate: r.startDate instanceof Date
            ? r.startDate.toISOString().split('T')[0]
            : String(r.startDate),
          endDate: r.endDate instanceof Date
            ? r.endDate.toISOString().split('T')[0]
            : String(r.endDate),
          estimatedCost: r.estimatedCost,
          purpose:       r.purpose,
          projectId:     r.projectId ?? null,
        });
        if (this.isReadOnly) this.form.disable();
        this.cdr.markForCheck();
      });
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
        this.router.navigate(['/admin/travel-requests']);
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
          next: () => this.router.navigate(['/admin/travel-requests']),
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
    const project = this.projects.find(p => p.id === v.projectId);
    return {
      destination:   v.destination!,
      startDate:     new Date(v.startDate!),
      endDate:       new Date(v.endDate!),
      estimatedCost: +v.estimatedCost!,
      purpose:       v.purpose!,
      projectId:     v.projectId ?? undefined,
      projectCode:   project?.code,
    };
  }
}
