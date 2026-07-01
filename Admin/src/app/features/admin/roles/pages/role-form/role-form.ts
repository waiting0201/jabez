import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {forkJoin, of} from 'rxjs';
import {RoleService} from '../../services/role.service';
import {PermissionService} from '../../../permissions/services/permission.service';
import {Permission} from '../../../permissions/models/permission.model';
import {AuthService} from '../../../../../core/auth/services/auth.service';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-role-form',
  templateUrl: './role-form.html',
  imports: [ReactiveFormsModule, RouterLink, ScrollIntoViewDirective],
})
export class RoleForm implements OnInit {
  private fb = inject(FormBuilder);
  private roleService = inject(RoleService);
  private permissionService = inject(PermissionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  permissions = signal<Permission[]>([]);
  modules = signal<string[]>([]);
  isEdit = false;
  roleId = '';
  isLoading = signal(true);
  errorMsg = signal('');

  form = this.fb.group({
    name:            ['', Validators.required],
    description:     [''],
    permissionCodes: [[] as string[]],
  });

  ngOnInit() {
    this.roleId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.roleId) this.isEdit = true;

    // 使用 forkJoin 確保權限列表與角色資料同時載入完成，避免 race condition
    forkJoin({
      permissions: this.permissionService.getAll(),
      role: this.roleId ? this.roleService.getById(this.roleId) : of(null),
    }).subscribe({
      next: ({permissions, role}) => {
        // 權限管理模組僅 Superadmin 可見
        const filtered = this.authService.isSuperAdmin()
          ? permissions
          : permissions.filter(x => x.module !== '權限管理');
        this.permissions.set(filtered);
        this.modules.set([...new Set(filtered.map(x => x.module))]);
        if (role) this.form.patchValue({...role});
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMsg.set('載入資料失敗，請稍後再試。');
        this.isLoading.set(false);
      },
    });
  }

  getPermissionsByModule(module: string): Permission[] {
    return this.permissions().filter(p => p.module === module);
  }

  isPermissionSelected(code: string): boolean {
    return (this.form.value.permissionCodes ?? []).includes(code);
  }

  togglePermission(code: string) {
    const current = this.form.value.permissionCodes ?? [];
    const updated = current.includes(code)
      ? current.filter(c => c !== code)
      : [...current, code];
    this.form.patchValue({permissionCodes: updated});
  }

  toggleModule(module: string, checked: boolean) {
    const moduleCodes = this.getPermissionsByModule(module).map(p => p.code);
    const current = this.form.value.permissionCodes ?? [];
    const updated = checked
      ? [...new Set([...current, ...moduleCodes])]
      : current.filter(c => !moduleCodes.includes(c));
    this.form.patchValue({permissionCodes: updated});
  }

  isModuleAllSelected(module: string): boolean {
    const moduleCodes = this.getPermissionsByModule(module).map(p => p.code);
    return moduleCodes.every(c => this.isPermissionSelected(c));
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.roleService.update(this.roleId, value)
      : this.roleService.create(value);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/roles']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
