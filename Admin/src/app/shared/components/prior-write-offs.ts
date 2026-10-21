import {Component, computed, input, signal} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {priorWriteOffRounds, WriteOffRound, writeOffRoundStatus} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 共用「前次沖銷紀錄」卡 — 列出同預支單底下非本單的沖銷（第 N 次｜單號｜狀態｜金額｜繳回），
 * 點列展開該張沖銷的明細。無前次沖銷時整張卡不顯示。
 * 狀態 badge 與 write-off-summary 共用 writeOffRoundStatus()。
 */
@Component({
  selector: 'app-prior-write-offs',
  imports: [DatePipe, DecimalPipe],
  template: `
    @if (rows().length > 0) {
      <div class="card border-0 shadow-sm mb-6">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#list"></use>
          </svg>
          前次沖銷紀錄
        </div>
        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-sm table-hover mb-0 small min-w-[560px]">
              <thead class="table-light">
                <tr>
                  <th style="width:32px"></th>
                  <th>次別</th>
                  <th>單號</th>
                  <th>狀態</th>
                  <th class="text-right">沖銷金額</th>
                  <th class="text-right">繳回</th>
                </tr>
              </thead>
              <tbody>
                @for (wo of rows(); track wo.id) {
                  <tr class="cursor-pointer" (click)="toggle(wo.id)">
                    <td class="text-muted">{{ isOpen(wo.id) ? '▾' : '▸' }}</td>
                    <td class="fw-500">第 {{ wo.writeOffNo }} 次沖銷</td>
                    <td class="text-muted font-monospace">{{ wo.requestNo || '—' }}</td>
                    <td><span class="badge" [class]="status(wo).cls">{{ status(wo).label }}</span></td>
                    <td class="text-right fw-500">{{ wo.grandTotal | number:'1.0-0' }}</td>
                    <td class="text-right text-muted">
                      @if (wo.returnedAmount) {
                        {{ wo.returnedAmount | number:'1.0-0' }}
                        @if (wo.returnedAt) { <div>{{ wo.returnedAt | date:'yyyy-MM-dd' }}</div> }
                      } @else { — }
                    </td>
                  </tr>
                  @if (isOpen(wo.id)) {
                    <tr>
                      <td></td>
                      <td colspan="5" class="p-0">
                        @if ((wo.items?.length ?? 0) === 0) {
                          <div class="text-muted px-3 py-2">此次沖銷無費用明細。</div>
                        } @else {
                          <div class="overflow-x-auto">
                            <table class="table table-sm mb-0 small min-w-[640px]">
                              <thead class="table-light">
                                <tr>
                                  <th>發票號碼</th><th>分類</th><th>項目說明</th>
                                  <th class="text-right">總價</th>
                                  <th class="text-right">現金</th>
                                  <th class="text-right">支票</th>
                                </tr>
                              </thead>
                              <tbody>
                                @for (it of wo.items!; track it.id) {
                                  <tr>
                                    <td class="font-monospace">{{ it.invoiceNo || '—' }}</td>
                                    <td>{{ it.category }}</td>
                                    <td>{{ it.itemName }}</td>
                                    <td class="text-right">{{ it.totalPrice | number:'1.0-0' }}</td>
                                    <td class="text-right">{{ it.cashAmount | number:'1.0-0' }}</td>
                                    <td class="text-right">{{ it.checkAmount | number:'1.0-0' }}</td>
                                  </tr>
                                }
                              </tbody>
                            </table>
                          </div>
                        }
                      </td>
                    </tr>
                  }
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `,
})
export class PriorWriteOffsComponent {
  /** 同預支單全部沖銷；只列排在本單之前者（priorWriteOffRounds） */
  history = input<WriteOffRound[] | null | undefined>([]);

  rows = computed(() => priorWriteOffRounds(this.history()));

  private open = signal<ReadonlySet<number>>(new Set());
  isOpen(id: number): boolean { return this.open().has(id); }
  toggle(id: number) {
    const next = new Set(this.open());
    if (!next.delete(id)) next.add(id);
    this.open.set(next);
  }

  status = writeOffRoundStatus;
}
