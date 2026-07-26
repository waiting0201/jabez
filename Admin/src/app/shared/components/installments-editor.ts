import {Component, computed, effect, inject, input, output, signal} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {InstallmentDto, InstallmentInput} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 共用「可編輯」撥款明細表（唯讀版為 installments-table）。
 *
 * 兩種模式：
 * - `review`：財務簽核當下填預計撥款日 + 各期金額（無實際撥款日欄、無儲存鈕，由外層表單一起送出）
 * - `manage`：核准後管理（多實際撥款日欄與「儲存撥款明細」按鈕，已撥款列鎖定）
 *
 * 一頁可放多個實例（例如沖銷簽核頁同時維護「本單差額撥款」與「關聯預支單撥款」）。
 * 規則（與後端 InstallmentValidator 對齊）：序號 1-based 連續、SUM == 應撥總額、已撥款列不可改不可刪。
 */
@Component({
  selector: 'app-installments-editor',
  imports: [ReactiveFormsModule, DecimalPipe],
  template: `
    <div class="border rounded p-4 bg-[--bg-base] mb-4">
      <div class="flex items-center justify-between mb-3 flex-wrap gap-2">
        <div class="fw-500">
          {{ title() }}
          @if (required()) { <span class="text-danger">*</span> }
          @if (statusLabel()) {
            <span class="badge ml-2" [class]="statusClass()">{{ statusLabel() }}</span>
          }
          <span class="text-muted small fw-400 ml-2">（{{ totalLabel() }}：{{ totalAmount() | number:'1.0-2' }} 元）</span>
          <span class="text-muted small fw-400 ml-2">剩餘 {{ totalAmount() - sum() | number:'1.0-2' }} 元</span>
        </div>
        <button type="button" class="btn btn-sm btn-outline-secondary"
                [disabled]="!canAddRow()" (click)="addRow()">+ 新增一期</button>
      </div>

      <div class="border rounded overflow-x-auto bg-white">
        <table class="table table-sm mb-0 small" [style.min-width]="mode() === 'manage' ? '760px' : '640px'">
          <thead class="bg-[--bg-base]">
            <tr>
              <th class="text-center" style="width: 60px">期數</th>
              <th style="width: 160px">預計撥款日</th>
              @if (mode() === 'manage') { <th style="width: 160px">實際撥款日</th> }
              <th style="width: 140px">金額</th>
              <th>備註</th>
              <th class="text-center" style="width: 60px"></th>
            </tr>
          </thead>
          <tbody>
            @for (row of form.controls; track $index; let i = $index) {
              <tr [formGroup]="row" [class.bg-[--bg-base]]="isLocked(row)">
                <td class="text-center align-middle">
                  {{ i + 1 }}
                  @if (isLocked(row)) {
                    <div class="text-success" style="font-size:10px">已撥</div>
                  }
                </td>
                <td>
                  <input type="date" class="form-control form-control-sm" formControlName="expectedDate"
                         [attr.readonly]="isLocked(row) ? true : null"
                         [class.bg-light]="isLocked(row)">
                </td>
                @if (mode() === 'manage') {
                  <td>
                    <input type="date" class="form-control form-control-sm" formControlName="paidAt"
                           [attr.readonly]="isLocked(row) ? true : null"
                           [class.bg-light]="isLocked(row)">
                  </td>
                }
                <td>
                  <input type="number" min="1" step="1" class="form-control form-control-sm text-right" formControlName="amount"
                         [attr.max]="isLocked(row) ? null : rowMax(i)"
                         [attr.readonly]="isLocked(row) ? true : null"
                         [class.bg-light]="isLocked(row)">
                </td>
                <td>
                  <input type="text" class="form-control form-control-sm" formControlName="note" placeholder="（選填）"
                         [attr.readonly]="isLocked(row) ? true : null"
                         [class.bg-light]="isLocked(row)">
                </td>
                <td class="text-center align-middle">
                  @if (!isLocked(row) && form.controls.length > 1) {
                    <button type="button" class="btn btn-sm btn-link text-danger p-0" (click)="removeRow(i)" title="刪除此期">⨯</button>
                  }
                </td>
              </tr>
            }
          </tbody>
          <tfoot class="bg-[--bg-base]">
            <tr>
              <td class="text-center fw-500" [attr.colspan]="mode() === 'manage' ? 3 : 2">加總</td>
              <td class="text-right fw-500"
                  [class.text-danger]="!sumValid()"
                  [class.text-success]="sumValid()">
                {{ sum() | number:'1.0-2' }} 元
              </td>
              <td colspan="2" class="small text-muted">
                @if (!sumValid()) {
                  <span class="text-danger">⚠ 各筆金額加總需等於 {{ totalAmount() | number:'1.0-2' }} 元</span>
                }
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      @if (hint()) {
        <div class="text-muted small mt-2">{{ hint() }}</div>
      }

      @if (mode() === 'manage') {
        <div class="mt-3 flex gap-2">
          <button type="button" class="btn btn-sm btn-primary"
                  [disabled]="!sumValid() || saving()"
                  (click)="emitSave()">儲存撥款明細</button>
        </div>
      }

      @if (message()) {
        <div class="text-success small mt-2">{{ message() }}</div>
      }
      @if (error()) {
        <div class="text-danger small mt-2">{{ error() }}</div>
      }
    </div>
  `,
})
export class InstallmentsEditorComponent {
  private fb = inject(FormBuilder);

