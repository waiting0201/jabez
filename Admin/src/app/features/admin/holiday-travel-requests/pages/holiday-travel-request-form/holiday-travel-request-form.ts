import {ChangeDetectorRef, Component, inject, OnInit, signal, TemplateRef, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {HolidayTravelRequestService} from '../../services/holiday-travel-request.service';
import {ProjectService} from '../../../projects/services/project.service';
import {Project} from '../../../projects/models/project.model';
import {
  ApprovalStatus,
  APPROVAL_STATUS_LABELS,
  APPROVAL_STATUS_CLASSES,
  DesignatedReviewer,
  HolidayTravelRequest,
  TravelParticipant,
} from '../../models/holiday-travel-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {ApprovalTaskService} from '../../../approval-tasks/services/approval-task.service';
import {ApprovalFlow, ApprovalRecord} from '../../../approval-tasks/models/approval-task.model';
import {ApprovalTimeline} from '../../../../../shared/components/approval-timeline';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-holiday-travel-request-form',
  templateUrl: './holiday-travel-request-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, ApprovalTimeline, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class HolidayTravelRequestForm implements OnInit {
  private fb          = inject(FormBuilder);
  private service     = inject(HolidayTravelRequestService);
  private projects$   = inject(ProjectService);
  private jobTitleSvc = inject(JobTitleService);
  private userSvc     = inject(UserService);
  private approvalSvc = inject(ApprovalService);
  private taskSvc     = inject(ApprovalTaskService);
  private deptSvc     = inject(DepartmentService);
  private route       = inject(ActivatedRoute);
  private router      = inject(Router);
  private cdr         = inject(ChangeDetectorRef);
  private modal       = inject(NgbModal);

  successModal = viewChild<TemplateRef<any>>('successModal');

  isEdit     = false;
  requestId  = 0;
  isReadOnly = false;
  isReturned = false;
  isDraft    = true;
  approvalStatus: ApprovalStatus = 'draft';
  existingRequest: HolidayTravelRequest | null = null;
  errorMsg = signal('');
  projects: Project[] = [];
  loadingProjects = true;

  /** 假日天數（從行事曆 API 查詢） */
  holidayDays = signal<number | null>(null);
  holidayDaysLoading = signal(false);
  holidayDaysNoCalendar = signal(false);

  /** 活動期間內的假日日期集合（yyyy-MM-dd，供參與日期 chips 標示） */
  holidayDateSet = signal<Set<string>>(new Set());
  /** 參與日期 chips（依活動期間逐日產生） */
  dayChips = signal<{date: string; label: string; isHoliday: boolean}[]>([]);
  /** 活動期間超過上限（92 天）時停用逐日勾選，所有人員視為全程參與 */
  chipsTooLong = signal(false);

  /** 簽核流程時間軸 */
  approvalFlow: ApprovalFlow | null = null;
  approvalRecords: ApprovalRecord[] = [];
  taskCurrentStepOrder = 0;
  taskStatus = '';

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
  departments: Department[] = [];
  /** 編輯回填給 picker 的 initial（含 approvalStepOrder / selectedDepartmentId） */
  pickerInitial: DesignatedReviewer[] = [];
  /** 唯讀模式下顯示的已指定審核者 */
  readonlyDesignatedReviewers: DesignatedReviewer[] = [];
  /** picker 每次 change 後存放最新 payload，送出時使用 */
  private _pickerPayload: DesignatedReviewerPayload[] = [];
  /** 被抑制（部門最高層級 → 自動略過）的指定步驟 stepOrder，驗證時排除 */
  private _suppressedSteps: number[] = [];

  /** 參與執行人員清單（selectedDates 空陣列＝全程參與） */
  participantEntries: {
    sortOrder: number;
    selectedUserId: string | null;
    selectedDates: string[];
  }[] = [];

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;

  form = this.fb.group({
    destination: ['', Validators.required],
    startDate:   ['', Validators.required],
    endDate:     ['', Validators.required],
    purpose:     ['', Validators.required],
    projectId:   [null as number | null],
  });

  /** 按鈕 disabled 時的提示訊息，null 表示可提交 */
  get disabledReason(): string | null {
    if (this.form.invalid) {
      const fields: [string, string][] = [
        ['destination', '執行活動地點'], ['startDate', '開始日期'],
        ['endDate', '結束日期'], ['purpose', '活動主旨及內容'],
      ];
      for (const [key, label] of fields) {
        if (this.form.get(key)?.invalid) return `請填寫「${label}」。`;
      }
      return '表單資料不完整，請檢查必填欄位。';
    }
    return null;
  }

  /** 日期變更時查詢假日天數，並重建參與日期 chips */
  onDateChange() {
    const v = this.form.value;
    if (!v.startDate || !v.endDate) {
      this.holidayDays.set(null);
      this.holidayDateSet.set(new Set());
      this.rebuildDayChips();
      return;
    }
    this.holidayDaysLoading.set(true);
    this.holidayDaysNoCalendar.set(false);
    this.service.countHolidays(v.startDate, v.endDate).subscribe({
      next: res => {
        this.holidayDays.set(res.holidayDays);
        this.holidayDaysNoCalendar.set(!res.hasCalendarData);
        this.holidayDaysLoading.set(false);
        this.holidayDateSet.set(new Set(res.holidayDates ?? []));
        this.rebuildDayChips();
      },
      error: () => {
        this.holidayDays.set(null);
        this.holidayDaysLoading.set(false);
        this.holidayDateSet.set(new Set());
        this.rebuildDayChips();
      },
    });
  }

  /** 依活動期間逐日產生 chips，並剪除各參與人員落出期間的已勾日期 */
  private rebuildDayChips() {
    const v = this.form.value;
    if (!v.startDate || !v.endDate) {
      this.dayChips.set([]);
      this.chipsTooLong.set(false);
      return;
    }
    const start = new Date(v.startDate + 'T00:00:00');
    const end   = new Date(v.endDate + 'T00:00:00');
    if (isNaN(start.getTime()) || isNaN(end.getTime()) || end < start) {
      this.dayChips.set([]);
      this.chipsTooLong.set(false);
      return;
    }
    const days = Math.round((end.getTime() - start.getTime()) / 86400000) + 1;
    if (days > 92) {
      this.chipsTooLong.set(true);
      this.dayChips.set([]);
      this.participantEntries.forEach(e => e.selectedDates = []);
      return;
    }
    this.chipsTooLong.set(false);
    const weekdays = ['日', '一', '二', '三', '四', '五', '六'];
    const holidaySet = this.holidayDateSet();
    const chips: {date: string; label: string; isHoliday: boolean}[] = [];
    for (let i = 0; i < days; i++) {
      const d = new Date(start.getTime() + i * 86400000);
      const iso = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      chips.push({date: iso, label: `${d.getMonth() + 1}/${d.getDate()}(${weekdays[d.getDay()]})`, isHoliday: holidaySet.has(iso)});
    }
    this.dayChips.set(chips);
    const valid = new Set(chips.map(c => c.date));
    this.participantEntries.forEach(e => e.selectedDates = e.selectedDates.filter(d => valid.has(d)));
  }

  /** 勾選/取消勾選參與日期 */
  toggleDate(entry: {selectedDates: string[]}, date: string) {
    const idx = entry.selectedDates.indexOf(date);
    if (idx >= 0) entry.selectedDates.splice(idx, 1);
    else {
      entry.selectedDates.push(date);
      entry.selectedDates.sort();
    }
  }

  isDateSelected(entry: {selectedDates: string[]}, date: string): boolean {
    return entry.selectedDates.includes(date);
  }

  /** 該列摘要：已選 N 天（假日 M 天）/ 全程參與（假日 X 天） */
  participantSummary(entry: {selectedDates: string[]}): string {
    if (entry.selectedDates.length === 0) {
      const total = this.holidayDays();
      return total !== null ? `全程參與（假日 ${total} 天）` : '全程參與';
    }
    if (this.holidayDaysNoCalendar()) return `已選 ${entry.selectedDates.length} 天`;
    const holidaySet = this.holidayDateSet();
    const holidayCount = entry.selectedDates.filter(d => holidaySet.has(d)).length;
    return `已選 ${entry.selectedDates.length} 天（假日 ${holidayCount} 天）`;
  }

  /** 唯讀顯示：M/d 格式日期清單（頓號分隔） */
  formatDates(entry: {selectedDates: string[]}): string {
    return entry.selectedDates
      .map(date => {
        const [, m, d] = date.split('-');
        return `${+m}/${+d}`;
      })
      .join('、');
  }

  // ── 指定審核者操作 ──

  /** picker change 事件：每次使用者操作時更新最新 payload */
  onPickerChange(payload: DesignatedReviewerPayload[]) {
    this._pickerPayload = payload;
  }

  /** picker 回報被抑制（部門最高層級 → 自動略過）的指定步驟 */
  onSuppressedSteps(stepOrders: number[]) {
    this._suppressedSteps = stepOrders;
  }

  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

  // ── 參與執行人員操作 ──

  addParticipant() {
    const nextOrder = this.participantEntries.length + 1;
    this.participantEntries.push({sortOrder: nextOrder, selectedUserId: null, selectedDates: []});
  }

  removeParticipant(i: number) {
    this.participantEntries.splice(i, 1);
    this.participantEntries.forEach((e, idx) => e.sortOrder = idx + 1);
  }

  ngOnInit() {
    // 載入使用者清單（用於指定審核者與參與執行人員）
    this.userSvc.getLookup().subscribe({
      next: users => {
        this.allUsers = users;
        this.cdr.markForCheck();
      },
    });

    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('holiday_travel').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); } });
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().subscribe({ next: d => { this.departments = d; this.cdr.markForCheck(); } });
        }
      }
      this.cdr.markForCheck();
    });

    this.projects$.getActive().subscribe({
      next: p => {
        this.projects = p;
        this.loadingProjects = false;
        this.cdr.markForCheck();
      },
      error: () => { this.loadingProjects = false; this.errorMsg.set('載入專案資料失敗。'); },
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit    = true;
      this.requestId = +id;
      this.service.getById(this.requestId).subscribe(r => {
        if (!r) return;
        this.existingRequest = r;
        this.approvalStatus = r.approvalStatus;
        this.isDraft    = r.approvalStatus === 'draft';
        this.isReturned = r.approvalStatus === 'returned';
        this.isReadOnly = r.approvalStatus !== 'draft' && r.approvalStatus !== 'returned';
        this.form.patchValue({
          destination: r.destination,
          startDate: r.startDate instanceof Date
            ? r.startDate.toISOString().split('T')[0]
            : String(r.startDate),
          endDate: r.endDate instanceof Date
            ? r.endDate.toISOString().split('T')[0]
            : String(r.endDate),
          purpose:   r.purpose,
          projectId: r.projectId ?? null,
        });

        // 回填日期後查詢假日天數
        this.onDateChange();

        // 回填參與執行人員（dates 正規化為 yyyy-MM-dd）
        if (r.participants?.length) {
          this.participantEntries = r.participants
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map(p => ({
              sortOrder: p.sortOrder,
              selectedUserId: p.userId,
              selectedDates: (p.dates ?? []).map(d => String(d).slice(0, 10)).sort(),
            }));
        }

        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }

        if (this.isReadOnly) this.form.disable();

        // 非草稿時載入簽核流程
        if (r.approvalStatus !== 'draft') {
          this.taskSvc.getById(this.requestId, 'holiday_travel').subscribe({
            next: task => {
              this.approvalFlow = task.flow ?? null;
              this.approvalRecords = task.approvalRecords ?? [];
              this.taskCurrentStepOrder = task.currentStepOrder;
              this.taskStatus = task.status;
              this.cdr.markForCheck();
            },
          });
        }
        this.cdr.markForCheck();
      });
    }
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.form.invalid || this.isReadOnly) return;
    const fd = this._buildFormData();
    const obs = this.isEdit
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    this.errorMsg.set('');
    obs.subscribe({
      next: saved => {
        if (!this.isEdit) this.requestId = saved.id;
        this.router.navigate(['/admin/holiday-travel-requests']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.form.invalid || this.isReadOnly) return;
    // 流程含「申請人指定審核」步驟時，每個 designated step 至少需要 1 位指定審核者（被抑制者除外）
    if (this.hasDesignatedStep) {
      for (const step of this.designatedSteps) {
        if (this._suppressedSteps.includes(step.stepOrder)) continue;
        const hasForStep = this._pickerPayload.some(p => p.approvalStepOrder === step.stepOrder);
        if (!hasForStep) {
          this.errorMsg.set(`此簽核流程的步驟 ${step.stepOrder} 包含申請人指定審核，請新增至少 1 位審核者。`);
          return;
        }
      }
    }
    const fd = this._buildFormData();
    const save$ = this.isEdit
      ? this.service.update(this.requestId, fd)
      : this.service.create(fd);
    this.errorMsg.set('');
    save$.subscribe({
      next: saved => {
        this.service.submit(saved.id).subscribe({
          next: () => {
            const tpl = this.successModal();
            if (tpl) {
              const ref = this.modal.open(tpl, { centered: true, backdrop: 'static', keyboard: false });
              ref.result
                .then(() => this.router.navigate(['/admin/holiday-travel-requests']))
                .catch(() => this.router.navigate(['/admin/holiday-travel-requests']));
            } else {
              this.router.navigate(['/admin/holiday-travel-requests']);
            }
          },
          error: (err: HttpErrorResponse) => {
            this.errorMsg.set(err.error?.message || '送出失敗，請稍後再試。');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  private _buildFormData(): FormData {
    const v = this.form.value;
    const project = this.projects.find(p => p.id === v.projectId);
    const fd = new FormData();

    fd.append('destination',   v.destination!);
    fd.append('startDate',     v.startDate!);
    fd.append('endDate',       v.endDate!);
    fd.append('purpose',       v.purpose!);
    if (v.projectId) {
      fd.append('projectId',   String(v.projectId));
      if (project?.code) fd.append('projectCode', project.code);
    }

    // 參與執行人員（dates 空陣列＝全程參與）
    const participants = this.participantEntries
      .filter(e => e.selectedUserId)
      .map(e => ({userId: e.selectedUserId!, sortOrder: e.sortOrder, dates: e.selectedDates}));
    if (participants.length > 0) {
      fd.append('participants', JSON.stringify(participants));
    }

    // 指定審核者
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    return fd;
  }
}
