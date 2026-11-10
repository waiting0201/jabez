import {Component, computed, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AdvanceRound, roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';
import {SettlementLedger} from '../models/settlement-ledger.model';
import {WriteOffRound, writeOffRoundStatus} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 共用「沖銷金額摘要」— 預支各批次金額 + 各次沖銷金額 + 待沖銷餘額 / 應撥差額。
 * 沖銷詳情頁與簽核頁共用同一份呈現，避免兩份複製。
 *
 * 上排＝預支批次（AdvanceRequestItem.RoundNo，第 1 次原始 + 第 N 次追加）
 * 下排＝沖銷單次（WriteOffRecord.WriteOffNo，第 1/2/3… 次沖銷）
 */
@Component({
  selector: 'app-write-off-summary',
  imports: [DatePipe, DecimalPipe],
  template: `
    <div class="row g-4">
      <!-- 預支批次 -->
      <div class="col-12 col-lg-6">
        <div class="text-muted small mb-2 fw-500">預支批次</div>
        <div class="border rounded overflow-x-auto">
          <table class="table table-sm mb-0 small">
            <tbody>
              @for (rd of advanceRounds(); track rd.roundNo) {
                <tr>
                  <td class="fw-500" style="width:110px">{{ roundLabel(rd.roundNo) }}預支</td>
                  <td class="text-muted">{{ rd.advanceDate | date:'yyyy-MM-dd' }}</td>
                  <td class="text-right fw-500">{{ rd.grandTotal | number:'1.0-0' }}</td>
                </tr>
              } @empty {
                <tr><td class="text-muted" colspan="3">—</td></tr>
              }
            </tbody>
            <tfoot class="bg-[--bg-base]">
              <tr>
                <td class="fw-600" colspan="2">預支加總</td>
                <td class="text-right fw-600">{{ advanceTotal() | number:'1.0-0' }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <!-- 各次沖銷 -->
      <div class="col-12 col-lg-6">
        <div class="text-muted small mb-2 fw-500">已沖銷</div>
        <div class="border rounded overflow-x-auto">
          <table class="table table-sm mb-0 small">
            <tbody>
              @for (wo of writeOffHistory(); track wo.id) {
                <tr [class.bg-[--bg-base]]="wo.isCurrent">
                  <td class="fw-500" style="width:110px">
                    第 {{ wo.writeOffNo }} 次沖銷
                    @if (wo.isCurrent) { <span class="badge bg-primary ml-1">本單</span> }
                    <div><span class="badge" [class]="status(wo).cls">{{ status(wo).label }}</span></div>
                  </td>
                  <td class="text-muted font-monospace">{{ wo.requestNo || '—' }}</td>
                  <td class="text-right fw-500"
                      [class.text-muted]="wo.approvalStatus !== 'approved' && !wo.isCurrent">
                    {{ wo.grandTotal | number:'1.0-0' }}
                    @if (wo.returnedAmount) {
                      <div class="text-muted small fw-normal">
                        繳回 {{ wo.returnedAmount | number:'1.0-0' }}
                        @if (wo.returnedAt) { （{{ wo.returnedAt | date:'yyyy-MM-dd' }}） }
                      </div>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td class="text-muted" colspan="3">—</td></tr>
              }
            </tbody>
            <tfoot class="bg-[--bg-base]">
              <tr>
                <td class="fw-600" colspan="2">已沖銷加總</td>
                <td class="text-right fw-600">{{ writtenOffTotal() | number:'1.0-0' }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </div>

    <!-- 餘額 / 應撥差額 -->
    <div class="row g-3 mt-2">
      <div class="col-6 col-md-3">
        <div class="text-muted small">本次沖銷</div>
        <div class="fw-600 text-lg text-primary">{{ currentGrandTotal() | number:'1.0-0' }}</div>
      </div>
      <div class="col-6 col-md-3">
        <div class="text-muted small">待沖銷餘額</div>
        <div class="fw-600 text-lg" [class]="balance() >= 0 ? 'text-green' : 'text-red'">
          {{ balance() | number:'1.0-0' }}
        </div>
      </div>
      @if (refundDue() > 0) {
        <div class="col-6 col-md-3">
          <div class="text-muted small">本次應撥差額</div>
          <div class="fw-600 text-lg text-red">{{ refundDue() | number:'1.0-0' }}</div>
          <div class="text-muted small mt-1">沖銷超出預支，公司須補撥給員工</div>
        </div>
      }
    </div>

    <!-- 結清對帳：預支 − 費用核銷 − 同仁匯回 = 尚未結清；
         原始預支 / 實際費用核銷 / 尚未結清各再拆「現金 / 月結」——
         現金＝申請人經手（應繳回或補撥的對象），月結＝公司直接支付廠商，不屬申請人應繳 / 補撥 -->
    <div class="border rounded mt-4 overflow-x-auto">
      <table class="table table-sm mb-0 small">
        <tbody>
          <tr>
            <td class="fw-500">原始預支金額</td>
            <td class="text-right">{{ advanceTotal() | number:'1.0-0' }}</td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">現金</td>
            <td class="text-right">{{ advanceCash() | number:'1.0-0' }}</td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">月結</td>
            <td class="text-right">{{ advanceCheck() | number:'1.0-0' }}</td>
          </tr>
          <tr>
            <td class="fw-500">實際費用核銷</td>
            <td class="text-right">{{ writtenOffTotal() | number:'1.0-0' }}</td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">現金</td>
            <td class="text-right">{{ writtenOffCash() | number:'1.0-0' }}</td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">月結</td>
            <td class="text-right">{{ writtenOffCheck() | number:'1.0-0' }}</td>
          </tr>
          <tr>
            <td class="fw-500">現金・匯款繳回</td>
            <td class="text-right">{{ returnedTotal() | number:'1.0-0' }}</td>
          </tr>
        </tbody>
        <tfoot class="bg-[--bg-base]">
          <tr>
            <td class="fw-600">尚未結清金額</td>
            <td class="text-right fw-600"
                [class.text-warning]="unsettled() !== 0 && !overspendSettled()"
                [class.text-green]="unsettled() === 0 || overspendSettled()">
              {{ unsettled() | number:'1.0-0' }}
              @if (overspendSettled()) {
                <span class="badge bg-success-subtle text-success ml-1">
                  已結清@if (overspendLastPaidAt()) {（{{ overspendLastPaidAt() | date:'yyyy-MM-dd' }} 已撥匯）}
                </span>
              } @else if (unsettled() !== 0) {
                <span class="badge bg-warning-subtle text-warning-emphasis ml-1">未結清</span>
              }
            </td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">現金 <span class="text-xs">（申請人應繳回 ／ 應補撥申請人）</span></td>
            <td class="text-right">{{ unsettledCash() | number:'1.0-0' }}</td>
          </tr>
          <tr class="text-muted">
            <td class="ps-6">月結 <span class="text-xs">（公司月結支付，不屬申請人應繳 ／ 補撥）</span></td>
            <td class="text-right">{{ unsettledCheck() | number:'1.0-0' }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  `,
})
export class WriteOffSummaryComponent {
  advanceRounds     = input<AdvanceRound[] | undefined>([]);
  writeOffHistory   = input<WriteOffRound[] | undefined>([]);
  /** 本張沖銷單金額（history 內若已含本單則以 history 為準，此值供「本次沖銷」欄顯示）*/
  currentGrandTotal = input.required<number>();
  /** 本次應撥差額（後端 refundDue）*/
  refundDue         = input(0);

  /** 同預支單「其他」已核准沖銷單的繳回加總（不含本單，後端 advanceReturnedTotal） */
  advanceReturnedTotal = input(0);
  /** 本單的繳回金額（編輯中 / 唯讀皆帶入；無則 0） */
  currentReturnedAmount = input<number | null | undefined>(0);
  /** 公司已補撥的超支差額合計（後端 overspendPaidTotal：同預支單已核准 ∪ 本單的已撥款差額分期）*/
  overspendPaidTotal = input<number | null | undefined>(0);
  /** 上述差額分期的最後撥款日（「已結清（日期 已撥匯）」顯示用）*/
  overspendLastPaidAt = input<string | null | undefined>(null);

  /**
   * 後端金額歷程（單一真相）。「尚未結清」在歷程與本表皆已核准時改取 ledger.outstanding，
   * 與歷程卡同一數字；本單或同預支單其他沖銷仍在簽核 / 草稿時，歷程不計入、
   * 本表仍以「含簽核中」的試算呈現（審核者要看本單核准後的結果）。
   */
  ledger = input<SettlementLedger | null | undefined>(null);

  readonly roundLabel = roundLabel;
  readonly status = writeOffRoundStatus;

  advanceTotal = computed(() =>
    (this.advanceRounds() ?? []).reduce((acc, r) => acc + r.grandTotal, 0));

  writtenOffTotal = computed(() =>
    (this.writeOffHistory() ?? []).reduce((acc, w) => acc + w.grandTotal, 0));

  balance = computed(() => this.advanceTotal() - this.writtenOffTotal());

  // 現金 / 月結分項：預支批次的 cashTotal / checkTotal、沖銷各次的 cashTotal / checkTotal（後端以明細加總）
  advanceCash  = computed(() => (this.advanceRounds() ?? []).reduce((acc, r) => acc + (r.cashTotal ?? 0), 0));
  advanceCheck = computed(() => (this.advanceRounds() ?? []).reduce((acc, r) => acc + (r.checkTotal ?? 0), 0));
  writtenOffCash  = computed(() => (this.writeOffHistory() ?? []).reduce((acc, w) => acc + (w.cashTotal ?? 0), 0));
  writtenOffCheck = computed(() => (this.writeOffHistory() ?? []).reduce((acc, w) => acc + (w.checkTotal ?? 0), 0));
  /** 現金尚未結清 = 現金預支 − 現金核銷 − 同仁繳回（繳回一律屬現金側）；正＝申請人應繳回、負＝公司應補撥 */
  unsettledCash  = computed(() => this.advanceCash() - this.writtenOffCash() - this.returnedTotal());
  /** 月結尚未結清 = 月結預支 − 月結核銷（公司與廠商之間，與申請人無關） */
  unsettledCheck = computed(() => this.advanceCheck() - this.writtenOffCheck());

  returnedTotal = computed(() => this.advanceReturnedTotal() + (this.currentReturnedAmount() ?? 0));

  /** 尚未結清 = 預支 − 費用核銷 − 繳回；≠ 0 以警示色標示（只警示不擋結案） */
  unsettled = computed(() => {
    const l = this.ledger();
    const history = this.writeOffHistory() ?? [];
    if (l && history.every(w => w.approvalStatus === 'approved')) return l.outstanding;
    return this.advanceTotal() - this.writtenOffTotal() - this.returnedTotal();
  });

  /** 沖銷大於預支（尚未結清為負）且公司已補撥足額 → 視為已結清 */
  overspendSettled = computed(() =>
    this.unsettled() < 0 && (this.overspendPaidTotal() ?? 0) >= -this.unsettled());
}
