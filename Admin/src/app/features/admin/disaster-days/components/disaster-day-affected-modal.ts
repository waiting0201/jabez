import {Component, OnInit, Input, inject, signal} from '@angular/core';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {DecimalPipe} from '@angular/common';
import {DisasterAffected, DisasterDayOff, DISASTER_SEGMENT_LABELS} from '../models/disaster-day.model';
import {DisasterDayService} from '../services/disaster-day.service';
import {LEAVE_TYPE_LABELS} from '@features/admin/leave-requests/models/leave-request.model';

/** 颱風假影響名單：受影響員工 + 被退回時數的假單 */
@Component({
  selector: 'app-disaster-day-affected-modal',
  imports: [DecimalPipe],
  template: `
    <div class="modal-header">
      <h5 class="modal-title">影響名單：{{ day.date }} {{ day.countyName }}（{{ segmentLabels[day.segment] }}・{{ day.isPaid ? '給薪' : '不給薪' }}）</h5>
      <button type="button" class="btn-close" (click)="activeModal.dismiss()"></button>
    </div>
    <div class="modal-body">
      @if (loading()) {
        <div class="text-center text-muted py-4">
          <div class="spinner-border spinner-border-sm me-2" aria-hidden="true"></div>載入中...
        </div>
      } @else if (errorMsg()) {
        <div class="alert alert-danger py-2 mb-0" role="alert">{{ errorMsg() }}</div>
      } @else if (data(); as d) {
        @if (d.missingCountyCount > 0) {
          <div class="alert alert-warning flex items-center gap-2 py-2" role="alert">
            <svg class="sa-icon" style="stroke: currentColor"><use href="/assets/icons/sprite.svg#alert-triangle"></use></svg>
            <span>另有 {{ d.missingCountyCount }} 位員工未設定工作地縣市（個人與所屬部門皆未設定），無法判定是否適用，未列入下表。</span>
          </div>
        }

        <h6 class="fw-600 mb-2">受影響員工（{{ d.employees.length }} 人）</h6>
        <div class="table-responsive mb-4">
          <table class="table table-sm mb-0 min-w-[480px]">
            <thead class="table-light">
              <tr>
                <th>員工</th><th>部門</th>
                <th class="text-center">當日出勤</th>
                <th class="text-end">扣薪天數</th>
              </tr>
            </thead>
            <tbody>
              @for (e of d.employees; track e.userId) {
                <tr>
                  <td>{{ e.name }}</td>
                  <td>{{ e.departmentName }}</td>
                  <td class="text-center">
                    @if (e.worked) { <span class="badge bg-success-subtle text-success">有出勤</span> }
                    @else { <span class="text-muted">—</span> }
                  </td>
                  <td class="text-end">
                    @if (e.deductionDays != null && e.deductionDays > 0) {
                      <span class="text-danger">{{ e.deductionDays | number:'1.0-1' }}</span>
                    } @else { <span class="text-muted">—</span> }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="text-center text-muted py-3">沒有受影響的員工。</td></tr>
              }
            </tbody>
          </table>
        </div>

        <h6 class="fw-600 mb-2">時數被退回的請假單（{{ d.adjustedLeaves.length }} 張）</h6>
        <div class="table-responsive">
          <table class="table table-sm mb-0 min-w-[520px]">
            <thead class="table-light">
              <tr>
                <th>單號</th><th>員工</th><th>假別</th>
                <th class="text-end">原時數</th><th class="text-end">調整後</th>
              </tr>
            </thead>
            <tbody>
              @for (l of d.adjustedLeaves; track l.leaveRequestId) {
                <tr>
                  <td class="font-monospace small">{{ l.requestNo || '—' }}</td>
                  <td>{{ l.employeeName }}</td>
                  <td>{{ leaveLabel(l.leaveType) }}</td>
                  <td class="text-end">{{ l.originalHours | number:'1.0-1' }}</td>
                  <td class="text-end fw-600">{{ l.hours | number:'1.0-1' }}</td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="text-center text-muted py-3">沒有被調整的請假單。</td></tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
    <div class="modal-footer">
      <button type="button" class="btn btn-outline-secondary" (click)="activeModal.dismiss()">關閉</button>
    </div>
  `,
})
export class DisasterDayAffectedModal implements OnInit {
  activeModal = inject(NgbActiveModal);
  private svc = inject(DisasterDayService);

  @Input({required: true}) day!: DisasterDayOff;

  readonly segmentLabels = DISASTER_SEGMENT_LABELS;
  data = signal<DisasterAffected | null>(null);
  loading = signal(true);
  errorMsg = signal('');

  ngOnInit(): void {
    this.svc.affected(this.day.id).subscribe({
      next: d => { this.data.set(d); this.loading.set(false); },
      error: () => { this.errorMsg.set('載入影響名單失敗。'); this.loading.set(false); },
    });
  }

  leaveLabel(t: string): string {
    return (LEAVE_TYPE_LABELS as Record<string, string>)[t] ?? t;
  }
}
