import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {environment} from '../../../../../../environments/environment';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {debounceTime, distinctUntilChanged, switchMap, catchError} from 'rxjs/operators';
import {of} from 'rxjs';
import {ToastrService} from 'ngx-toastr';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {UserService} from '../../services/user.service';
import {EmployeeProfileService} from '../../services/employee-profile.service';
import {HrProfilePdfService} from '../../services/hr-profile-pdf.service';
import {ImageCompressionService} from '../../../../../shared/services/image-compression.service';
import {RoleService} from '../../../roles/services/role.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {InsuranceBracketService} from '../../../insurance-brackets/services/insurance-bracket.service';
import {Role} from '../../../roles/models/role.model';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {User, UserStatus} from '../../models/user.model';
import {EmployeeProfileDetail} from '../../models/employee-profile.model';

const MAX_FILE_BYTES = 1 * 1024 * 1024; // 1 MB

/**
 * Tab1 受 payroll:read 管制的薪資 / 勞健保控制項。
 * 與後端 Api/Common/PayrollFieldAccess.cs 的 Mask 一一對應 —— 新增薪資欄位時兩邊都要改。
 */
const SALARY_CONTROLS = [
  'baseSalary', 'mealAllowance', 'overtimePay',
  'otherAllowance', 'adjustmentDifference',
  'healthInsuranceOverride', 'laborInsuranceOverride', 'laborPensionSelfContributionRate',
] as const;

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, ScrollIntoViewDirective],
})
export class UserForm implements OnInit {
  private fb                   = inject(FormBuilder);
  private userService          = inject(UserService);
  private profileService       = inject(EmployeeProfileService);
  private hrPdfService         = inject(HrProfilePdfService);
  private imageCompression     = inject(ImageCompressionService);
  private roleService          = inject(RoleService);
  private deptService          = inject(DepartmentService);
  private jtService            = inject(JobTitleService);
  private bracketService       = inject(InsuranceBracketService);
  private route                = inject(ActivatedRoute);
  private router               = inject(Router);
  private destroyRef           = inject(DestroyRef);
  private authService          = inject(AuthService);
  private toastr               = inject(ToastrService);

  isSuperAdmin = this.authService.isSuperAdmin;

  /**
   * 薪資欄位級權限：Tab1 的 11 個薪資 / 勞健保欄、Tab2 薪資調整歷史、Tab3 健保費試算、
   * 以及列印 PDF 的薪資頁，全部共用這一個真相。
   * 沿用 payroll:read（與「人事薪資」模組同一把鑰匙）。後端 UserHandler / EmployeeProfileHandler
   * 亦會抹除這些欄位並拒絕寫入 —— 前端隱藏只是視覺層（縱深防禦）。
   */
  readonly canSeeSalary = this.authService.hasPermission('payroll:read');
  sending      = signal(false);
  printing     = signal(false);
  roles        = signal<Role[]>([]);
  departments  = signal<Department[]>([]);
  jobTitles    = signal<JobTitle[]>([]);
  allUsers     = signal<User[]>([]);
  isEdit       = false;
  userId       = '';
  errorMsg          = signal('');
  laborInsurance    = signal<number | null>(null);
  healthInsurance   = signal<number | null>(null);

  /** Tab 切換（basic / hr / dependents） */
  activeTab = signal<'basic' | 'hr' | 'dependents'>('basic');
  /** 是否已載入 HR profile（延遲載入） */
  hrLoaded  = signal(false);
  /** 儲存已載入的 HR profile 供 PDF 列印用 */
  private _hrProfile: EmployeeProfileDetail | null = null;
  /** 目前使用者資料（PDF 列印用） */
  private _currentUser: User | null = null;

  // ── 簽名檔 ──────────────────────────────────────
  signatureUrl     = signal<string | null>(null);
  signaturePreview = signal<string | null>(null);
  signatureFile    = signal<File | null>(null);
  removeSignature  = signal(false);

  // ── 頭像 ─────────────────────────────────────────
  avatarUrl     = signal<string | null>(null);
  avatarPreview = signal<string | null>(null);
  avatarFile    = signal<File | null>(null);
  removeAvatar  = signal(false);
  avatarPosX    = signal(50);
  avatarPosY    = signal(50);
  avatarScale   = signal(1);
  private avatarDragStart: { x: number; y: number; posX: number; posY: number } | null = null;

  // ── 原住民證明 ───────────────────────────────────
  indigenousProofUrl      = signal<string | null>(null);
  indigenousProofFile     = signal<File | null>(null);
  indigenousProofFileName = signal<string | null>(null);
  removeIndigenousProof   = signal(false);

  // ── 低收入戶證明 ─────────────────────────────────
  lowIncomeProofUrl      = signal<string | null>(null);
  lowIncomeProofFile     = signal<File | null>(null);
  lowIncomeProofFileName = signal<string | null>(null);
  removeLowIncomeProof   = signal(false);

  // ── 身心障礙證明 ─────────────────────────────────────
  disabledProofUrl      = signal<string | null>(null);
  disabledProofFile     = signal<File | null>(null);
  disabledProofFileName = signal<string | null>(null);
  removeDisabledProof   = signal(false);

  // ── 身分證正反面（HR Tab） ───────────────────────
  idCardFrontUrl      = signal<string | null>(null);
  idCardFrontFile     = signal<File | null>(null);
  idCardFrontPreview  = signal<string | null>(null);
  idCardFrontFileName = signal<string | null>(null);
  removeIdCardFront   = signal(false);

  idCardBackUrl       = signal<string | null>(null);
  idCardBackFile      = signal<File | null>(null);
  idCardBackPreview   = signal<string | null>(null);
  idCardBackFileName  = signal<string | null>(null);
  removeIdCardBack    = signal(false);

  // ── 最高學歷證明（HR Tab） ────────────────────────
  highestEducationProofUrl      = signal<string | null>(null);
  highestEducationProofFile     = signal<File | null>(null);
  highestEducationProofPreview  = signal<string | null>(null);
  highestEducationProofFileName = signal<string | null>(null);
  removeHighestEducationProof   = signal(false);

  bankBookImageUrl      = signal<string | null>(null);
  bankBookImageFile     = signal<File | null>(null);
  bankBookImagePreview  = signal<string | null>(null);
  bankBookImageFileName = signal<string | null>(null);
  removeBankBook        = signal(false);

  bankBookImageUrl2      = signal<string | null>(null);
  bankBookImageFile2     = signal<File | null>(null);
  bankBookImagePreview2  = signal<string | null>(null);
  bankBookImageFileName2 = signal<string | null>(null);
  removeBankBook2        = signal(false);

  // ── 通訊地址同戶籍 ───────────────────────────────
  mailingAddressSameAsResidential = false;

