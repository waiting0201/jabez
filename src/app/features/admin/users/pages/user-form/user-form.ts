import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {Role} from '../../../roles/models/role.model';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {User, UserStatus} from '../../models/user.model';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class UserForm implements OnInit {
  private fb             = inject(FormBuilder);
  private userService    = inject(UserService);
  private roleService    = inject(RoleService);
  private deptService    = inject(DepartmentService);
  private jtService      = inject(JobTitleService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);

  roles: Role[]             = [];
  departments: Department[] = [];
  jobTitles: JobTitle[]     = [];
  allUsers: User[]          = [];
  isEdit = false;
  userId = '';

  form = this.fb.group({
    name:         ['', Validators.required],
    email:        ['', [Validators.required, Validators.email]],
    roleId:       ['' as string, Validators.required],
    status:       ['active' as UserStatus, Validators.required],
    departmentId: [null as number | null],
    jobTitleId:   [null as number | null],
    hireDate:     ['' as string],
    resignDate:   ['' as string],
    baseSalary:   [null as number | null],
    agentUserId:  ['' as string],
  });

  ngOnInit() {
    this.roleService.getAll().subscribe(r => this.roles = r);
    this.deptService.getAll().subscribe(d => this.departments = d);
    this.jtService.getAll().subscribe(j => this.jobTitles = j);
    this.userService.getAll().subscribe(u => this.allUsers = u);

    this.userId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.userId) {
      this.isEdit = true;
      this.userService.getById(this.userId).subscribe(user => {
        if (!user) return;
        this.form.patchValue({
          ...user,
          roleId:       user.roleIds[0] ?? '',
          departmentId: user.departmentId ?? null,
          jobTitleId:   user.jobTitleId ?? null,
          hireDate:     user.hireDate   ? this.toDateString(user.hireDate)   : '',
          resignDate:   user.resignDate ? this.toDateString(user.resignDate) : '',
          baseSalary:   user.baseSalary ?? null,
          agentUserId:  user.agentUserId ?? '',
        });
      });
    }
  }

  getAvailableAgents(): User[] {
    return this.allUsers.filter(u => u.id !== this.userId);
  }

  private toDateString(d: Date): string {
    return new Date(d).toISOString().substring(0, 10);
  }

  submit() {
    if (this.form.invalid) return;
    const {roleId, hireDate, resignDate, departmentId, jobTitleId, agentUserId, ...rest} = this.form.value as any;

    const deptId  = departmentId  || undefined;
    const jtId    = jobTitleId    || undefined;

    const deptName = deptId ? this.departments.find(d => d.id === deptId)?.name : undefined;
    const jtName   = jtId   ? this.jobTitles.find(j => j.id === jtId)?.name    : undefined;
    const agent    = agentUserId ? this.allUsers.find(u => u.id === agentUserId) : undefined;

    const payload = {
      ...rest,
      roleIds:       roleId ? [roleId] : [],
      departmentId:  deptId,
      departmentName: deptName,
      jobTitleId:    jtId,
      jobTitleName:  jtName,
      hireDate:      hireDate   ? new Date(hireDate)   : undefined,
      resignDate:    resignDate ? new Date(resignDate) : undefined,
      agentUserId:   agentUserId || undefined,
      agentName:     agent?.name,
    };

    const obs = this.isEdit
      ? this.userService.update(this.userId, payload)
      : this.userService.create(payload);
    obs.subscribe(() => this.router.navigate(['/admin/users']));
  }
}
