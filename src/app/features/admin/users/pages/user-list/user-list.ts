import {Component, inject, OnInit} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe, CurrencyPipe} from '@angular/common';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {User} from '../../models/user.model';
import {Role} from '../../../roles/models/role.model';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe, CurrencyPipe],
})
export class UserList implements OnInit {
  private userService = inject(UserService);
  private roleService = inject(RoleService);

  users$ = this.userService.getAll();
  roles: Role[] = [];

  ngOnInit() {
    this.roleService.getAll().subscribe(r => this.roles = r);
  }

  getRoleNames(roleIds: string[]): string {
    return roleIds.map(id => this.roles.find(r => r.id === id)?.name ?? id).join(', ');
  }

  delete(user: User) {
    if (confirm(`確定要刪除員工「${user.name}」嗎？`)) {
      this.userService.delete(user.id).subscribe();
    }
  }
}
