import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {environment} from '../../../../../../environments/environment';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {debounceTime, distinctUntilChanged, switchMap, catchError} from 'rxjs/operators';
import {of} from 'rxjs';
import {ToastrService} from 'ngx-toastr';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {UserService} from '../../services/user.service';
import {RoleService} from '../../../roles/services/role.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {InsuranceBracketService} from '../../../insurance-brackets/services/insurance-bracket.service';
import {Role} from '../../../roles/models/role.model';
import {Department} from '../../../departments/models/department.model';
import {JobTitle} from '../../../job-titles/models/job-title.model';
import {User, UserStatus} from '../../models/user.model';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.html',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
})
export class UserForm implements OnInit {
  private fb              = inject(FormBuilder);
  private userService     = inject(UserService);
  private roleService     = inject(RoleService);
  private deptService     = inject(DepartmentService);
  private jtService       = inject(JobTitleService);
  private bracketService  = inject(InsuranceBracketService);
  private route           = inject(ActivatedRoute);
  private router          = inject(Router);
  private destroyRef      = inject(DestroyRef);
  private authService     = inject(AuthService);
  private toastr          = inject(ToastrService);

  isSuperAdmin = this.authService.isSuperAdmin;
  sending = signal(false);
  roles       = signal<Role[]>([]);
  departments = signal<Department[]>([]);
  jobTitles   = signal<JobTitle[]>([]);
  allUsers    = signal<User[]>([]);
  isEdit = false;
  userId = '';
  errorMsg        = signal('');
  laborInsurance  = signal<number | null>(null);
  healthInsurance = signal<number | null>(null);

  // 簽名檔
  signatureUrl     = signal<string | null>(null);  // 既有的遠端 URL
  signaturePreview = signal<string | null>(null);  // 本地預覽 (data URL)
  signatureFile    = signal<File | null>(null);     // 待上傳檔案
  removeSignature  = signal(false);                 // 標記刪除

  // 頭像
  avatarUrl     = signal<string | null>(null);
  avatarPreview = signal<string | null>(null);
  avatarFile    = signal<File | null>(null);
  removeAvatar  = signal(false);

  // 原住民證明文件（圖或 PDF）
  indigenousProofUrl      = signal<string | null>(null);
  indigenousProofFile     = signal<File | null>(null);
  indigenousProofFileName = signal<string | null>(null); // 上傳時保留檔名，方便 UI 顯示
  removeIndigenousProof   = signal(false);

  form = this.fb.group({
    name:         ['', Validators.required],
    email:        ['', [Validators.required, Validators.email]],
    password:     ['', Validators.minLength(6)],
    roleId:       ['' as string],
    status:       ['active' as UserStatus, Validators.required],
    departmentId: [null as number | null],
    jobTitleId:   [null as number | null],
    hireDate:     ['' as string],
    resignDate:   ['' as string],
    baseSalary:   [null as number | null],
    mealAllowance: [null as number | null],
    overtimePay:   [null as number | null],
    sendPaySlip:   [false],
    isIndigenous:  [false],
    agentUserId:  ['' as string],
    birthday:     ['' as string, Validators.required],
  });

