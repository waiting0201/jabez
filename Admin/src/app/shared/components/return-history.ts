import {Component, computed, input} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ApprovalFlow, ApprovalReturnLog} from '../../features/admin/approval-tasks/models/approval-task.model';
import {roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';

/**
 * 退回 / 拒絕歷程（放在「簽核流程」卡片內、時間軸下方）。
 * 退回後重送時簽核紀錄會整批重來，時間軸只看得到這一輪；本區塊列出歷次「誰、在哪一關、為何」退回或拒絕，
 * 讓已簽過的人再收到同一張單時知道前因。無歷程時整塊不渲染。
 */
@Component({
  selector: 'app-return-history',
  standalone: true,
  imports: [DatePipe],
  template: `
    @if (items().length) {
      <div class="border-top mt-6 pt-4">
        <div class="flex items-center gap-2 fw-600 mb-3">
          <svg class="sa-icon text-[--yellow]" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#rotate-ccw"></use></svg>
          退回 / 拒絕歷程
          <span class="badge bg-[--bg-elevated] text-[--text-muted]" style="font-size:.7rem">{{ items().length }} 次</span>
        </div>
        <ol class="list-none p-0 mb-0">
          @for (log of items(); track $index; let last = $last) {
            <li class="flex items-start gap-3" [class.mb-4]="!last">
              <span class="badge rounded-pill shrink-0 mt-px"
                    [class.bg-warning]="log.action === 'returned'"
                    [class.bg-danger]="log.action === 'rejected'"
                    style="font-size:.7rem">
                {{ log.action === 'returned' ? '退回修改' : '已拒絕' }}
              </span>
              <div class="grow min-w-0">
                <div class="small">
                  <span class="fw-500">{{ log.reviewedBy || '—' }}</span>
                  @if (log.reviewerDepartmentName || log.reviewerJobTitle) {
                    <span class="text-muted">（{{ log.reviewerDepartmentName }}{{ log.reviewerDepartmentName && log.reviewerJobTitle ? ' · ' : '' }}{{ log.reviewerJobTitle }}）</span>
                  }
                  @if (log.isEscalated && log.onBehalfOf) {
                    <span class="badge bg-[--bg-elevated] text-[--accent] ms-1" style="font-size:.7rem">代理 {{ log.onBehalfOf }}</span>
                  } @else if (log.isEscalated) {
                    <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">升級審核</span>
                  }
                </div>
                <div class="text-muted small mt-1">
                  {{ stepLabel(log) }} · {{ log.reviewedAt | date:'yyyy-MM-dd HH:mm' }}
                </div>
                @if (log.reviewNote) {
                  <div class="small mt-1 break-words">「{{ log.reviewNote }}」</div>
                }
              </div>
            </li>
          }
        </ol>
      </div>
    }
  `,
})
export class ReturnHistory {
  history = input<ApprovalReturnLog[] | null | undefined>([]);
  flow = input<ApprovalFlow | null | undefined>(null);

  readonly items = computed(() => this.history() ?? []);

  /** 「第 2 關 · 總監」；追加預支另冠批次標籤 */
  stepLabel(log: ApprovalReturnLog): string {
    const step = this.flow()?.steps.find(s => s.stepOrder === log.stepOrder);
    const name = !step ? ''
      : step.useApplicantDesignated ? '指定審核'
      : step.useDirectSupervisor ? '上層級'
      : (step.jobTitleName ?? '');
    const base = `第 ${log.stepOrder} 關${name ? ' · ' + name : ''}`;
    return (log.roundNo ?? 1) > 1 ? `${roundLabel(log.roundNo)} ${base}` : base;
  }
}
