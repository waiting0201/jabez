import {Component, Input, Output, EventEmitter, OnChanges, SimpleChanges} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {UserLookup} from '../../../features/admin/users/models/user.model';
import {JobTitleLookup} from '../../../features/admin/job-titles/models/job-title.model';
import {Department} from '../../../features/admin/departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../features/admin/approvals/models/approval.model';
import {DesignatedReviewer} from '../../../features/admin/payment-requests/models/payment-request.model';

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
  filteredUsers: UserLookup[];
  selectedDepartmentId: number | null;
  departmentFilteredUsers: UserLookup[];
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

  @Output() change = new EventEmitter<DesignatedReviewerPayload[]>();

  groups: PickerGroup[] = [];

  ngOnChanges(changes: SimpleChanges) {
    if (changes['designatedSteps'] || changes['users'] || changes['initial']) {
      this._buildGroups();
      // 重建後立即同步 payload 給父元件（編輯回填時使用者未互動也要有值，否則送出 / 驗證會誤判為空）
      this._emitChange();
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

      const entries: PickerEntry[] = stepInitials.length > 0
        ? stepInitials.map((init, idx) => {
            const user = this.users.find(u => u.id === init.reviewerId);
            return {
              rowIndex: idx,
              selectedJobTitleId: user?.jobTitleId ?? null,
              selectedUserId: init.reviewerId,
              filteredUsers: user?.jobTitleId
                ? this.users.filter(u => u.jobTitleId === user.jobTitleId && u.status === 'active')
                : [],
              selectedDepartmentId: init.selectedDepartmentId ?? null,
              departmentFilteredUsers: (init.selectedDepartmentId != null)
                ? this.users.filter(u => u.departmentId === init.selectedDepartmentId && u.status === 'active')
                : [],
            };
          })
        : [];

      return {designatedStep: step, entries};
    });
  }

  addEntry(group: PickerGroup) {
    group.entries.push({
      rowIndex: group.entries.length,
      selectedJobTitleId: null,
      selectedUserId: null,
      filteredUsers: [],
      selectedDepartmentId: null,
      departmentFilteredUsers: [],
    });
    this._emitChange();
  }

  removeEntry(group: PickerGroup, i: number) {
    group.entries.splice(i, 1);
    group.entries.forEach((e, idx) => e.rowIndex = idx);
    this._emitChange();
  }

  onJobTitleChange(entry: PickerEntry) {
    entry.filteredUsers = entry.selectedJobTitleId
      ? this.users.filter(u => u.jobTitleId === entry.selectedJobTitleId && u.status === 'active')
      : [];
    entry.selectedUserId = null;
    this._emitChange();
  }

  onDeptChange(entry: PickerEntry) {
    entry.departmentFilteredUsers = entry.selectedDepartmentId != null
      ? this.users.filter(u => u.departmentId === entry.selectedDepartmentId && u.status === 'active')
      : [];
    entry.selectedUserId = null;
    this._emitChange();
  }

  onUserChange() {
    this._emitChange();
  }

  private _emitChange() {
    this.change.emit(this._buildPayload());
  }

  /** 取得目前所有有效的 payload（供父元件主動讀取） */
  getPayload(): DesignatedReviewerPayload[] {
    return this._buildPayload();
  }

  private _buildPayload(): DesignatedReviewerPayload[] {
    const result: DesignatedReviewerPayload[] = [];
    for (const group of this.groups) {
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
