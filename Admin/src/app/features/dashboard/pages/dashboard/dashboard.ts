import {Component, computed, inject, signal, OnInit, OnDestroy, ChangeDetectionStrategy, ElementRef, viewChild} from '@angular/core';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {ConfirmModal, ConfirmModalResult} from '@/app/shared/components/confirm-modal';
import {DatePipe, DecimalPipe} from '@angular/common';
import {firstValueFrom, Observable} from 'rxjs';
import {AuthService} from '@core/auth/services/auth.service';
import {AttendanceService} from '../../services/attendance.service';
import {LineQuotaService} from '../../services/line-quota.service';
import {TurnstileService} from '@/app/shared/services/turnstile.service';
import {LineQuota} from '../../models/line-quota.model';
import {OvertimeRequestService} from '@features/admin/overtime-requests/services/overtime-request.service';
import {OvertimeRequest} from '@features/admin/overtime-requests/models/overtime-request.model';
import {TodayAttendance, ClockActionType, ActiveLeave, SHIFT_DAY_TYPE_LABELS} from '../../models/attendance.model';
import {LEAVE_TYPE_LABELS} from '@features/admin/leave-requests/models/leave-request.model';

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
  private modal = inject(NgbModal);
  private overtimeService = inject(OvertimeRequestService);
  private lineQuotaService = inject(LineQuotaService);
  private turnstile = inject(TurnstileService);

  /** Cloudflare Turnstile 容器：平常為空、看不到；Cloudflare 起疑時才浮出勾選框 */
  private turnstileBox = viewChild<ElementRef<HTMLElement>>('turnstileBox');

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

  /**
   * 本次打卡是否為出差（整天一個旗標）。
   * 初始值由 /attendances/today 帶回，故已標記出差的當日再次打卡不會被誤清。
   */
  isBusinessTrip = signal(false);

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
    // canClockInOut 是後端依當日日別算好的旗標（例假全鎖 / 休假鎖 / 國定假日僅活動日預定人力解鎖）。
    // 前端不自行重組規則，只吃旗標 —— 規則變動時只要改後端一處。
    return !r?.clockInTime && !this.loading() && !this.currentLeave() && this.dayAllowsClock();
  });

  canClockOut = computed(() => {
    const r = this.todayRecord();
    return !!r?.clockInTime && !r?.clockOutTime && !this.loading() && !this.currentLeave() && this.dayAllowsClock();
  });

  /** 當日日別是否允許上下班打卡（舊制恆 true） */
  private dayAllowsClock = computed(() => this.todayRecord()?.canClockInOut !== false);

  /** 今日日別的中文標籤（新制才有） */
  dayTypeLabel = computed(() => {
    const r = this.todayRecord();
    return r?.flexibleEnabled && r.dayType ? SHIFT_DAY_TYPE_LABELS[r.dayType] : '';
  });

  /** 不可打卡時的說明（來自後端） */
  clockLockReason = computed(() => this.todayRecord()?.clockLockReason ?? '');

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
    return (LEAVE_TYPE_LABELS as Record<string, string>)[type] ?? type;
  }

  ngOnInit() {
    this.timerId = setInterval(() => this.now.set(new Date()), 1000);

    this.attendanceService.getToday().subscribe(r => {
      // 後端永遠回傳非 null（即使無打卡紀錄也會回傳含 todayLeaves 的空殼 DTO）
      if (r) this.applyTodayRecord(r);
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

  /**
   * 上班打卡：超過遲到界線（後端 `lateAfter`：公司 09:30、自訂上下班時段 S+2 分、請上午半天假者 13:05）
   * 時先跳對話框填遲到原因，出差當日改為非必填；未遲到直接打卡（不增加一般情況的操作步驟）。
   * 舊制（尚未切換）一律直接打卡。
   */
  async confirmAndClockIn(): Promise<void> {
    const r = this.todayRecord();
    const lateAfter = r?.flexibleEnabled && r.lateAfter ? new Date(r.lateAfter) : null;
    const now = new Date();
    if (!lateAfter || now <= lateAfter) {
      this.performAction('clock-in');
      return;
    }

    const lateMin = Math.max(1, Math.round((now.getTime() - lateAfter.getTime()) / 60000));
    const ref = this.modal.open(ConfirmModal, {centered: true});
    const ci = ref.componentInstance as ConfirmModal;
    ci.title = '確認上班打卡';
    ci.message = '今日上班打卡已逾時';
    ci.confirmText = '確定打卡';
    ci.tone = 'warning';
    ci.detail = `已超過上班準時時間（${this.timeText(lateAfter)}）${lateMin} 分鐘，將記為遲到。`;
    ci.reasonLabel = '遲到原因';
    ci.reasonRequired = !this.isBusinessTrip();

    let result: ConfirmModalResult;
    try {
      result = await ref.result;            // 按「取消」會 reject → 不產生任何紀錄
    } catch {
      return;
    }
    this.performAction('clock-in', result.reason);
  }

  /**
   * 下班打卡一律先跳確認對話框（防誤觸），早退／逾時的必填原因**併入同一個視窗**
   * （規格明訂不另開第二個視窗）。三種情況以應下班時間 T 為界，互斥且涵蓋全部：
   * `< T` 早退必填原因、`[T, T+30分]` 正常只需確認、`> T+30分` 逾時必填原因
   * （自訂上下班時段者 T 為固定下班時刻、容許帶 5 分，界線皆由後端 `normalClockOutUntil` 帶回）。
   * 出差當日原因欄位仍顯示但改為非必填。
   *
   * 舊制（尚未切換）不跳對話框，行為與原本完全一致。
   */
  async confirmAndClockOut(): Promise<void> {
    const r = this.todayRecord();
    if (!r?.flexibleEnabled || !r.clockInTime) {
      this.performAction('clock-out');
      return;
    }

    const now = new Date();
    const clockIn = new Date(r.clockInTime);
    const worked = Math.max(0, now.getTime() - clockIn.getTime());
    const workedText = `${Math.floor(worked / 3600000)} 小時 ${Math.floor((worked % 3600000) / 60000)} 分`;

    const expected = r.expectedClockOutTime ? new Date(r.expectedClockOutTime) : null;
    const diffMin = expected ? Math.round((expected.getTime() - now.getTime()) / 60000) : 0;
    const isEarly = !!expected && now < expected;
    // 逾時界線由後端依個人打卡參數算好（公司預設 +30 分、自訂上下班時段 +5 分），不在前端寫死
    const normalUntil = r.normalClockOutUntil ? new Date(r.normalClockOutUntil) : null;
    const isOvertime = !!normalUntil && now > normalUntil;
    const businessTrip = this.isBusinessTrip();

    const ref = this.modal.open(ConfirmModal, {centered: true});
    const ci = ref.componentInstance as ConfirmModal;
    ci.title = '確認下班打卡';
    ci.message = `目前出勤 ${workedText}`;
    ci.confirmText = '確定打卡';

    if (isEarly) {
      ci.tone = 'warning';
      ci.detail = `今日出勤未達應下班時間（${this.timeText(expected!)}），尚差 ${diffMin} 分鐘。`;
      ci.reasonLabel = '早退原因';
      ci.reasonRequired = !businessTrip;
    } else if (isOvertime) {
      ci.tone = 'warning';
      ci.detail = `已超過應下班時間（${this.timeText(expected!)}）${Math.abs(diffMin)} 分鐘。`;
      ci.reasonLabel = '逾時原因';
      ci.reasonRequired = !businessTrip;
    } else if (expected) {
      ci.detail = `應下班時間 ${this.timeText(expected)}，屬正常下班。`;
    }

    let result: ConfirmModalResult;
    try {
      result = await ref.result;            // 按「取消」會 reject → 不產生任何紀錄
    } catch {
      return;
    }
    this.performAction('clock-out', result.reason);
  }

  private timeText(d: Date): string {
    return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  }

  /**
   * 打卡：同時「取得 GPS」「向後端取一次性挑戰碼」「取得 Turnstile token」→ 不足挑戰碼最短停留時間則補等 → 送出。
   * 防機器人打卡（後端 AttendancePunchGuard）：沒有 GPS 或挑戰碼不合規一律擋下，
   * 前端先擋 GPS 是為了給明確指引，不是唯一防線。Turnstile token 取不到時送 null，放不放行由後端模式決定。
   */
  async performAction(type: ClockActionType, reason: string | null = null) {
    if (this.loading()) return;
    this.loading.set(true);
    this.gpsStatus.set('locating');

    try {
      const [coords, challenge, turnstileToken] = await Promise.all([
        this._getGps(),
        firstValueFrom(this.attendanceService.getChallenge(type)),
        this.turnstile.getToken(this.turnstileBox()?.nativeElement, type),
      ]);
      const challengeReceivedAt = Date.now();

      this.gpsCoords.set(coords);
      this.gpsStatus.set(coords ? 'success' : 'failed');
      if (!coords) {
        this.showToast('無法取得定位，請開啟手機／瀏覽器的定位權限後再打卡。', 'error');
        return;
      }

      // 挑戰碼簽發後須停留 minWaitMs；以「收到回應」起算並多留緩衝，避免與後端時間差擦邊被擋
      const waitMs = challenge.minWaitMs + 300 - (Date.now() - challengeReceivedAt);
      if (waitMs > 0) await new Promise(resolve => setTimeout(resolve, waitMs));

      const body = {
        latitude: coords.lat,
        longitude: coords.lng,
        accuracy: coords.accuracy,
        overtimeRequestId: type === 'overtime-start' ? (this.selectedOvertimeId() ?? undefined) : undefined,
        isBusinessTrip: this.isBusinessTrip(),
        reason: type === 'clock-in' || type === 'clock-out' ? reason : undefined,
        challengeToken: challenge.token,
        turnstileToken: turnstileToken ?? undefined,
      };

      let obs$: Observable<TodayAttendance>;
      switch (type) {
        case 'clock-in':       obs$ = this.attendanceService.clockIn(body); break;
        case 'clock-out':      obs$ = this.attendanceService.clockOut(body); break;
        case 'overtime-start': obs$ = this.attendanceService.overtimeStart(body); break;
        case 'overtime-end':   obs$ = this.attendanceService.overtimeEnd(body); break;
      }

      const record = await firstValueFrom(obs$);
      this.applyTodayRecord(record);
      const labels: Record<ClockActionType, string> = {
        'clock-in': '上班打卡', 'clock-out': '下班打卡',
        'overtime-start': '加班開始', 'overtime-end': '加班結束',
      };
      this.showToast(`${labels[type]}成功！`, 'success');
    } catch (err: any) {
      // 後端 ApiResponse.Fail 在 ExceptionMiddleware 包成 { success:false, message, errors } 結構
      const message = err?.error?.message ?? err?.message ?? '打卡失敗，請稍後重試';
      this.showToast(message, 'error');
    } finally {
      this.loading.set(false);
    }
  }

  /** 套用今日打卡紀錄，並把出差勾選框同步回後端的當日狀態（單一真相為後端紀錄） */
  private applyTodayRecord(record: TodayAttendance) {
    this.todayRecord.set(record);
    this.isBusinessTrip.set(!!record.isBusinessTrip);
  }

  showToast(message: string, type: 'success' | 'warning' | 'error') {
    this.toast.set({message, type});
    setTimeout(() => this.toast.set(null), 3000);
  }

  private _getGps(): Promise<{lat: number; lng: number; accuracy: number} | null> {
    return new Promise(resolve => {
      if (!navigator.geolocation) {
        resolve(null);
        return;
      }
      navigator.geolocation.getCurrentPosition(
        pos => resolve({lat: pos.coords.latitude, lng: pos.coords.longitude, accuracy: pos.coords.accuracy}),
        () => resolve(null),
        {enableHighAccuracy: true, timeout: 8000, maximumAge: 0}
      );
    });
  }
}
