import {Component, inject, OnInit, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {SettingsService} from '../../services/settings.service';
import {ToastrService} from 'ngx-toastr';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.html',
  imports: [ReactiveFormsModule, ScrollIntoViewDirective],
})
export class Settings implements OnInit {
  private fb = inject(FormBuilder);
  private settingsService = inject(SettingsService);
  private toastr = inject(ToastrService);

  saved = signal(false);
  errorMsg = signal('');

  // ── 四週彈性工時切換日 ────────────────────────────────────────────────────
  // 刻意**不放進上面的 form**：這是高後果、極少動的開關，不該因為有人來改
  // 「每月加班時數限制」順手按了儲存就被一起送出去。故獨立 signal + 獨立送出。
  readonly switchDate     = signal<string | null>(null);   // 目前已生效的切換日
  readonly switchInput    = signal('');                    // 輸入中的新值
  readonly switching      = signal(false);                 // in-flight 鎖
  readonly switchError    = signal('');

  /** 可選的最早切換日＝下個月 1 號（後端同樣硬擋，這裡只是讓選擇器轉不過去） */
  readonly minSwitchDate = (() => {
    const t = new Date();
    return `${t.getFullYear()}-${String(t.getMonth() + 2).padStart(2, '0')}-01`;
  })();

  form = this.fb.group({
    siteUrl:                  ['https://admin.jabez.com', Validators.required],
    workStartTime:            ['09:00', Validators.required],
    workEndTime:              ['18:00', Validators.required],
    monthlyOvertimeLimit:     [46, [Validators.required, Validators.min(0), Validators.max(200)]],
    approvalEmailEnabled:     [true],
    approvalLineEnabled:      [true],
    paymentReminderDaysBefore: [3, [Validators.required, Validators.min(0), Validators.max(30)]],
  });

  ngOnInit() {
    this.settingsService.get().subscribe(s => {
      this.form.patchValue(s);
      this.switchDate.set(s.flexibleWorkStartDate ? s.flexibleWorkStartDate.slice(0, 10) : null);
    });
  }

  /** 設定切換日。後端只收未來某月的 1 號，錯誤訊息直接呈現（那些理由使用者需要看到）。 */
  applySwitchDate() {
    const v = this.switchInput();
    if (!v || this.switching()) return;
    if (!confirm(
      `確定將四週彈性工時的切換日設為 ${v} 嗎？\n\n`
      + '該日起：上下班改為 09:00–18:00（午休 12:30–13:30）、員工須自行排定例假與休假日、'
      + '打卡依當日日別鎖定、補休改為逐筆計算、假日執行活動申請停止新增。\n\n'
      + '切換日之前的請假、打卡、加班與薪資資料維持舊制，不會被改動。')) return;

    this.switching.set(true);
    this.switchError.set('');
    this.settingsService.setFlexibleWorkStartDate(v).subscribe({
      next: s => {
        this.switchDate.set(s.flexibleWorkStartDate ? s.flexibleWorkStartDate.slice(0, 10) : null);
        this.switchInput.set('');
        this.switching.set(false);
        this.toastr.success('切換日已設定。', '四週彈性工時');
      },
      error: (err: HttpErrorResponse) => {
        this.switching.set(false);
        this.switchError.set(err.error?.message || '設定失敗，請稍後再試。');
      },
    });
  }

  /** 退回舊制。刻意不驗任何條件 —— 出事時要能隨時關掉。 */
  clearSwitchDate() {
    if (this.switching()) return;
    if (!confirm(
      '確定退回現行制度（08:00–17:00）嗎？\n\n'
      + '個人排班資料不會被刪除，只是不再生效；'
      + '切換期間已產生的補休批次與加班日別快照會保留。')) return;

    this.switching.set(true);
    this.switchError.set('');
    this.settingsService.clearFlexibleWorkStartDate().subscribe({
      next: () => {
        this.switchDate.set(null);
        this.switching.set(false);
        this.toastr.success('已退回現行制度。', '四週彈性工時');
      },
      error: (err: HttpErrorResponse) => {
        this.switching.set(false);
        this.switchError.set(err.error?.message || '設定失敗，請稍後再試。');
      },
    });
  }

  submit() {
    if (this.form.invalid) return;
    this.errorMsg.set('');
    this.settingsService.save(this.form.value as any).subscribe({
      next: () => {
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
