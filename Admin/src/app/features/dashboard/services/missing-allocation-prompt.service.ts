import {Injectable, inject} from '@angular/core';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ToastrService} from 'ngx-toastr';
import {firstValueFrom} from 'rxjs';
import {AuthService} from '@core/auth/services/auth.service';
import {ClockOutModal, ClockOutModalResult} from '../components/clock-out-modal/clock-out-modal';
import {MissingAllocationDay} from '../models/attendance.model';
import {AttendanceService} from './attendance.service';

const WEEKDAYS = ['日', '一', '二', '三', '四', '五', '六'];

/**
 * 進入系統時強制補填「系統補下班卡」日子的專案工時佔比。
 * 登入時 AttendanceAutoClockService 補的下班卡沒經過下班打卡視窗，工時總表會列「未分配」；
 * 這裡逐日跳出無法關閉的 ClockOutModal，填完才放行。
 *
 * - 查清單失敗一律放行（安全側：不因一次網路錯誤讓整站卡死）。
 * - 送出失敗則 toastr 後重開同一天，不放行。
 * - in-flight 旗標避免重複開窗（MainLayout 重建、重複呼叫）。
 */
@Injectable({providedIn: 'root'})
export class MissingAllocationPromptService {
  private attendanceService = inject(AttendanceService);
  private auth = inject(AuthService);
  private modal = inject(NgbModal);
  private toastr = inject(ToastrService);

  private running = false;

  async check(): Promise<void> {
    if (this.running || !this.auth.hasPermission('attendances:write')) return;
    this.running = true;
    try {
      let days: MissingAllocationDay[];
      try {
        days = await firstValueFrom(this.attendanceService.getMissingAllocations());
      } catch {
        return;
      }

      while (days.length > 0) {
        const day = days[0];
        let result: ClockOutModalResult;
        try {
          result = await this.prompt(day, days.length - 1);
        } catch {
          return;   // 只有「沒有可選專案」時視窗才關得掉，此時放行、下次進系統再問
        }
        const ids = result.applyToRest ? days.map(d => d.id) : [day.id];
        try {
          days = await firstValueFrom(this.attendanceService.fillMissingAllocations(ids, result.allocations));
          this.toastr.success(result.applyToRest ? `已補填 ${ids.length} 天的專案工時` : `${this.dateText(day.recordDate)} 專案工時已補填`);
        } catch (err: any) {
          this.toastr.error(err?.error?.message ?? err?.message ?? '補填失敗，請稍後重試');
          // 不放行：重新取一次清單（可能已被其他分頁補完），取不到就沿用原清單重開同一天
          try {
            days = await firstValueFrom(this.attendanceService.getMissingAllocations());
          } catch { /* 沿用原清單 */ }
        }
      }
    } finally {
      this.running = false;
    }
  }

  private async prompt(day: MissingAllocationDay, restCount: number): Promise<ClockOutModalResult> {
    const ref = this.modal.open(ClockOutModal, {centered: true, size: 'lg', backdrop: 'static', keyboard: false});
    const ci = ref.componentInstance as ClockOutModal;
    const date = this.dateText(day.recordDate);
    ci.title = '補填專案工時';
    ci.tone = 'warning';
    ci.forced = true;
    ci.message = `${date} 您沒有打下班卡，系統已自動補上（上班 ${this.timeText(day.clockInTime)}／下班 ${this.timeText(day.clockOutTime)}）。`;
    ci.detail = '請填寫當日各專案的工時佔比，填完才能繼續使用系統。';
    ci.allocationLabel = `${date} 專案工時佔比`;
    ci.confirmText = '送出';
    ci.applyToRestCount = restCount;
    return await ref.result;
  }

  /** 2026-10-15 → 10/15（三） */
  private dateText(ymd: string): string {
    const [y, m, d] = ymd.split('-').map(Number);
    return `${m}/${d}（${WEEKDAYS[new Date(y, m - 1, d).getDay()]}）`;
  }

  /** 後端為台北 naive 時間字串，直接取 HH:mm 避免時區換算 */
  private timeText(t: string | null): string {
    return t ? t.substring(11, 16) : '—';
  }
}
