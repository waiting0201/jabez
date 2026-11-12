import {Component, computed, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {
  SettlementEvent, SettlementLedger,
  SETTLEMENT_KIND_CLASSES, SETTLEMENT_KIND_LABELS, checkDifferenceText, offsetText,
} from '../models/settlement-ledger.model';

/**
 * 共用「預支金額歷程」卡片 — 預支 / 出差預支從預支、撥款、各次沖銷、繳回 / 補撥到結案實際金額的時間軸。
 * 數字全部來自後端 settlementLedger（單一真相），前端不重算。
 *
 * 上方三格摘要：預支金額 / 實際金額（未結案顯示已核准沖銷並標「核銷中」）/ 差額；
 * 差額（同仁應繳回 / 公司應補撥）為後端的保守互抵結果，月結差額另以灰字說明、不算未結清；
 * 現金與月結互抵時另以灰字一行說明；
 * 下方時間軸表格：日期 / 事項 / 單號 / 金額 / 累計預支 / 累計核銷 / 尚未結清（手機橫向捲動）。
 * ledger 為 null（例如假日執行活動）整張卡不渲染。
 */
@Component({
  selector: 'app-advance-settlement-ledger',
  imports: [DatePipe, DecimalPipe],
  template: `
    @if (ledger(); as l) {
      <div class="card border-0 shadow-sm {{ cardClass() }}">
        <div class="card-header bg-transparent border-bottom flex items-center gap-2 fw-600 flex-wrap">
          <svg class="sa-icon text-primary" style="stroke: currentColor">
            <use href="/assets/icons/sprite.svg#file-minus"></use>
          </svg>
          {{ title() }}
          @if (l.isClosed) {
            <span class="badge bg-elevated text-secondary">已結案</span>
          } @else {
            <span class="badge bg-warning-subtle text-warning-emphasis">核銷中</span>
          }
        </div>
        <div class="card-body">
          <div class="row g-3">
            <div class="col-12 col-md-4">
              <div class="text-muted small">預支金額</div>
              <div class="fw-600 text-lg">{{ l.advanceTotal | number:'1.0-0' }}</div>
            </div>
            <div class="col-12 col-md-4">
              <div class="text-muted small">
                實際金額
                @if (!l.isClosed) { <span class="badge bg-warning-subtle text-warning-emphasis ml-1">核銷中</span> }
              </div>
              <div class="fw-600 text-lg text-primary">{{ actual() | number:'1.0-0' }}</div>
              @if (!l.isClosed && l.pendingWriteOffTotal > 0) {
                <div class="text-muted small mt-1">另有 {{ l.pendingWriteOffTotal | number:'1.0-0' }} 元沖銷簽核中</div>
              }
            </div>
            <div class="col-12 col-md-4">
              <div class="text-muted small">差額</div>
              <div class="fw-600 text-lg" [class]="diffClass()">
                {{ diffAmount() | number:'1.0-0' }}
                <span class="badge ml-1" [class]="diffBadgeClass()">{{ diffLabel() }}</span>
              </div>
              @if (l.isClosed && l.outstanding !== 0) {
                <div class="mt-1">
                  <span class="badge bg-warning-subtle text-warning-emphasis">結案時差額未處理</span>
                </div>
              }
              @if (checkNote(); as n) {
                <div class="text-muted small mt-1">{{ n }}</div>
              }
              @if (offsetNote(); as n) {
                <div class="text-muted small mt-1">{{ n }}</div>
              }
            </div>
          </div>
        </div>

        @if (l.events.length > 0) {
          <div class="table-responsive border-top">
            <table class="table table-sm mb-0" style="min-width: 820px">
              <thead class="table-light">
                <tr>
                  <th style="width: 110px">日期</th>
                  <th style="min-width: 150px">事項</th>
                  <th style="min-width: 140px">單號</th>
                  <th class="text-right" style="min-width: 100px">金額</th>
                  <th class="text-right" style="min-width: 100px">累計預支</th>
                  <th class="text-right" style="min-width: 100px">累計核銷</th>
                  <th class="text-right" style="min-width: 110px">尚未結清</th>
                </tr>
              </thead>
              <tbody>
                @for (e of l.events; track $index) {
                  <tr [class.text-muted]="e.kind === 'advance_paid'">
                    <td class="small">{{ e.date ? (e.date | date:'yyyy-MM-dd') : '—' }}</td>
                    <td class="small">
                      <span class="badge" [class]="kindClass(e)">{{ kindLabel(e) }}</span>
                      <span class="ml-1">{{ e.label }}</span>
                      @if (e.note) { <div class="text-muted text-xs">{{ e.note }}</div> }
                    </td>
                    <td class="small font-monospace">{{ e.requestNo || '—' }}</td>
                    <td class="text-right small fw-500">{{ e.amount != null ? (e.amount | number:'1.0-0') : '—' }}</td>
                    <td class="text-right small">{{ e.runningAdvance | number:'1.0-0' }}</td>
                    <td class="text-right small">{{ e.runningWrittenOff | number:'1.0-0' }}</td>
                    <td class="text-right small fw-500"
                        [class.text-warning]="e.runningOutstanding !== 0"
                        [class.text-green]="e.runningOutstanding === 0">
                      {{ e.runningOutstanding | number:'1.0-0' }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }
  `,
})
export class AdvanceSettlementLedgerComponent {
  ledger    = input<SettlementLedger | null | undefined>(null);
  title     = input('金額歷程');
  /** detail 頁卡片自帶 mb-6，簽核頁改用 mt-6 */
  cardClass = input('mb-6');

  /** 實際金額：結案取 actualAmount，未結案顯示已核准沖銷 */
  actual = computed(() => {
    const l = this.ledger();
    if (!l) return 0;
    return l.isClosed && l.actualAmount != null ? l.actualAmount : l.approvedWriteOffTotal;
  });

  diffAmount = computed(() => Math.abs(this.ledger()?.outstanding ?? 0));

  diffLabel = computed(() => {
    const o = this.ledger()?.outstanding ?? 0;
    return o > 0 ? '同仁應繳回' : o < 0 ? '公司應補撥' : '已結清';
  });
  diffClass = computed(() => {
    const o = this.ledger()?.outstanding ?? 0;
    return o === 0 ? 'text-green' : 'text-warning';
  });
  diffBadgeClass = computed(() => {
    const o = this.ledger()?.outstanding ?? 0;
    return o === 0 ? 'bg-success-subtle text-success' : 'bg-warning-subtle text-warning-emphasis';
  });

  /** 月結差額說明（月結實支較預支少 / 多 N 元）；無差額為 null */
  checkNote = computed(() => checkDifferenceText(this.ledger()?.checkDifference, this.ledger()?.isClosed ?? false));

  /** 現金與月結互抵的提醒（後端 cashDifference ≠ outstanding）；出差預支無此欄位，不顯示 */
  offsetNote = computed(() => {
    const l = this.ledger();
    return l && l.cashDifference != null ? offsetText(l.cashDifference, l.outstanding) : null;
  });

  kindLabel(e: SettlementEvent) { return SETTLEMENT_KIND_LABELS[e.kind] ?? e.kind; }
  kindClass(e: SettlementEvent) { return SETTLEMENT_KIND_CLASSES[e.kind] ?? 'bg-secondary'; }
}
