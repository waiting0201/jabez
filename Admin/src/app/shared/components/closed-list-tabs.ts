import {Component, input, output} from '@angular/core';

export type ClosedListTab = 'open' | 'closed';

/**
 * 個人申請清單「進行中 / 已結案」頁籤（2026-11，請款 / 預支 / 預支沖銷 / 出差請款 / 出差預支 / 出差預支沖銷共用）。
 * 已結案的判定在後端 ClosedListFilter（單一真相），這裡只負責切換與說明文字。
 * 頁籤狀態由各清單頁存在網址 ?tab=closed（見各 list 的 switchTab）。
 */
@Component({
  selector: 'app-closed-list-tabs',
  standalone: true,
  template: `
    <div class="flex flex-wrap items-center gap-x-3 gap-y-2 mb-4">
      <div class="flex gap-1">
        <button type="button" class="btn btn-sm"
                [class]="active() === 'open' ? 'btn-primary' : 'btn-outline-secondary'"
                (click)="tabChange.emit('open')">進行中</button>
        <button type="button" class="btn btn-sm"
                [class]="active() === 'closed' ? 'btn-primary' : 'btn-outline-secondary'"
                (click)="tabChange.emit('closed')">已結案</button>
      </div>
      @if (active() === 'closed' && hint()) {
        <span class="text-muted small">{{ hint() }}</span>
      }
    </div>
  `,
})
export class ClosedListTabs {
  active = input<ClosedListTab>('open');
  /** 已結案頁籤旁的判定說明（各申請類型不同） */
  hint = input('');
  tabChange = output<ClosedListTab>();
}
