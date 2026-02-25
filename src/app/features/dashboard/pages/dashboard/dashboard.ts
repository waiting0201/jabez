import {Component, computed, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy} from '@angular/core';
import {DatePipe, DecimalPipe} from '@angular/common';
import {AuthService} from '@core/auth/services/auth.service';
import {AttendanceService} from '../../services/attendance.service';
import {OvertimeRequestService} from '@features/admin/overtime-requests/services/overtime-request.service';
import {OvertimeRequest} from '@features/admin/overtime-requests/models/overtime-request.model';
import {TodayAttendance, ClockActionType} from '../../models/attendance.model';

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

  /** Button enable states */
  canClockIn = computed(() => {
    const r = this.todayRecord();
    return !r?.clockInTime && !this.loading();
  });

  canClockOut = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockInTime && !r?.clockOutTime && !this.loading();
  });

  canOvertimeStart = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockOutTime && !r?.overtimeStartTime
      && this.approvedRequests().length > 0 && !this.loading();
  });

  canOvertimeEnd = computed(() => {
    const r = this.todayRecord();
    return !!r?.overtimeStartTime && !r?.overtimeEndTime && !this.loading();
  });

  ngOnInit() {
    this.timerId = setInterval(() => this.now.set(new Date()), 1000);

    this.attendanceService.getToday().subscribe(r => {
      if (r) this.todayRecord.set(r);
    });

    this.overtimeService.getApprovedForToday().subscribe(list => {
      this.approvedRequests.set(list);
      if (list.length > 0) this.selectedOvertimeId.set(list[0].id);
    });
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
    const d = new Date(isoString);
    return [d.getHours(), d.getMinutes()].map(n => String(n).padStart(2, '0')).join(':');
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
        error: () => {
          this.loading.set(false);
          this.showToast('打卡失敗，請稍後重試', 'error');
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
