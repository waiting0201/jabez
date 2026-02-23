import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {TravelRequestService} from '../../services/travel-request.service';
import {
  TravelRequest,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/travel-request.model';

@Component({
  selector: 'app-travel-request-list',
  templateUrl: './travel-request-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe, DecimalPipe],
})
export class TravelRequestList {
  private service = inject(TravelRequestService);
  requests$ = this.service.getAll();

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  delete(r: TravelRequest) {
    if (confirm(`確定要刪除此出差申請嗎？`)) {
      this.service.delete(r.id).subscribe();
    }
  }
}
