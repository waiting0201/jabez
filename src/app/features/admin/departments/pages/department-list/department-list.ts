import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {BehaviorSubject, switchMap} from 'rxjs';
import {DepartmentService} from '../../services/department.service';
import {Department} from '../../models/department.model';

@Component({
  selector: 'app-department-list',
  templateUrl: './department-list.html',
  imports: [RouterLink, AsyncPipe],
})
export class DepartmentList {
  private deptService = inject(DepartmentService);
  private refresh$ = new BehaviorSubject<void>(undefined);
  departments$ = this.refresh$.pipe(switchMap(() => this.deptService.getAll()));

  delete(dept: Department) {
    if (confirm(`確定要刪除部門「${dept.name}」嗎？`)) {
      this.deptService.delete(dept.id).subscribe(() => this.refresh$.next());
    }
  }
}
