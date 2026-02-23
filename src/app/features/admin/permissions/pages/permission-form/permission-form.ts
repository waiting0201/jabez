import {Component, inject, OnInit} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {PermissionService} from '../../services/permission.service';

@Component({
  selector: 'app-permission-form',
  templateUrl: './permission-form.html',
  imports: [ReactiveFormsModule, RouterLink],
})
export class PermissionForm implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(PermissionService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  isEdit = false;
  permId = '';

  form = this.fb.group({
    code:        ['', Validators.required],
    name:        ['', Validators.required],
    module:      ['', Validators.required],
    description: [''],
  });

  ngOnInit() {
    this.permId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.permId) {
      this.isEdit = true;
      this.service.getById(this.permId).subscribe(p => {
        if (p) this.form.patchValue({...p});
      });
    }
  }

  submit() {
    if (this.form.invalid) return;
    const value = this.form.value as any;
    const obs = this.isEdit
      ? this.service.update(this.permId, value)
      : this.service.create(value);
    obs.subscribe(() => this.router.navigate(['/admin/permissions']));
  }
}