  ngOnInit() {
    // 載入角色清單並加上必填驗證
    this.form.get('roleId')!.setValidators(Validators.required);
    this.form.get('roleId')!.updateValueAndValidity();
    this.roleService.getAll().subscribe({
      next: r => this.roles.set(r),
      error: err => console.error('[UserForm] 無法載入角色清單', err),
    });
    this.deptService.getAll().subscribe(d => this.departments.set(d));
    this.jtService.getAll().subscribe(j => this.jobTitles.set(j));
    this.userService.getAll().subscribe(u => this.allUsers.set(u));

    // 監聽底薪變化，非同步查詢對應勞健保級距（switchMap 自動取消前次請求）
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

    this.userId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.userId) {
      this.isEdit = true;
      this.userService.getById(this.userId).subscribe(user => {
        if (!user) return;
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
          isIndigenous:  user.isIndigenous ?? false,
          agentUserId:   user.agentUserId ?? '',
          birthday:     user.birthday ? this.toDateString(user.birthday) : '',
        });
        this.signatureUrl.set(user.signatureUrl ?? null);
        this.avatarUrl.set(user.avatar ?? null);
        this.indigenousProofUrl.set(user.indigenousProofUrl ?? null);
      });
    }
  }

  getAvailableAgents(): User[] {
    return this.allUsers().filter(u => u.id !== this.userId);
  }

  private toDateString(d: Date): string {
    return new Date(d).toISOString().substring(0, 10);
  }

  onSignatureSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.signatureFile.set(file);
    this.removeSignature.set(false);
    // 本地預覽
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

  onAvatarSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.avatarFile.set(file);
    this.removeAvatar.set(false);
    const reader = new FileReader();
    reader.onload = () => this.avatarPreview.set(reader.result as string);
    reader.readAsDataURL(file);
    input.value = '';
  }

  onRemoveAvatar() {
    this.avatarFile.set(null);
    this.avatarPreview.set(null);
    this.removeAvatar.set(true);
  }

  onIndigenousProofSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
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

  /** 以 JWT fetch 原住民證明，開新分頁檢視（受 users:read 權限保護） */
  viewIndigenousProof() {
    const url = this.indigenousProofUrl();
    if (!url) return;
    const match = url.match(/\/indigenous-proofs\/(.+)$/);
    const fileName = match?.[1];
    if (!fileName) return;
    this.userService.getIndigenousProof(fileName).subscribe({
      next: blob => {
        const objectUrl = URL.createObjectURL(blob);
        window.open(objectUrl, '_blank');
        // 延遲釋放，避免新分頁還沒載入就被 revoke
        setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
      },
      error: err => {
        this.toastr.error(err.error?.message || '無法載入證明文件。', '載入失敗');
      },
    });
  }

  /**
   * 顯示的簽名圖片：
   * - 本地預覽（data URL）優先，無需轉換
   * - 既有遠端 URL：相對路徑加上 apiUrl 前綴；完整 blob URL 轉為 API 代理路徑
   */
  get displaySignature(): string | null {
    if (this.removeSignature()) return null;
    const preview = this.signaturePreview();
    if (preview) return preview;
    const url = this.signatureUrl();
    if (!url) return null;
    if (!url.startsWith('http')) {
      return `${environment.apiUrl}/${url}`;
    }
    const match = url.match(/\/signatures\/(.+)$/);
    if (match) {
      return `${environment.apiUrl}/files/signatures/${match[1]}`;
    }
    return url;
  }

  /** 顯示的頭像圖片（本地預覽優先，否則轉換既有 URL 為代理路徑） */
  get displayAvatar(): string | null {
    if (this.removeAvatar()) return null;
    const preview = this.avatarPreview();
    if (preview) return preview;
    const url = this.avatarUrl();
    if (!url) return null;
    if (!url.startsWith('http')) {
      return `${environment.apiUrl}/${url}`;
    }
    const match = url.match(/\/avatars\/(.+)$/);
    if (match) {
      return `${environment.apiUrl}/files/avatars/${match[1]}`;
    }
    return url;
  }

  /** 原住民證明的顯示檔名（新上傳 > 既有檔名從 URL 取） */
  get indigenousProofDisplayName(): string | null {
    if (this.removeIndigenousProof()) return null;
    const pending = this.indigenousProofFileName();
    if (pending) return pending;
    const url = this.indigenousProofUrl();
    if (!url) return null;
    const match = url.match(/\/([^/]+)$/);
    return match?.[1] ?? url;
  }

  /** 是否已經有既有的（已上傳）原住民證明，供 UI 顯示「檢視」按鈕 */
  get hasExistingIndigenousProof(): boolean {
    return !!this.indigenousProofUrl() && !this.indigenousProofFile() && !this.removeIndigenousProof();
  }

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

  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // 勾選原住民但未上傳／已刪除證明文件時，拒絕送出
    if (this.form.value.isIndigenous === true) {
      const hasExisting = !!this.indigenousProofUrl() && !this.removeIndigenousProof();
      const hasNewFile  = !!this.indigenousProofFile();
      if (!hasExisting && !hasNewFile) {
        this.errorMsg.set('勾選原住民身份時必須上傳證明文件（圖片或 PDF）。');
        return;
      }
    }

    const {roleId, hireDate, resignDate, departmentId, jobTitleId, agentUserId, birthday, password, ...rest} = this.form.value as any;

    const payload: Record<string, any> = {
      ...rest,
      password:     password || undefined,
      roleIds:      roleId ? [roleId] : [],
      departmentId: departmentId || undefined,
      jobTitleId:   jobTitleId || undefined,
      hireDate:     hireDate   ? new Date(hireDate)   : undefined,
      resignDate:   resignDate ? new Date(resignDate) : undefined,
      agentUserId:  agentUserId || undefined,
      birthday:     birthday ? new Date(birthday) : undefined,
    };

    const obs = this.isEdit
      ? this.userService.update(this.userId, payload, {
          signatureFile:       this.signatureFile(),
          avatarFile:          this.avatarFile(),
          indigenousProofFile: this.indigenousProofFile(),
          removeSignature:       this.removeSignature(),
          removeAvatar:          this.removeAvatar(),
          removeIndigenousProof: this.removeIndigenousProof(),
        })
      : this.userService.create(payload, {
          signatureFile:       this.signatureFile(),
          avatarFile:          this.avatarFile(),
          indigenousProofFile: this.indigenousProofFile(),
        });
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/users']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
