import {Component, inject, OnInit} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
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
    this.settingsService.save(this.form.value as any).subscribe(() => {
      this.saved = true;
      setTimeout(() => this.saved = false, 3000);
    });
  }
}
