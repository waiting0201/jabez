import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {LeaveRequestService} from '../../services/leave-request.service';
import {
  LeaveRequest,
  LEAVE_TYPE_LABELS, LEAVE_TYPE_CLASSES,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/leave-request.model';

@Component({
  selector: 'app-leave-request-list',
  templateUrl: './leave-request-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe, DecimalPipe],
})
export class LeaveRequestList {
  private service = inject(LeaveRequestService);
  requests$ = this.service.getAll();

  readonly typeLabel   = LEAVE_TYPE_LABELS;
  readonly typeClass   = LEAVE_TYPE_CLASSES;
  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  delete(r: LeaveRequest) {
    if (confirm(`確定要刪除此請假申請嗎？`)) {
      this.service.delete(r.id).subscribe();
    }
  }
}
