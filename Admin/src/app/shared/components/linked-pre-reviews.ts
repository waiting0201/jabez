import {ChangeDetectionStrategy, Component, inject, input, signal, viewChildren} from '@angular/core';
import {DatePipe} from '@angular/common';
import {Observable} from 'rxjs';
import {DraggableWindow} from './draggable-window';
import {PreReviewItemsReadonly} from './pre-review-items-readonly';
import {AttachmentsList} from './attachments-list';
import {PreReviewRequest} from '../../features/admin/pre-review-requests/models/pre-review-request.model';
import {AdvanceLinkedPreReview, roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';
import {ApprovalTaskService} from '../../features/admin/approval-tasks/services/approval-task.service';

/**
 * 預審單浮動視窗管理器：`open(id)` 取得預審內容後開出可拖拉視窗（可同時多開，
 * 重複點同一張只置頂既有視窗）。取得內容的方式由呼叫端以 `fetcher` 決定：
 * - 簽核頁 / 詳情頁：`/approval-tasks/advance/{advanceId}/pre-reviews/{id}`（審核者無預審權限）
 * - 預支表單（申請人本人）：`PreReviewRequestService.getById`
 */
@Component({
  selector: 'app-pre-review-windows',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DraggableWindow, PreReviewItemsReadonly, AttachmentsList],
  template: `
    @for (w of windows(); track w.id) {
      <app-draggable-window [key]="w.id" [title]="'預審單 ' + (w.data.requestNo || '（未取號）')" (closed)="close(w.id)">
        <div class="row g-2 mb-3 small">
          <div class="col-6">
            <div class="text-muted">預審單號</div>
            <div class="fw-500 font-monospace">{{ w.data.requestNo || '—' }}</div>
          </div>
          <div class="col-6">
            <div class="text-muted">送簽日</div>
            <div class="fw-500">{{ w.data.submittedAt ? (w.data.submittedAt | date:'yyyy-MM-dd') : '—' }}</div>
          </div>
          <div class="col-12">
            <div class="text-muted">專案</div>
            <div class="fw-500 font-monospace">{{ w.data.projectCode }}{{ w.data.projectName ? ' - ' + w.data.projectName : '' }}</div>
          </div>
          @if (w.data.reason) {
            <div class="col-12">
              <div class="text-muted">預審說明</div>
              <div class="border rounded p-2 bg-[--bg-base]">{{ w.data.reason }}</div>
            </div>
          }
        </div>
        <div class="border rounded mb-3">
          <app-pre-review-items-readonly [items]="w.data.items" [totalAmount]="w.data.totalAmount" [taxAmount]="w.data.taxAmount" />
        </div>
        @if (w.data.attachments?.length) {
          <div class="text-muted small mb-1">附件</div>
          <app-attachments-list [attachments]="w.data.attachments" />
        }
      </app-draggable-window>
    }
  `,
})
export class PreReviewWindows {
  /** 依預審單 id 取得完整內容 */
  fetcher = input.required<(preReviewId: number) => Observable<PreReviewRequest>>();

  windows = signal<{id: number; data: PreReviewRequest}[]>([]);
  private wins = viewChildren(DraggableWindow);
  private loading = new Set<number>();

  open(id: number) {
    const existing = this.wins().find(w => w.key() === id);
    if (existing) { existing.bringToFront(); return; }
    if (this.loading.has(id)) return;
    this.loading.add(id);
    this.fetcher()(id).subscribe({
      next: data => {
        this.loading.delete(id);
        this.windows.update(l => l.some(x => x.id === id) ? l : [...l, {id, data}]);
      },
      error: () => this.loading.delete(id),   // 錯誤訊息由全域 interceptor 提示
    });
  }

  close(id: number) {
    this.windows.update(l => l.filter(w => w.id !== id));
  }
}

/**
 * 預支單「關聯預審單」chip 列：`PRV-xxx｜專案代號｜含稅 $N｜第N次`。
 * 點 chip 開出可拖拉視窗對照預審品項 / 附件。無關聯時整塊不渲染。
 */
@Component({
  selector: 'app-linked-pre-reviews',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PreReviewWindows],
  template: `
    @if (preReviews()?.length) {
      <div class="text-muted small mb-2">{{ label() }}</div>
      <div class="flex flex-wrap gap-2">
        @for (p of preReviews(); track p.preReviewRequestId) {
          <button type="button" class="btn btn-sm btn-outline-secondary inline-flex items-center gap-1"
                  (click)="windows.open(p.preReviewRequestId)" [title]="p.reason || ''">
            <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#search"></use></svg>
            {{ chipText(p) }}
          </button>
        }
      </div>
      <app-pre-review-windows #windows [fetcher]="fetcher" />
    }
  `,
})
export class LinkedPreReviews {
  advanceId  = input.required<number>();
  preReviews = input<AdvanceLinkedPreReview[] | null | undefined>(null);
  label      = input('關聯預審單（點擊可開啟對照視窗）');

  private taskService = inject(ApprovalTaskService);

  readonly fetcher = (preReviewId: number) => this.taskService.getAdvancePreReview(this.advanceId(), preReviewId);

  chipText(p: AdvanceLinkedPreReview): string {
    const gross = (p.totalAmount ?? 0) + (p.taxAmount ?? 0);
    return `${p.requestNo || '（未取號）'}｜${p.projectCode || '—'}｜含稅 $${gross.toLocaleString('zh-TW')}｜${roundLabel(p.roundNo)}`;
  }
}
