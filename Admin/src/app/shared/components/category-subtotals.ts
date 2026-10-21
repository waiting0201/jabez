import {Component, computed, input} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {priorWriteOffRounds, WriteOffRound} from '../../features/admin/approval-tasks/models/approval-task.model';
import {CategorizedAmount, sumByCategory, UNCATEGORIZED_LABEL} from '../utils/category-subtotals';

interface SubtotalRow {
  category: string;
  advance: number;
  prior: number;
  current: number;
  diff: number;
}

/**
 * 共用「分類小計」— 只給一份明細時為單欄小計；同時給預支與沖銷時為「預支 vs 沖銷」分類對照表。
 * 差額 = 預支 − 前次沖銷 − 本次沖銷（>0 綠、<0 紅）。
 * 注意：傳入的陣列請保持穩定參照（signal / 輸入資料），勿在模板內即時 map 新陣列。
 */
@Component({
  selector: 'app-category-subtotals',
  imports: [DecimalPipe],
  template: `
    @if (rows().length > 0) {
      <div class="mt-3">
        @if (title()) { <div class="text-muted small mb-2 fw-500">{{ title() }}</div> }
        <div class="border rounded overflow-x-auto">
          @if (comparison()) {
            <table class="table table-sm mb-0 small min-w-[420px]">
              <thead class="table-light">
                <tr>
                  <th>分類</th>
                  <th class="text-right">預支</th>
                  @if (hasPrior()) { <th class="text-right">前次沖銷</th> }
                  <th class="text-right">本次沖銷</th>
                  <th class="text-right">差額</th>
                </tr>
              </thead>
              <tbody>
                @for (r of rows(); track r.category) {
                  <tr>
                    <td class="fw-500">{{ r.category }}</td>
                    <td class="text-right">{{ r.advance | number:'1.0-0' }}</td>
                    @if (hasPrior()) { <td class="text-right">{{ r.prior | number:'1.0-0' }}</td> }
                    <td class="text-right">{{ r.current | number:'1.0-0' }}</td>
                    <td class="text-right fw-500" [class.text-green]="r.diff > 0" [class.text-red]="r.diff < 0">
                      {{ r.diff | number:'1.0-0' }}
                    </td>
                  </tr>
                }
              </tbody>
              <tfoot class="bg-[--bg-base]">
                <tr>
                  <td class="fw-600">合計</td>
                  <td class="text-right fw-600">{{ total().advance | number:'1.0-0' }}</td>
                  @if (hasPrior()) { <td class="text-right fw-600">{{ total().prior | number:'1.0-0' }}</td> }
                  <td class="text-right fw-600">{{ total().current | number:'1.0-0' }}</td>
                  <td class="text-right fw-600" [class.text-green]="total().diff > 0" [class.text-red]="total().diff < 0">
                    {{ total().diff | number:'1.0-0' }}
                  </td>
                </tr>
              </tfoot>
            </table>
          } @else {
            <table class="table table-sm mb-0 small">
              <thead class="table-light">
                <tr><th>分類</th><th class="text-right">小計</th></tr>
              </thead>
              <tbody>
                @for (r of rows(); track r.category) {
                  <tr>
                    <td class="fw-500">{{ r.category }}</td>
                    <td class="text-right">{{ r.single | number:'1.0-0' }}</td>
                  </tr>
                }
              </tbody>
              <tfoot class="bg-[--bg-base]">
                <tr>
                  <td class="fw-600">合計</td>
                  <td class="text-right fw-600">{{ singleTotal() | number:'1.0-0' }}</td>
                </tr>
              </tfoot>
            </table>
          }
        </div>
      </div>
    }
  `,
})
export class CategorySubtotalsComponent {
  /** 預支明細（含追加批次）；與 currentItems 同時給＝對照模式 */
  advanceItems = input<readonly CategorizedAmount[] | null | undefined>(null);
  /** 本次（本單）明細 */
  currentItems = input<readonly CategorizedAmount[] | null | undefined>(null);
  /** 前次沖銷明細（有才顯示「前次沖銷」欄） */
  priorItems   = input<readonly CategorizedAmount[] | null | undefined>(null);
  /** 同預支單的沖銷歷史（只取本單之前者，攤平其 items 作為「前次沖銷」）；與 priorItems 擇一 */
  priorHistory = input<readonly WriteOffRound[] | null | undefined>(null);
  title        = input('分類小計');

  private priorAll = computed<readonly CategorizedAmount[]>(() => {
    const fromHistory = priorWriteOffRounds(this.priorHistory()).flatMap(w => w.items ?? []);
    return [...(this.priorItems() ?? []), ...fromHistory];
  });

  /** 預支與沖銷兩邊都有資料來源才進對照模式 */
  comparison = computed(() => this.advanceItems() != null && this.currentItems() != null);
  hasPrior   = computed(() => this.priorAll().length > 0);

  private advMap = computed(() => sumByCategory(this.advanceItems()));
  private curMap = computed(() => sumByCategory(this.currentItems()));
  private priMap = computed(() => sumByCategory(this.priorAll()));

  rows = computed<(SubtotalRow & {single: number})[]>(() => {
    const cmp = this.comparison();
    const maps = cmp ? [this.advMap(), this.priMap(), this.curMap()] : [this.advanceItems() != null ? this.advMap() : this.curMap()];
    // 合併分類：沿用 sumByCategory 排序，再把後面 map 才有的 key 依序補上，未分類置末
    const keys: string[] = [];
    for (const m of maps) for (const k of m.keys()) if (!keys.includes(k)) keys.push(k);
    const order = sumByCategory([...keys.map(k => ({category: k === UNCATEGORIZED_LABEL ? '' : k, totalPrice: 0}))]);
    const single = cmp ? null : maps[0];
    return [...order.keys()].map(category => {
      const advance = this.advMap().get(category) ?? 0;
      const prior = this.priMap().get(category) ?? 0;
      const current = this.curMap().get(category) ?? 0;
      return {category, advance, prior, current, diff: advance - prior - current, single: single?.get(category) ?? 0};
    });
  });

  total = computed(() => {
    const t = {advance: 0, prior: 0, current: 0, diff: 0};
    for (const r of this.rows()) { t.advance += r.advance; t.prior += r.prior; t.current += r.current; }
    t.diff = t.advance - t.prior - t.current;
    return t;
  });

  singleTotal = computed(() => this.rows().reduce((s, r) => s + r.single, 0));
}
