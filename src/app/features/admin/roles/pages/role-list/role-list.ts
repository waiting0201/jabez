import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {BehaviorSubject, switchMap} from 'rxjs';
import {RoleService} from '../../services/role.service';
import {Role} from '../../models/role.model';

@Component({
  selector: 'app-role-list',
  templateUrl: './role-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe],
})
export class RoleList {
  private roleService = inject(RoleService);
  private refresh$ = new BehaviorSubject<void>(undefined);
  roles$ = this.refresh$.pipe(switchMap(() => this.roleService.getAll()));

  delete(role: Role) {
    if (confirm(`Delete role "${role.name}"?`)) {
      this.roleService.delete(role.id).subscribe(() => this.refresh$.next());
    }
  }
}
