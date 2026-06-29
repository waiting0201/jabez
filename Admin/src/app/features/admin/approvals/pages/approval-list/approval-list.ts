import {Component, computed, inject, signal} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DatePipe} from '@angular/common';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {BehaviorSubject, switchMap, map} from 'rxjs';
import {take} from 'rxjs';
import {ApprovalService} from '../../services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {AuthService} from '@core/auth/services/auth.service';

import {
  ApprovalItem, ApplicationType,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
} from '../../models/approval.model';

@Component({
  selector: 'app-approval-list',
  templateUrl: './approval-list.html',
  imports: [RouterLink, DatePipe, ReactiveFormsModule],
})
export class ApprovalList {
  private approvalService = inject(ApprovalService);
  private deptService = inject(DepartmentService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  departments: Department[] = [];

  readonly canWrite  = this.authService.hasPermission('approvals:write');
  readonly canDelete = this.authService.hasPermission('approvals:delete');

  // 列表依「申請類型」分組排序，使同類型相鄰；類型順序比照下方 appTypeOptions（通用無類型者排最後）。
  private readonly typeOrder: ApplicationType[] =
    Object.keys(APPLICATION_TYPE_LABELS) as ApplicationType[];

  private refresh$ = new BehaviorSubject<void>(undefined);
  private items$ = this.refresh$.pipe(
    switchMap(() => this.approvalService.getAll()),
    map(items => [...items].sort((a, b) => {
      const ta = a.applicationType ? this.typeOrder.indexOf(a.applicationType) : Number.MAX_SAFE_INTEGER;
      const tb = b.applicationType ? this.typeOrder.indexOf(b.applicationType) : Number.MAX_SAFE_INTEGER;
      if (ta !== tb) return ta - tb;
      // 同類型內：通用預設流程（無部門）排前，其餘依部門名稱、再依 id。
      const da = a.departmentId ?? -1, dbb = b.departmentId ?? -1;
      if ((da === -1) !== (dbb === -1)) return da === -1 ? -1 : 1;
      const cmp = (a.departmentName ?? '').localeCompare(b.departmentName ?? '', 'zh-Hant');
      return cmp !== 0 ? cmp : a.id - b.id;
    })),
  );
  // 全集（已排序）；inline 表單唯一性判斷與部門頁籤皆吃此全集。
  itemsSig = toSignal(this.items$, {initialValue: [] as ApprovalItem[]});

  // Tab 切換：'all'=全部、'generic'=通用（預設）、number=部門 id。
  // 以網址 query param `tab` 記住分頁，進入流程管理再返回時可停留在原分頁。
  activeTab = signal<number | 'all' | 'generic'>(this.parseTab(this.route.snapshot.queryParamMap.get('tab')));

  // 部門頁籤：自清單中實際出現過的部門去重，依部門名稱排序（只顯示有流程的部門）。
  tabDepartments = computed(() => {
    const map = new Map<number, string>();
    for (const it of this.itemsSig()) {
      if (it.departmentId != null) map.set(it.departmentId, it.departmentName ?? '');
    }
    return [...map.entries()]
      .map(([id, name]) => ({id, name}))
      .sort((a, b) => a.name.localeCompare(b.name, 'zh-Hant'));
  });

  // 目前 Tab 過濾後的清單（排序沿用 items$，不變）。
  visibleItems = computed(() => {
    const tab = this.activeTab();
    return this.itemsSig().filter(it => {
      if (tab === 'all') return true;
      if (tab === 'generic') return it.departmentId == null;
      return it.departmentId === tab;
    });
  });

  showForm = false;
  editItem: ApprovalItem | null = null;
  errorMsg = signal('');

  readonly appTypeLabels  = APPLICATION_TYPE_LABELS;
  readonly appTypeClasses = APPLICATION_TYPE_CLASSES;
  readonly appTypeOptions: {value: ApplicationType | ''; label: string}[] = [
    {value: '',                label: '通用（不綁定）'},
    {value: 'payment_request', label: '請款申請'},
    {value: 'leave',           label: '請假申請'},
    {value: 'travel',          label: '出差預支申請'},
    {value: 'overtime',        label: '加班申請'},
    {value: 'advance',         label: '預支申請'},
    {value: 'write_off',       label: '預支沖銷申請'},
    {value: 'travel_write_off', label: '出差預支沖銷申請'},
    {value: 'holiday_travel',   label: '假日執行活動申請'},
    {value: 'travel_payment',   label: '出差請款申請'},
    {value: 'pre_review',       label: '預審申請'},
  ];

  form = this.fb.group({
    name:            ['', Validators.required],
    code:            ['', Validators.required],
    description:     [''],
    isActive:        [true],
    applicationType: ['' as ApplicationType | ''],
    departmentId:    [null as number | null],
  });

  constructor() {
    this.deptService.getAll().pipe(take(1)).subscribe(d => this.departments = d);
  }

  openCreate() {
    this.editItem = null;
    this.errorMsg.set('');
    this.form.reset({isActive: true, applicationType: '', departmentId: null});
    this.showForm = true;
  }

  openEdit(item: ApprovalItem) {
    this.editItem = item;
    this.errorMsg.set('');
    this.form.patchValue({...item, applicationType: item.applicationType ?? '', departmentId: item.departmentId ?? null});
    this.showForm = true;
  }

  closeForm() {
    this.showForm = false;
  }

  switchTab(tab: number | 'all' | 'generic') {
    this.activeTab.set(tab);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {tab: tab === 'all' ? null : tab},
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  // 將 query param 還原成 activeTab 值；數字字串轉部門 id，其餘比照預設 'all'。
  private parseTab(raw: string | null): number | 'all' | 'generic' {
    if (raw === 'generic') return 'generic';
    if (raw && /^\d+$/.test(raw)) return Number(raw);
    return 'all';
  }

  // 同一 (申請類型, 部門) 至多一個流程；故依目前選取的部門判斷此類型是否已被佔用。
  // departmentId 為 null 代表「通用預設」流程。
  isTypeDisabled(value: ApplicationType | '', items: ApprovalItem[]): boolean {
    if (!value) return false;
    const deptId = this.form.get('departmentId')?.value ?? null;
    return items.some(i => i.applicationType === value && (i.departmentId ?? null) === deptId && i.id !== this.editItem?.id);
  }

  submit() {
    if (this.form.invalid) return;
    const raw = this.form.value as any;
    const data = {
      ...raw,
      applicationType: raw.applicationType || undefined,
      departmentId: raw.departmentId ?? null,
    };
    const obs = this.editItem
      ? this.approvalService.update(this.editItem.id, data)
      : this.approvalService.create(data);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => {
        this.showForm = false;
        this.refresh$.next();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  delete(item: ApprovalItem) {
    if (confirm(`確定要刪除簽核項目「${item.name}」嗎？`)) {
      this.approvalService.delete(item.id).subscribe(() => this.refresh$.next());
    }
  }

  toggleActive(item: ApprovalItem) {
    this.approvalService.update(item.id, {isActive: !item.isActive}).subscribe(() => this.refresh$.next());
  }
}
