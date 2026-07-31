import {Component, Input, Output, EventEmitter, OnChanges, SimpleChanges} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {UserLookup} from '../../../features/admin/users/models/user.model';
import {JobTitleLookup} from '../../../features/admin/job-titles/models/job-title.model';
import {Department} from '../../../features/admin/departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../features/admin/approvals/models/approval.model';
import {DesignatedReviewer} from '../../../features/admin/payment-requests/models/payment-request.model';

/**
 * 指定審核者「部門最高層級自動略過」限定部門（2026-07 新增）。
 * 須與後端 Constants.cs 的 DepartmentCodes.DesignatedTopLevelSuppression 同步。
 */
const DESIGNATED_TOP_LEVEL_SUPPRESSION_DEPT_CODES = new Set([
  'Operations Department',
  'Brand Department(疆界地域美學)',
]);

export interface DesignatedReviewerPayload {
  reviewerId: string;
  /** 該 designated step 區塊內的列次序（1, 2, 3…） */
  stepOrder: number;
  /** 該列屬於哪個 designated step 的 stepOrder */
  approvalStepOrder: number;
  selectedDepartmentId: number | null;
}

/** 每個 designated step 底下的一列（一位指定審核者） */
interface PickerEntry {
  rowIndex: number;
  selectedJobTitleId: number | null;
  selectedUserId: string | null;
  selectedDepartmentId: number | null;
  /** 此列目前可選的人員（單一真相，由 _candidatesFor 計算） */
  candidateUsers: UserLookup[];
  /** 使用者是否手動改過此列部門（改過的列不被步驟一自動帶入覆寫） */
  deptManuallyChanged: boolean;
}

/** 每個 designated step 的分組 */
interface PickerGroup {
  designatedStep: ApprovalFlowStepSummary;
  entries: PickerEntry[];
}

/**
 * 共用「指定審核者」選取元件。
 * 支援多個 designated step，每 step 一個分組：
 * - designatedRequiresDepartment=false：先選職稱 → 再選人（沿用 payment-form 既有互動）
 * - designatedRequiresDepartment=true：先選部門 → 依部門 filter 出人 → 選人
 * - designatedJobTitleIds 非空（例外指定審核的限定職稱）：人員一律限縮在這些職稱內；
 *   非部門模式時職稱下拉沒有意義故隱藏，直接列出全公司符合職稱者。
 */
@Component({
  selector: 'app-designated-reviewers-picker',
  standalone: true,
  templateUrl: './designated-reviewers-picker.html',
  imports: [FormsModule],
})
export class DesignatedReviewersPicker implements OnChanges {
  @Input() designatedSteps: ApprovalFlowStepSummary[] = [];
  @Input() users: UserLookup[] = [];
  @Input() jobTitles: JobTitleLookup[] = [];
  @Input() departments: Department[] = [];
  @Input() initial: DesignatedReviewer[] = [];

  // 命名避開原生 DOM 事件名稱（zoneless 全域事件代理下曾誤路由到原生 change 事件，見 docs/frontend-design.md §12.6）
  @Output() reviewersChange = new EventEmitter<DesignatedReviewerPayload[]>();
  /** 被抑制（部門最高層級 → 自動略過）的指定步驟 stepOrder 清單，供父元件驗證時排除 */
  @Output() suppressedStepsChange = new EventEmitter<number[]>();

  groups: PickerGroup[] = [];

