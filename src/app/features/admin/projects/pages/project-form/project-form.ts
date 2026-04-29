import {ChangeDetectorRef, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {DecimalPipe} from '@angular/common';
import {FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {ProjectService} from '../../services/project.service';
import {ProjectPaymentSchedule, ProjectStatus} from '../../models/project.model';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {AuthService} from '../../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-project-form',
  templateUrl: './project-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
})
export class ProjectForm implements OnInit {
  private fb = inject(FormBuilder);
  private projectService = inject(ProjectService);
  private deptService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private auth = inject(AuthService);

  departments: Department[] = [];
  loadingDepts = true;
  isEdit = false;
  isClosed = false;
  /** 唯讀模式：已結案 OR 無 projects:write 權限 */
  isReadOnly = false;
  projectId = 0;
  errorMsg = signal('');
  businessPercentage = signal<number | null>(null);
  reservedAmount = signal<number | null>(null);
  reservedPercentage = signal<number | null>(null);

  form = this.fb.group({
    code:           ['', Validators.required],
    name:           ['', Validators.required],
    status:         ['active' as ProjectStatus, Validators.required],
    startDate:      ['', Validators.required],
    endDate:        [''],
    departmentId:   [{value: null as number | null, disabled: true}, Validators.required],
    receivedAmount: [null as number | null, [Validators.required, Validators.min(0)]],
    contractAmount: [null as number | null, [Validators.required, Validators.min(0)]],
    businessAmount: [null as number | null, [Validators.required, Validators.min(0)]],
    googleDriveUrl: ['', Validators.required],
    schedules:      this.fb.array<FormGroup>([]),
  });

  get schedulesArray(): FormArray<FormGroup> {
    return this.form.get('schedules') as FormArray<FormGroup>;
  }

  get scheduleControls(): FormGroup[] {
    return this.schedulesArray.controls as FormGroup[];
  }

