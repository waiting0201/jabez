import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ProjectService} from '../../services/project.service';
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

  departments: Department[] = [];
  isEdit = false;
  projectId = 0;

  form = this.fb.group({
    code:           ['', Validators.required],
    departmentId:   [null as number | null, Validators.required],
    budgetAmount:   [null as number | null, [Validators.required, Validators.min(0)]],
    actualAmount:   [null as number | null, [Validators.required, Validators.min(0)]],
    businessAmount: [null as number | null, [Validators.required, Validators.min(0)]],
    googleDriveUrl: ['', Validators.required],
  });

  ngOnInit() {
    this.deptService.getAll().subscribe(d => this.departments = d);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.projectId = +id;
      this.projectService.getById(this.projectId).subscribe(p => {
        if (p) this.form.patchValue({
          code:           p.code,
          departmentId:   p.departmentId ?? null,
          budgetAmount:   p.budgetAmount ?? null,
          actualAmount:   p.actualAmount ?? null,
          businessAmount: p.businessAmount ?? null,
          googleDriveUrl: p.googleDriveUrl ?? '',
        });
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const v = this.form.value;
    const dept = this.departments.find(d => d.id === v.departmentId);
    const payload = {
      code:           v.code!,
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
    obs.subscribe(() => this.router.navigate(['/admin/projects']));
  }
}
