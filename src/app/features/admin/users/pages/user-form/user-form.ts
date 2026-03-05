import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {debounceTime, distinctUntilChanged, switchMap, catchError} from 'rxjs/operators';
import {of} from 'rxjs';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {InsuranceBracketService} from '../../../insurance-brackets/services/insurance-bracket.service';
import {Role} from '../../../roles/models/role.model';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {User, UserStatus} from '../../models/user.model';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
})
export class UserForm implements OnInit {
  private fb              = inject(FormBuilder);
  private userService     = inject(UserService);
  private roleService     = inject(RoleService);
  private deptService     = inject(DepartmentService);
  private jtService       = inject(JobTitleService);
  private bracketService  = inject(InsuranceBracketService);
  private route           = inject(ActivatedRoute);
  private router          = inject(Router);
  private destroyRef      = inject(DestroyRef);
  private authService     = inject(AuthService);

  isSuperAdmin = this.authService.isSuperAdmin;
  roles       = signal<Role[]>([]);
  departments = signal<Department[]>([]);
  jobTitles   = signal<JobTitle[]>([]);
  allUsers    = signal<User[]>([]);
  isEdit = false;
  userId = '';
  errorMsg        = signal('');
  laborInsurance  = signal<number | null>(null);
  healthInsurance = signal<number | null>(null);

  form = this.fb.group({
    name:         ['', Validators.required],
    email:        ['', [Validators.required, Validators.email]],
    password:     ['', [Validators.required, Validators.minLength(6)]],
    roleId:       ['viewer' as string],
    status:       ['active' as UserStatus, Validators.required],
    departmentId: [null as number | null],
    jobTitleId:   [null as number | null],
    hireDate:     ['' as string],
    resignDate:   ['' as string],
    baseSalary:   [null as number | null],
    agentUserId:  ['' as string],
  });

  ngOnInit() {
    // 僅 Superadmin 可選擇角色，需載入角色清單並加上必填驗證
    if (this.isSuperAdmin()) {
      this.form.get('roleId')!.setValidators(Validators.required);
      this.form.get('roleId')!.updateValueAndValidity();
      this.roleService.getAll().subscribe({
        next: r => this.roles.set(r),
        error: err => console.error('[UserForm] 無法載入角色清單', err),
      });
    }
    this.deptService.getAll().subscribe(d => this.departments.set(d));
    this.jtService.getAll().subscribe(j => this.jobTitles.set(j));
    this.userService.getAll().subscribe(u => this.allUsers.set(u));

    // 監聽底薪變化，非同步查詢對應勞健保級距（switchMap 自動取消前次請求）
    this.form.get('baseSalary')!.valueChanges.pipe(
      takeUntilDestroyed(this.destroyRef),
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(val =>
        val !== null && val > 0
          ? this.bracketService.lookupBySalary(val).pipe(catchError(() => of(null)))
          : of(null)
      ),
    ).subscribe(bracket => {
      this.laborInsurance.set(bracket?.laborInsuranceEmployee ?? null);
      this.healthInsurance.set(bracket?.healthInsuranceEmployee ?? null);
    });

    this.userId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.userId) {
      this.isEdit = true;
      // 編輯模式：密碼非必填
      this.form.get('password')!.clearValidators();
      this.form.get('password')!.setValidators(Validators.minLength(6));
      this.form.get('password')!.updateValueAndValidity();
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
    return this.allUsers().filter(u => u.id !== this.userId);
  }

  private toDateString(d: Date): string {
    return new Date(d).toISOString().substring(0, 10);
  }

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const {roleId, hireDate, resignDate, departmentId, jobTitleId, agentUserId, password, ...rest} = this.form.value as any;

    const deptId  = departmentId  || undefined;
    const jtId    = jobTitleId    || undefined;

    const deptName = deptId ? this.departments().find(d => d.id === deptId)?.name : undefined;
    const jtName   = jtId   ? this.jobTitles().find(j => j.id === jtId)?.name    : undefined;
    const agent    = agentUserId ? this.allUsers().find(u => u.id === agentUserId) : undefined;

    const payload = {
      ...rest,
      password:      password || undefined,
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
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/users']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
