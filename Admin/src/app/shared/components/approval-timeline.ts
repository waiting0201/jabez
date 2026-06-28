import {Component, input} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ApprovalFlow, ApprovalRecord} from '../../features/admin/approval-tasks/models/approval-task.model';

@Component({
  selector: 'app-approval-timeline',
  standalone: true,
  imports: [DatePipe],
  template: `
    @if (flow()) {
      <div class="card border-0 shadow-sm mt-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#git-merge"></use></svg>
          簽核流程
        </div>
        <div class="card-body">
          <ol class="list-none p-0 mb-0">
            @for (step of flow()!.steps; track step.stepOrder; let last = $last) {
              <li class="flex items-start gap-3" [class.mb-6]="!last">
                <!-- 步驟圓圈 -->
                @if (getRecord(step.stepOrder); as rec) {
                  @if (rec.action === 'approved') {
                    <span class="badge bg-success rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">✓</span>
                  } @else {
                    <span class="badge bg-danger rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">✗</span>
                  }
                } @else if (currentStepOrder() === step.stepOrder && status() === 'pending') {
                  <span class="badge bg-primary rounded-circle flex items-center justify-center shrink-0"
                        style="width:28px;height:28px;min-width:28px;font-size:.75rem">{{ step.stepOrder }}</span>
                } @else {
                  <span class="badge bg-[--bg-base] text-[--text-muted] rounded-circle flex items-center justify-center shrink-0"
                        style="width:28px;height:28px;min-width:28px;font-size:.75rem">{{ step.stepOrder }}</span>
                }
                <!-- 步驟內容 -->
                <div class="grow">
                  <div class="fw-500">
                    @if (step.useApplicantDesignated) {
                      指定審核
                    } @else if (step.useDirectSupervisor) {
                      上層級
                    } @else {
                      {{ step.jobTitleName || '—' }}
                      @if (step.departmentName) {
                        <span class="text-muted font-normal">（{{ step.departmentName }}）</span>
                      }
                    }
                  </div>
                  @if (step.note) {
                    <div class="text-muted small">{{ step.note }}</div>
                  }
                  @if (getRecord(step.stepOrder); as rec) {
                    <div class="text-muted small mt-1">
                      {{ rec.reviewedBy }}
                      @if (rec.isEscalated && rec.onBehalfOf) {
                        <span class="badge bg-[--bg-elevated] text-[--accent] ms-1" style="font-size:.7rem">代理 {{ rec.onBehalfOf }}</span>
                      } @else if (rec.isEscalated) {
                        <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">升級審核</span>
                      }
                      · {{ rec.reviewedAt | date:'yyyy-MM-dd' }} ·
                      @if (rec.action === 'approved') {
                        <span class="text-success">已核准</span>
                      } @else if (rec.action === 'returned') {
                        <span class="text-[--yellow]">退回修改</span>
                      } @else {
                        <span class="text-danger">已拒絕</span>
                      }
                    </div>
                    @if (rec.reviewNote) {
                      <div class="text-muted small italic mt-1">「{{ rec.reviewNote }}」</div>
                    }
                  } @else if (currentStepOrder() === step.stepOrder && status() === 'pending') {
                    <div class="text-primary small mt-1">審核中…</div>
                  }
                </div>
              </li>
            }
          </ol>
        </div>
      </div>
    }
  `,
})
export class ApprovalTimeline {
  flow = input<ApprovalFlow | null>(null);
  approvalRecords = input<ApprovalRecord[]>([]);
  currentStepOrder = input(0);
  status = input('');

  getRecord(stepOrder: number): ApprovalRecord | undefined {
    return this.approvalRecords().find(r => r.stepOrder === stepOrder);
  }
}
