import {Component, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {ShiftChangeService} from '../../services/shift-change.service';
import {ChangeableShiftDates, ShiftChangeDate, ShiftChangeRequest} from '../../models/shift-change.model';
import {DAY_TYPE_LABELS, SELECTABLE_DAY_TYPES, ShiftDayType} from '../../models/shift-schedule.model';

type Mode = 'new' | 'edit' | 'view';

/**
 * 改班申請（四週彈性工時 §3.5.2）—— 開放期（10–25 日）結束、班表定案鎖定後的異動途徑。
 *
 * 三模式共用一個元件（new / edit / view），靠 route data 的 `mode` 切換，
 * 比照銷假申請 leave-revocation-form 的做法。
 *
 * ⚠ **核准後才寫入班表**：送簽期間原班表完全不動，所以這頁顯示的「現況」
 * 一直是真正生效中的班表，不會因為有單在跑而變動。
 */
@Component({
  selector: 'app-shift-change-form',
  templateUrl: './shift-change-form.html',
  imports: [CommonModule, FormsModule],
})
export class ShiftChangeForm implements OnInit {
  private svc = inject(ShiftChangeService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  readonly dayTypeLabels = DAY_TYPE_LABELS;
  readonly selectableTypes = SELECTABLE_DAY_TYPES;

  mode = signal<Mode>('new');
  requestId = signal<number | null>(null);

  loading = signal(false);
  saving = signal(false);

  year = signal(new Date().getFullYear());
  month = signal(new Date().getMonth() + 1);
  reason = signal('');

  source = signal<ChangeableShiftDates | null>(null);
  existing = signal<ShiftChangeRequest | null>(null);

  /** 使用者挑的異動：date → 目標日別 */
  private picked = signal<Record<string, ShiftDayType>>({});

  readonly readOnly = computed(() => this.mode() === 'view');

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y, y + 1];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  /** 可挑選的日子（後端已排除國定假日與過去日期，此處只做顯示） */
  readonly dates = computed(() => this.source()?.dates ?? []);

  readonly pickedList = computed(() => {
    const p = this.picked();
    return this.dates()
      .filter(d => p[d.date.slice(0, 10)])
      .map(d => ({...d, toDayType: p[d.date.slice(0, 10)]}));
  });

  readonly dirty = computed(() => Object.keys(this.picked()).length > 0);

  ngOnInit(): void {
    this.mode.set((this.route.snapshot.data['mode'] as Mode) ?? 'new');
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (this.mode() === 'new') {
      // 預設看次月：改班的情境就是「已定案的次月班表要調整」
      const next = new Date();
      next.setMonth(next.getMonth() + 1);
      this.year.set(next.getFullYear());
      this.month.set(next.getMonth() + 1);
      this.loadSource();
      return;
    }

    this.requestId.set(id);
    this.loadExisting(id);
  }

  loadSource(): void {
    this.loading.set(true);
    this.svc.getChangeableDates(this.year(), this.month()).subscribe({
      next: res => { this.source.set(res); this.loading.set(false); },
      error: err => {
        this.toastr.error(err?.error?.message ?? '載入班表失敗');
        this.loading.set(false);
      },
    });
  }

  private loadExisting(id: number): void {
    this.loading.set(true);
    this.svc.getById(id).subscribe({
      next: res => {
        this.existing.set(res);
        this.year.set(res.year);
        this.month.set(res.month);
        this.reason.set(res.reason);
        this.picked.set(Object.fromEntries(
          (res.dates ?? []).map(d => [d.date.slice(0, 10), d.toDayType])));
        this.loading.set(false);
        if (this.mode() === 'edit') this.loadSource();
      },
      error: err => {
        this.toastr.error(err?.error?.message ?? '載入改班申請失敗');
        this.loading.set(false);
      },
    });
  }

  onPeriodChange(): void {
    this.picked.set({});      // 換月後原本挑的日子已不適用
    this.loadSource();
  }

  /** 點一下循環切換目標日別；切回原本的日別＝取消這天的異動 */
  cycle(d: ShiftChangeDate): void {
    if (this.readOnly() || d.isPublicHoliday) return;

    const key = d.date.slice(0, 10);
    const current = this.picked()[key] ?? d.fromDayType;
    const idx = SELECTABLE_DAY_TYPES.indexOf(current);
    const next = SELECTABLE_DAY_TYPES[(idx + 1) % SELECTABLE_DAY_TYPES.length];

    this.picked.update(p => {
      const copy = {...p};
      if (next === d.fromDayType) delete copy[key];   // 回到原狀 → 不算異動
      else copy[key] = next;
      return copy;
    });
  }

  displayType(d: ShiftChangeDate): ShiftDayType {
    return this.picked()[d.date.slice(0, 10)] ?? d.fromDayType;
  }

  isChanged(d: ShiftChangeDate): boolean {
    return !!this.picked()[d.date.slice(0, 10)];
  }

  save(submit: boolean): void {
    if (this.saving()) return;        // in-flight 鎖：避免連按建出兩張單

    const dates = Object.entries(this.picked()).map(([date, toDayType]) => ({date, toDayType}));
    if (dates.length === 0) { this.toastr.warning('請至少選擇一天要調整的日期'); return; }
    if (submit && !this.reason().trim()) { this.toastr.warning('請填寫改班原因'); return; }

    this.saving.set(true);
    const payload = {year: this.year(), month: this.month(), reason: this.reason().trim(), dates};

    // create 成功後記住 id，送簽失敗時重送走 update 而非再建一張（全站申請表單共同規範）
    const id = this.requestId();
    const req$ = id ? this.svc.update(id, payload) : this.svc.create(payload);

    req$.subscribe({
      next: res => {
        this.requestId.set(res.id);
        if (!submit) {
          this.saving.set(false);
          this.toastr.success('改班申請已儲存');
          return;
        }
        this.svc.submit(res.id).subscribe({
          next: done => {
            this.saving.set(false);
            this.toastr.success(done.approvalStatus === 'approved' ? '改班申請已核准' : '改班申請已送出');
            this.router.navigate(['/admin/shift-schedules']);
          },
          error: err => {
            this.saving.set(false);
            this.toastr.error(err?.error?.message ?? '送出失敗');
          },
        });
      },
      error: err => {
        this.saving.set(false);
        this.toastr.error(err?.error?.message ?? '儲存失敗');
      },
    });
  }

  back(): void {
    this.router.navigate(['/admin/shift-schedules']);
  }
}
