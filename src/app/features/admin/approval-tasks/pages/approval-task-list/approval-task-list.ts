import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {ApprovalTaskService} from '../../services/approval-task.service';
import {
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
  ApprovalTask,
} from '../../models/approval-task.model';

@Component({
  selector: 'app-approval-task-list',
  templateUrl: './approval-task-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe],
})
export class ApprovalTaskList {
  private service = inject(ApprovalTaskService);
  tasks$ = this.service.getAll();

  readonly statusLabel    = TASK_STATUS_LABELS;
  readonly statusClass    = TASK_STATUS_CLASSES;
  readonly appTypeLabel   = APPLICATION_TYPE_LABELS;
  readonly appTypeClass   = APPLICATION_TYPE_CLASSES;
  readonly payTypeLabel   = PAYMENT_TYPE_LABELS;
  readonly leaveTypeLabel = LEAVE_TYPE_LABELS;

  getSummary(t: ApprovalTask): string {
    if (t.paymentDetail) {
      return `${this.payTypeLabel[t.paymentDetail.paymentType]}・${t.paymentDetail.projectCode}（${t.paymentDetail.totalAmount.toLocaleString()} 元）`;
    }
    if (t.leaveDetail) {
      return `${this.leaveTypeLabel[t.leaveDetail.leaveType]}・${t.leaveDetail.days} 天`;
    }
    if (t.travelDetail) {
      return `${t.travelDetail.destination}（${t.travelDetail.estimatedCost.toLocaleString()} 元）`;
    }
    return '—';
  }
}
