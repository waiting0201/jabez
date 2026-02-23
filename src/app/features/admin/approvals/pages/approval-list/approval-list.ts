import {Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AsyncPipe, DatePipe} from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ApprovalService} from '../../services/approval.service';
import {
  ApprovalItem, ApplicationType,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
} from '../../models/approval.model';

@Component({
  selector: 'app-approval-list',
  templateUrl: './approval-list.html',
  imports: [RouterLink, AsyncPipe, DatePipe, ReactiveFormsModule],
})
export class ApprovalList {
  private approvalService = inject(ApprovalService);
  private fb = inject(FormBuilder);

  items$ = this.approvalService.getAll();
  showForm = false;
  editItem: ApprovalItem | null = null;

  readonly appTypeLabels  = APPLICATION_TYPE_LABELS;
  readonly appTypeClasses = APPLICATION_TYPE_CLASSES;
  readonly appTypeOptions: {value: ApplicationType | ''; label: string}[] = [
    {value: '',                label: '通用（不綁定）'},
    {value: 'payment_request', label: '請款申請'},
    {value: 'leave',           label: '請假申請'},
    {value: 'travel',          label: '出差申請'},
  ];

  form = this.fb.group({
    name:            ['', Validators.required],
    code:            ['', Validators.required],
    description:     [''],
    isActive:        [true],
    applicationType: ['' as ApplicationType | ''],
  });

  openCreate() {
    this.editItem = null;
    this.form.reset({isActive: true, applicationType: ''});
    this.showForm = true;
  }

  openEdit(item: ApprovalItem) {
    this.editItem = item;
    this.form.patchValue({...item, applicationType: item.applicationType ?? ''});
    this.showForm = true;
  }

  closeForm() {
    this.showForm = false;
  }

  isTypeDisabled(value: ApplicationType | '', items: ApprovalItem[]): boolean {
    if (!value) return false;
    // Disable if type is already used by another item
    return items.some(i => i.applicationType === value && i.id !== this.editItem?.id);
  }

  submit() {
    if (this.form.invalid) return;
    const raw = this.form.value as any;
    const data = {
      ...raw,
      applicationType: raw.applicationType || undefined,
    };
    const obs = this.editItem
      ? this.approvalService.update(this.editItem.id, data)
      : this.approvalService.create(data);
    obs.subscribe(() => this.showForm = false);
  }

  delete(item: ApprovalItem) {
    if (confirm(`確定要刪除簽核項目「${item.name}」嗎？`)) {
      this.approvalService.delete(item.id).subscribe();
    }
  }

  toggleActive(item: ApprovalItem) {
    this.approvalService.update(item.id, {isActive: !item.isActive}).subscribe();
  }
}
