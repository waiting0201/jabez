import {Component, OnInit, inject, signal, computed} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {DatePipe} from '@angular/common';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {HttpErrorResponse} from '@angular/common/http';
import {ToastrService} from 'ngx-toastr';
import {AuthService} from '@core/auth/services/auth.service';
import {ConfirmModal} from '@/app/shared/components/confirm-modal';
import {DisasterDayService} from '../../services/disaster-day.service';
import {
  DISASTER_SEGMENT_LABELS, DisasterDayOff, DisasterFetchLog, DisasterSegment,
} from '../../models/disaster-day.model';
import {DisasterDayAddModal} from '../../components/disaster-day-add-modal';
import {DisasterDayAffectedModal} from '../../components/disaster-day-affected-modal';

const WEEKDAY_LABELS = ['日', '一', '二', '三', '四', '五', '六'];

function ymd(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

@Component({
  selector: 'app-disaster-day-list',
  templateUrl: './disaster-day-list.html',
  imports: [FormsModule, DatePipe],
})
export class DisasterDayList implements OnInit {
  private svc = inject(DisasterDayService);
  private auth = inject(AuthService);
  private toastr = inject(ToastrService);
  private modal = inject(NgbModal);

  canWrite = computed(() => this.auth.hasPermission('disaster-days:write'));
  isSuperAdmin = this.auth.isSuperAdmin;

  readonly segmentLabels = DISASTER_SEGMENT_LABELS;
  readonly segmentOptions: DisasterSegment[] = ['full', 'am', 'pm'];

  /** 預設區間：上個月 1 號 ～ 下個月最後一天（颱風假多為近期事件，並可預排明日） */
  dateFrom = signal((() => { const n = new Date(); return ymd(new Date(n.getFullYear(), n.getMonth() - 1, 1)); })());
  dateTo = signal((() => { const n = new Date(); return ymd(new Date(n.getFullYear(), n.getMonth() + 2, 0)); })());

  items = signal<DisasterDayOff[]>([]);
  loading = signal(false);
  /** 正在變更的紀錄 id（in-flight 鎖，避免連點） */
  busyId = signal<number | null>(null);

  logs = signal<DisasterFetchLog[]>([]);
  fetching = signal(false);

  /** 最近一次抓取的待人工判斷項目（醒目呈現） */
  pendingManual = computed(() => {
    const latest = this.logs()[0];
    return latest?.manualItems ?? [];
  });

  ngOnInit(): void {
    this.load();
    this.loadLogs();
  }

  weekday(dateStr: string): string {
    return WEEKDAY_LABELS[new Date(dateStr).getDay()];
  }

  load(): void {
    this.loading.set(true);
    this.svc.list(this.dateFrom(), this.dateTo()).subscribe({
      next: rows => { this.items.set(rows); this.loading.set(false); },
      error: () => { this.toastr.error('載入颱風假資料失敗'); this.loading.set(false); },
    });
  }

  loadLogs(): void {
    this.svc.fetchLogs(20).subscribe({
      next: l => this.logs.set(l),
      error: () => this.logs.set([]),
    });
  }

  /** 日期早於本月 ＝ 該月薪資多半已發放 */
  private isPastMonth(dateStr: string): boolean {
    const now = new Date();
    const cur = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    return dateStr.slice(0, 7) < cur;
  }

  /** 過去月份的異動先跳警示（薪資即時重算，已發薪月份會跟著變） */
  private async confirmIfPast(day: DisasterDayOff, action: string): Promise<boolean> {
    if (!this.isPastMonth(day.date)) return true;
    const ref = this.modal.open(ConfirmModal, {centered: true});
    const ci = ref.componentInstance as ConfirmModal;
    ci.title = '變更過去月份的颱風假';
    ci.message = `${day.date} ${day.countyName} 屬於過去的月份，${action}。`;
    ci.detail = '薪資為即時重算、沒有月結快照：已發放月份的薪資金額與請假時數會跟著改變。確定要繼續嗎？';
    ci.tone = 'warning';
    ci.confirmText = '確定變更';
    try {
      await ref.result;
      return true;
    } catch {
      return false;
    }
  }

  private applyUpdated(updated: DisasterDayOff): void {
    this.items.update(list => list.map(d => d.id === updated.id ? updated : d));
  }

  private onError(err: HttpErrorResponse, fallback: string): void {
    this.toastr.error(err.error?.message || fallback);
    this.busyId.set(null);
    this.load();   // 失敗時重新載入，避免畫面停在樂觀值
  }

  async togglePaid(day: DisasterDayOff, isPaid: boolean): Promise<void> {
    if (this.busyId() !== null || day.status !== 'active' || day.isPaid === isPaid) return;
    const ok = await this.confirmIfPast(day, isPaid ? '改為給薪後，已扣的薪資會恢復、請假時數會被退回' : '改為不給薪後，未出勤者會被扣薪、請假時數會恢復');
    if (!ok) { this.load(); return; }   // 取消：重載讓 checkbox 還原
    this.busyId.set(day.id);
    this.svc.update(day.id, {isPaid}).subscribe({
      next: u => { this.applyUpdated(u); this.busyId.set(null); this.toastr.success(isPaid ? '已改為給薪' : '已改為不給薪'); },
      error: (e: HttpErrorResponse) => this.onError(e, '更新失敗'),
    });
  }

  async changeSegment(day: DisasterDayOff, segment: DisasterSegment): Promise<void> {
    if (this.busyId() !== null || day.status !== 'active' || day.segment === segment) return;
    const ok = await this.confirmIfPast(day, `時段改為${DISASTER_SEGMENT_LABELS[segment]}`);
    if (!ok) { this.load(); return; }
    this.busyId.set(day.id);
    this.svc.update(day.id, {segment}).subscribe({
      next: u => { this.applyUpdated(u); this.busyId.set(null); this.toastr.success('時段已更新'); },
      error: (e: HttpErrorResponse) => this.onError(e, '更新失敗'),
    });
  }

  async cancel(day: DisasterDayOff): Promise<void> {
    if (this.busyId() !== null) return;
    const past = this.isPastMonth(day.date);
    if (!past) {
      const ref = this.modal.open(ConfirmModal, {centered: true});
      const ci = ref.componentInstance as ConfirmModal;
      ci.title = '取消颱風假';
      ci.message = `確定取消 ${day.date} ${day.countyName} 的颱風假嗎？`;
      ci.detail = '取消後該縣市員工恢復正常出勤要求，被退回的請假時數會重新計入。手動取消的同日同縣市不會再被自動抓取重建。';
      ci.tone = 'warning';
      ci.confirmText = '取消颱風假';
      try { await ref.result; } catch { return; }
    } else if (!(await this.confirmIfPast(day, '取消颱風假'))) {
      return;
    }
    this.busyId.set(day.id);
    this.svc.cancel(day.id).subscribe({
      next: u => { this.applyUpdated(u); this.busyId.set(null); this.toastr.success('已取消'); },
      error: (e: HttpErrorResponse) => this.onError(e, '取消失敗'),
    });
  }

  openAffected(day: DisasterDayOff): void {
    const ref = this.modal.open(DisasterDayAffectedModal, {size: 'lg', scrollable: true});
    (ref.componentInstance as DisasterDayAffectedModal).day = day;
  }

  async openAdd(): Promise<void> {
    const ref = this.modal.open(DisasterDayAddModal, {size: 'lg', scrollable: true});
    try {
      const rows: DisasterDayOff[] = await ref.result;
      this.toastr.success(`已建立 ${rows.length} 筆颱風假`);
      this.load();
    } catch { /* 取消 */ }
  }

  fetchNow(): void {
    if (this.fetching()) return;
    this.fetching.set(true);
    this.svc.fetchNow().subscribe({
      next: log => {
        this.fetching.set(false);
        if (log.success) this.toastr.success(log.summary || '抓取完成');
        else this.toastr.warning(log.summary || '抓取失敗');
        this.load();
        this.loadLogs();
      },
      error: (e: HttpErrorResponse) => {
        this.fetching.set(false);
        this.toastr.error(e.error?.message || '抓取失敗');
      },
    });
  }
}
