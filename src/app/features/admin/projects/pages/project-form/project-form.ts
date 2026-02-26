import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {ProjectService} from '../../services/project.service';
import {ProjectStatus} from '../../models/project.model';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';

@Component({
  selector: 'app-project-form',
  templateUrl: './project-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class ProjectForm implements OnInit {
  private fb = inject(FormBuilder);
  private projectService = inject(ProjectService);
  private deptService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  departments: Department[] = [];
  loadingDepts = true;
  isEdit = false;
  isClosed = false;
  projectId = 0;
  errorMsg = signal('');

  form = this.fb.group({
    code:           ['', Validators.required],
    status:         ['active' as ProjectStatus, Validators.required],
    departmentId:   [{value: null as number | null, disabled: true}, Validators.required],
    budgetAmount:   [null as number | null, [Validators.required, Validators.min(0)]],
    actualAmount:   [null as number | null, [Validators.required, Validators.min(0)]],
    businessAmount: [null as number | null, [Validators.required, Validators.min(0)]],
    googleDriveUrl: ['', Validators.required],
  });

  ngOnInit() {
    this.deptService.getAll().subscribe({
      next: d => {
        this.departments = d;
        this.loadingDepts = false;
        if (!this.isClosed) this.form.get('departmentId')!.enable();
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingDepts = false;
        if (!this.isClosed) this.form.get('departmentId')!.enable();
        this.errorMsg.set('載入部門資料失敗。');
      },
    });
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.projectId = +id;
      this.projectService.getById(this.projectId).subscribe({
        next: p => {
          if (!p) return;
          this.isClosed = p.status === 'closed';
          this.form.patchValue({
            code:           p.code,
            status:         p.status,
            departmentId:   p.departmentId ?? null,
            budgetAmount:   p.budgetAmount ?? null,
            actualAmount:   p.actualAmount ?? null,
            businessAmount: p.businessAmount ?? null,
            googleDriveUrl: p.googleDriveUrl ?? '',
          });
          if (this.isClosed) this.form.disable();
          this.cdr.markForCheck();
        },
        error: () => this.errorMsg.set('載入專案資料失敗。'),
      });
    }
  }

  submit() {
    if (this.form.invalid || this.isClosed) return;
    const v = this.form.getRawValue();
    if (v.status === 'closed' && !confirm('確定要將此專案設為「已結案」嗎？結案後將無法再修改或刪除。')) return;
    const dept = this.departments.find(d => d.id === v.departmentId);
    const payload = {
      code:           v.code!,
      status:         v.status! as ProjectStatus,
      departmentId:   v.departmentId ?? undefined,
      departmentName: dept?.name,
      budgetAmount:   v.budgetAmount ?? undefined,
      actualAmount:   v.actualAmount ?? undefined,
      businessAmount: v.businessAmount ?? undefined,
      googleDriveUrl: v.googleDriveUrl || undefined,
    };
    const obs = this.isEdit
      ? this.projectService.update(this.projectId, payload)
      : this.projectService.create(payload);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/projects']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
