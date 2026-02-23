import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {TravelRequestService} from '../../services/travel-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES} from '../../models/travel-request.model';

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

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
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

  ngOnInit() {
    this.projects$.getAll().subscribe(p => this.projects = p);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'pending' && r.approvalStatus !== 'returned';
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
      });
    }
  }

  submit() {
    if (this.form.invalid || this.isReadOnly) return;
    const v = this.form.value;
    const project = this.projects.find(p => p.id === v.projectId);
    const payload = {
      destination:   v.destination!,
      startDate:     new Date(v.startDate!),
      endDate:       new Date(v.endDate!),
      estimatedCost: +v.estimatedCost!,
      purpose:       v.purpose!,
      projectId:     v.projectId ?? undefined,
      projectCode:   project?.code,
    };
    const obs = this.isEdit
      ? this.service.update(this.requestId, payload)
      : this.service.create(payload);
    obs.subscribe(() => this.router.navigate(['/admin/travel-requests']));
  }
}
