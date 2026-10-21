import {Component, computed, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AdvanceRequestItem, AdvanceRound, roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';

/**
 * 共用「預支費用明細」唯讀表（含全部追加批次）。
 * 沖銷申請表單 / 沖銷詳情 / 簽核頁（write_off）共用，供對照預支內容。
 * 批次欄：同批次第二列起留白；合計由 items 加總。
 */
@Component({
  selector: 'app-advance-items-readonly',
  imports: [DatePipe, DecimalPipe],
  template: `
    <div class="card border-0 shadow-sm mb-6">
      <div class="card-header bg-transparent border-bottom flex items-center justify-between">
        <div class="flex items-center gap-2 fw-600">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#list"></use>
          </svg>
          {{ title() }}
        </div>
        @if (batchCount() > 1) {
          <span class="badge bg-blue-subtle text-blue-emphasis">含追加（共 {{ batchCount() }} 批）</span>
        }
      </div>
      <div class="card-body p-0">
        @if (items().length === 0) {
          <div class="text-muted small px-4 py-3">此預支單無費用明細。</div>
        } @else {
          <div class="table-responsive">
            <table class="table table-sm mb-0 min-w-[900px]">
              <thead class="table-light">
                <tr>
                  <th style="width:130px">批次</th>
                  <th style="min-width:90px">分類</th>
                  <th style="width:48px">項次</th>
                  <th style="min-width:130px">項目說明</th>
                  <th style="min-width:110px">單價</th>
                  <th style="min-width:70px">數量/單位</th>
                  <th class="text-right" style="min-width:110px">總價</th>
                  <th class="text-right" style="min-width:110px">現金(預支)</th>
                  <th class="text-right" style="min-width:110px">支票(月結)</th>
                  <th style="min-width:80px">備註</th>
                </tr>
              </thead>
              <tbody>
                @for (item of items(); track item.id; let i = $index) {
                  <tr>
                    <td class="small">
                      @if (isFirstOfRound(i)) {
                        <span class="fw-500">{{ roundLabel(item.roundNo) }}</span>
                        @if (roundDate(item.roundNo); as d) {
                          <div class="text-muted">{{ d | date:'yyyy-MM-dd' }}</div>
                        }
                      }
                    </td>
                    <td class="small">{{ item.category }}</td>
                    <td class="small">{{ item.seqNo }}</td>
                    <td class="small">{{ item.itemName }}</td>
                    <td class="small">{{ item.unitPrice | number:'1.0-0' }}</td>
                    <td class="small">{{ item.quantity }}</td>
                    <td class="text-right small fw-500">{{ item.totalPrice | number:'1.0-0' }}</td>
                    <td class="text-right small">{{ item.cashAmount | number:'1.0-0' }}</td>
                    <td class="text-right small">{{ item.checkAmount | number:'1.0-0' }}</td>
                    <td class="small text-muted">{{ item.note || '—' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot>
                <tr class="table-light">
                  <td colspan="6" class="text-right fw-500 small">合計</td>
                  <td class="text-right fw-600">{{ totals().total | number:'1.0-0' }}</td>
                  <td class="text-right fw-600">{{ totals().cash | number:'1.0-0' }}</td>
                  <td class="text-right fw-600">{{ totals().check | number:'1.0-0' }}</td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
        }
      </div>
    </div>
  `,
})
export class AdvanceItemsReadonlyComponent {
  items  = input<AdvanceRequestItem[]>([]);
  /** 各預支批次（取批次日期；未給則不顯示日期） */
  rounds = input<AdvanceRound[] | null | undefined>([]);
  title  = input('預支費用明細');

  readonly roundLabel = roundLabel;

  batchCount = computed(() => {
    const r = this.rounds()?.length ?? 0;
    return r || new Set(this.items().map(i => i.roundNo)).size;
  });

  totals = computed(() => this.items().reduce(
    (a, i) => ({total: a.total + i.totalPrice, cash: a.cash + i.cashAmount, check: a.check + i.checkAmount}),
    {total: 0, cash: 0, check: 0}));

  roundDate(roundNo: number): string | null {
    return this.rounds()?.find(r => r.roundNo === roundNo)?.advanceDate ?? null;
  }

  isFirstOfRound(index: number): boolean {
    const it = this.items();
    return index === 0 || it[index - 1].roundNo !== it[index].roundNo;
  }
}