  /** req1：第一個指定步驟是否已至少選好一位審核者（未完成前，其後步驟閘控 disabled） */
  firstStepComplete = false;
  /** req3：第一個指定步驟（先選部門模式）首位是否選到所選部門最高職稱 → 其後步驟自動略過 */
  topLevelSuppressed = false;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['designatedSteps'] || changes['users'] || changes['initial']) {
      this._buildGroups();
      // 重建後立即同步 payload 給父元件（編輯回填時使用者未互動也要有值，否則送出 / 驗證會誤判為空）
      this._emitChange();
    }
  }

  /** 第一個指定步驟之後的步驟才受閘控 / 抑制影響 */
  isGated(groupIndex: number): boolean {
    return groupIndex > 0 && !this.firstStepComplete;
  }

  isSuppressed(groupIndex: number): boolean {
    return groupIndex > 0 && this.topLevelSuppressed;
  }

  /** 其後步驟的 select / 按鈕是否 disabled（未完成第一步 或 已被最高層級抑制） */
  isDisabled(groupIndex: number): boolean {
    return this.isGated(groupIndex) || this.isSuppressed(groupIndex);
  }

  /** 此步驟是否有限定職稱（例外指定審核專用）→ 非部門模式時隱藏職稱下拉 */
  hasJobTitleLimit(group: PickerGroup): boolean {
    return (group.designatedStep.designatedJobTitleIds?.length ?? 0) > 0;
  }

  /**
   * 人員下拉候選的單一真相：
   *   部門模式         → 所選部門 ∩ 限定職稱
   *   一般模式 + 有限定 → 全公司符合限定職稱者（職稱下拉隱藏）
   *   一般模式 + 無限定 → 所選職稱者（既有行為）
   */
  private _candidatesFor(group: PickerGroup, entry: PickerEntry): UserLookup[] {
    const limit = group.designatedStep.designatedJobTitleIds ?? [];
    const pool = this.users.filter(u =>
      u.status === 'active'
      && (limit.length === 0 || (u.jobTitleId != null && limit.includes(u.jobTitleId))));

    if (group.designatedStep.designatedRequiresDepartment) {
      return entry.selectedDepartmentId != null
        ? pool.filter(u => u.departmentId === entry.selectedDepartmentId)
        : [];
    }
    if (limit.length > 0) return pool;
    return entry.selectedJobTitleId != null
      ? pool.filter(u => u.jobTitleId === entry.selectedJobTitleId)
      : [];
  }

  /**
   * 重算此列候選並維護已選人員。
   * mode='reset'：使用者主動改條件（職稱 / 部門）→ 清空已選人員
   * mode='keep' ：程式帶入（回填 / 部門自動帶入）→ 僅在已選人員落選時才清空
   */
  private _refreshEntry(group: PickerGroup, entry: PickerEntry, mode: 'reset' | 'keep') {
    entry.candidateUsers = this._candidatesFor(group, entry);
    if (mode === 'reset') {
      entry.selectedUserId = null;
    } else if (entry.selectedUserId && !entry.candidateUsers.some(u => u.id === entry.selectedUserId)) {
      entry.selectedUserId = null;
    }
  }

  private _buildGroups() {
    this.groups = this.designatedSteps.map(step => {
      // 找出屬於這個 step 的既有 designee（以 approvalStepOrder 對應，兼容舊資料沒有 approvalStepOrder 時用第一個 step）
      const stepInitials = this.initial.filter(e =>
        e.approvalStepOrder != null
          ? e.approvalStepOrder === step.stepOrder
          : this.designatedSteps[0]?.stepOrder === step.stepOrder
      );

      const entries: PickerEntry[] = stepInitials.map((init, idx) => ({
        rowIndex: idx,
        selectedJobTitleId: this.users.find(u => u.id === init.reviewerId)?.jobTitleId ?? null,
        selectedUserId: init.reviewerId,
        selectedDepartmentId: init.selectedDepartmentId ?? null,
        candidateUsers: [],
        deptManuallyChanged: init.selectedDepartmentId != null,
      }));

      return {designatedStep: step, entries};
    });

    // 回填的已選人員要保留（'keep'），僅在限定職稱等條件變更導致落選時才清掉
    for (const g of this.groups)
      for (const e of g.entries) this._refreshEntry(g, e, 'keep');
  }

  addEntry(group: PickerGroup) {
    const isLaterGroup = this.groups.indexOf(group) > 0;
    // req2：其後步驟（部門模式）新增列時，部門預設帶步驟一所選部門
    const inheritedDept = isLaterGroup && group.designatedStep.designatedRequiresDepartment
      ? this._firstGroupDept()
      : null;
    const entry: PickerEntry = {
      rowIndex: group.entries.length,
      selectedJobTitleId: null,
      selectedUserId: null,
      selectedDepartmentId: inheritedDept,
      candidateUsers: [],
      deptManuallyChanged: false,
    };
    group.entries.push(entry);
    this._refreshEntry(group, entry, 'keep');
    this._emitChange();
  }

  removeEntry(group: PickerGroup, i: number) {
    group.entries.splice(i, 1);
    group.entries.forEach((e, idx) => e.rowIndex = idx);
    this._emitChange();
  }

  onJobTitleChange(group: PickerGroup, entry: PickerEntry) {
    this._refreshEntry(group, entry, 'reset');
    this._emitChange();
  }

  onDeptChange(group: PickerGroup, entry: PickerEntry) {
    this._refreshEntry(group, entry, 'reset');
    // 使用者手動改動其後步驟的部門 → 標記，避免被步驟一自動帶入覆寫
    if (this.groups.indexOf(group) > 0) entry.deptManuallyChanged = true;
    // 步驟一首列部門變更 → req2 帶入其後步驟
    if (entry === this.groups[0]?.entries[0]) this._propagateFirstDept();
    this._emitChange();
  }

  onUserChange() {
    this._emitChange();
  }

  /** 步驟一首列所選部門（僅部門模式有值） */
  private _firstGroupDept(): number | null {
    const g0 = this.groups[0];
    if (!g0?.designatedStep.designatedRequiresDepartment) return null;
    return g0.entries[0]?.selectedDepartmentId ?? null;
  }

  /** req2：把步驟一部門帶入其後（部門模式）步驟未手動改過的列 */
  private _propagateFirstDept() {
    const dept = this._firstGroupDept();
    for (let gi = 1; gi < this.groups.length; gi++) {
      const g = this.groups[gi];
      if (!g.designatedStep.designatedRequiresDepartment) continue;
      for (const e of g.entries) {
        if (e.deptManuallyChanged) continue;
        e.selectedDepartmentId = dept;
        this._refreshEntry(g, e, 'keep');
      }
    }
  }

  /**
   * req3：步驟一（部門模式）首位是否選到所選部門最高職稱（Level 最小）。
   * 僅限定部門（Operations Department / Brand Department(疆界地域美學)）才適用，其餘部門不抑制。
   */
  private _computeTopLevelSuppressed(): boolean {
    const g0 = this.groups[0];
    if (!g0?.designatedStep.designatedRequiresDepartment) return false;
    const e0 = g0.entries[0];
    if (!e0?.selectedUserId || e0.selectedDepartmentId == null) return false;

    const deptId = e0.selectedDepartmentId;
    const deptCode = this.departments.find(d => d.id === deptId)?.code;
    if (!deptCode || !DESIGNATED_TOP_LEVEL_SUPPRESSION_DEPT_CODES.has(deptCode)) return false;

    const deptUsers = this.users.filter(u =>
      u.departmentId === deptId && u.status === 'active' && u.jobTitleLevel != null);
    if (deptUsers.length === 0) return false;

    const minLevel = Math.min(...deptUsers.map(u => u.jobTitleLevel!));
    const selected = this.users.find(u => u.id === e0.selectedUserId);
    return !!selected
      && selected.departmentId === deptId
      && selected.jobTitleLevel != null
      && selected.jobTitleLevel === minLevel;
  }

  private _emitChange() {
    this.firstStepComplete = this.groups[0]?.entries.some(e => !!e.selectedUserId) ?? false;
    this.topLevelSuppressed = this._computeTopLevelSuppressed();
    this.reviewersChange.emit(this._buildPayload());
    this.suppressedStepsChange.emit(this._suppressedStepOrders());
  }

  /** 被抑制的指定步驟 stepOrder（步驟一之後的全部指定步驟） */
  private _suppressedStepOrders(): number[] {
    if (!this.topLevelSuppressed) return [];
    return this.groups.slice(1).map(g => g.designatedStep.stepOrder);
  }

  /** 取得目前所有有效的 payload（供父元件主動讀取） */
  getPayload(): DesignatedReviewerPayload[] {
    return this._buildPayload();
  }

  private _buildPayload(): DesignatedReviewerPayload[] {
    const result: DesignatedReviewerPayload[] = [];
    for (let gi = 0; gi < this.groups.length; gi++) {
      // req3：被抑制的其後步驟不輸出（後端亦會自動略過）
      if (this.isSuppressed(gi)) continue;
      const group = this.groups[gi];
      let stepOrder = 1;
      for (const entry of group.entries) {
        if (entry.selectedUserId) {
          result.push({
            reviewerId: entry.selectedUserId,
            stepOrder: stepOrder++,
            approvalStepOrder: group.designatedStep.stepOrder,
            selectedDepartmentId: group.designatedStep.designatedRequiresDepartment
              ? (entry.selectedDepartmentId ?? null)
              : null,
          });
        }
      }
    }
    return result;
  }
}
