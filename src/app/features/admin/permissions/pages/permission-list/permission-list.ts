import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {BehaviorSubject, map, shareReplay, switchMap} from 'rxjs';
import {PermissionService} from '../../services/permission.service';
import {Permission} from '../../models/permission.model';

@Component({
  selector: 'app-permission-list',
  templateUrl: './permission-list.html',
  imports: [RouterLink, AsyncPipe],
})
export class PermissionList {
  private permissionService = inject(PermissionService);
  private refresh$ = new BehaviorSubject<void>(undefined);

  private all$ = this.refresh$.pipe(
    switchMap(() => this.permissionService.getAll()),
    shareReplay(1),
  );
  permissions$ = this.all$;
  modules$ = this.all$.pipe(
    map(perms => [...new Set(perms.map(p => p.module))]),
  );

  getByModule(perms: Permission[], module: string): Permission[] {
    return perms.filter(p => p.module === module);
  }

  delete(perm: Permission) {
    if (confirm(`Delete permission "${perm.name}"?`)) {
      this.permissionService.delete(perm.id).subscribe(() => this.refresh$.next());
    }
  }
}
