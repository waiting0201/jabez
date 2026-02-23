import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {AsyncPipe} from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
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

  showStepForm = false;
  editStep: ApprovalStep | null = null;

  stepForm = this.fb.group({
    stepOrder:    [1, [Validators.required, Validators.min(1)]],
    departmentId: [null as number | null],
    jobTitleId:   [null as number | null],
    note:         [''],
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
    this.stepForm.reset({stepOrder: nextOrder, departmentId: null, jobTitleId: null, note: ''});
    this.showStepForm = true;
  }

  openEditStep(step: ApprovalStep) {
    this.editStep = step;
    this.stepForm.patchValue({
      stepOrder:    step.stepOrder,
      departmentId: step.departmentId ?? null,
      jobTitleId:   step.jobTitleId ?? null,
      note:         step.note ?? '',
    });
    this.showStepForm = true;
  }

  closeStepForm() {
    this.showStepForm = false;
  }

  submitStep() {
    if (this.stepForm.invalid) return;
    const v = this.stepForm.value;
    const deptId = v.departmentId || undefined;
    const jtId   = v.jobTitleId   || undefined;

    if (!deptId && !jtId) {
      alert('部門或職稱至少選一。');
      return;
    }

    const deptName = deptId ? this.departments.find(d => d.id === deptId)?.name : undefined;
    const jtName   = jtId   ? this.jobTitles.find(j => j.id === jtId)?.name   : undefined;

    const stepData = {
      stepOrder:      v.stepOrder!,
      departmentId:   deptId,
      departmentName: deptName,
      jobTitleId:     jtId,
      jobTitleName:   jtName,
      note:           v.note ?? '',
    };

    const obs = this.editStep
      ? this.approvalService.updateStep(this.itemId, this.editStep.id, stepData)
      : this.approvalService.addStep(this.itemId, stepData);

    obs.subscribe(item => {
      this.item$.next(item);
      this.showStepForm = false;
    });
  }

  deleteStep(step: ApprovalStep) {
    if (confirm(`確定要刪除步驟 ${step.stepOrder} 嗎？`)) {
      this.approvalService.deleteStep(this.itemId, step.id).subscribe(item => this.item$.next(item));
    }
  }
}
