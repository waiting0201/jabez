import {Component, computed, input} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ApprovalFlow, ApprovalRecord, PendingReviewer} from '../../features/admin/approval-tasks/models/approval-task.model';
import {roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';

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
          @for (round of rounds(); track round) {
          <!-- 追加預支才會有多批次；單批次時不顯示批次標題，維持原有版面 -->
          @if (rounds().length > 1) {
            <div class="text-muted small fw-600 mb-3" [class.mt-6]="!$first">
              {{ roundLabel(round) }}簽核
            </div>
          }
          <ol class="list-none p-0 mb-0">
            @for (step of flow()!.steps; track step.stepOrder; let last = $last) {
              <li class="flex items-start gap-3" [class.mb-6]="!last">
                <!-- 步驟圓圈 -->
                @if (getRecord(step.stepOrder, round); as rec) {
                  @if (rec.action === 'approved') {
                    <span class="badge bg-success rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">✓</span>
                  } @else {
                    <span class="badge bg-danger rounded-circle flex items-center justify-center shrink-0"
                          style="width:28px;height:28px;min-width:28px;font-size:.85rem">✗</span>
                  }
                } @else if (isActiveStep(step.stepOrder, round)) {
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
                  @if (getRecord(step.stepOrder, round); as rec) {
                    <div class="text-muted small mt-1">
                      {{ rec.reviewedBy }}
                      @if (step.useApplicantDesignated && (rec.reviewerDepartmentName || rec.reviewerJobTitle)) {
                        <span class="text-muted">（{{ rec.reviewerDepartmentName }}{{ rec.reviewerDepartmentName && rec.reviewerJobTitle ? ' · ' : '' }}{{ rec.reviewerJobTitle }}）</span>
                      }
                      @if (rec.isEscalated && rec.onBehalfOf) {
                        <span class="badge bg-[--bg-elevated] text-[--accent] ms-1" style="font-size:.7rem">代理 {{ rec.onBehalfOf }}</span>
                      } @else if (rec.isEscalated) {
                        <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">升級審核</span>
                      }
                      · {{ rec.reviewedAt | date:'yyyy-MM-dd HH:mm:ss' }} ·
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
                  } @else if (isActiveStep(step.stepOrder, round)) {
                    <div class="text-primary small mt-1">審核中…</div>
                    <!-- 上層級 / 指定審核在簽核前沒有人名，這裡把後端解析出的實際可簽核者列出來 -->
                    @if (pendingReviewers().length) {
                      <div class="text-muted small mt-1">
                        待簽核：
                        @for (r of pendingReviewers(); track r.id; let lastReviewer = $last) {
                          {{ r.name }}
                          @if (r.departmentName || r.jobTitleName) {
                            <span class="text-muted">（{{ r.departmentName }}{{ r.departmentName && r.jobTitleName ? ' · ' : '' }}{{ r.jobTitleName }}）</span>
                          }
                          @if (r.isEscalated) {
                            <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">升級審核</span>
                          }
                          @if (!lastReviewer) { <span>、</span> }
                        }
                      </div>
                    } @else {
                      <div class="text-danger small mt-1">查無可簽核人員，請聯絡管理員調整簽核流程或人員職稱</div>
                    }
                  } @else if (isSkippedStep(step.stepOrder, round)) {
                    <div class="text-muted small mt-1">已跳過</div>
                  }
                </div>
              </li>
            }
          </ol>
          }
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
  /** 目前進行中的簽核批次（僅預支追加會 > 1；其餘申請維持 1）*/
  currentRoundNo = input(1);
  /** 目前關卡實際可簽核的人（後端解析，空陣列＝查無可簽核人員）*/
  pendingReviewers = input<PendingReviewer[]>([]);

  protected readonly roundLabel = roundLabel;

  /** 有紀錄的批次 ∪ 目前批次，由舊到新 */
  readonly rounds = computed(() => {
    const set = new Set(this.approvalRecords().map(r => r.roundNo ?? 1));
    set.add(this.currentRoundNo());
    return [...set].sort((a, b) => a - b);
  });

  getRecord(stepOrder: number, roundNo: number): ApprovalRecord | undefined {
    return this.approvalRecords().find(r => r.stepOrder === stepOrder && (r.roundNo ?? 1) === roundNo);
  }

  /** 藍色「審核中」只標在目前批次 */
  isActiveStep(stepOrder: number, roundNo: number): boolean {
    return roundNo === this.currentRoundNo()
        && this.currentStepOrder() === stepOrder
        && this.status() === 'pending';
  }

  /**
   * 已跳過的關卡：目前批次中「序號在目前關卡之前、卻沒有任何簽核紀錄」者。
   * 送單時被跳過的步驟不會留下 ApprovalRecord，與「還沒輪到」的灰圈長得一模一樣，故明確標示。
   */
  isSkippedStep(stepOrder: number, roundNo: number): boolean {
    return roundNo === this.currentRoundNo()
        && stepOrder < this.currentStepOrder()
        && !this.getRecord(stepOrder, roundNo);
  }
}
