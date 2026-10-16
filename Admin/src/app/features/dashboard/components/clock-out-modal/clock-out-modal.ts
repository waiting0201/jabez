import {Component, DestroyRef, OnInit, inject, signal} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {ProjectService} from '@features/admin/projects/services/project.service';
import {ProjectOption, ProjectTypeahead} from '@/app/shared/components/project-typeahead';
import {ProjectAllocation, ProjectAllocationInput} from '../../models/attendance.model';

/** 下班打卡視窗的結果。`reason` 只在有開原因欄位時有值。 */
export interface ClockOutModalResult {
  reason: string | null;
  allocations: ProjectAllocationInput[];
}

/**
 * 下班打卡視窗：取代原本的 ConfirmModal（規格不允許跳兩個視窗）。
 * 上方沿用出勤時長說明與早退／逾時原因欄（新制才有），下方為專案工時佔比（新舊制皆必填、合計須 = 100%）。
 * `mode='edit'` 為當日修改佔比：沒有原因欄與時長說明，帶入既有佔比。
 *
 * 用法與 ConfirmModal 相同：以 `componentInstance` 塞欄位，`await ref.result`，取消時 reject。
 */
@Component({
  selector: 'app-clock-out-modal',
  imports: [ReactiveFormsModule, FormsModule, ProjectTypeahead],
  template: `
    <div class="modal-header">
      <h5 class="modal-title flex items-center gap-2">
        @if (tone === 'warning') {
          <svg class="sa-icon" style="stroke: currentColor; color: var(--yellow)">
            <use href="/assets/icons/sprite.svg#alert-triangle"></use>
          </svg>
        }
        {{ title }}
      </h5>
      <button type="button" class="btn-close" (click)="cancel()"></button>
    </div>

    <div class="modal-body">
      @if (message) { <p class="mb-2">{{ message }}</p> }
      @if (detail) { <p class="text-secondary small mb-3">{{ detail }}</p> }

      @if (reasonLabel) {
        <label class="form-label fw-500">
          {{ reasonLabel }}
          @if (reasonRequired) { <span style="color: var(--red)">*</span> }
          @else { <span class="text-secondary small">（選填）</span> }
        </label>
        <textarea class="form-control mb-1" rows="2" maxlength="500"
                  [ngModel]="reason()" (ngModelChange)="onReasonChange($event)"
                  [ngModelOptions]="{standalone: true}"
                  [placeholder]="reasonPlaceholder"></textarea>
        @if (showReasonError()) {
          <div class="small mt-1" style="color: var(--red)">請填寫{{ reasonLabel }}。</div>
        }
        <hr class="my-3">
      }

      <div class="flex items-center justify-between mb-2">
        <label class="form-label fw-500 mb-0">今日專案工時佔比 <span style="color: var(--red)">*</span></label>
        <span class="small fw-600" [style.color]="totalOk() ? 'var(--green)' : 'var(--red)'">
          合計 {{ total() }}%
        </span>
      </div>

      @if (loadingProjects()) {
        <p class="text-muted small mb-0">專案載入中…</p>
      } @else {
        <div [formGroup]="form">
          <div formArrayName="rows" class="flex flex-col gap-2">
            @for (row of rows.controls; track row; let i = $index) {
              <div class="flex items-start gap-2" [formGroupName]="i">
                <div class="flex-1 min-w-0">
                  <app-project-typeahead formControlName="projectId" [projects]="projects()"
                                         [excludeIds]="excludedIds(i)"
                                         [invalid]="!!(row.get('projectId')?.invalid && submitted())"/>
                </div>
                <div class="flex items-center gap-1" style="width: 7.5rem">
                  <input type="number" class="form-control text-right" formControlName="percentage"
                         min="0.01" max="100" step="0.01" placeholder="佔比"
                         [class.is-invalid]="row.get('percentage')?.invalid && submitted()">
                  <span class="text-secondary">%</span>
                </div>
                <button type="button" class="btn btn-ghost-danger inline-flex items-center"
                        title="刪除" [disabled]="rows.length <= 1" (click)="removeRow(i)">
                  <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
                </button>
              </div>
            }
          </div>
        </div>
        @if (projects().length === 0) {
          <div class="small mt-2" style="color: var(--red)">目前沒有可選的專案，請聯絡管理員。</div>
        }
        <button type="button" class="btn btn-sm btn-outline-primary inline-flex items-center gap-1 mt-3"
                (click)="addRow()">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#plus"></use></svg>
          新增專案
        </button>
        @if (errorText()) {
          <div class="small mt-2" style="color: var(--red)">{{ errorText() }}</div>
        }
      }
    </div>

    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="cancel()">{{ cancelText }}</button>
      <button type="button" class="btn btn-primary" [disabled]="loadingProjects() || !totalOk()" (click)="confirm()">{{ confirmText }}</button>
    </div>
  `,
})
export class ClockOutModal implements OnInit {
  activeModal = inject(NgbActiveModal);
  private fb = inject(FormBuilder);
  private projectService = inject(ProjectService);
  private destroyRef = inject(DestroyRef);