  // ── 表單 ─────────────────────────────────────────
  form = this.fb.group({
    // Tab 1 既有欄位
    name:         ['', Validators.required],
    email:        ['', [Validators.required, Validators.email]],
    password:     ['', Validators.minLength(6)],
    roleId:       ['' as string],
    status:       ['active' as UserStatus, Validators.required],
    departmentId: [null as number | null, Validators.required],
    jobTitleId:   [null as number | null],
    hireDate:     ['' as string],
    resignDate:   ['' as string],
    baseSalary:   [null as number | null],
    mealAllowance: [null as number | null],
    overtimePay:   [null as number | null],
    sendPaySlip:   [false],
    compensatoryOpeningHours: [null as number | null],
    isShiftWorker: [false],
    isIndigenous:  [false],
    agentUserId:  ['' as string],
    birthday:     ['' as string, Validators.required],
    // Tab 1 新欄位
    isLowIncome:              [false],
    isDisabled:               [false],
    healthInsuranceOverride:  [null as number | null],
    laborInsuranceOverride:   [null as number | null],
    laborPensionSelfContributionRate: [null as number | null,
      [Validators.min(0), Validators.max(6), Validators.pattern(/^\d+$/)]],
    // 加給（同步自最新薪資調整紀錄，可手動覆寫）
    otherAllowance:           [null as number | null],
    adjustmentDifference:     [null as number | null],
    // Tab 2 – HR profile
    hrProfile: this.fb.group({
      employeeNumber:       [''],
      englishName:          [''],
      idNumber:             [''],
      gender:               [''],
      maritalStatus:        [''],
      birthPlace:           [''],
      mobilePhone:          [''],
      residentialAddress:   [''],
      residentialPhone:     [''],
      mailingAddress:       [''],
      mailingPhone:         [''],
      emergencyContactName: [''],
      emergencyContactPhone:[''],
      bankCode:             [''],
      bankAccount:          [''],
      bankCode2:            [''],
      bankAccount2:         [''],
      insuranceStartDate:   [''],
      dependentCount:       [null as number | null],
      specialties:          [''],
      resignationReason:    [''],
      educationRecords:     this.fb.array([]),
      employmentHistoryRecords: this.fb.array([]),
      familyMembers:        this.fb.array([]),
      professionalTrainings:this.fb.array([]),
      languageAbilities:    this.fb.array([]),
      jobTransferRecords:   this.fb.array([]),
      rewardPunishmentRecords: this.fb.array([]),
      salaryAdjustmentRecords: this.fb.array([]),
    }),
    // Tab 3 – 健保眷屬
    healthDependents: this.fb.array([]),
  });

  ngOnInit() {
    this.form.get('roleId')!.setValidators(Validators.required);
    this.form.get('roleId')!.updateValueAndValidity();
    this.roleService.getAll().subscribe({
      next: r => this.roles.set(r),
      error: err => console.error('[UserForm] 無法載入角色清單', err),
    });
    this.deptService.getAll().subscribe(d => this.departments.set(d));
    this.jtService.getAll().subscribe(j => this.jobTitles.set(j));
    this.userService.getAll().subscribe(u => this.allUsers.set(u));

    // 無薪資權限：控制項一律 disable。disabled 控制項不進 form.value、也不參與驗證
    // （勞退自提的 min/max 不會擋住存檔），比條件式建立 FormGroup 改動面小得多。
    if (!this.canSeeSalary) {
      SALARY_CONTROLS.forEach(n => this.form.get(n)!.disable({emitEvent: false}));
    }

    // 監聽底薪變化，查詢對應勞健保級距
    // 無薪資權限時不訂閱：級距 lookup 走 insurance-brackets:read（與 users 正交），
    // 留著等於開一條由底薪反推投保級距的側門，且會噴必然 403 的 XHR。
    if (this.canSeeSalary) {
      this.form.get('baseSalary')!.valueChanges.pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(500),
        distinctUntilChanged(),
        switchMap(val =>
          val !== null && val > 0
            ? this.bracketService.lookupBySalary(val).pipe(catchError(() => of(null)))
            : of(null)
        ),
      ).subscribe(bracket => {
        this.laborInsurance.set(bracket?.laborInsuranceEmployee ?? null);
        this.healthInsurance.set(bracket?.healthInsuranceEmployee ?? null);
      });
    }

