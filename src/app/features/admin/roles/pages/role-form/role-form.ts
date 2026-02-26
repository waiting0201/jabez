import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {RoleService} from '../../services/role.service';
import {PermissionService} from '../../../permissions/services/permission.service';
import {Permission} from '../../../permissions/models/permission.model';

@Component({
  selector: 'app-role-form',
  templateUrl: './role-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class RoleForm implements OnInit {
  private fb = inject(FormBuilder);
  private roleService = inject(RoleService);
  private permissionService = inject(PermissionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  permissions: Permission[] = [];
  modules: string[] = [];
  isEdit = false;
  roleId = '';
  errorMsg = signal('');

  form = this.fb.group({
    name:            ['', Validators.required],
    description:     [''],
    permissionCodes: [[] as string[]],
  });

  ngOnInit() {
    this.permissionService.getAll().subscribe(p => {
      this.permissions = p;
      this.modules = [...new Set(p.map(x => x.module))];
    });
    this.roleId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.roleId) {
      this.isEdit = true;
      this.roleService.getById(this.roleId).subscribe(role => {
        if (role) this.form.patchValue({...role});
      });
    }
  }

  getPermissionsByModule(module: string): Permission[] {
    return this.permissions.filter(p => p.module === module);
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