  title = '確認下班打卡';
  message = '';
  detail = '';
  confirmText = '確定打卡';
  cancelText = '取消';
  tone: 'default' | 'warning' = 'default';
  reasonLabel = '';
  reasonRequired = false;
  reasonPlaceholder = '';
  /** 編輯模式的既有佔比（也可能含已結案專案，會併入候選清單） */
  initial: ProjectAllocation[] = [];

  reason = signal('');
  showReasonError = signal(false);
  submitted = signal(false);
  loadingProjects = signal(true);
  projects = signal<ProjectOption[]>([]);
  total = signal(0);
  errorText = signal('');

  form: FormGroup = this.fb.group({rows: this.fb.array<FormGroup>([])});
  get rows(): FormArray<FormGroup> { return this.form.get('rows') as FormArray<FormGroup>; }

  totalOk(): boolean { return this.total() === 100; }

  ngOnInit(): void {
    if (this.initial.length) {
      this.initial.forEach(a => this.rows.push(this.newRow(a.projectId, a.percentage)));
    } else {
      this.rows.push(this.newRow(null, 100));
    }
    this.recomputeTotal();
    this.rows.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recomputeTotal());

    this.projectService.getActiveAll().subscribe({
      next: list => {
        const opts: ProjectOption[] = list.map(p => ({id: p.id, code: p.code, name: p.name, departmentName: p.departmentName}));
        // 既有佔比若含已結案專案，仍須能顯示
        for (const a of this.initial) {
          if (!opts.some(o => o.id === a.projectId)) opts.push({id: a.projectId, code: a.projectCode, name: a.projectName});
        }
        this.projects.set(opts);
        this.loadingProjects.set(false);
      },
      error: () => {
        this.projects.set(this.initial.map(a => ({id: a.projectId, code: a.projectCode, name: a.projectName})));
        this.loadingProjects.set(false);
        this.errorText.set('專案清單載入失敗，請關閉視窗後重試。');
      },
    });
  }

  private newRow(projectId: number | null, percentage: number | null): FormGroup {
    return this.fb.group({
      projectId: [projectId as number | null, Validators.required],
      percentage: [percentage as number | null, [Validators.required, Validators.min(0.01), Validators.max(100)]],
    });
  }

  addRow(): void {
    const remaining = Math.round((100 - this.total()) * 100) / 100;
    this.rows.push(this.newRow(null, remaining > 0 ? remaining : null));
  }

  removeRow(i: number): void {
    if (this.rows.length <= 1) return;
    this.rows.removeAt(i);
  }

  /** 第 i 列的 autocomplete 要排除其他列已選的專案 */
  excludedIds(i: number): number[] {
    return this.rows.controls
      .filter((_, idx) => idx !== i)
      .map(g => g.get('projectId')!.value as number | null)
      .filter((v): v is number => v != null);
  }

  private recomputeTotal(): void {
    const sum = this.rows.controls.reduce((acc, g) => acc + (Number(g.get('percentage')!.value) || 0), 0);
    this.total.set(Math.round(sum * 100) / 100);
  }

  onReasonChange(value: string): void {
    this.reason.set(value);
    if (value.trim()) this.showReasonError.set(false);
  }

  confirm(): void {
    if (this.reasonLabel && this.reasonRequired && !this.reason().trim()) {
      this.showReasonError.set(true);
      return;
    }
    this.submitted.set(true);
    this.form.markAllAsTouched();
    if (this.form.invalid) { this.errorText.set('請為每一列選擇專案並填寫 0.01 ~ 100 的佔比。'); return; }
    if (!this.totalOk()) { this.errorText.set('專案佔比合計必須等於 100%。'); return; }
    const allocations: ProjectAllocationInput[] = this.rows.controls.map(g => ({
      projectId: g.get('projectId')!.value as number,
      percentage: Number(g.get('percentage')!.value),
    }));
    if (new Set(allocations.map(a => a.projectId)).size !== allocations.length) {
      this.errorText.set('同一專案不可重複。');
      return;
    }
    this.errorText.set('');
    const result: ClockOutModalResult = {reason: this.reason().trim() || null, allocations};
    this.activeModal.close(result);
  }

  cancel(): void {
    this.activeModal.dismiss();
  }
}
