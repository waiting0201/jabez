import {Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {InsuranceBracketService} from '../../services/insurance-bracket.service';
import {ToastrService} from 'ngx-toastr';

@Component({
  selector: 'app-insurance-bracket-form',
  templateUrl: './insurance-bracket-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class InsuranceBracketForm implements OnInit {
  private fb = inject(FormBuilder);
  private svc = inject(InsuranceBracketService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  isEdit = false;
  bracketId = 0;
  errorMsg = signal('');

  form = this.fb.group({
    salaryBracket:           [0, [Validators.required, Validators.min(1)]],
    laborInsuranceEmployee:  [0, Validators.required],
    healthInsuranceEmployee: [0, Validators.required],
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.bracketId = +id;
      this.svc.getById(this.bracketId).subscribe(b => {
        if (b) this.form.patchValue(b);
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.svc.update(this.bracketId, value)
      : this.svc.create(value);

    this.errorMsg.set('');
    obs.subscribe({
      next: () => {
        this.toastr.success(this.isEdit ? '更新成功' : '新增成功');
        this.router.navigate(['/admin/insurance-brackets']);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }
}