    this.userId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.userId) {
      this.isEdit = true;
      this.userService.getById(this.userId).subscribe(user => {
        if (!user) return;
        this._currentUser = user;
        this.form.patchValue({
          ...user,
          roleId:       user.roleIds[0] ?? '',
          departmentId: user.departmentId ?? null,
          jobTitleId:   user.jobTitleId ?? null,
          hireDate:     user.hireDate   ? this.toDateString(user.hireDate)   : '',
          resignDate:   user.resignDate ? this.toDateString(user.resignDate) : '',
          baseSalary:    user.baseSalary ?? null,
          mealAllowance: user.mealAllowance ?? null,
          overtimePay:   user.overtimePay ?? null,
          sendPaySlip:   user.sendPaySlip ?? false,
          compensatoryOpeningHours: user.compensatoryOpeningHours ?? null,
          isShiftWorker: user.isShiftWorker ?? false,
          isIndigenous:  user.isIndigenous ?? false,
          agentUserId:   user.agentUserId ?? '',
          birthday:     user.birthday ? this.toDateString(user.birthday) : '',
          isLowIncome:  user.isLowIncome ?? false,
          isDisabled:   user.isDisabled ?? false,
          healthInsuranceOverride: user.healthInsuranceOverride ?? null,
          laborInsuranceOverride:  user.laborInsuranceOverride  ?? null,
          laborPensionSelfContributionRate: user.laborPensionSelfContributionRate ?? null,
          otherAllowance:          user.otherAllowance          ?? null,
          adjustmentDifference:    user.adjustmentDifference    ?? null,
        });
        this.signatureUrl.set(user.signatureUrl ?? null);
        this.avatarUrl.set(user.avatar ?? null);
        this.avatarPosX.set(user.avatarPositionX ?? 50);
        this.avatarPosY.set(user.avatarPositionY ?? 50);
        this.avatarScale.set(user.avatarScale ?? 1);
        this.indigenousProofUrl.set(user.indigenousProofUrl ?? null);
        this.lowIncomeProofUrl.set(user.lowIncomeProofUrl ?? null);
        this.disabledProofUrl.set(user.disabledProofUrl ?? null);
      });
    }
  }

  getAvailableAgents(): User[] {
    return this.allUsers().filter(u => u.id !== this.userId);
  }

  private toDateString(d: Date): string {
    // 以本地時區取 YYYY-MM-DD，避免 toISOString() 轉 UTC（台北 +8）造成日期少一天
    const dt = new Date(d);
    const y = dt.getFullYear();
    const m = String(dt.getMonth() + 1).padStart(2, '0');
    const day = String(dt.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  /** 切換 Tab；HR / 依附 Tab 第一次切換時才 lazy fetch */
  switchTab(tab: 'basic' | 'hr' | 'dependents') {
    if ((tab === 'hr' || tab === 'dependents') && !this.isEdit) return;
    this.activeTab.set(tab);
    if ((tab === 'hr' || tab === 'dependents') && !this.hrLoaded() && this.isEdit) {
      this._loadHrProfile();
    }
  }

  private _loadHrProfile() {
    this.profileService.getByUserId(this.userId).subscribe({
      next: profile => {
        this._hrProfile = profile;
        this._populateHrForm(profile);
        this.hrLoaded.set(true);
      },
      error: err => {
        console.error('[UserForm] 無法載入人事資料', err);
        this.toastr.warning('人事資料載入失敗，請重新切換 Tab 重試。');
        this.hrLoaded.set(true); // 避免無限 loading
      },
    });
  }

  /** 將後端 profile 回填進 FormArray + scalar 欄位 */
  private _populateHrForm(p: EmployeeProfileDetail) {
    const hr = this.form.get('hrProfile') as FormGroup;
    hr.patchValue({
      employeeNumber:        p.employeeNumber ?? '',
      englishName:           p.englishName ?? '',
      idNumber:              p.idNumber ?? '',
      gender:                p.gender ?? '',
      maritalStatus:         p.maritalStatus ?? '',
      birthPlace:            p.birthPlace ?? '',
      mobilePhone:           p.mobilePhone ?? '',
      residentialAddress:    p.residentialAddress ?? '',
      residentialPhone:      p.residentialPhone ?? '',
      mailingAddress:        p.mailingAddress ?? '',
      mailingPhone:          p.mailingPhone ?? '',
      emergencyContactName:  p.emergencyContactName ?? '',
      emergencyContactPhone: p.emergencyContactPhone ?? '',
      bankCode:              p.bankCode ?? '',
      bankAccount:           p.bankAccount ?? '',
      bankCode2:             p.bankCode2 ?? '',
      bankAccount2:          p.bankAccount2 ?? '',
      insuranceStartDate:    p.insuranceStartDate?.slice(0, 10) ?? '',
      dependentCount:        p.dependentCount ?? null,
      specialties:           p.specialties ?? '',
      resignationReason:     p.resignationReason ?? '',
    });

    // 身分證影本 URLs
    this.idCardFrontUrl.set(p.idCardFrontUrl ?? null);
    this.idCardBackUrl.set(p.idCardBackUrl ?? null);

    // 最高學歷證明 URL
    this.highestEducationProofUrl.set(p.highestEducationProofUrl ?? null);
    this.bankBookImageUrl.set(p.bankBookImageUrl ?? null);
    this.bankBookImageUrl2.set(p.bankBookImageUrl2 ?? null);

    // FormArrays
    this.educationArray.clear();
    (p.educationRecords ?? []).forEach(r => this.educationArray.push(this._educationGroup(r)));

    this.employmentArray.clear();
    (p.employmentHistoryRecords ?? []).forEach(r => this.employmentArray.push(this._employmentGroup(r)));

    this.familyArray.clear();
    (p.familyMembers ?? []).forEach(r => this.familyArray.push(this._familyGroup(r)));

    this.trainingArray.clear();
    (p.professionalTrainings ?? []).forEach(r => this.trainingArray.push(this._trainingGroup(r)));

    this.languageArray.clear();
    (p.languageAbilities ?? []).forEach(r => this.languageArray.push(this._languageGroup(r)));

    this.jobTransferArray.clear();
    (p.jobTransferRecords ?? []).forEach(r => this.jobTransferArray.push(this._jobTransferGroup(r)));

    this.rewardArray.clear();
    (p.rewardPunishmentRecords ?? []).forEach(r => this.rewardArray.push(this._rewardGroup(r)));

    this.salaryArray.clear();
    (p.salaryAdjustmentRecords ?? []).forEach(r => this.salaryArray.push(this._salaryGroup(r)));

    this.dependentsArray.clear();
    (p.healthInsuranceDependents ?? []).forEach(r => this.dependentsArray.push(this._dependentGroup(r)));
  }

  // ═══════════════════════════════════════════════
  // FormArray getters
  // ═══════════════════════════════════════════════
  get educationArray(): FormArray    { return this.form.get('hrProfile.educationRecords') as FormArray; }
  get educationControls(): AbstractControl[] { return this.educationArray.controls; }

  get employmentArray(): FormArray   { return this.form.get('hrProfile.employmentHistoryRecords') as FormArray; }
  get employmentControls(): AbstractControl[] { return this.employmentArray.controls; }

  get familyArray(): FormArray       { return this.form.get('hrProfile.familyMembers') as FormArray; }
  get familyControls(): AbstractControl[] { return this.familyArray.controls; }

  get trainingArray(): FormArray     { return this.form.get('hrProfile.professionalTrainings') as FormArray; }
  get trainingControls(): AbstractControl[] { return this.trainingArray.controls; }

  get languageArray(): FormArray     { return this.form.get('hrProfile.languageAbilities') as FormArray; }
  get languageControls(): AbstractControl[] { return this.languageArray.controls; }

  get jobTransferArray(): FormArray  { return this.form.get('hrProfile.jobTransferRecords') as FormArray; }
  get jobTransferControls(): AbstractControl[] { return this.jobTransferArray.controls; }

  get rewardArray(): FormArray       { return this.form.get('hrProfile.rewardPunishmentRecords') as FormArray; }
  get rewardControls(): AbstractControl[] { return this.rewardArray.controls; }

  get salaryArray(): FormArray       { return this.form.get('hrProfile.salaryAdjustmentRecords') as FormArray; }
  get salaryControls(): AbstractControl[] { return this.salaryArray.controls; }

  get dependentsArray(): FormArray   { return this.form.get('healthDependents') as FormArray; }
  get dependentsControls(): AbstractControl[] { return this.dependentsArray.controls; }

  // ── 薪資合計試算 ──────────────────────────────
  salaryRowTotal(ctrl: AbstractControl): number {
    const v = ctrl.value;
    return (+(v.baseSalary) || 0)
      + (+(v.otherAllowance) || 0)
      + (+(v.adjustmentDifference) || 0)
      + (+(v.mealAllowance) || 0);
  }

  // ── 健保費試算（眷屬最多計 3 口） ─────────────
  // 試算值 = 健保覆寫金額 ×(1+眷屬數)，可反推投保金額 → 隨薪資欄位一起受管制
  get estimatedHealthInsurance(): number | null {
    if (!this.canSeeSalary) return null;
    const base = this.form.get('healthInsuranceOverride')?.value ?? this.healthInsurance();
    if (base === null || base === undefined) return null;
    const n    = this.dependentsArray.length;
    const capped = Math.min(n, 3);
    return +base * (1 + capped);
  }

  // ── 勞退自提試算（底薪 × 自提率%，四捨五入） ─────
  // 直接由底薪算出，等同洩漏底薪 → 隨薪資欄位一起受管制
  get estimatedLaborPensionDeduction(): number | null {
    if (!this.canSeeSalary) return null;
    const rate = this.form.get('laborPensionSelfContributionRate')?.value;
    const base = this.form.get('baseSalary')?.value;
    if (!rate || !base) return null;
    return Math.round((+base * +rate) / 100);
  }

  // ═══════════════════════════════════════════════
  // FormGroup factories（仿 payment-form _invoiceGroup）
  // ═══════════════════════════════════════════════
  addEducation()       { if (this.educationArray.length  < 3) this.educationArray.push(this._educationGroup()); }
  removeEducation(i: number) { this.educationArray.removeAt(i); }
  private _educationGroup(r?: any) {
    return this.fb.group({
      id:         [r?.id ?? null],
      school:     [r?.school ?? ''],
      department: [r?.department ?? ''],
      degree:     [r?.degree ?? 'graduated'],
      startDate:  [r?.startDate?.slice(0, 7) ?? ''],
      endDate:    [r?.endDate?.slice(0, 7) ?? ''],
      order:      [r?.order ?? (this.educationArray.length + 1)],
    });
  }

  addEmployment()       { if (this.employmentArray.length < 3) this.employmentArray.push(this._employmentGroup()); }
  removeEmployment(i: number) { this.employmentArray.removeAt(i); }
  private _employmentGroup(r?: any) {
    return this.fb.group({
      id:           [r?.id ?? null],
      organization: [r?.organization ?? ''],
      jobTitle:     [r?.jobTitle ?? ''],
      startDate:    [r?.startDate?.slice(0, 10) ?? ''],
      endDate:      [r?.endDate?.slice(0, 10) ?? ''],
      order:        [r?.order ?? (this.employmentArray.length + 1)],
    });
  }

  addFamily()       { this.familyArray.push(this._familyGroup()); }
  removeFamily(i: number) { this.familyArray.removeAt(i); }
  private _familyGroup(r?: any) {
    return this.fb.group({
      id:           [r?.id ?? null],
      name:         [r?.name ?? ''],
      relationship: [r?.relationship ?? ''],
      age:          [r?.age ?? null],
      occupation:   [r?.occupation ?? ''],
    });
  }

  addTraining()       { this.trainingArray.push(this._trainingGroup()); }
  removeTraining(i: number) { this.trainingArray.removeAt(i); }
  private _trainingGroup(r?: any) {
    return this.fb.group({
      id:           [r?.id ?? null],
      trainingName: [r?.trainingName ?? ''],
      trainingOrg:  [r?.trainingOrg ?? ''],
      startDate:    [r?.startDate?.slice(0, 10) ?? ''],
      endDate:      [r?.endDate?.slice(0, 10) ?? ''],
      hours:        [r?.hours ?? null],
    });
  }

  addLanguage()       { this.languageArray.push(this._languageGroup()); }
  removeLanguage(i: number) { this.languageArray.removeAt(i); }
  private _languageGroup(r?: any) {
    return this.fb.group({
      id:        [r?.id ?? null],
      language:  [r?.language ?? ''],
      listening: [r?.listening ?? 'fair'],
      speaking:  [r?.speaking ?? 'fair'],
      reading:   [r?.reading ?? 'fair'],
      writing:   [r?.writing ?? 'fair'],
    });
  }

  addJobTransfer()       { this.jobTransferArray.push(this._jobTransferGroup()); }
  removeJobTransfer(i: number) { this.jobTransferArray.removeAt(i); }
  private _jobTransferGroup(r?: any) {
    return this.fb.group({
      id:             [r?.id ?? null],
      effectiveDate:  [r?.effectiveDate?.slice(0, 10) ?? '', Validators.required],
      fromDepartment: [r?.fromDepartment ?? ''],
      toDepartment:   [r?.toDepartment ?? ''],
      fromJobTitle:   [r?.fromJobTitle ?? ''],
      toJobTitle:     [r?.toJobTitle ?? ''],
    });
  }

  addReward()       { this.rewardArray.push(this._rewardGroup()); }
  removeReward(i: number) { this.rewardArray.removeAt(i); }
  private _rewardGroup(r?: any) {
    return this.fb.group({
      id:            [r?.id ?? null],
      effectiveDate: [r?.effectiveDate?.slice(0, 10) ?? '', Validators.required],
      type:          [r?.type ?? 'reward'],
      category:      [r?.category ?? ''],
      count:         [r?.count ?? null],
      reason:        [r?.reason ?? ''],
    });
  }

  addSalary()       { this.salaryArray.push(this._salaryGroup()); }
  removeSalary(i: number) { this.salaryArray.removeAt(i); }
  private _salaryGroup(r?: any) {
    return this.fb.group({
      id:                   [r?.id ?? null],
      effectiveDate:        [r?.effectiveDate?.slice(0, 10) ?? '', Validators.required],
      baseSalary:           [r?.baseSalary ?? null],
      otherAllowance:       [r?.otherAllowance ?? null],
      adjustmentDifference: [r?.adjustmentDifference ?? null],
      mealAllowance:        [r?.mealAllowance ?? null],
      notes:                [r?.notes ?? ''],
    });
  }

  addDependent()       { this.dependentsArray.push(this._dependentGroup()); }
  removeDependent(i: number) { this.dependentsArray.removeAt(i); }
  private _dependentGroup(r?: any) {
    return this.fb.group({
      id:           [r?.id ?? null],
      name:         [r?.name ?? ''],
      relationship: [r?.relationship ?? 'spouse'],
      idNumber:     [r?.idNumber ?? ''],
      birthDate:    [r?.birthDate?.slice(0, 10) ?? ''],
    });
  }

  // ═══════════════════════════════════════════════
  // 簽名檔
  // ═══════════════════════════════════════════════
  onSignatureSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    this.signatureFile.set(file);
    this.removeSignature.set(false);
    const reader = new FileReader();
    reader.onload = () => this.signaturePreview.set(reader.result as string);
    reader.readAsDataURL(file);
    input.value = '';
  }

  onRemoveSignature() {
    this.signatureFile.set(null);
    this.signaturePreview.set(null);
    this.removeSignature.set(true);
  }

  // ═══════════════════════════════════════════════
  // 頭像
  // ═══════════════════════════════════════════════
  async onAvatarSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 800, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.warning('上傳照片勿超過1MB');
        return;
      }
      this.avatarFile.set(compressed);
      this.removeAvatar.set(false);
      const reader = new FileReader();
      reader.onload = () => this.avatarPreview.set(reader.result as string);
      reader.readAsDataURL(compressed);
    } catch (err) {
      console.error('[UserForm] 頭像壓縮失敗', err);
      this.toastr.error('頭像處理失敗，請改用其他圖片。', '處理失敗');
    }
  }

  onRemoveAvatar() {
    this.avatarFile.set(null);
    this.avatarPreview.set(null);
    this.removeAvatar.set(true);
    this.avatarPosX.set(50);
    this.avatarPosY.set(50);
    this.avatarScale.set(1);
  }

  onAvatarPointerDown(e: PointerEvent) {
    if (!this.displayAvatar) return;
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    this.avatarDragStart = {
      x: e.clientX,
      y: e.clientY,
      posX: this.avatarPosX(),
      posY: this.avatarPosY(),
    };
  }

  onAvatarPointerMove(e: PointerEvent) {
    if (!this.avatarDragStart) return;
    const rect  = (e.currentTarget as HTMLElement).getBoundingClientRect();
    const scale = this.avatarScale();
    const dx    = (e.clientX - this.avatarDragStart.x) / rect.width  / scale * 100;
    const dy    = (e.clientY - this.avatarDragStart.y) / rect.height / scale * 100;
    this.avatarPosX.set(Math.max(0, Math.min(100, this.avatarDragStart.posX - dx)));
    this.avatarPosY.set(Math.max(0, Math.min(100, this.avatarDragStart.posY - dy)));
  }

  onAvatarPointerUp() { this.avatarDragStart = null; }

  onAvatarScaleChange(event: Event) {
    const v = parseFloat((event.target as HTMLInputElement).value);
    if (Number.isFinite(v)) this.avatarScale.set(Math.max(1, Math.min(3, v)));
  }

  resetAvatarPosition() {
    this.avatarPosX.set(50);
    this.avatarPosY.set(50);
    this.avatarScale.set(1);
  }

  // ═══════════════════════════════════════════════
  // 原住民證明
  // ═══════════════════════════════════════════════
  onIndigenousProofSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    this.indigenousProofFile.set(file);
    this.indigenousProofFileName.set(file.name);
    this.removeIndigenousProof.set(false);
    input.value = '';
  }

  onRemoveIndigenousProof() {
    this.indigenousProofFile.set(null);
    this.indigenousProofFileName.set(null);
    this.removeIndigenousProof.set(true);
  }

  viewIndigenousProof() {
    const url = this.indigenousProofUrl();
    if (!url) return;
    const match    = url.match(/\/indigenous-proofs\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getIndigenousProof(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入證明文件。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 低收入戶證明
  // ═══════════════════════════════════════════════
  async onLowIncomeProofSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.lowIncomeProofFile.set(compressed);
      this.lowIncomeProofFileName.set(file.name);
      this.removeLowIncomeProof.set(false);
    } catch (err) {
      console.error('[UserForm] 低收入戶證明處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveLowIncomeProof() {
    this.lowIncomeProofFile.set(null);
    this.lowIncomeProofFileName.set(null);
    this.removeLowIncomeProof.set(true);
  }

  viewLowIncomeProof() {
    const url = this.lowIncomeProofUrl();
    if (!url) return;
    const match    = url.match(/\/low-income-proofs\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getLowIncomeProof(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入證明文件。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 身心障礙證明
  // ═══════════════════════════════════════════════
  async onDisabledProofSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.disabledProofFile.set(compressed);
      this.disabledProofFileName.set(file.name);
      this.removeDisabledProof.set(false);
    } catch (err) {
      console.error('[UserForm] 身心障礙證明處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveDisabledProof() {
    this.disabledProofFile.set(null);
    this.disabledProofFileName.set(null);
    this.removeDisabledProof.set(true);
  }

  viewDisabledProof() {
    const url = this.disabledProofUrl();
    if (!url) return;
    const match    = url.match(/\/disabled-proofs\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getDisabledProof(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入證明文件。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 身分證正面
  // ═══════════════════════════════════════════════
  async onIdCardFrontSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.idCardFrontFile.set(compressed);
      this.idCardFrontFileName.set(file.name);
      this.removeIdCardFront.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.idCardFrontPreview.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.idCardFrontPreview.set(null);
      }
    } catch (err) {
      console.error('[UserForm] 身分證正面處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveIdCardFront() {
    this.idCardFrontFile.set(null);
    this.idCardFrontPreview.set(null);
    this.idCardFrontFileName.set(null);
    this.removeIdCardFront.set(true);
  }

  viewIdCardFront() {
    const url = this.idCardFrontUrl();
    if (!url) return;
    const match    = url.match(/\/id-cards\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getIdCard(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入身分證影本。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 身分證反面
  // ═══════════════════════════════════════════════
  async onIdCardBackSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.idCardBackFile.set(compressed);
      this.idCardBackFileName.set(file.name);
      this.removeIdCardBack.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.idCardBackPreview.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.idCardBackPreview.set(null);
      }
    } catch (err) {
      console.error('[UserForm] 身分證反面處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveIdCardBack() {
    this.idCardBackFile.set(null);
    this.idCardBackPreview.set(null);
    this.idCardBackFileName.set(null);
    this.removeIdCardBack.set(true);
  }

  viewIdCardBack() {
    const url = this.idCardBackUrl();
    if (!url) return;
    const match    = url.match(/\/id-cards\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getIdCard(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入身分證影本。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 最高學歷證明
  // ═══════════════════════════════════════════════
  async onHighestEducationProofSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.highestEducationProofFile.set(compressed);
      this.highestEducationProofFileName.set(file.name);
      this.removeHighestEducationProof.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.highestEducationProofPreview.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.highestEducationProofPreview.set(null);
      }
    } catch (err) {
      console.error('[UserForm] 最高學歷證明處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveHighestEducationProof() {
    this.highestEducationProofFile.set(null);
    this.highestEducationProofPreview.set(null);
    this.highestEducationProofFileName.set(null);
    this.removeHighestEducationProof.set(true);
  }

  viewHighestEducationProof() {
    const url = this.highestEducationProofUrl();
    if (!url) return;
    const match    = url.match(/\/education-proofs\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getEducationProof(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入最高學歷證明。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 存摺封面
  // ═══════════════════════════════════════════════
  async onBankBookSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.bankBookImageFile.set(compressed);
      this.bankBookImageFileName.set(file.name);
      this.removeBankBook.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.bankBookImagePreview.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.bankBookImagePreview.set(null);
      }
    } catch (err) {
      console.error('[UserForm] 存摺封面處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveBankBook() {
    this.bankBookImageFile.set(null);
    this.bankBookImagePreview.set(null);
    this.bankBookImageFileName.set(null);
    this.removeBankBook.set(true);
  }

  viewBankBook() {
    const url = this.bankBookImageUrl();
    if (!url) return;
    const match    = url.match(/\/passbooks\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getPassbook(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入存摺封面。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 存摺封面（第二帳戶）
  // ═══════════════════════════════════════════════
  async onBankBook2Selected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    input.value = '';
    try {
      const compressed = await this.imageCompression.compress(file, { maxSize: 1600, quality: 0.85 });
      if (compressed.size > MAX_FILE_BYTES) {
        this.toastr.error('上傳照片勿超過1MB');
        return;
      }
      this.bankBookImageFile2.set(compressed);
      this.bankBookImageFileName2.set(file.name);
      this.removeBankBook2.set(false);
      if (compressed.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => this.bankBookImagePreview2.set(reader.result as string);
        reader.readAsDataURL(compressed);
      } else {
        this.bankBookImagePreview2.set(null);
      }
    } catch (err) {
      console.error('[UserForm] 第二帳戶存摺封面處理失敗', err);
      this.toastr.error('檔案處理失敗，請重試。', '處理失敗');
    }
  }

  onRemoveBankBook2() {
    this.bankBookImageFile2.set(null);
    this.bankBookImagePreview2.set(null);
    this.bankBookImageFileName2.set(null);
    this.removeBankBook2.set(true);
  }

  viewBankBook2() {
    const url = this.bankBookImageUrl2();
    if (!url) return;
    const match    = url.match(/\/passbooks\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getPassbook(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => this.toastr.error(err.error?.message || '無法載入存摺封面。', '載入失敗'),
    });
  }

  // ═══════════════════════════════════════════════
  // 通訊地址同戶籍
  // ═══════════════════════════════════════════════
  copyResidentialToMailing() {
    const hrGroup = this.form.get('hrProfile') as FormGroup;
    if (this.mailingAddressSameAsResidential) {
      hrGroup.patchValue({
        mailingAddress: hrGroup.get('residentialAddress')?.value ?? '',
        mailingPhone:   hrGroup.get('residentialPhone')?.value ?? '',
      });
    }
  }

  // ═══════════════════════════════════════════════
  // Display getters（既有 pattern 延伸）
  // ═══════════════════════════════════════════════
  get displaySignature(): string | null {
    if (this.removeSignature()) return null;
    const preview = this.signaturePreview();
    if (preview) return preview;
    const url = this.signatureUrl();
    if (!url) return null;
    if (!url.startsWith('http')) return `${environment.apiUrl}/${url}`;
    const match = url.match(/\/signatures\/(.+)$/);
    if (match) return `${environment.apiUrl}/files/signatures/${match[1]}`;
    return url;
  }

  get displayAvatar(): string | null {
    if (this.removeAvatar()) return null;
    const preview = this.avatarPreview();
    if (preview) return preview;
    const url = this.avatarUrl();
    if (!url) return null;
    if (!url.startsWith('http')) return `${environment.apiUrl}/${url}`;
    const match = url.match(/\/avatars\/(.+)$/);
    if (match) return `${environment.apiUrl}/files/avatars/${match[1]}`;
    return url;
  }

  get indigenousProofDisplayName(): string | null {
    if (this.removeIndigenousProof()) return null;
    const pending = this.indigenousProofFileName();
    if (pending) return pending;
    const url = this.indigenousProofUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingIndigenousProof(): boolean {
    return !!this.indigenousProofUrl() && !this.indigenousProofFile() && !this.removeIndigenousProof();
  }

  get lowIncomeProofDisplayName(): string | null {
    if (this.removeLowIncomeProof()) return null;
    const pending = this.lowIncomeProofFileName();
    if (pending) return pending;
    const url = this.lowIncomeProofUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingLowIncomeProof(): boolean {
    return !!this.lowIncomeProofUrl() && !this.lowIncomeProofFile() && !this.removeLowIncomeProof();
  }

  get disabledProofDisplayName(): string | null {
    if (this.removeDisabledProof()) return null;
    const pending = this.disabledProofFileName();
    if (pending) return pending;
    const url = this.disabledProofUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingDisabledProof(): boolean {
    return !!this.disabledProofUrl() && !this.disabledProofFile() && !this.removeDisabledProof();
  }

  get idCardFrontDisplayName(): string | null {
    if (this.removeIdCardFront()) return null;
    const pending = this.idCardFrontFileName();
    if (pending) return pending;
    const url = this.idCardFrontUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingIdCardFront(): boolean {
    return !!this.idCardFrontUrl() && !this.idCardFrontFile() && !this.removeIdCardFront();
  }

  get idCardBackDisplayName(): string | null {
    if (this.removeIdCardBack()) return null;
    const pending = this.idCardBackFileName();
    if (pending) return pending;
    const url = this.idCardBackUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingIdCardBack(): boolean {
    return !!this.idCardBackUrl() && !this.idCardBackFile() && !this.removeIdCardBack();
  }

  get highestEducationProofDisplayName(): string | null {
    if (this.removeHighestEducationProof()) return null;
    const pending = this.highestEducationProofFileName();
    if (pending) return pending;
    const url = this.highestEducationProofUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingHighestEducationProof(): boolean {
    return !!this.highestEducationProofUrl() && !this.highestEducationProofFile() && !this.removeHighestEducationProof();
  }

  get bankBookDisplayName(): string | null {
    if (this.removeBankBook()) return null;
    const pending = this.bankBookImageFileName();
    if (pending) return pending;
    const url = this.bankBookImageUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingBankBook(): boolean {
    return !!this.bankBookImageUrl() && !this.bankBookImageFile() && !this.removeBankBook();
  }

  get bankBookDisplayName2(): string | null {
    if (this.removeBankBook2()) return null;
    const pending = this.bankBookImageFileName2();
    if (pending) return pending;
    const url = this.bankBookImageUrl2();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  get hasExistingBankBook2(): boolean {
    return !!this.bankBookImageUrl2() && !this.bankBookImageFile2() && !this.removeBankBook2();
  }

  // ═══════════════════════════════════════════════
  // 寄送帳號通知
  // ═══════════════════════════════════════════════
  sendCredentials() {
    if (!this.userId || this.sending()) return;
    this.sending.set(true);
    this.userService.sendCredentials(this.userId).subscribe({
      next: () => {
        this.sending.set(false);
        this.toastr.success('通知信已寄出，員工首次登入後需修改密碼。', '寄送成功');
      },
      error: (err: HttpErrorResponse) => {
        this.sending.set(false);
        this.toastr.error(err.error?.message || '寄送失敗，請確認 SMTP 設定。', '寄送失敗');
      },
    });
  }

  // ═══════════════════════════════════════════════
  // 列印人事資料卡
  // ═══════════════════════════════════════════════
  async printHrCard() {
    if (this.printing()) return;
    this.printing.set(true);
    try {
      // 若尚未載入，先 fetch HR profile
      if (!this.hrLoaded() || !this._hrProfile) {
        await new Promise<void>((resolve, reject) => {
          this.profileService.getByUserId(this.userId).subscribe({
            next: p => { this._hrProfile = p; this.hrLoaded.set(true); resolve(); },
            error: reject,
          });
        });
      }
      if (!this._hrProfile || !this._currentUser) {
        this.toastr.error('無法取得員工資料，請重試。');
        return;
      }
      await this.hrPdfService.generate(this._hrProfile, this._currentUser, this.canSeeSalary);
    } catch (err) {
      console.error('[UserForm] PDF 列印失敗', err);
      this.toastr.error('PDF 生成失敗，請稍後再試。', '列印失敗');
    } finally {
      this.printing.set(false);
    }
  }

  // ═══════════════════════════════════════════════
  // submit
  // ═══════════════════════════════════════════════
  submit() {
    // HR 子表「生效日期」必填驗證（職務調動 / 獎懲 / 薪資調整）
    // 後端 DTO 的 EffectiveDate 為非 nullable DateTime，未填會讓 JSON 反序列化失敗。
    // 在送出前主動檢查並將 user 帶到 HR Tab，給予明確訊息，避免出現「請求內容格式不正確」。
    const hrMissing = this._findMissingHrEffectiveDates();
    if (hrMissing) {
      this.activeTab.set('hr');
      this.form.markAllAsTouched();
      this.errorMsg.set(hrMissing);
      this.toastr.warning(hrMissing, '人事資料未完成');
      return;
    }

    // 學歷起訖年月格式驗證（Safari 的 month 輸入框允許任意文字）
    const eduInvalid = this._findInvalidEducationMonths();
    if (eduInvalid) {
      this.activeTab.set('hr');
      this.form.markAllAsTouched();
      this.errorMsg.set(eduInvalid);
      this.toastr.warning(eduInvalid, '人事資料未完成');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // 原住民驗證
    if (this.form.value.isIndigenous === true) {
      const hasExisting = !!this.indigenousProofUrl() && !this.removeIndigenousProof();
      const hasNewFile  = !!this.indigenousProofFile();
      if (!hasExisting && !hasNewFile) {
        this.errorMsg.set('勾選原住民身份時必須上傳證明文件（圖片或 PDF）。');
        return;
      }
    }

    const { roleId, hireDate, resignDate, departmentId, jobTitleId, agentUserId,
            birthday, password, hrProfile, healthDependents, ...rest } = this.form.value as any;

    const payload: Record<string, any> = {
      ...rest,
      password:     password || undefined,
      roleIds:      roleId ? [roleId] : [],
      departmentId: departmentId || undefined,
      jobTitleId:   jobTitleId || undefined,
      hireDate:     hireDate    ? new Date(hireDate)    : undefined,
      resignDate:   resignDate  ? new Date(resignDate)  : undefined,
      agentUserId:  agentUserId || undefined,
      birthday:     birthday    ? new Date(birthday)    : undefined,
      avatarPositionX: this.removeAvatar() ? undefined : this.avatarPosX(),
      avatarPositionY: this.removeAvatar() ? undefined : this.avatarPosY(),
      avatarScale:     this.removeAvatar() ? undefined : this.avatarScale(),
      // 新欄位
      isLowIncome:              rest.isLowIncome ?? false,
      isDisabled:               rest.isDisabled ?? false,
      healthInsuranceOverride:  rest.healthInsuranceOverride ?? undefined,
      laborInsuranceOverride:   rest.laborInsuranceOverride ?? undefined,
      laborPensionSelfContributionRate: rest.laborPensionSelfContributionRate ?? undefined,
      // 加給（2 種）
      otherAllowance:           rest.otherAllowance           ?? undefined,
      adjustmentDifference:     rest.adjustmentDifference     ?? undefined,
    };

    // 薪資欄位級權限：disabled 控制項本就不在 form.value，這裡再明確剔除一次。
    // 薪資屬安全相關，不倚賴 Angular 的隱含行為（後端 UserHandler 亦有同一道 gate）。
    if (!this.canSeeSalary) {
      for (const k of SALARY_CONTROLS) delete payload[k];
    }

    const obs = this.isEdit
      ? this.userService.update(this.userId, payload, {
          signatureFile:         this.signatureFile(),
          avatarFile:            this.avatarFile(),
          indigenousProofFile:   this.indigenousProofFile(),
          lowIncomeProofFile:    this.lowIncomeProofFile(),
          disabledProofFile:     this.disabledProofFile(),
          removeSignature:       this.removeSignature(),
          removeAvatar:          this.removeAvatar(),
          removeIndigenousProof: this.removeIndigenousProof(),
          removeLowIncomeProof:  this.removeLowIncomeProof(),
          removeDisabledProof:   this.removeDisabledProof(),
        })
      : this.userService.create(payload, {
          signatureFile:       this.signatureFile(),
          avatarFile:          this.avatarFile(),
          indigenousProofFile: this.indigenousProofFile(),
          lowIncomeProofFile:  this.lowIncomeProofFile(),
          disabledProofFile:   this.disabledProofFile(),
        });

    this.errorMsg.set('');

    obs.subscribe({
      next: savedUser => {
        const targetUserId = savedUser.id ?? this.userId;
        // 編輯模式：HR Tab 已載入過才儲存（避免覆蓋未動的資料）
        // 新增模式：一律儲存使用者剛剛在 Tab 2/3 填的內容（多此一舉但無害）
        const shouldSaveHr = this.isEdit ? this.hrLoaded() : this._hasAnyHrInput();
        if (shouldSaveHr && targetUserId) {
          this._saveHrProfile(targetUserId);
          return; // saveHrProfile 完成後再導航
        }
        this._afterSaveNavigate();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  // 新增模式判斷：使用者是否有在 Tab 2/3 輸入任何資料？
  // 避免完全沒填卻仍打 PUT /users/{id}/profile
  private _hasAnyHrInput(): boolean {
    const hrVal = this.form.get('hrProfile')?.value as Record<string, unknown> | null;
    if (hrVal) {
      for (const v of Object.values(hrVal)) {
        if (Array.isArray(v) ? v.length > 0 : !!v) return true;
      }
    }
    if ((this.form.get('healthDependents') as FormArray).length > 0) return true;
    if (this.idCardFrontFile() || this.idCardBackFile()) return true;
    return false;
  }

  /**
   * 找出未填生效日期的 HR 子表（職務調動 / 獎懲 / 薪資調整）。
   * 回傳 null 代表全部 OK；回傳字串代表錯誤訊息（供 errorMsg + toastr）。
   * 後端 DTO 的 EffectiveDate 是非 nullable DateTime，沒填會 JSON 反序列化失敗。
   */
  private _findMissingHrEffectiveDates(): string | null {
    const missing: string[] = [];
    const has = (arr: FormArray) =>
      arr.controls.some(c => !c.get('effectiveDate')?.value);
    if (has(this.jobTransferArray)) missing.push('職務調動');
    if (has(this.rewardArray))      missing.push('獎懲');
    if (has(this.salaryArray))      missing.push('薪資調整');
    if (missing.length === 0) return null;
    return `請填寫 ${missing.join('、')} 的「生效日期」，或刪除未使用的列。`;
  }

  /**
   * 學歷「起訖年月」格式檢查。
   * Safari 不支援 <input type="month">（退化為純文字框），使用者可能輸入任意文字；
   * 在送出前主動驗證並給予明確訊息，避免後端回傳籠統的「請求內容格式不正確」。
   */
  private _findInvalidEducationMonths(): string | null {
    const ok = (v: string | null) => {
      const s = (v ?? '').trim();
      return !s || /^\d{4}[-/.]\d{1,2}([-/.]\d{1,2})?$/.test(s);
    };
    const bad = this.educationArray.controls.some(c =>
      !ok(c.get('startDate')?.value) || !ok(c.get('endDate')?.value));
    if (!bad) return null;
    return '學歷「起始 / 結束年月」格式不正確，請以「YYYY-MM」格式填寫（例：2020-09）。';
  }

  private _saveHrProfile(userId: string) {
    const hrVal = this.form.get('hrProfile')!.value as any;
    const depsVal = (this.form.get('healthDependents') as FormArray).value as any[];

    // 日期正規化（後端 DTO 為 DateTime?，空字串或 yyyy-MM 會 JSON 反序列化失敗）
    // 學歷起訖用 type="month"（值為 yyyy-MM）；Safari 不支援 month 會退化成純文字框，
    // 使用者可能手打 2020/09、2020.9、2020-9-1 等格式 → 統一正規化為 yyyy-MM-dd
    const monthToDate = (v: string | null) => {
      const s = (v ?? '').trim();
      if (!s) return null;
      const m = s.match(/^(\d{4})[-/.](\d{1,2})(?:[-/.](\d{1,2}))?$/);
      if (!m) return s;   // 無法解析 → 原樣送出，交由後端寬鬆日期解析嘗試
      return `${m[1]}-${m[2].padStart(2, '0')}-${(m[3] ?? '1').padStart(2, '0')}`;
    };
    const dateOrNull  = (v: string | null) => v || null;              // 空字串 → null

    const educations = (hrVal.educationRecords as any[] ?? []).map(r => ({
      ...r,
      startDate: monthToDate(r.startDate),
      endDate:   monthToDate(r.endDate),
    }));
    const employments = (hrVal.employmentHistoryRecords as any[] ?? []).map(r => ({
      ...r,
      startDate: dateOrNull(r.startDate),
      endDate:   dateOrNull(r.endDate),
    }));
    const trainings = (hrVal.professionalTrainings as any[] ?? []).map(r => ({
      ...r,
      startDate: dateOrNull(r.startDate),
      endDate:   dateOrNull(r.endDate),
    }));

    // 獎懲「次數」與健保眷屬「出生日期」：後端 DTO 為非 nullable int / DateTime?，
    // 留空會 JSON 反序列化失敗 → 次數未填以 1 計、日期空字串轉 null
    const rewards = (hrVal.rewardPunishmentRecords as any[] ?? []).map(r => ({
      ...r,
      count: r.count ?? 1,
    }));
    const dependents = (depsVal ?? []).map(r => ({
      ...r,
      birthDate: dateOrNull(r.birthDate),
    }));

    // 薪資紀錄補上 totalAmount（後端 DTO 為非 nullable decimal，表單未提供 → 由各項加總補上）
    const salaries = (hrVal.salaryAdjustmentRecords as any[] ?? []).map(r => ({
      ...r,
      baseSalary: r.baseSalary ?? 0,
      totalAmount:
        (r.baseSalary           ?? 0) +
        (r.otherAllowance       ?? 0) +
        (r.adjustmentDifference ?? 0) +
        (r.mealAllowance        ?? 0),
    }));

    const profilePayload: any = {
      employeeNumber:        hrVal.employeeNumber || null,
      englishName:           hrVal.englishName || null,
      idNumber:              hrVal.idNumber || null,
      gender:                hrVal.gender || null,
      maritalStatus:         hrVal.maritalStatus || null,
      birthPlace:            hrVal.birthPlace || null,
      mobilePhone:           hrVal.mobilePhone || null,
      residentialAddress:    hrVal.residentialAddress || null,
      residentialPhone:      hrVal.residentialPhone || null,
      mailingAddress:        hrVal.mailingAddress || null,
      mailingPhone:          hrVal.mailingPhone || null,
      emergencyContactName:  hrVal.emergencyContactName || null,
      emergencyContactPhone: hrVal.emergencyContactPhone || null,
      bankCode:              hrVal.bankCode || null,
      bankAccount:           hrVal.bankAccount || null,
      bankCode2:             hrVal.bankCode2 || null,
      bankAccount2:          hrVal.bankAccount2 || null,
      insuranceStartDate:    hrVal.insuranceStartDate || null,
      dependentCount:        hrVal.dependentCount ?? null,
      specialties:           hrVal.specialties || null,
      resignationReason:     hrVal.resignationReason || null,
      educationRecords:            educations,
      employmentHistoryRecords:    employments,
      familyMembers:               hrVal.familyMembers,
      professionalTrainings:       trainings,
      languageAbilities:           hrVal.languageAbilities,
      jobTransferRecords:          hrVal.jobTransferRecords,
      rewardPunishmentRecords:     rewards,
      healthInsuranceDependents:   dependents,
    };

    // 薪資調整歷史：無權限時整個 key 不送（後端為整批替換，送 [] 會把既有薪資歷史刪光；
    // 後端收到 undefined 視為「不變更」）。
    if (this.canSeeSalary) profilePayload.salaryAdjustmentRecords = salaries;

    this.profileService.upsert(userId, profilePayload, {
      idCardFront:                 this.idCardFrontFile(),
      idCardBack:                  this.idCardBackFile(),
      removeIdCardFront:           this.removeIdCardFront(),
      removeIdCardBack:            this.removeIdCardBack(),
      highestEducationProof:       this.highestEducationProofFile(),
      removeHighestEducationProof: this.removeHighestEducationProof(),
      bankBookImage:               this.bankBookImageFile(),
      removeBankBook:              this.removeBankBook(),
      bankBookImage2:              this.bankBookImageFile2(),
      removeBankBook2:             this.removeBankBook2(),
    }).subscribe({
      next: profile => {
        this._hrProfile = profile;
        this._afterSaveNavigate();
      },
      error: (err: HttpErrorResponse) => {
        // HR 儲存失敗不阻止主要員工資料已存成功，但顯示警告
        this.toastr.warning(err.error?.message || '人事資料儲存失敗，基本資料已更新。');
        this._afterSaveNavigate();
      },
    });
  }

  private _afterSaveNavigate() {
    const currentUserId = this.authService.currentUser()?.id;
    if (this.isEdit && currentUserId === this.userId) {
      this.authService.refreshAccessToken().subscribe({
        next:  () => this.router.navigate(['/admin/users']),
        error: () => this.router.navigate(['/admin/users']),
      });
    } else {
      this.router.navigate(['/admin/users']);
    }
  }
}
