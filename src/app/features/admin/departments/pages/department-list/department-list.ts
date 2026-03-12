import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {BehaviorSubject, map, switchMap} from 'rxjs';
import {DepartmentService} from '../../services/department.service';
import {Department} from '../../models/department.model';

interface DepartmentRow extends Department {
  depth: number;
  hasChildren: boolean;
}

@Component({
  selector: 'app-department-list',
  templateUrl: './department-list.html',
  imports: [RouterLink, AsyncPipe],
})
export class DepartmentList {
  private deptService = inject(DepartmentService);
  private refresh$ = new BehaviorSubject<void>(undefined);

  departments$ = this.refresh$.pipe(
    switchMap(() => this.deptService.getAll()),
    map(depts => this.buildTree(depts)),
  );

  delete(dept: DepartmentRow) {
    if (confirm(`確定要刪除部門「${dept.name}」嗎？`)) {
      this.deptService.delete(dept.id).subscribe(() => this.refresh$.next());
    }
  }

  /** 將平面列表轉為依母子層級排列的樹狀列表 */
  private buildTree(depts: Department[]): DepartmentRow[] {
    const childrenMap = new Map<number | null, Department[]>();
    const hasChildrenSet = new Set<number>();

    for (const d of depts) {
      const key = d.parentId ?? null;
      if (!childrenMap.has(key)) childrenMap.set(key, []);
      childrenMap.get(key)!.push(d);
      if (d.parentId) hasChildrenSet.add(d.parentId);
    }

    const result: DepartmentRow[] = [];
    const walk = (parentId: number | null, depth: number) => {
      const children = childrenMap.get(parentId) ?? [];
      for (const c of children) {
        result.push({...c, depth, hasChildren: hasChildrenSet.has(c.id)});
        walk(c.id, depth + 1);
      }
    };
    walk(null, 0);
    return result;
  }
}
