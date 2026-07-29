import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {WriteOffRequestService} from '../../services/write-off-request.service';
import {
  AdvanceWriteOffOverview, WriteOffRequest,
  APPROVAL_STATUS_LABELS, APPROVAL_STATUS_CLASSES,
} from '../../models/write-off-request.model';
import {AdvanceRequest, roundLabel} from '../../../advance-requests/models/advance-request.model';
import {AttachmentsList} from '../../../../../shared/components/attachments-list';
import {InstallmentsTable} from '../../../../../shared/components/installments-table';
import {HasPermissionDirective} from '@shared/directives/has-permission.directive';

/**
 * 預支沖銷彙總頁：以「預支單」為母層，一次看完該張預支單的完整資訊
 * 與其底下每一張沖銷單的完整資訊（明細 / 附件 / 差額撥款）。
 * 由沖銷清單母層列的「檢視」進入。
 */
@Component({
  selector: 'app-write-off-overview',
  templateUrl: './write-off-overview.html',
  imports: [RouterLink, DecimalPipe, DatePipe, FilePreviewModal, AttachmentsList, InstallmentsTable, HasPermissionDirective],
})
export class WriteOffOverview implements OnInit {
  private service   = inject(WriteOffRequestService);
  private route     = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);

  /** File preview modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  overview = signal<AdvanceWriteOffOverview | null>(null);
  errorMsg = signal('');

  readonly statusLabel = APPROVAL_STATUS_LABELS;
  readonly statusClass = APPROVAL_STATUS_CLASSES;
  readonly roundLabel  = roundLabel;

  advance   = computed<AdvanceRequest | null>(() => this.overview()?.advance ?? null);
  writeOffs = computed<WriteOffRequest[]>(() => this.overview()?.writeOffs ?? []);

  /** 已沖銷加總（已拒絕的不計入，與沖銷金額摘要同一規則）*/
  writtenOffTotal = computed(() =>
    this.writeOffs().filter(w => w.approvalStatus !== 'rejected').reduce((acc, w) => acc + w.grandTotal, 0));

  /** 待沖銷餘額 = 預支總額 − 已沖銷加總 */
  balance = computed(() => (this.advance()?.grandTotal ?? 0) - this.writtenOffTotal());

  /** 應撥差額加總（沖銷超出預支、公司須補撥給員工的部分）*/
  refundDueTotal = computed(() =>
    this.writeOffs().filter(w => w.approvalStatus !== 'rejected').reduce((acc, w) => acc + (w.refundDue ?? 0), 0));

  ngOnInit() {
    const advanceId = +this.route.snapshot.paramMap.get('advanceId')!;
    this.service.getByAdvance(advanceId).subscribe({
      next:  o => this.overview.set(o),
      error: () => this.errorMsg.set('查無此預支單的沖銷資料，或您沒有檢視權限。'),
    });
  }

  /** 明細列是否為該批次第一列（同批次第二列起批次欄留白）*/
  isFirstOfRound(r: AdvanceRequest, index: number): boolean {
    return index === 0 || r.items[index - 1].roundNo !== r.items[index].roundNo;
  }

  roundDate(r: AdvanceRequest, roundNo: number): string | null {
    return r.rounds?.find(x => x.roundNo === roundNo)?.advanceDate ?? null;
  }

  /** 有支票金額的明細筆數（支票由公司直接付廠商，此處唯讀）*/
  checkItemCount(items: {checkAmount: number}[]): number {
    return items.filter(i => i.checkAmount > 0).length;
  }

  /** 已註記支票支付的明細筆數 */
  checkPaidCount(items: {checkAmount: number; checkPaid?: boolean}[]): number {
    return items.filter(i => i.checkAmount > 0 && i.checkPaid).length;
  }
}
