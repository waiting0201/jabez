import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {DepartmentService} from '../../services/department.service';
import {Department} from '../../models/department.model';

@Component({
  selector: 'app-department-form',
  templateUrl: './department-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class DepartmentForm implements OnInit {
  private fb = inject(FormBuilder);
  private deptService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  departments: Department[] = [];
  isEdit = false;
  deptId = 0;
  errorMsg = signal('');

  form = this.fb.group({
    name:        ['', Validators.required],
    code:        [''],
    description: [''],
    parentId:    [null as number | null],
    sortOrder:   [0],
  });

  ngOnInit() {
    this.deptService.getAll().subscribe(d => this.departments = d);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.deptId = +id;
      this.deptService.getById(this.deptId).subscribe(dept => {
        if (dept) this.form.patchValue({...dept, parentId: dept.parentId ?? null});
      });
    }
  }

  getAvailableParents(): Department[] {
    return this.departments.filter(d => d.id !== this.deptId);
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.deptService.update(this.deptId, value)
      : this.deptService.create(value);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/departments']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
