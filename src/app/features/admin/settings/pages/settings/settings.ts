import {Component, inject, OnInit, signal} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {SettingsService} from '../../services/settings.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.html',
  imports: [ReactiveFormsModule],
})
export class Settings implements OnInit {
  private fb = inject(FormBuilder);
  private settingsService = inject(SettingsService);

  saved = false;
  errorMsg = signal('');

  form = this.fb.group({
    workStartTime:        ['09:00', Validators.required],
    workEndTime:          ['18:00', Validators.required],
    monthlyOvertimeLimit: [46, [Validators.required, Validators.min(0), Validators.max(200)]],
  });

  ngOnInit() {
    this.settingsService.get().subscribe(s => this.form.patchValue(s));
  }

  submit() {
    if (this.form.invalid) return;
    this.errorMsg.set('');
    this.settingsService.save(this.form.value as any).subscribe({
      next: () => {
        this.saved = true;
        setTimeout(() => this.saved = false, 3000);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
