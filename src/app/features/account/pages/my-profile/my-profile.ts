import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { environment } from '@/environments/environment';
import { AuthService } from '@core/auth/services/auth.service';
import { MyProfileService } from '../../services/my-profile.service';
import { User } from '../../../admin/users/models/user.model';
import { EmployeeProfileDetail, SalaryAdjustmentRecord } from '../../../admin/users/models/employee-profile.model';

@Component({
  selector: 'app-my-profile',
  templateUrl: './my-profile.html',
  imports: [DatePipe, DecimalPipe],
})
export class MyProfile implements OnInit {
  private auth = inject(AuthService);
  private myProfileService = inject(MyProfileService);

  // ─── 頭像 signals（來自 JWT） ────────────────────────────────
  avatarUrl = this.auth.avatarUrl;
  avatarPosX = this.auth.avatarPositionX;
  avatarPosY = this.auth.avatarPositionY;
  avatarScale = this.auth.avatarScale;

  // ─── 狀態 signals ────────────────────────────────────────────
  loading = signal(false);
  hrLoading = signal(false);
  hrLoaded = signal(false);
  errorMsg = signal<string | null>(null);

  // ─── 資料 signals ────────────────────────────────────────────
  user = signal<User | null>(null);
  profile = signal<EmployeeProfileDetail | null>(null);

  // ─── Tab signals ─────────────────────────────────────────────
  activeTab = signal<'basic' | 'hr' | 'dependents'>('basic');

  ngOnInit(): void {
    this._loadUser();
  }

  switchTab(tab: 'basic' | 'hr' | 'dependents'): void {
    this.activeTab.set(tab);
    if ((tab === 'hr' || tab === 'dependents') && !this.hrLoaded()) {
      this._loadProfile();
    }
  }

  // ─── 健保費試算（getter）─────────────────────────────────────
  get estimatedHealthInsurance(): number | null {
    const p = this.profile();
    const u = this.user();
    if (!p || !u) return null;
    const base = u.healthInsuranceOverride ?? null;
    if (base === null) return null;
    const n = (p.healthInsuranceDependents ?? []).length;
    const capped = Math.min(n, 3);
    return base * (1 + capped);
  }

  // ─── 工具方法 ────────────────────────────────────────────────
  maritalStatusLabel(v: string | null | undefined): string {
    const map: Record<string, string> = { single: '未婚', married: '已婚', divorced: '離婚', widowed: '喪偶' };
    return v ? (map[v] ?? v) : '—';
  }

  dependentRelLabel(v: string): string {
    const map: Record<string, string> = {
      spouse: '配偶', father: '父', mother: '母', son: '子', daughter: '女',
      father_in_law: '公（公公）', mother_in_law: '婆（婆婆）',
      father_in_law_wife: '翁（岳父）', mother_in_law_wife: '姑（岳母）', other: '其他',
    };
    return map[v] ?? v;
  }

  salaryRowTotal(r: SalaryAdjustmentRecord): number {
    return (+(r.baseSalary ?? 0))
      + (+(r.positionAllowance ?? 0))
      + (+(r.dutyAllowance ?? 0))
      + (+(r.otherAllowance ?? 0))
      + (+(r.adjustmentDifference ?? 0))
      + (+(r.overseasAllowance ?? 0))
      + (+(r.mealAllowance ?? 0));
  }

  /** 組成簽名檔可直接顯示的 URL。
   *  簽名檔容器為公開路由（/files/signatures 免 JWT），故 <img src> 直接走公開路徑；
   *  不可走 /me/files（需 Authorization header，<img> 無法帶 token 會 401 破圖）。 */
  signatureDisplayUrl(rawUrl: string | null | undefined): string | null {
    if (!rawUrl) return null;
    if (!rawUrl.startsWith('http')) return `${environment.apiUrl}/${rawUrl}`;
    const match = rawUrl.match(/\/files\/signatures\/([^/?]+)/);
    return match ? `${environment.apiUrl}/files/signatures/${match[1]}` : rawUrl;
  }

  /** 開新分頁顯示 PII 檔案（下載 Blob + Object URL，確保帶 Authorization header） */
  viewFile(rawUrl: string | null | undefined): void {
    if (!rawUrl) return;
    // 解析 container 與 fileName
    let path = rawUrl;
    if (rawUrl.startsWith('http')) {
      try { path = new URL(rawUrl).pathname.replace(/^\//, ''); } catch { return; }
    }
    path = path.replace(/^files\//, '');
    const parts = path.split('/');
    if (parts.length < 2) return;
    const container = parts[0];
    const fileName = parts.slice(1).join('/');
    this.myProfileService.downloadFile(container, fileName).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: () => {}, // 靜默失敗（避免暴露 PII 存取錯誤）
    });
  }

  // ─── 私有載入方法 ────────────────────────────────────────────
  private _loadUser(): void {
    this.loading.set(true);
    this.myProfileService.getMyUser().subscribe({
      next: u => { this.user.set(u); this.loading.set(false); },
      error: () => { this.errorMsg.set('無法載入個人資料，請稍後再試。'); this.loading.set(false); },
    });
  }

  private _loadProfile(): void {
    this.hrLoading.set(true);
    this.myProfileService.getMyProfile().subscribe({
      next: p => { this.profile.set(p); this.hrLoaded.set(true); this.hrLoading.set(false); },
      error: () => { this.errorMsg.set('無法載入人事資料，請稍後再試。'); this.hrLoading.set(false); },
    });
  }
}
