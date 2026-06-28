import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {BehaviorSubject, switchMap} from 'rxjs';
import {ToastrService} from 'ngx-toastr';
import {VendorService} from '../../services/vendor.service';
import {Vendor} from '../../models/vendor.model';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

@Component({
  selector: 'app-vendor-list',
  templateUrl: './vendor-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe, HasPermissionDirective],
})
export class VendorList {
  private vendorService = inject(VendorService);
  private toastr = inject(ToastrService);
  private refresh$ = new BehaviorSubject<void>(undefined);
  vendors$ = this.refresh$.pipe(switchMap(() => this.vendorService.getAll()));

  delete(v: Vendor) {
    if (!confirm(`確定要刪除廠商「${v.name}」嗎？`)) return;
    this.vendorService.delete(v.id).subscribe({
      next: () => {
        this.toastr.success(`已刪除廠商「${v.name}」。`);
        this.refresh$.next();
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || '刪除失敗，請稍後再試。';
        this.toastr.error(msg, '無法刪除');
      },
    });
  }
}
