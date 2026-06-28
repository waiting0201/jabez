import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {JobTitleService} from '../../services/job-title.service';

@Component({
  selector: 'app-job-title-form',
  templateUrl: './job-title-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class JobTitleForm implements OnInit {
  private fb = inject(FormBuilder);
  private jobTitleService = inject(JobTitleService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEdit = false;
  jobTitleId = 0;
  errorMsg = signal('');

  form = this.fb.group({
    name:        ['', Validators.required],
    level:       [1, [Validators.required, Validators.min(1)]],
    description: [''],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.jobTitleId = +id;
      this.jobTitleService.getById(this.jobTitleId).subscribe(jt => {
        if (jt) this.form.patchValue(jt);
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.jobTitleService.update(this.jobTitleId, value)
      : this.jobTitleService.create(value);
    this.errorMsg.set('');
    obs.subscribe({
      next: () => this.router.navigate(['/admin/job-titles']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
