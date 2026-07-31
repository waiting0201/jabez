import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {BehaviorSubject, take} from 'rxjs';
import {ApprovalService} from '../../services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalItem, ApprovalStep} from '../../models/approval.model';
import {AuthService} from '@core/auth/services/auth.service';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-approval-flow',
  templateUrl: './approval-flow.html',
  imports: [RouterLink, AsyncPipe, ReactiveFormsModule, ScrollIntoViewDirective],
})
export class ApprovalFlow implements OnInit {
  private route = inject(ActivatedRoute);
  private approvalService = inject(ApprovalService);
  private authService = inject(AuthService);
  private deptService = inject(DepartmentService);
  private jobTitleService = inject(JobTitleService);
  private userService = inject(UserService);
  private fb = inject(FormBuilder);

  readonly canWrite  = this.authService.hasPermission('approvals:write');
  readonly canDelete = this.authService.hasPermission('approvals:delete');

  itemId = 0;
  item$ = new BehaviorSubject<ApprovalItem | undefined>(undefined);
  departments: Department[] = [];
  jobTitles: JobTitle[] = [];
  allUsers: UserLookup[] = [];

  errorMsg = signal('');
  showStepForm = false;
  editStep: ApprovalStep | null = null;

  /** 例外指定審核的 UI 開關；後端不存此旗標，是否啟用一律以名單是否非空為準 */
  useException = false;

  stepForm = this.fb.group({
    stepOrder:                    [1, [Validators.required, Validators.min(1)]],
    departmentId:                 [null as number | null],
    jobTitleId:                   [null as number | null],
    useApplicantDepartment:       [false],
    useDirectSupervisor:          [false],
    useApplicantDesignated:       [false],
    designatedRequiresDepartment: [false],
    minDays:                      [null as number | null],
    note:                         [''],
    exceptionUserIds:             this.fb.array([] as FormControl<string | null>[]),
    designatedJobTitleIds:        this.fb.array([] as FormControl<number | null>[]),
  });

  get exceptionUserIds(): FormArray<FormControl<string | null>> {
    return this.stepForm.controls.exceptionUserIds;
  }

  get designatedJobTitleIds(): FormArray<FormControl<number | null>> {
    return this.stepForm.controls.designatedJobTitleIds;
  }

  ngOnInit() {
    this.itemId = +(this.route.snapshot.paramMap.get('id') ?? 0);
    this.approvalService.getById(this.itemId).subscribe(item => this.item$.next(item));
    this.deptService.getAll().pipe(take(1)).subscribe(d => this.departments = d);
    this.jobTitleService.getAll().pipe(take(1)).subscribe(j => this.jobTitles = j);
    this.userService.getLookup().pipe(take(1)).subscribe(u => this.allUsers = u.filter(x => x.status === 'active'));
  }

  openAddStep() {
    this.editStep = null;
    const nextOrder = (this.item$.getValue()?.steps.length ?? 0) + 1;
    this.exceptionUserIds.clear();
    this.designatedJobTitleIds.clear();
    this.useException = false;
    this.stepForm.reset({stepOrder: nextOrder, departmentId: null, jobTitleId: null, useApplicantDepartment: false, useDirectSupervisor: false, useApplicantDesignated: false, designatedRequiresDepartment: false, minDays: null, note: ''});
    this.showStepForm = true;
  }

  openEditStep(step: ApprovalStep) {
    this.editStep = step;
    this.exceptionUserIds.clear();
    for (const id of step.exceptionUserIds ?? []) {
      this.exceptionUserIds.push(this.fb.control<string | null>(id));
    }
    this.designatedJobTitleIds.clear();
    for (const id of step.designatedJobTitleIds ?? []) {
      this.designatedJobTitleIds.push(this.fb.control<number | null>(id));
    }
    this.useException = this.exceptionUserIds.length > 0;
    this.stepForm.patchValue({
      stepOrder:                    step.stepOrder,
      departmentId:                 step.departmentId ?? null,
      jobTitleId:                   step.jobTitleId ?? null,
      useApplicantDepartment:       step.useApplicantDepartment ?? false,
      useDirectSupervisor:          step.useDirectSupervisor ?? false,
      useApplicantDesignated:       step.useApplicantDesignated ?? false,
      designatedRequiresDepartment: step.designatedRequiresDepartment ?? false,
      minDays:                      step.minDays ?? null,
      note:                         step.note ?? '',
    });
    this.showStepForm = true;
  }

  closeStepForm() {
    this.showStepForm = false;
  }

  onUseApplicantDepartmentChange() {
    const checked = this.stepForm.value.useApplicantDepartment;
    if (checked) {
      this.stepForm.patchValue({departmentId: null, useDirectSupervisor: false});
    }
  }

  onUseDirectSupervisorChange() {
    const checked = this.stepForm.value.useDirectSupervisor;
    if (checked) {
      this.stepForm.patchValue({departmentId: null, jobTitleId: null, useApplicantDepartment: false, useApplicantDesignated: false});
    }
  }

  onUseApplicantDesignatedChange() {
    const checked = this.stepForm.value.useApplicantDesignated;
    if (checked) {
      this.stepForm.patchValue({departmentId: null, jobTitleId: null, useApplicantDepartment: false, useDirectSupervisor: false});
      // 原生指定審核已涵蓋全部申請人，與例外名單互斥（限定職稱只服務例外，一併清空）
      this.useException = false;
      this.exceptionUserIds.clear();
      this.designatedJobTitleIds.clear();
    }
    if (!checked) {
      this.stepForm.patchValue({designatedRequiresDepartment: false});
    }
  }