  /** 應撥總額 — SUM(各期金額) 必須等於此值 */
  totalAmount = input.required<number>();
  /** 既有分期；為空時自動建立 1 列（金額帶入 totalAmount）*/
  installments = input<InstallmentDto[] | undefined>(undefined);
  mode        = input<'review' | 'manage'>('review');
  title       = input('撥款明細');
  totalLabel  = input('申請總額');
  hint        = input('');
  required    = input(false);
  statusLabel = input<string | null>(null);
  statusClass = input('');
  saving      = input(false);
  message     = input('');
  error       = input('');

  save = output<InstallmentInput[]>();

  form = this.fb.array<FormGroup<any>>([]);

  /** form 值變動的觸發器 — FormArray 非 signal，靠 valueChanges 推動 computed 重算 */
  private revision = signal(0);
  private lockedIds = new Set<number>();

  constructor() {
    this.form.valueChanges.subscribe(() => this.revision.update(v => v + 1));
    // installments 輸入換新參考時重建表單（外層儲存後重新載入 task 會觸發）
    effect(() => this.rebuild(this.installments(), this.totalAmount()));
  }

  // ── 對外 API（供外層表單於送出時取值 / 驗證）────────────────────────────

  /** 目前表單值，installmentNo 依當前順序重編 */
  value(): InstallmentInput[] {
    return this.form.controls.map((row, idx) => ({
      id:            row.get('id')?.value ?? undefined,
      installmentNo: idx + 1,
      expectedDate:  row.get('expectedDate')?.value,
      paidAt:        row.get('paidAt')?.value || undefined,
      amount:        Number(row.get('amount')?.value) || 0,
      note:          row.get('note')?.value || undefined,
    }));
  }

  valid(): boolean {
    return this.form.valid && this.sumValid();
  }

  markAllAsTouched(): void {
    this.form.markAllAsTouched();
  }

  // ── 內部狀態 ────────────────────────────────────────────────────────────

  sum = computed(() => {
    this.revision();
    return this.form.controls.reduce((acc, c) => acc + (Number(c.get('amount')?.value) || 0), 0);
  });

  sumValid = computed(() => Math.abs(this.sum() - this.totalAmount()) <= 0.01);

  canAddRow = computed(() => this.sum() < this.totalAmount() - 0.01);

  isLocked(row: FormGroup): boolean {
    const id = row.get('id')?.value as number | null;
    return id != null && this.lockedIds.has(id);
  }

  /** 該列可填的上限 = 應撥總額 − 其他列已填金額 */
  rowMax(index: number): number {
    this.revision();
    const others = this.form.controls.reduce((acc, c, i) =>
      i === index ? acc : acc + (Number(c.get('amount')?.value) || 0), 0);
    return this.totalAmount() - others;
  }

  addRow(): void {
    if (!this.canAddRow()) return;
    this.form.push(this.buildRow({
      installmentNo: this.form.length + 1,
      expectedDate:  '',
      paidAt:        '',
      amount:        this.totalAmount() - this.sum(),
      note:          '',
    }));
    this.revision.update(v => v + 1);
  }

  removeRow(index: number): void {
    if (this.isLocked(this.form.at(index) as FormGroup)) return;
    this.form.removeAt(index);
    this.revision.update(v => v + 1);
  }

  emitSave(): void {
    if (!this.valid()) {
      this.form.markAllAsTouched();
      return;
    }
    this.save.emit(this.value());
  }

  private rebuild(list: InstallmentDto[] | undefined, total: number): void {
    this.form.clear({emitEvent: false});
    this.lockedIds.clear();

    if (list && list.length > 0) {
      for (const ins of list) {
        if (ins.paidAt) this.lockedIds.add(ins.id);
        this.form.push(this.buildRow({
          id:            ins.id,
          installmentNo: ins.installmentNo,
          expectedDate:  ins.expectedDate?.toString().slice(0, 10) ?? '',
          paidAt:        ins.paidAt?.toString().slice(0, 10) ?? '',
          amount:        ins.amount,
          note:          ins.note ?? '',
        }), {emitEvent: false});
      }
    } else {
      this.form.push(this.buildRow({
        installmentNo: 1,
        expectedDate:  '',
        paidAt:        '',
        amount:        total,
        note:          '',
      }), {emitEvent: false});
    }

    this.revision.update(v => v + 1);
  }

  private buildRow(v: {id?: number; installmentNo: number; expectedDate: string; paidAt: string; amount: number; note: string}) {
    return this.fb.group({
      id:            [v.id ?? null],
      installmentNo: [v.installmentNo],
      expectedDate:  [v.expectedDate, Validators.required],
      paidAt:        [v.paidAt],
      amount:        [v.amount, [Validators.required, Validators.min(1)]],
      note:          [v.note],
    });
  }
}
