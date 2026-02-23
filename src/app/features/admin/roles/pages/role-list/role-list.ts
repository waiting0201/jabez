import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {RoleService} from '../../services/role.service';
import {Role} from '../../models/role.model';

@Component({
  selector: 'app-role-list',
  templateUrl: './role-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe],
})
export class RoleList {
  private roleService = inject(RoleService);
  roles$ = this.roleService.getAll();

  delete(role: Role) {
    if (confirm(`Delete role "${role.name}"?`)) {
      this.roleService.delete(role.id).subscribe();
    }
  }
}
