import {Component, OnInit, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {HttpErrorResponse} from '@angular/common/http';
import {DisasterDayService} from '../services/disaster-day.service';
import {SettingsService} from '../../settings/services/settings.service';
import {DISASTER_SEGMENT_LABELS, DisasterDayOff, DisasterSegment} from '../models/disaster-day.model';
import {TAIWAN_COUNTIES} from '@shared/utils/taiwan-counties';

/** 手動新增颱風假（可一次選多個縣市） */
@Component({
  selector: 'app-disaster-day-add-modal',
  imports: [FormsModule],
  template: `
    <div class="modal-header">
      <h5 class="modal-title">手動新增颱風假</h5>
      <button type="button" class="btn-close" (click)="activeModal.dismiss()"></button>
    </div>
    <div class="modal-body">
      @if (errorMsg()) {
        <div class="alert alert-danger py-2 mb-3" role="alert">{{ errorMsg() }}</div>
      }

      <div class="mb-3">
        <label class="form-label fw-500">日期 <span class="text-danger">*</span></label>
        <input type="date" class="form-control" [ngModel]="date()" (ngModelChange)="date.set($event)">
      </div>

      <div class="mb-3">
        <label class="form-label fw-500">時段 <span class="text-danger">*</span></label>
        <div class="flex flex-wrap gap-4 mt-1">
          @for (s of segments; track s) {
            <div class="form-check">
              <input class="form-check-input" type="radio" name="disasterSegment" [id]="'seg-' + s"
                     [checked]="segment() === s" (change)="segment.set(s)">
              <label class="form-check-label" [for]="'seg-' + s">{{ segmentLabels[s] }}</label>
            </div>
          }
        </div>
      </div>

      <div class="mb-3">
        <div class="form-check">
          <input class="form-check-input" type="checkbox" id="disasterIsPaid"
                 [ngModel]="isPaid()" (ngModelChange)="isPaid.set($event)">
          <label class="form-check-label fw-500" for="disasterIsPaid">給薪</label>
        </div>
        <div class="text-muted small mt-1">預設值取自系統設定「颱風假預設給薪」；不給薪時，未出勤的員工將依日薪扣薪。</div>
      </div>

      <div class="mb-0">
        <div class="flex items-center justify-between">
          <label class="form-label fw-500 mb-1">縣市 <span class="text-danger">*</span>
            <span class="text-muted small font-normal">（已選 {{ selected().size }}）</span>
          </label>
          @if (selected().size > 0) {
            <button type="button" class="btn btn-sm btn-ghost-primary" (click)="clearSelected()">清除</button>
          }
        </div>
        <div class="row g-2">
          @for (c of counties; track c.code) {
            <div class="col-6 col-md-4">
              <div class="form-check">
                <input class="form-check-input" type="checkbox" [id]="'cty-' + c.code"
                       [checked]="selected().has(c.code)" (change)="toggle(c.code)">
                <label class="form-check-label" [for]="'cty-' + c.code">{{ c.name }}</label>
              </div>
            </div>
          }
        </div>
      </div>
    </div>
    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="activeModal.dismiss()">取消</button>
      <button type="button" class="btn btn-primary inline-flex items-center gap-1"
              [disabled]="saving() || !date() || selected().size === 0" (click)="submit()">
        @if (saving()) { <span class="spinner-border spinner-border-sm" aria-hidden="true"></span> }
        建立
      </button>
    </div>
  `,
})
export class DisasterDayAddModal implements OnInit {
  activeModal = inject(NgbActiveModal);
  private svc = inject(DisasterDayService);
  private settings = inject(SettingsService);

  readonly counties = TAIWAN_COUNTIES;
  readonly segments: DisasterSegment[] = ['full', 'am', 'pm'];
  readonly segmentLabels = DISASTER_SEGMENT_LABELS;

  date = signal('');
  segment = signal<DisasterSegment>('full');
  isPaid = signal(true);
  selected = signal<Set<string>>(new Set());
  saving = signal(false);
  errorMsg = signal('');

  ngOnInit(): void {
    // 預帶系統預設；無 settings:read 權限（403）或失敗時維持「給薪」
    this.settings.get().subscribe({
      next: s => this.isPaid.set(s.disasterDayOffPaidDefault ?? true),
      error: () => { /* 維持預設給薪 */ },
    });
  }

  clearSelected(): void {
    this.selected.set(new Set());
  }

  toggle(code: string): void {
    this.selected.update(s => {
      const next = new Set(s);
      if (next.has(code)) next.delete(code); else next.add(code);
      return next;
    });
  }

  submit(): void {
    if (this.saving() || !this.date() || this.selected().size === 0) return;
    this.saving.set(true);
    this.errorMsg.set('');
    // 依 TAIWAN_COUNTIES 順序送出，結果穩定
    const counties = this.counties.filter(c => this.selected().has(c.code)).map(c => c.code);
    this.svc.create({date: this.date(), counties, segment: this.segment(), isPaid: this.isPaid()}).subscribe({
      next: (rows: DisasterDayOff[]) => this.activeModal.close(rows),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '建立失敗，請稍後再試。');
      },
    });
  }
}
