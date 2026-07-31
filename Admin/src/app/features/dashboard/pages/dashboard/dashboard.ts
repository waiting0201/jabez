import {Component, computed, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AuthService} from '@core/auth/services/auth.service';
import {AttendanceService} from '../../services/attendance.service';
import {LineQuotaService} from '../../services/line-quota.service';
import {LineQuota} from '../../models/line-quota.model';
import {OvertimeRequestService} from '@features/admin/overtime-requests/services/overtime-request.service';
import {OvertimeRequest} from '@features/admin/overtime-requests/models/overtime-request.model';
import {TodayAttendance, ClockActionType, ActiveLeave} from '../../models/attendance.model';

const LEAVE_TYPE_LABELS: Record<string, string> = {
  annual: '特休假', personal: '事假', sick: '病假', compensatory: '補休',
  official: '公假', marriage: '婚假', maternity: '產假',
  miscarriage_3m: '流產假(3個月以上)', miscarriage_2to3m: '流產假(2-3個月)',
  miscarriage_under2m: '流產假(未滿2個月)', prenatal_checkup: '產檢假',
  paternity: '陪產假', bereavement: '喪假',
  ceremonial_festival: '歲時祭儀假', senior_executive: '高階主管假',
};

const DAY_NAMES = ['日', '一', '二', '三', '四', '五', '六'];

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  imports: [DatePipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard implements OnInit, OnDestroy {
  private auth = inject(AuthService);
  private attendanceService = inject(AttendanceService);
  private overtimeService = inject(OvertimeRequestService);
  private lineQuotaService = inject(LineQuotaService);

  private timerId: ReturnType<typeof setInterval> | null = null;

  /** Real-time clock signal updated every second */
  now = signal(new Date());

  /** Today's attendance record */
  todayRecord = signal<TodayAttendance | null>(null);

  /** Approved overtime requests available for today */
  approvedRequests = signal<OvertimeRequest[]>([]);

  /** Selected overtime request ID for overtime start */
  selectedOvertimeId = signal<number | null>(null);

  /** Whether the overtime request selector is visible (shown on "加班開始" click) */
  showOvertimeSelector = signal(false);

  /** GPS state */
  gpsStatus = signal<'idle' | 'locating' | 'success' | 'failed'>('idle');
  gpsCoords = signal<{lat: number; lng: number} | null>(null);

  /** Loading state for clock actions */
  loading = signal(false);

  /** Toast message */
  toast = signal<{message: string; type: 'success' | 'warning' | 'error'} | null>(null);

  /** LINE 推播用量（needs line-quota:read permission to load） */
  lineQuota = signal<LineQuota | null>(null);
  /** 用量查詢失敗（LINE API 不可用 / Token 無效），給卡片顯示提示用 */
  lineQuotaFailed = signal(false);

  /** 是否有權限看 LINE 用量卡片（line-quota:read 或 superadmin） */
  canViewLineQuota = computed(() => this.auth.hasPermission('line-quota:read'));

  /** User display name */
  userName = computed(() => this.auth.currentUser()?.name ?? '使用者');

  /** Formatted date: yyyy/MM/dd 星期X */
  dateDisplay = computed(() => {
    const d = this.now();
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}/${mm}/${dd} 星期${DAY_NAMES[d.getDay()]}`;
  });

  /** Formatted time: HH:mm:ss */
  timeDisplay = computed(() => {
    const d = this.now();
    return [d.getHours(), d.getMinutes(), d.getSeconds()]
      .map(n => String(n).padStart(2, '0'))
      .join(':');
  });

  /**
   * 目前是否落在某個已核准請假時段內（[startDate, endDate) 半開區間）。
   * 依賴 now signal（每秒更新），請假時段切換時 computed 會自動重算。
   */
  currentLeave = computed<ActiveLeave | null>(() => {
    const r = this.todayRecord();
    if (!r?.todayLeaves?.length) return null;
    const nowMs = this.now().getTime();
    return r.todayLeaves.find(lv => {
      const start = new Date(lv.startDate).getTime();
      const end   = new Date(lv.endDate).getTime();
      return start <= nowMs && nowMs < end;
    }) ?? null;
  });

  /** Button enable states */
  canClockIn = computed(() => {
    const r = this.todayRecord();
    return !r?.clockInTime && !this.loading() && !this.currentLeave();
  });

  canClockOut = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockInTime && !r?.clockOutTime && !this.loading() && !this.currentLeave();
  });

  canOvertimeStart = computed(() => {
    const r = this.todayRecord();
    if (this.loading() || r?.overtimeStartTime) return false;
    if (this.approvedRequests().length === 0) return false;
    // 一般上班日須先打下班卡；休假日 / 全日請假由後端旗標豁免
    return !!r?.clockOutTime || !!r?.canOvertimeWithoutClockOut;
  });

  /** 加班開始 disabled 時的原因提示（比照上下班按鈕的 [title] 做法） */
  overtimeStartHint = computed<string>(() => {
    const r = this.todayRecord();
    if (r?.overtimeStartTime) return '今日已打加班開始卡';
    if (this.approvedRequests().length === 0) return '今日無已核准的加班申請單';
    if (!r?.clockOutTime && !r?.canOvertimeWithoutClockOut) return '請先打下班卡（今日為上班日）';
    return '';
  });

  /** 今日免下班卡即可打加班卡（休假日 / 全日請假），且手上有已核准加班單且尚未打卡 */
  overtimeExemptNotice = computed(() => {
    const r = this.todayRecord();
    return !!r?.canOvertimeWithoutClockOut
      && this.approvedRequests().length > 0
      && !r?.overtimeStartTime;
  });

  canOvertimeEnd = computed(() => {
    const r = this.todayRecord();
    return !!r?.overtimeStartTime && !r?.overtimeEndTime && !this.loading();
  });

  /** 用量百分比（type=limited 才有意義；夾在 0~100 避免極端值衝破進度條） */
  usagePercent = computed<number>(() => {
    const q = this.lineQuota();
    if (!q || q.type !== 'limited' || !q.limit) return 0;
    return Math.min(100, Math.round((q.used / q.limit) * 100));
  });

  /** 進度條色塊：< 70% 綠，70~89% 黃，≥ 90% 紅 */
  quotaWarningClass = computed<string>(() => {
    const p = this.usagePercent();
    if (p >= 90) return 'bg-danger';
    if (p >= 70) return 'bg-warning';
    return 'bg-success';
  });

  leaveTypeLabel(type: string): string {
    return LEAVE_TYPE_LABELS[type] ?? type;
  }

  ngOnInit() {
    this.timerId = setInterval(() => this.now.set(new Date()), 1000);

    this.attendanceService.getToday().subscribe(r => {
      // 後端永遠回傳非 null（即使無打卡紀錄也會回傳含 todayLeaves 的空殼 DTO）
      if (r) this.todayRecord.set(r);
    });

    this.overtimeService.getApprovedForToday().subscribe(list => {
      this.approvedRequests.set(list);
      if (list.length > 0) this.selectedOvertimeId.set(list[0].id);
    });

    // 有權限才呼叫，避免一般員工觸發 403（router 守門）
    if (this.canViewLineQuota()) {
      this.lineQuotaService.getQuota().subscribe({
        next: q => this.lineQuota.set(q),
        error: () => this.lineQuotaFailed.set(true),
      });
    }
  }

  ngOnDestroy() {
    if (this.timerId) clearInterval(this.timerId);
  }

  onOvertimeSelect(event: Event) {
    const val = (event.target as HTMLSelectElement).value;
    this.selectedOvertimeId.set(val ? +val : null);
  }

  formatTime(isoString?: string): string {
    if (!isoString) return '--:--';
    // 直接從 ISO 字串解析時間，避免 new Date() 時區轉換問題
    const match = isoString.match(/T(\d{2}):(\d{2})/);
    if (!match) return '--:--';
    return `${match[1]}:${match[2]}`;
  }

  /** 加班單下拉的專案標籤（多案以逗號串接；舊單可能無關聯專案） */
  projectLabel(req: OvertimeRequest): string {
    return req.projects?.length ? req.projects.map(p => p.projectCode).join(', ') : '無專案';
  }

  /** 點擊加班開始 → 先顯示選擇器 */
  onOvertimeStartClick() {
    if (this.approvedRequests().length === 0) return;
    if (!this.selectedOvertimeId()) {
      this.selectedOvertimeId.set(this.approvedRequests()[0].id);
    }
    this.showOvertimeSelector.set(true);
  }

  /** 選擇器中確認 → 執行打卡 */
  confirmOvertimeStart() {
    this.showOvertimeSelector.set(false);
    this.performAction('overtime-start');
  }

  cancelOvertimeSelector() {
    this.showOvertimeSelector.set(false);
  }

  /** Perform a clock action: get GPS → call service → update state */
  performAction(type: ClockActionType) {
    if (this.loading()) return;
    this.loading.set(true);
    this.gpsStatus.set('locating');

    this._getGps().then(coords => {
      this.gpsCoords.set(coords);
      this.gpsStatus.set(coords ? 'success' : 'failed');

      const body = {
        latitude: coords?.lat,
        longitude: coords?.lng,
        overtimeRequestId: type === 'overtime-start' ? (this.selectedOvertimeId() ?? undefined) : undefined,
      };

      let obs$;
      switch (type) {
        case 'clock-in':       obs$ = this.attendanceService.clockIn(body); break;
        case 'clock-out':      obs$ = this.attendanceService.clockOut(body); break;
        case 'overtime-start': obs$ = this.attendanceService.overtimeStart(body); break;
        case 'overtime-end':   obs$ = this.attendanceService.overtimeEnd(body); break;
      }

      obs$.subscribe({
        next: record => {
          this.todayRecord.set(record);
          this.loading.set(false);
          const labels: Record<ClockActionType, string> = {
            'clock-in': '上班打卡', 'clock-out': '下班打卡',
            'overtime-start': '加班開始', 'overtime-end': '加班結束',
          };
          this.showToast(`${labels[type]}成功！`, coords ? 'success' : 'warning');
        },
        error: (err) => {
          this.loading.set(false);
          // 後端 ApiResponse.Fail 在 ExceptionMiddleware 包成 { success:false, message, errors } 結構
          const message = err?.error?.message ?? err?.message ?? '打卡失敗，請稍後重試';
          this.showToast(message, 'error');
        },
      });
    });
  }

  showToast(message: string, type: 'success' | 'warning' | 'error') {
    this.toast.set({message, type});
    setTimeout(() => this.toast.set(null), 3000);
  }

  private _getGps(): Promise<{lat: number; lng: number} | null> {
    return new Promise(resolve => {
      if (!navigator.geolocation) {
        resolve(null);
        return;
      }
      navigator.geolocation.getCurrentPosition(
        pos => resolve({lat: pos.coords.latitude, lng: pos.coords.longitude}),
        () => resolve(null),
        {enableHighAccuracy: true, timeout: 8000, maximumAge: 0}
      );
    });
  }
}
