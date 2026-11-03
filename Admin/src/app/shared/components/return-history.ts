import {Component, computed, input} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ApprovalRecord, ApprovalReturnLog} from '../../features/admin/approval-tasks/models/approval-task.model';
import {roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';

/**
 * 單一關卡的歷次退回 / 拒絕紀錄（放在簽核流程時間軸每一關的內容下方）。
 * 退回後重送時簽核紀錄會整批重來，時間軸只看得到這一輪；這裡把該關過去「誰、何時、為何」退回列出來，
 * 讓已簽過的人再收到同一張單時知道前因。該關無歷程時不渲染。
 *
 * 去重：本輪該關若正是「退回 / 拒絕」（單子停在 returned / rejected），時間軸上方已列出這一筆，
 * 歷程裡最新那筆就是它，故略過。
 */
@Component({
  selector: 'app-step-return-history',
  standalone: true,
  imports: [DatePipe],
  template: `
    @if (items().length) {
      <div class="mt-2 ps-3" style="border-left: 2px solid var(--yellow)">
        <div class="text-muted small mb-1">歷次退回</div>
        @for (log of items(); track $index) {
          <div class="small" [class.mt-2]="!$first">
            <span [class]="log.action === 'returned' ? 'text-[--yellow]' : 'text-danger'">
              {{ log.action === 'returned' ? '退回修改' : '已拒絕' }}
            </span>
            · {{ log.reviewedBy || '—' }}
            @if (log.reviewerDepartmentName || log.reviewerJobTitle) {
              <span class="text-muted">（{{ log.reviewerDepartmentName }}{{ log.reviewerDepartmentName && log.reviewerJobTitle ? ' · ' : '' }}{{ log.reviewerJobTitle }}）</span>
            }
            @if (log.isEscalated && log.onBehalfOf) {
              <span class="badge bg-[--bg-elevated] text-[--accent] ms-1" style="font-size:.7rem">代理 {{ log.onBehalfOf }}</span>
            } @else if (log.isEscalated) {
              <span class="badge bg-[--bg-elevated] text-[--purple] ms-1" style="font-size:.7rem">升級審核</span>
            }
            <span class="text-muted">
              · {{ log.reviewedAt | date:'yyyy-MM-dd HH:mm' }}
              @if (showRound() && (log.roundNo ?? 1) > 1) { · {{ roundLabel(log.roundNo) }} }
            </span>
            @if (log.reviewNote) {
              <div class="break-words">「{{ log.reviewNote }}」</div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class StepReturnHistory {
  history = input<ApprovalReturnLog[] | null | undefined>([]);
  stepOrder = input.required<number>();
  /** 只看某批次（追加預支）；null＝不分批次 */
  roundNo = input<number | null>(null);
  /** 本輪該關的簽核紀錄（用於去重，見類別註解） */
  currentRecord = input<ApprovalRecord | null | undefined>(null);
  /** 不分批次顯示時，追加批次的紀錄加註批次標籤 */
  readonly showRound = computed(() => this.roundNo() === null);

  protected readonly roundLabel = roundLabel;

  readonly items = computed(() => {
    const round = this.roundNo();
    const list = (this.history() ?? []).filter(l =>
      l.stepOrder === this.stepOrder() && (round === null || (l.roundNo ?? 1) === round));
    const rec = this.currentRecord();
    return rec && rec.action !== 'approved' ? list.slice(0, -1) : list;
  });
}
