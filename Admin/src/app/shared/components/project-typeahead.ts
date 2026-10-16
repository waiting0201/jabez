import {Component, computed, forwardRef, input, signal, viewChild} from '@angular/core';
import {ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR} from '@angular/forms';
import {NgbTypeahead} from '@ng-bootstrap/ng-bootstrap';
import {Observable, OperatorFunction, Subject, debounceTime, distinctUntilChanged, filter, map, merge} from 'rxjs';

/** 打字篩選時最多列出的筆數（未打字的下拉模式不設限） */
const MAX_TYPED_RESULTS = 50;

/** 專案 autocomplete 需要的最小欄位（Project / 各表單的專案 lookup 皆相容） */
export interface ProjectOption {
  id: number;
  code: string;
  name: string;
  departmentName?: string | null;
}

/**
 * 專案 combobox（NgbTypeahead 包裝，ControlValueAccessor，值 = projectId | null）。
 * 同一欄位兼具 autocomplete 與下拉選單：打字即時篩選（最多 MAX_TYPED_RESULTS 筆）；
 * 點欄位 / 取得焦點 / 點右側 ▼ 則不篩選、列出全部專案（popup 高度上限與捲動在 tailwind.css）。
 * 已選定時點欄位同樣列出全部（以空字串觸發，不會被目前選定的顯示文字篩掉），方便直接換選。
 *
 * 取代全站原生 `<select>` 專案下拉（專案數百筆無法搜尋）。清單由呼叫端載入後傳入，元件內不打 API；
 * `projects` 晚於 writeValue 抵達時會自動重新解析顯示文字。
 *
 * 用法：
 * ```html
 * <app-project-typeahead formControlName="projectId" [projects]="projects" [invalid]="…"/>
 * ```
 * 規範見 docs/frontend-design.md「專案 autocomplete」。
 */
@Component({
  selector: 'app-project-typeahead',
  imports: [FormsModule, NgbTypeahead],
  providers: [{provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ProjectTypeahead), multi: true}],
  template: `
    <div class="relative">
      <input type="text" class="form-control"
             [class.form-control-sm]="size() === 'sm'"
             [class.is-invalid]="invalid()"
             [style.padding-right]="showClear() ? '3.75rem' : '2rem'"
             [ngbTypeahead]="search"
             #instance="ngbTypeahead"
             (focus)="focus$.next('')"
             (click)="click$.next('')"
             [resultFormatter]="resultFormatter"
             [inputFormatter]="resultFormatter"
             [editable]="false"
             container="body"
             [ngModel]="selected()"
             [ngModelOptions]="{standalone: true}"
             [disabled]="disabled()"
             (ngModelChange)="onModelChange($event)"
             (blur)="onTouched()"
             [placeholder]="placeholder()">
      @if (showClear()) {
        <button type="button" class="btn btn-sm btn-ghost-danger absolute inline-flex items-center"
                style="right: 1.75rem; top: 50%; transform: translateY(-50%); padding: 2px 4px"
                title="清除" aria-label="清除專案" (click)="clear()">
          <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#x"></use></svg>
        </button>
      }
      <!-- ▼ 純提示：pointer-events:none 讓點擊落到 input 上，走同一條 click$ 展開清單 -->
      <svg class="sa-icon absolute text-secondary"
           style="right: 0.6rem; top: 50%; transform: translateY(-50%); stroke: currentColor; pointer-events: none"
           [style.opacity]="disabled() ? 0.4 : 1">
        <use href="/assets/icons/sprite.svg#chevron-down"></use>
      </svg>
    </div>
  `,
})
export class ProjectTypeahead implements ControlValueAccessor {
  /** 候選清單（由呼叫端載入；可晚於 writeValue 抵達） */
  projects = input<readonly ProjectOption[] | null | undefined>([]);
  /** 排除的專案 id（加班多列互斥：其他列已選的專案） */
  excludeIds = input<readonly number[]>([]);
  placeholder = input('輸入案號或專案名稱搜尋…');
  size = input<'sm' | 'md'>('md');
  /** 顯示清除（×）鈕；用於選填欄位與報表篩選（空 = 不限） */
  allowClear = input(false);
  /** 套用 is-invalid 紅框（呼叫端以 `control.invalid && control.touched` 傳入） */
  invalid = input(false);

  private value = signal<number | null>(null);
  disabled = signal(false);

  private instance = viewChild.required<NgbTypeahead>('instance');
  /** 取得焦點 / 點欄位時送出空字串 → 列出全部專案 */
  focus$ = new Subject<string>();
  click$ = new Subject<string>();

  showClear = computed(() => this.allowClear() && !!this.selected() && !this.disabled());

  /** 目前選定的專案；projects 晚到時自動重新解析 */
  selected = computed<ProjectOption | null>(() => {
    const id = this.value();
    if (id == null) return null;
    return (this.projects() ?? []).find(p => p.id === id) ?? null;
  });

  private onChange: (v: number | null) => void = () => {};
  onTouched: () => void = () => {};

  resultFormatter = (p: ProjectOption) =>
    `${p.code} - ${p.name}${p.departmentName ? '（' + p.departmentName + '）' : ''}`;

  search: OperatorFunction<string, readonly ProjectOption[]> = (text$: Observable<string>) => {
    const typed = text$.pipe(debounceTime(150), distinctUntilChanged());
    // 清單已開著時再點欄位不重送（否則會把使用者正在打的篩選結果洗回全部）
    const clicks = this.click$.pipe(filter(() => !this.instance().isPopupOpen()));
    return merge(typed, this.focus$, clicks).pipe(
      map(term => {
        const t = (term ?? '').toString().toLowerCase().trim();
        const excluded = new Set(this.excludeIds());
        const list = (this.projects() ?? []).filter(p => !excluded.has(p.id));
        if (t.length === 0) return list;   // 下拉模式：全部列出，靠 popup 捲動
        return list
          .filter(p => p.code.toLowerCase().includes(t) || p.name.toLowerCase().includes(t))
          .slice(0, MAX_TYPED_RESULTS);
      })
    );
  };

  /** NgbTypeahead（editable=false）：選定回傳物件；打字中回傳 null → 值同步清空，強迫重選 */
  onModelChange(v: ProjectOption | string | null | undefined): void {
    const id = v && typeof v === 'object' ? v.id : null;
    if (id === this.value()) return;
    this.value.set(id);
    this.onChange(id);
  }

  clear(): void {
    this.value.set(null);
    this.onChange(null);
    this.onTouched();
  }

  writeValue(v: number | null): void {
    this.value.set(v ?? null);
  }

  registerOnChange(fn: (v: number | null) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(isDisabled: boolean): void { this.disabled.set(isDisabled); }
}
