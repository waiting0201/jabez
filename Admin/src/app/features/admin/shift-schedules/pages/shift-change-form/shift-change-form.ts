import {Component, OnDestroy, OnInit, computed, inject, signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {ToastrService} from 'ngx-toastr';
import {ShiftChangeService} from '../../services/shift-change.service';
import {ShiftChangeMonthView, ShiftChangeRequest} from '../../models/shift-change.model';
import {DAY_TYPE_LABELS, ShiftDayType, ShiftScheduleDay, dateKey, nextDayType, isLockedOffCell} from '../../models/shift-schedule.model';
import {
  APPROVAL_STATUS_CLASSES,
  APPROVAL_STATUS_LABELS,
  ApprovalStatus,
} from '../../../payment-requests/models/payment-request.model';
import {ShiftMonthCalendar, lockedCellMessage} from '../../../../../shared/components/shift-month-calendar/shift-month-calendar';

type Mode = 'new' | 'edit' | 'view';

/**
 * 改班申請（四週彈性工時 §3.5.2）—— 開放期（前兩個月 10 日 ～ 前一個月 25 日）結束、班表定案鎖定後的異動途徑。
 *
 * 三模式共用一個元件（new / edit / view），靠 route data 的 `mode` 切換，
 * 比照銷假申請 leave-revocation-form 的做法。
 *
 * 2026-09-28 改版：版面比照〈個人排班〉的月曆（共用 `<app-shift-month-calendar>`），
 * 每點一格就呼叫 `POST /shift-changes/preview` 試算，**配額與「是否符合排班規範」即時更新**；
 * 不符規範時不得送出（後端送簽與每一關核准前亦以同一份判準重驗）。
 *
 * ⚠ **核准後才寫入班表**：送簽期間原班表完全不動，所以這頁顯示的「現況」
 * 一直是真正生效中的班表，不會因為有單在跑而變動。
 */
@Component({
  selector: 'app-shift-change-form',
  templateUrl: './shift-change-form.html',
  imports: [CommonModule, FormsModule, ShiftMonthCalendar],
})
export class ShiftChangeForm implements OnInit, OnDestroy {
  private svc = inject(ShiftChangeService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  readonly dayTypeLabels = DAY_TYPE_LABELS;
  readonly statusLabels = APPROVAL_STATUS_LABELS;
  readonly statusClasses = APPROVAL_STATUS_CLASSES;

  mode = signal<Mode>('new');
  requestId = signal<number | null>(null);

  loading = signal(false);
  previewing = signal(false);
  saving = signal(false);

  year = signal(new Date().getFullYear());
  month = signal(new Date().getMonth() + 1);
  reason = signal('');

  existing = signal<ShiftChangeRequest | null>(null);
  view = signal<ShiftChangeMonthView | null>(null);

  /** 使用者挑的異動：date → 目標日別 */
  private picked = signal<Record<string, ShiftDayType>>({});

  /** 現行班表（未套用本次調整）：view 的調整後日別，扣回 changes 的原日別 */
  private baseTypes = computed(() => {
    const v = this.view();
    const map: Record<string, ShiftDayType> = {};
    if (!v) return map;
    for (const d of v.days) map[dateKey(d.date)] = d.dayType;
    for (const c of v.changes) map[dateKey(c.date)] = c.fromDayType;
    return map;
  });

  /** 月曆呈現：現行班表疊上本次挑選（點格當下立即反映，不必等試算回來） */
  readonly displayTypes = computed(() => ({...this.baseTypes(), ...this.picked()}));

  /** 異動格：key → 原日別 */
  readonly changedFrom = computed(() => {
    const base = this.baseTypes();
    return Object.fromEntries(Object.keys(this.picked()).map(k => [k, base[k] ?? 'work'])) as Record<string, ShiftDayType>;
  });

  readonly readOnly = computed(() => {
    if (this.mode() === 'view') return true;
    const status = this.existing()?.approvalStatus;
    return !!status && status !== 'draft' && status !== 'returned';
  });

  readonly yearOptions = computed(() => {
    const y = new Date().getFullYear();
    return [y - 1, y, y + 1];
  });
  readonly monthOptions = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  readonly pickedList = computed(() => {
    const from = this.changedFrom();
    return Object.entries(this.picked())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([date, toDayType]) => ({date, fromDayType: from[date], toDayType}));
  });

  readonly dirty = computed(() => Object.keys(this.picked()).length > 0);

  /** 試算結果符合規範（試算中視為未定，送出鈕先鎖住） */
  readonly canSubmit = computed(() =>
    this.dirty() && !this.previewing() && !!this.view()?.validation.canSave);

  private previewTimer: ReturnType<typeof setTimeout> | null = null;
  private previewSeq = 0;

  ngOnInit(): void {
    this.mode.set((this.route.snapshot.data['mode'] as Mode) ?? 'new');
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (this.mode() === 'new') {
      // 預設看次月：改班的情境就是「已定案的次月班表要調整」
      const next = new Date();
      next.setMonth(next.getMonth() + 1, 1);
      this.year.set(next.getFullYear());
      this.month.set(next.getMonth() + 1);
      this.runPreview();
      return;
    }

    this.requestId.set(id);
    this.loadExisting(id);
  }

  ngOnDestroy(): void {
    if (this.previewTimer) clearTimeout(this.previewTimer);
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
          (res.dates ?? []).map(d => [dateKey(d.date), d.toDayType])));
        this.view.set(res.view ?? null);
        this.loading.set(false);
      },
      error: err => {
        this.toastr.error(err?.error?.message ?? '載入改班申請失敗');
        this.loading.set(false);
      },
    });
  }

  onPeriodChange(): void {
    this.picked.set({});      // 換月後原本挑的日子已不適用
    this.view.set(null);
    this.runPreview();
  }

  /** 點一下循環切換目標日別；切回原本的日別＝取消這天的異動 */
  onCellClick({key, cell}: {key: string; cell: ShiftScheduleDay}): void {
    if (this.readOnly()) return;
    if (cell.readOnly) {
      this.toastr.info(lockedCellMessage(cell, '該日已過，不可申請變更。'));
      return;
    }

    const base = this.baseTypes()[key] ?? 'work';

    // 活動日／請假鎖定格卻排著例假／休假：只能在「改成上班日」與「維持原狀」之間切換
    if (cell.lockReason) {
      if (!isLockedOffCell(cell, base)) {
        this.toastr.info(lockedCellMessage(cell, '此日期不可變更。'));
        return;
      }
      this.picked.update(p => {
        const copy = {...p};
        if (copy[key]) delete copy[key];
        else copy[key] = 'work';
        return copy;
      });
      this.schedulePreview();
      return;
    }

    const next = nextDayType(this.picked()[key] ?? base);

    this.picked.update(p => {
      const copy = {...p};
      if (next === base) delete copy[key];   // 回到原狀 → 不算異動
      else copy[key] = next;
      return copy;
    });
    this.schedulePreview();
  }

  private schedulePreview(): void {
    this.previewing.set(true);
    if (this.previewTimer) clearTimeout(this.previewTimer);
    this.previewTimer = setTimeout(() => this.runPreview(), 250);
  }

  /** 試算套用後的整月月曆 + 檢核。以序號丟棄過時回應（連點時只採用最後一次） */
  private runPreview(): void {
    const seq = ++this.previewSeq;
    this.previewing.set(true);
    if (!this.view()) this.loading.set(true);

    this.svc.preview({
      year: this.year(),
      month: this.month(),
      dates: Object.entries(this.picked()).map(([date, toDayType]) => ({date, toDayType})),
      excludeRequestId: this.requestId(),
    }).subscribe({
      next: res => {
        if (seq !== this.previewSeq) return;
        this.view.set(res);
        this.previewing.set(false);
        this.loading.set(false);
      },
      error: err => {
        if (seq !== this.previewSeq) return;
        this.toastr.error(err?.error?.message ?? '載入班表失敗');
        this.previewing.set(false);
        this.loading.set(false);
      },
    });
  }

  save(submit: boolean): void {
    if (this.saving()) return;        // in-flight 鎖：避免連按建出兩張單

    const dates = Object.entries(this.picked()).map(([date, toDayType]) => ({date, toDayType}));
    if (dates.length === 0) { this.toastr.warning('請至少選擇一天要調整的日期'); return; }
    if (submit && !this.reason().trim()) { this.toastr.warning('請填寫改班原因'); return; }
    if (submit && !this.view()?.validation.canSave) {
      this.toastr.warning('調整後班表不符合排班規範，請先依提示修正');
      return;
    }

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
            this.router.navigate(['/admin/shift-changes', done.id]);
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

  statusLabel(status: string): string {
    return this.statusLabels[status as ApprovalStatus] ?? status;
  }

  statusClass(status: string): string {
    return this.statusClasses[status as ApprovalStatus] ?? 'bg-secondary-subtle text-secondary';
  }

  back(): void {
    this.router.navigate(['/admin/shift-schedules']);
  }
}