  onUseExceptionChange() {
    if (this.useException) {
      this.stepForm.patchValue({useApplicantDesignated: false});
      if (this.exceptionUserIds.length === 0) this.addExceptionUser();
    } else {
      this.exceptionUserIds.clear();
      this.designatedJobTitleIds.clear();
      this.stepForm.patchValue({designatedRequiresDepartment: false});
    }
  }

  addExceptionUser() {
    this.exceptionUserIds.push(this.fb.control<string | null>(null));
  }

  removeExceptionUser(index: number) {
    this.exceptionUserIds.removeAt(index);
  }

  addDesignatedJobTitle() {
    this.designatedJobTitleIds.push(this.fb.control<number | null>(null));
  }

  removeDesignatedJobTitle(index: number) {
    this.designatedJobTitleIds.removeAt(index);
  }

  /** 已被其他列選走的職稱不再出現，避免重複觸發後端 unique index */
  availableJobTitles(index: number): JobTitle[] {
    const taken = new Set(
      this.designatedJobTitleIds.controls
        .filter((c, i) => i !== index && c.value != null)
        .map(c => c.value as number));
    return this.jobTitles.filter(j => !taken.has(j.id));
  }

  /** 已被其他列選走的人不再出現，避免重複觸發後端 unique index */
  availableUsers(index: number): UserLookup[] {
    const taken = new Set(
      this.exceptionUserIds.controls
        .filter((c, i) => i !== index && c.value)
        .map(c => c.value as string));
    return this.allUsers.filter(u => !taken.has(u.id));
  }

  userName(id: string | null | undefined): string {
    return this.allUsers.find(u => u.id === id)?.name ?? '';
  }

  /** timeline badge 的 tooltip：例外名單姓名清單 */
  exceptionNames(step: ApprovalStep): string {
    return (step.exceptionUserIds ?? []).map(id => this.userName(id)).filter(n => n).join('、');
  }

  /** timeline badge：限定職稱名稱清單 */
  designatedJobTitleNames(step: ApprovalStep): string {
    return (step.designatedJobTitleIds ?? [])
      .map(id => this.jobTitles.find(j => j.id === id)?.name ?? '')
      .filter(n => n)
      .join('、');
  }

  submitStep() {
    if (this.stepForm.invalid) return;
    const v = this.stepForm.value;
    const useApplicantDesignated = v.useApplicantDesignated ?? false;
    const useDirectSupervisor = v.useDirectSupervisor ?? false;
    const useAppDept = v.useApplicantDepartment ?? false;

    // 特殊模式不需要部門與職稱
    if (useApplicantDesignated || useDirectSupervisor) {
      // no validation needed
    } else if (useAppDept) {
      if (!v.jobTitleId) {
        alert('使用申請人部門時，職稱為必填。');
        return;
      }
    } else if (!v.departmentId && !v.jobTitleId) {
      alert('部門或職稱至少選一。');
      return;
    }

    // 例外指定審核名單（整批替換；勾了開關就至少要挑一位，否則等同沒設定）
    const exceptionUserIds = useApplicantDesignated
      ? []
      : this.exceptionUserIds.controls.map(c => c.value).filter((id): id is string => !!id);
    if (!useApplicantDesignated && this.useException && exceptionUserIds.length === 0) {
      alert('已勾選「例外指定審核」，請至少挑選一位使用者。');
      return;
    }

    // 限定職稱（整批替換；只服務例外指定審核，沒有例外名單就一律清空）
    const designatedJobTitleIds = exceptionUserIds.length > 0
      ? this.designatedJobTitleIds.controls.map(c => c.value).filter((id): id is number => id != null)
      : [];

    const isSpecialMode = useApplicantDesignated || useDirectSupervisor;
    const deptId = (isSpecialMode || useAppDept) ? undefined : (v.departmentId || undefined);
    const jtId   = isSpecialMode ? undefined : (v.jobTitleId || undefined);

    const deptName = deptId ? this.departments.find(d => d.id === deptId)?.name : undefined;
    const jtName   = jtId   ? this.jobTitles.find(j => j.id === jtId)?.name   : undefined;

    const stepData = {
      stepOrder:                    v.stepOrder!,
      departmentId:                 deptId,
      departmentName:               deptName,
      jobTitleId:                   jtId,
      jobTitleName:                 jtName,
      useApplicantDepartment:       !isSpecialMode && (useDirectSupervisor || useAppDept),
      useDirectSupervisor:          !useApplicantDesignated && useDirectSupervisor,
      useApplicantDesignated,
      // 指定審核（原生或例外）時此旗標才有意義
      designatedRequiresDepartment: (useApplicantDesignated || exceptionUserIds.length > 0)
        ? (v.designatedRequiresDepartment ?? false) : false,
      minDays:                      v.minDays && v.minDays > 0 ? v.minDays : null,
      note:                         v.note ?? '',
      exceptionUserIds,
      designatedJobTitleIds,
    };

    const obs = this.editStep
      ? this.approvalService.updateStep(this.itemId, this.editStep.id, stepData)
      : this.approvalService.addStep(this.itemId, stepData);

    this.errorMsg.set('');
    obs.subscribe({
      next: item => {
        this.item$.next(item);
        this.showStepForm = false;
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  deleteStep(step: ApprovalStep) {
    if (confirm(`確定要刪除步驟 ${step.stepOrder} 嗎？`)) {
      this.errorMsg.set('');
      this.approvalService.deleteStep(this.itemId, step.id).subscribe({
        next: item => this.item$.next(item),
        error: (err: HttpErrorResponse) => {
          this.errorMsg.set(err.error?.message || '刪除失敗，請稍後再試。');
        },
      });
    }
  }
}
