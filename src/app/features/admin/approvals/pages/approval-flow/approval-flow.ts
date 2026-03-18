import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {BehaviorSubject, take} from 'rxjs';
import {ApprovalService} from '../../services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {ApprovalItem, ApprovalStep} from '../../models/approval.model';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';

@Component({
  selector: 'app-approval-flow',
  templateUrl: './approval-flow.html',
  imports: [RouterLink, AsyncPipe, ReactiveFormsModule],
})
export class ApprovalFlow implements OnInit {
  private route = inject(ActivatedRoute);
  private approvalService = inject(ApprovalService);
  private deptService = inject(DepartmentService);
  private jobTitleService = inject(JobTitleService);
  private fb = inject(FormBuilder);

  itemId = 0;
  item$ = new BehaviorSubject<ApprovalItem | undefined>(undefined);
  departments: Department[] = [];
  jobTitles: JobTitle[] = [];

  errorMsg = signal('');
  showStepForm = false;
  editStep: ApprovalStep | null = null;

  stepForm = this.fb.group({
    stepOrder:              [1, [Validators.required, Validators.min(1)]],
    departmentId:           [null as number | null],
    jobTitleId:             [null as number | null],
    useApplicantDepartment: [false],
    useDirectSupervisor:    [false],
    useApplicantDesignated: [false],
    note:                   [''],
  });

  ngOnInit() {
    this.itemId = +(this.route.snapshot.paramMap.get('id') ?? 0);
    this.approvalService.getById(this.itemId).subscribe(item => this.item$.next(item));
    this.deptService.getAll().pipe(take(1)).subscribe(d => this.departments = d);
    this.jobTitleService.getAll().pipe(take(1)).subscribe(j => this.jobTitles = j);
  }

  openAddStep() {
    this.editStep = null;
    const nextOrder = (this.item$.getValue()?.steps.length ?? 0) + 1;
    this.stepForm.reset({stepOrder: nextOrder, departmentId: null, jobTitleId: null, useApplicantDepartment: false, useDirectSupervisor: false, useApplicantDesignated: false, note: ''});
    this.showStepForm = true;
  }

  openEditStep(step: ApprovalStep) {
    this.editStep = step;
    this.stepForm.patchValue({
      stepOrder:              step.stepOrder,
      departmentId:           step.departmentId ?? null,
      jobTitleId:             step.jobTitleId ?? null,
      useApplicantDepartment: step.useApplicantDepartment ?? false,
      useDirectSupervisor:    step.useDirectSupervisor ?? false,
      useApplicantDesignated: step.useApplicantDesignated ?? false,
      note:                   step.note ?? '',
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
    }
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

    const isSpecialMode = useApplicantDesignated || useDirectSupervisor;
    const deptId = (isSpecialMode || useAppDept) ? undefined : (v.departmentId || undefined);
    const jtId   = isSpecialMode ? undefined : (v.jobTitleId || undefined);

    const deptName = deptId ? this.departments.find(d => d.id === deptId)?.name : undefined;
    const jtName   = jtId   ? this.jobTitles.find(j => j.id === jtId)?.name   : undefined;

    const stepData = {
      stepOrder:              v.stepOrder!,
      departmentId:           deptId,
      departmentName:         deptName,
      jobTitleId:             jtId,
      jobTitleName:           jtName,
      useApplicantDepartment: !isSpecialMode && (useDirectSupervisor || useAppDept),
      useDirectSupervisor:    !useApplicantDesignated && useDirectSupervisor,
      useApplicantDesignated,
      note:                   v.note ?? '',
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
