import {Component, computed, input} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AdvanceRound, roundLabel} from '../../features/admin/advance-requests/models/advance-request.model';
import {WriteOffRound} from '../../features/admin/approval-tasks/models/approval-task.model';

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
                  </td>
                  <td class="text-muted font-monospace">{{ wo.requestNo || '—' }}</td>
                  <td class="text-right fw-500"
                      [class.text-muted]="wo.approvalStatus !== 'approved' && !wo.isCurrent">
                    {{ wo.grandTotal | number:'1.0-0' }}
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
  `,
})
export class WriteOffSummaryComponent {
  advanceRounds     = input<AdvanceRound[] | undefined>([]);
  writeOffHistory   = input<WriteOffRound[] | undefined>([]);
  /** 本張沖銷單金額（history 內若已含本單則以 history 為準，此值供「本次沖銷」欄顯示）*/
  currentGrandTotal = input.required<number>();
  /** 本次應撥差額（後端 refundDue）*/
  refundDue         = input(0);

  readonly roundLabel = roundLabel;

  advanceTotal = computed(() =>
    (this.advanceRounds() ?? []).reduce((acc, r) => acc + r.grandTotal, 0));

  writtenOffTotal = computed(() =>
    (this.writeOffHistory() ?? []).reduce((acc, w) => acc + w.grandTotal, 0));

  balance = computed(() => this.advanceTotal() - this.writtenOffTotal());
}