  ngOnInit() {
    // 預設依權限決定唯讀（新增模式或尚未載入專案資料時亦適用）
    this.isReadOnly = !this.auth.hasPermission('projects:write');

    this.deptService.getAll().subscribe({
      next: d => {
        this.departments = d;
        this.loadingDepts = false;
        if (!this.isReadOnly) this.form.get('departmentId')!.enable();
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingDepts = false;
        if (!this.isReadOnly) this.form.get('departmentId')!.enable();
        this.errorMsg.set('載入部門資料失敗。');
      },
    });
    this.form.get('contractAmount')!.valueChanges.subscribe(val => {
      const computed = val != null && val >= 0 ? Math.round(val * 0.6) : null;
      this.form.get('businessAmount')!.setValue(computed, { emitEvent: false });
      this.updateBusinessPercentage();
    });
    this.form.get('businessAmount')!.valueChanges.subscribe(() => {
      this.updateBusinessPercentage();
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.projectId = +id;
      this.projectService.getById(this.projectId).subscribe({
        next: p => {
          if (!p) return;
          this.isClosed = p.status === 'closed';
          this.isReadOnly = this.isClosed || !this.auth.hasPermission('projects:write');
          this.form.patchValue({
            code:           p.code,
            name:           p.name,
            status:         p.status,
            startDate:      p.startDate ? p.startDate.substring(0, 10) : '',
            endDate:        p.endDate ? p.endDate.substring(0, 10) : '',
            departmentId:   p.departmentId ?? null,
            receivedAmount: p.receivedAmount ?? null,
            contractAmount: p.contractAmount ?? null,
            businessAmount: p.businessAmount ?? null,
            googleDriveUrl: p.googleDriveUrl ?? '',
          });

          this.schedulesArray.clear();
          const schedules = [...(p.paymentSchedules ?? [])].sort((a, b) => a.periodNo - b.periodNo);
          schedules.forEach(s => this.schedulesArray.push(this.buildScheduleGroup(s)));

          if (this.isReadOnly) this.form.disable();
          this.cdr.markForCheck();
        },
        error: () => this.errorMsg.set('載入專案資料失敗。'),
      });
    }
  }

  private buildScheduleGroup(s?: Partial<ProjectPaymentSchedule>): FormGroup {
    return this.fb.group({
      id:            [s?.id ?? (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : this.fallbackUuid())],
      billingDate:   [s?.billingDate ? String(s.billingDate).substring(0, 10) : ''],
      billingAmount: [s?.billingAmount ?? null],
      invoiceDate:   [s?.invoiceDate ? String(s.invoiceDate).substring(0, 10) : ''],
      invoiceAmount: [s?.invoiceAmount ?? null],
      depositDate:   [s?.depositDate ? String(s.depositDate).substring(0, 10) : ''],
      depositAmount: [s?.depositAmount ?? null],
      deductionNote: [s?.deductionNote ?? ''],
    });
  }

  /** 舊瀏覽器或非 HTTPS 情境下的簡易 UUID fallback */
  private fallbackUuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  addSchedule() {
    if (this.isReadOnly) return;
    this.schedulesArray.push(this.buildScheduleGroup());
  }

  removeSchedule(index: number) {
    if (this.isReadOnly) return;
    this.schedulesArray.removeAt(index);
  }

  /** 即時計算某一期扣款金額（發票 − 入帳），兩值任一缺失則回傳 null */
  deductionFor(index: number): number | null {
    const g = this.scheduleControls[index];
    if (!g) return null;
    const inv = g.get('invoiceAmount')!.value as number | null;
    const dep = g.get('depositAmount')!.value as number | null;
    if (inv == null || dep == null) return null;
    return inv - dep;
  }

  private updateBusinessPercentage() {
    const contract = this.form.get('contractAmount')!.value;
    const business = this.form.get('businessAmount')!.value;
    if (contract != null && contract > 0 && business != null && business >= 0) {
      const pct = Math.round((business / contract) * 100);
      this.businessPercentage.set(pct);
      this.reservedAmount.set(contract - business);
      this.reservedPercentage.set(100 - pct);
    } else {
      this.businessPercentage.set(null);
      this.reservedAmount.set(null);
      this.reservedPercentage.set(null);
    }
  }

  submit() {
    if (this.form.invalid || this.isReadOnly) return;
    const v = this.form.getRawValue();
    if (!v.departmentId) {
      this.errorMsg.set('請選擇部門。');
      return;
    }
    if (v.status === 'closed' && !confirm('確定要將此專案設為「已結案」嗎？結案後將無法再修改或刪除。')) return;
    const dept = this.departments.find(d => d.id === v.departmentId);
    const schedules = this.scheduleControls.map((g, idx) => ({
      id:            g.get('id')!.value as string,
      periodNo:      idx + 1,
      billingDate:   (g.get('billingDate')!.value as string) || null,
      billingAmount: this.toNumberOrNull(g.get('billingAmount')!.value),
      invoiceDate:   (g.get('invoiceDate')!.value as string) || null,
      invoiceAmount: this.toNumberOrNull(g.get('invoiceAmount')!.value),
      depositDate:   (g.get('depositDate')!.value as string) || null,
      depositAmount: this.toNumberOrNull(g.get('depositAmount')!.value),
      deductionNote: (g.get('deductionNote')!.value as string) || null,
    }));

    const payload = {
      code:             v.code!,
      name:             v.name!,
      status:           v.status! as ProjectStatus,
      startDate:        v.startDate!,
      endDate:          v.endDate || undefined,
      departmentId:     v.departmentId!,
      departmentName:   dept?.name,
      receivedAmount:   v.receivedAmount ?? undefined,
      contractAmount:   v.contractAmount ?? undefined,
      businessAmount:   v.businessAmount ?? undefined,
      googleDriveUrl:   v.googleDriveUrl || undefined,
      paymentSchedules: schedules,
    };
    const obs = this.isEdit
      ? this.projectService.update(this.projectId, payload)
      : this.projectService.create(payload);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/projects']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  private toNumberOrNull(val: unknown): number | null {
    if (val === null || val === undefined || val === '') return null;
    const n = Number(val);
    return Number.isFinite(n) ? n : null;
  }
}
