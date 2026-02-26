import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {PermissionService} from '../../services/permission.service';
import {Permission} from '../../models/permission.model';

@Component({
  selector: 'app-permission-list',
  templateUrl: './permission-list.html',
  imports: [RouterLink],
})
export class PermissionList {
  private permissionService = inject(PermissionService);

  modules = this.permissionService.getModules();
  allPermissions: Permission[] = [];

  constructor() {
    this.load();
  }

  getByModule(module: string): Permission[] {
    return this.allPermissions.filter(p => p.module === module);
  }

  delete(perm: Permission) {
    if (confirm(`Delete permission "${perm.name}"?`)) {
      this.permissionService.delete(perm.id).subscribe(() => this.load());
    }
  }

  private load() {
    this.permissionService.getAll().subscribe(p => this.allPermissions = p);
  }
}
