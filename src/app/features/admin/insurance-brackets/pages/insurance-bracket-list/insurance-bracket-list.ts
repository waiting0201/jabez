import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DecimalPipe} from '@angular/common';
import {BehaviorSubject, switchMap} from 'rxjs';
import {InsuranceBracketService} from '../../services/insurance-bracket.service';
import {InsuranceBracket} from '../../models/insurance-bracket.model';
import {ToastrService} from 'ngx-toastr';

@Component({
  selector: 'app-insurance-bracket-list',
  templateUrl: './insurance-bracket-list.html',
  imports: [RouterLink, AsyncPipe, DecimalPipe],
})
export class InsuranceBracketList {
  private svc = inject(InsuranceBracketService);
  private toastr = inject(ToastrService);

  private refresh$ = new BehaviorSubject<void>(undefined);
  brackets$ = this.refresh$.pipe(
    switchMap(() => this.svc.getAll())
  );

  delete(item: InsuranceBracket) {
    const label = `${item.salaryBracket.toLocaleString()}`;
    if (confirm(`確定要刪除級距「${label}」嗎？`)) {
      this.svc.delete(item.id).subscribe({
        next: () => {
          this.toastr.success('刪除成功');
          this.refresh$.next();
        },
        error: () => this.toastr.error('刪除失敗'),
      });
    }
  }
}
