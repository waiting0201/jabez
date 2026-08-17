import {ChangeDetectorRef, Component, inject, OnInit, signal, viewChild} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {AbstractControl, FormArray, FormBuilder, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {DatePipe, DecimalPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import heic2any from 'heic2any';
import {NgbModal} from '@ng-bootstrap/ng-bootstrap';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AttachmentsUpload} from '../../../../../shared/components/attachments-upload';
import {SubmitSuccessModal} from '../../../../../shared/components/submit-success-modal';
import {AttachmentItem} from '../../../approval-tasks/models/approval-task.model';
import {WriteOffRequestService} from '../../services/write-off-request.service';
import {PaymentRequestService, OcrItem} from '../../../payment-requests/services/payment-request.service';
import {validateInvoiceBuyer} from '../../../../../shared/utils/invoice-buyer-validator';
import {AdvanceSummary, ITEM_CATEGORIES, DesignatedReviewer} from '../../models/write-off-request.model';
import {AdvanceRequestItem, roundLabel} from '../../../advance-requests/models/advance-request.model';
import {JobTitleService} from '../../../job-titles/services/job-title.service';
import {UserService} from '../../../users/services/user.service';
import {ApprovalService} from '../../../approvals/services/approval.service';
import {DepartmentService} from '../../../departments/services/department.service';
import {Department} from '../../../departments/models/department.model';
import {ApprovalFlowStepSummary} from '../../../approvals/models/approval.model';
import {JobTitleLookup} from '../../../job-titles/models/job-title.model';
import {UserLookup} from '../../../users/models/user.model';
import {DesignatedReviewersPicker, DesignatedReviewerPayload} from '../../../../../shared/components/designated-reviewers-picker/designated-reviewers-picker';

import {ScrollIntoViewDirective} from '@shared/directives/scroll-into-view.directive';

/** 明細列的三個連動金額欄（總價 = 現金花費 + 支票金額） */
type AmountField = 'total' | 'cash' | 'check';

@Component({
  selector: 'app-write-off-request-form',
  templateUrl: './write-off-form.html',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DecimalPipe, DatePipe, FilePreviewModal, AttachmentsUpload, DesignatedReviewersPicker, ScrollIntoViewDirective],
})
export class WriteOffRequestForm implements OnInit {
  private fb             = inject(FormBuilder);
  private service        = inject(WriteOffRequestService);
  private paymentService = inject(PaymentRequestService);
  private jobTitleSvc    = inject(JobTitleService);
  private userSvc        = inject(UserService);
  private approvalSvc    = inject(ApprovalService);
  private deptSvc        = inject(DepartmentService);
  private route          = inject(ActivatedRoute);
  private router         = inject(Router);
  private cdr            = inject(ChangeDetectorRef);
  private modal          = inject(NgbModal);
  attachmentsUpload = viewChild(AttachmentsUpload);
  private sanitizer      = inject(DomSanitizer);

  /** 編輯模式回填的既有附件 */
  loadedAttachments: AttachmentItem[] = [];

  /**
   * 「已存在於後端的沖銷單 ID」：編輯模式進場即有；新增模式在 create 成功後填入。
   * 有值即代表後續儲存 / 送出一律走 update，不會再建一張新單。
   */
  editId: number | null = null;
  /** 路由模式旗標（僅影響標題與預支單區塊的呈現），create 成功後不改動 */
  isEdit = false;

  /** 選擇的預支申請 ID（新增模式中由使用者選擇） */
  selectedAdvanceId: number | null = null;

  /** 已撥款的預支申請清單（供新增模式下拉選擇） */
  advanceRequests = signal<AdvanceSummary[]>([]);

  /** 選中的預支申請摘要（供右側顯示金額資訊 + 下方費用明細對照） */
  get selectedAdvance(): AdvanceSummary | null {
    return this.advanceRequests().find(a => a.id === this.selectedAdvanceId) ?? null;
  }
  loadingAdvances = true;

  /** 預支批次標籤（單一真相，與 advance-requests 模組共用） */
  readonly roundLabel = roundLabel;

  /** 該批次的預支日期（比照 advance-detail.roundDate） */
  roundDate(adv: AdvanceSummary, roundNo: number): string | null {
    return adv.rounds?.find(r => r.roundNo === roundNo)?.advanceDate ?? null;
  }

  /** 是否為該批次的第一列（同批次第二列起批次欄留白，比照分類欄慣例） */
  isFirstOfRound(items: AdvanceRequestItem[], index: number): boolean {
    return index === 0 || items[index - 1].roundNo !== items[index].roundNo;
  }

  /** 編輯模式時顯示的預支申請資訊（唯讀） */
  editModeAdvanceNo = '';
  editModeProjectCode = '';
  editModeActivityName = '';
  editModeAdvanceGrandTotal = 0;
  editModeAdvanceWrittenOffTotal = 0;

  errorMsg = signal('');
  categories = ITEM_CATEGORIES;

  /**
   * 儲存 / 送出進行中：鎖住兩顆按鈕並顯示 spinner。
   * 沖銷單常帶多張發票照片，multipart 上傳需數秒，期間畫面若無反應使用者會重按，
   * 造成同一張沖銷單被建立兩筆（每按一次就是一個 POST /write-off-requests）。
   */
  saving = signal(false);

  /** 指定審核者相關 */
  hasDesignatedStep = false;
  /** 流程中所有 useApplicantDesignated=true 的步驟（傳給 picker） */
  designatedSteps: ApprovalFlowStepSummary[] = [];
  jobTitles: JobTitleLookup[] = [];
  allUsers: UserLookup[] = [];
  departments: Department[] = [];
  /** 編輯回填給 picker 的 initial（含 approvalStepOrder / selectedDepartmentId） */
  pickerInitial: DesignatedReviewer[] = [];
  /** picker 每次 change 後存放最新 payload，送出時使用 */
  private _pickerPayload: DesignatedReviewerPayload[] = [];
  /** 被抑制（部門最高層級 → 自動略過）的指定步驟 stepOrder，驗證時排除 */
  private _suppressedSteps: number[] = [];
  /** 唯讀模式下顯示的已指定審核者（從 DTO 取得） */
  readonlyDesignatedReviewers: DesignatedReviewer[] = [];

  /** picker change 事件：每次使用者操作時更新最新 payload */
  onPickerChange(payload: DesignatedReviewerPayload[]) {
    this._pickerPayload = payload;
  }

  /** picker 回報被抑制（部門最高層級 → 自動略過）的指定步驟 */
  onSuppressedSteps(stepOrders: number[]) {
    this._suppressedSteps = stepOrders;
  }

  getUserName(userId: string | null): string {
    if (!userId) return '—';
    return this.allUsers.find(u => u.id === userId)?.name ?? userId;
  }

  /** invoice id → File 物件（新上傳的檔案） */
  fileMap = new Map<string, File>();

  /** IDs of rows currently being OCR-processed */
  ocrLoadingIds = new Set<string>();
  get isAnyOcrPending(): boolean { return this.ocrLoadingIds.size > 0; }

  /** 發票買方抬頭/統編驗證警告（key = 列 id，value = 警告訊息）；僅供顯示，不阻擋送出 */
  invoiceWarnings = new Map<string, string>();

  /** OCR 填值後驗證買方抬頭/統編（僅統一發票）；不符則記錄該列警告 */
  private _checkBuyer(rowId: string, item: OcrItem) {
    if (item.docType !== 'invoice') { this.invoiceWarnings.delete(rowId); return; }
    const r = validateInvoiceBuyer(item.buyerName ?? '', item.buyerTaxId ?? '');
    if (r.level === 'warn') this.invoiceWarnings.set(rowId, r.message!);
    else this.invoiceWarnings.delete(rowId);
  }

  /** File preview modal */
  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  form = this.fb.group({
    note:  [''],
    items: this.fb.array([]),
  });

  get itemArray(): FormArray { return this.form.get('items') as FormArray; }
  get itemControls(): AbstractControl[] { return this.itemArray.controls; }

  get cashTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('cashAmount')?.value) || 0), 0);
  }
  get checkTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('checkAmount')?.value) || 0), 0);
  }
  get grandTotal(): number {
    return this.itemArray.controls.reduce((s, c) => s + (+(c.get('totalPrice')?.value) || 0), 0);
  }

  ngOnInit() {
    // 檢查簽核流程是否有「申請人指定審核」步驟（呼叫輕量端點，免 approvals:read 權限）
    this.approvalSvc.getActiveByType('write_off').subscribe(flow => {
      const designated = (flow?.steps ?? []).filter(s => s.useApplicantDesignated);
      this.hasDesignatedStep = designated.length > 0;
      this.designatedSteps = designated;

      if (this.hasDesignatedStep) {
        this.jobTitleSvc.getLookup().subscribe({ next: jts => { this.jobTitles = jts; this.cdr.markForCheck(); } });
        this.userSvc.getLookup().subscribe({
          next: users => { this.allUsers = users; this.cdr.markForCheck(); },
        });
        if (designated.some(s => s.designatedRequiresDepartment)) {
          this.deptSvc.getAll().subscribe({ next: d => { this.departments = d; this.cdr.markForCheck(); } });
        }
      }
      this.cdr.markForCheck();
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      // 編輯模式：載入現有預支沖銷申請
      this.isEdit = true;
      this.editId = +idParam;
      this.service.getById(this.editId).subscribe(r => {
        this.editModeAdvanceNo    = r.advanceRequestNo;
        this.editModeProjectCode  = r.projectCode;
        this.editModeActivityName = r.activityName;
        this.selectedAdvanceId    = r.advanceRequestId;
        this.editModeAdvanceGrandTotal      = r.advanceGrandTotal;
        this.editModeAdvanceWrittenOffTotal  = r.advanceWrittenOffTotal;
        this.form.patchValue({note: r.note ?? ''});
        this.loadedAttachments = r.attachments ?? [];
        // 回填指定審核者：唯讀模式與編輯模式皆由 pickerInitial 傳給 picker
        if (r.designatedReviewers?.length) {
          this.pickerInitial = r.designatedReviewers;
          this.readonlyDesignatedReviewers = r.designatedReviewers;
        }
        // 回填明細行（保留既有檔案 URL）
        r.items.forEach((item, idx) => {
          this.itemArray.push(this._itemGroup(
            `existing-${item.id}`,
            item.fileName ?? '',
            item.seqNo,
            item.itemName,
            item.unitPrice,
            item.quantity,
            item.totalPrice,
            item.cashAmount,
            item.checkAmount,
            item.note ?? '',
            idx,
            '',           // 無本地 blob URL
            item.fileUrl ?? '',
          ));
          const ctrl = this.itemArray.at(idx);
          // invoiceDate 需切掉後端回傳的時間部分（"2026-03-24T00:00:00"），
          // <input type="date"> 只接受 yyyy-MM-dd，否則會顯示空白
          ctrl.patchValue({
            invoiceNo:   item.invoiceNo ?? '',
            invoiceDate: item.invoiceDate?.toString().slice(0, 10) ?? '',
            category:    item.category,
          });
        });
        this.cdr.markForCheck();
      });
    } else {
      // 新增模式：載入已撥款的預支申請清單
      this.service.getAvailableAdvances().subscribe({
        next: list => {
          this.advanceRequests.set(list);
          this.loadingAdvances = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.loadingAdvances = false;
          this.errorMsg.set('載入預支申請清單失敗。');
          this.cdr.markForCheck();
        },
      });
    }
  }

  addItem() {
    this.itemArray.push(this._itemGroup('', '', 0, '', 0, '', 0, 0, 0, '', this.itemArray.length));
  }

  removeItem(i: number) {
    const ctrl = this.itemArray.at(i);
    const id  = ctrl.get('id')?.value as string;
    const url = ctrl.get('previewUrl')?.value as string;
    if (url?.startsWith('blob:')) URL.revokeObjectURL(url);
    this.fileMap.delete(id);
    this.invoiceWarnings.delete(id);
    this.amountWarnings.delete(id);
    this._pinnedTotals.delete(id);
    this.itemArray.removeAt(i);
  }

  /** 發票檔案上傳 — 自動新增行、OCR 辨識 */
  async onFilesSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const rawFiles = Array.from(input.files);
    input.value = '';

    const files = await Promise.all(rawFiles.map(f => this._convertHeicIfNeeded(f)));

    const entries = files.map(file => {
      const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
      const previewUrl = URL.createObjectURL(file);
      this.ocrLoadingIds.add(id);
      this.fileMap.set(id, file);
      this.itemArray.push(this._itemGroup(id, file.name, 0, '', 0, '', 0, 0, 0, '', this.itemArray.length, previewUrl));
      return {id, file};
    });

    // OCR 辨識（並行；一張圖可辨識出多筆 → 展開多列）
    await Promise.all(entries.map(async ({id, file}) => {
      try {
        const results = await firstValueFrom(this.paymentService.ocrInvoice(file));
        const idx = this.itemArray.controls.findIndex(c => c.get('id')?.value === id);
        // 第 1 筆填入 placeholder 列；第 2..N 筆各新增一列（共用同一檔案，各存一份複本）
        if (results.length >= 1 && idx >= 0) {
          this.itemArray.controls[idx].patchValue({
            invoiceNo:   results[0].invoiceNo ?? '',
            invoiceDate: results[0].invoiceDate ?? '',
            unitPrice:   results[0].amount ?? 0,
            totalPrice:  results[0].amount ?? 0,
            cashAmount:  results[0].amount ?? 0,
            quantity:    '1式',
            ...(results[0].docType === 'ticket' ? { note: '票號' } : {}),
          });
          // OCR 帶入的總價視為已確立，之後改現金 / 支票不反推總價
          if ((results[0].amount ?? 0) > 0) this._pinnedTotals.add(id);
          this._checkBuyer(id, results[0]);
        }
        for (const item of results.slice(1)) {
          const newId      = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
          const previewUrl = URL.createObjectURL(file);
          const amount     = item.amount ?? 0;
          this.fileMap.set(newId, file);
          const group = this._itemGroup(
            newId, file.name, 0, '', amount, '1式', amount, amount, 0,
            item.docType === 'ticket' ? '票號' : '', this.itemArray.length, previewUrl,
          );
          group.patchValue({invoiceNo: item.invoiceNo ?? '', invoiceDate: item.invoiceDate ?? ''});
          this.itemArray.push(group);
          this._checkBuyer(newId, item);
          this.itemArray.at(this.itemArray.length - 1).markAllAsTouched();
        }
      } catch {
        // OCR 失敗 — 保留空白欄位
      } finally {
        // OCR 辨識完成（無論成功或失敗）立即標記該列 touched，讓漏填的必填欄位馬上顯示紅框，
        // 避免使用者不知道表單無效、送出按鈕鎖住卻找不到原因
        this.itemArray.controls.find(c => c.get('id')?.value === id)?.markAllAsTouched();
        this.ocrLoadingIds.delete(id);
        this.cdr.markForCheck();
      }
    }));
  }

  private async _convertHeicIfNeeded(file: File): Promise<File> {
    const name = file.name.toLowerCase();
    if (!name.endsWith('.heic') && !name.endsWith('.heif')) return file;
    try {
      const blob = await heic2any({blob: file, toType: 'image/jpeg', quality: 0.85}) as Blob;
      const jpegName = file.name.replace(/\.heic$/i, '.jpg').replace(/\.heif$/i, '.jpg');
      return new File([blob], jpegName, {type: 'image/jpeg'});
    } catch {
      return file;
    }
  }

  calcTotal(ctrl: AbstractControl) {
    const unitPrice = +(ctrl.get('unitPrice')?.value) || 0;
    const qtyStr = (ctrl.get('quantity')?.value ?? '').toString();
    const qtyNum = parseFloat(qtyStr) || 0;
    const total = Math.round(unitPrice * qtyNum);
    // 單價 / 數量 算出的總價視為已確立；支票金額保留，由現金吸收差額（支票為 0 時即等於總價，同舊行為）
    this.setTotal(ctrl, total);
  }

  /**
   * 金額三欄連動：**總價 = 現金花費 + 支票金額**，輸入其中兩欄自動算出第三欄。
   *
   * 推算哪一欄取決於總價是否「已確立」（單價×數量 / 手動輸入 / OCR / 編輯載入）：
   * - 已確立 → 改現金推支票、改支票推現金（總價不被反推變動，維持與單價×數量一致）
   * - 未確立（手動新增的空白列）→ 現金 + 支票 反推總價
   *
   * 存放已確立總價的列 id。
   */
  private _pinnedTotals = new Set<string>();

  /** 總價 ≠ 現金 + 支票 時的提示（key = 列 id）；僅顯示，不阻擋送出 */
  amountWarnings = new Map<string, string>();

  /** 總價由外部（單價×數量 / OCR / 編輯載入）寫入：標記已確立並讓現金吸收差額 */
  setTotal(ctrl: AbstractControl, total: number) {
    ctrl.get('totalPrice')?.setValue(total, {emitEvent: false});
    this._pinnedTotals.add(ctrl.get('id')?.value);
    this.onAmountInput(ctrl, 'total');
  }

  onAmountInput(ctrl: AbstractControl, field: AmountField) {
    const id  = ctrl.get('id')?.value as string;
    const val = (name: string) => Math.max(0, +(ctrl.get(name)?.value) || 0);
    const set = (name: string, v: number) => ctrl.get(name)?.setValue(v, {emitEvent: false});

    if (field === 'total') this._pinnedTotals.add(id);

    if (!this._pinnedTotals.has(id)) {
      set('totalPrice', val('cashAmount') + val('checkAmount'));
    } else if (field === 'cash') {
      set('checkAmount', Math.max(0, val('totalPrice') - val('cashAmount')));
    } else {
      set('cashAmount', Math.max(0, val('totalPrice') - val('checkAmount')));
    }

    // 推算欄被 0 截斷時（如支票金額大於總價）三欄會對不起來，出提示讓使用者自行修正
    const sum = val('cashAmount') + val('checkAmount');
    if (sum !== val('totalPrice')) {
      this.amountWarnings.set(id, `現金花費 + 支票金額（${sum.toLocaleString()}）與總價（${val('totalPrice').toLocaleString()}）不符，請確認。`);
    } else {
      this.amountWarnings.delete(id);
    }
  }

  /**
   * 表單內按 Enter 不送出（textarea 換行不受影響）。
   * `<form (ngSubmit)="save()">` 會讓任一 input 的 Enter 直接建立草稿並跳回列表，
   * 使用者只會看到頁面莫名跳走，誤以為資料沒存到而重做一次。
   */
  onEnterKey(event: Event) {
    const tag = (event.target as HTMLElement)?.tagName;
    if (tag !== 'TEXTAREA') event.preventDefault();
  }

  /** 儲存（草稿或更新，不改變狀態） */
  save() {
    if (this.saving()) return;
    if (this.itemArray.length === 0) return;
    if (!this.isEdit && !this.selectedAdvanceId) {
      this.errorMsg.set('請選擇預支單。');
      return;
    }
    const fd = this._buildFormData();
    this.errorMsg.set('');
    this.saving.set(true);
    const obs = this.editId
      ? this.service.update(this.editId, fd)
      : this.service.create(fd);
    obs.subscribe({
      next: () => this.router.navigate(['/admin/write-off-requests']),
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  /** 送出申請（先儲存再將狀態改為 pending） */
  submitForApproval() {
    if (this.saving()) return;
    if (this.itemArray.length === 0) return;
    if (!this.isEdit && !this.selectedAdvanceId) {
      this.errorMsg.set('請選擇預支單。');
      return;
    }
    // 流程含「申請人指定審核」步驟時，每個 designated step 至少需要 1 位指定審核者（被抑制者除外）
    if (this.hasDesignatedStep) {
      for (const step of this.designatedSteps) {
        if (this._suppressedSteps.includes(step.stepOrder)) continue;
        const hasForStep = this._pickerPayload.some(p => p.approvalStepOrder === step.stepOrder);
        if (!hasForStep) {
          this.errorMsg.set(`此簽核流程的步驟 ${step.stepOrder} 包含申請人指定審核，請新增至少 1 位審核者。`);
          return;
        }
      }
    }
    const fd = this._buildFormData();
    this.errorMsg.set('');
    this.saving.set(true);
    const save$ = this.editId
      ? this.service.update(this.editId, fd)
      : this.service.create(fd);
    save$.subscribe({
      next: saved => {
        // 草稿已建立 → 記住 ID，後續重送走 update。
        // 否則 submit 失敗時（指定審核者驗證、預支單狀態改變…）使用者重按送出，
        // 會再 POST 一張全新的沖銷單，變成同一筆沖銷有兩張單。
        this.editId = saved.id;
        this.service.submit(saved.id).subscribe({
          next: () => {
            this.saving.set(false);
            this._onSubmitted(['/admin/write-off-requests']);
          },
          error: (err: HttpErrorResponse) => {
            this.saving.set(false);
            this.errorMsg.set(
              (err.error?.message || '送出失敗，請稍後再試。') + '（草稿已保留，修正後可直接再送出）');
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.errorMsg.set(err.error?.message || '儲存失敗，請稍後再試。');
      },
    });
  }

  private _onSubmitted(target: unknown[]) {
    const ref = this.modal.open(SubmitSuccessModal, { centered: true, backdrop: 'static', keyboard: false });
    ref.componentInstance.formType = 'write_off';
    ref.result.then(() => this.router.navigate(target))
              .catch(() => this.router.navigate(target));
  }

  private _buildFormData(): FormData {
    const fd = new FormData();
    fd.append('note', this.form.get('note')?.value || '');

    // 新增模式需帶入 advanceRequestId
    if (!this.isEdit && this.selectedAdvanceId) {
      fd.append('advanceRequestId', String(this.selectedAdvanceId));
    }

    // 指定審核者清單（從 picker payload 組成，含 approvalStepOrder 與 selectedDepartmentId）
    if (this._pickerPayload.length > 0) {
      fd.append('designatedReviewers', JSON.stringify(this._pickerPayload));
    }

    const itemsMeta: object[] = [];
    let fileIndex = 0;
    let sortIdx = 0;

    for (const ctrl of this.itemArray.controls) {
      const id   = ctrl.get('id')?.value;
      const file = this.fileMap.get(id);
      const meta = {
        category:    ctrl.get('category')?.value || '',
        seqNo:       +(ctrl.get('seqNo')?.value) || 0,
        itemName:    ctrl.get('itemName')?.value || '',
        unitPrice:   +(ctrl.get('unitPrice')?.value) || 0,
        quantity:    ctrl.get('quantity')?.value || '',
        totalPrice:  +(ctrl.get('totalPrice')?.value) || 0,
        cashAmount:  +(ctrl.get('cashAmount')?.value) || 0,
        checkAmount: +(ctrl.get('checkAmount')?.value) || 0,
        note:        ctrl.get('note')?.value || null,
        invoiceNo:   ctrl.get('invoiceNo')?.value || null,
        invoiceDate: ctrl.get('invoiceDate')?.value || null,
        fileName:    ctrl.get('fileName')?.value || null,
        fileUrl:     ctrl.get('fileUrl')?.value || null,
        fileIndex:   file ? fileIndex : -1,
        sortOrder:   sortIdx++,
      };
      if (file) {
        fd.append('files', file, file.name);
        fileIndex++;
      }
      itemsMeta.push(meta);
    }

    fd.append('items', JSON.stringify(itemsMeta));

    // 整單批次附件
    const att = this.attachmentsUpload();
    fd.append('attachments', JSON.stringify(att ? att.getMeta() : []));
    if (att) att.getNewFiles().forEach(f => fd.append('attachmentFiles', f, f.name));
    return fd;
  }

  private _itemGroup(
    id: string, fileName: string, seqNo: number, itemName: string, unitPrice: number,
    quantity: string, totalPrice: number, cashAmount: number, checkAmount: number,
    note: string, sortOrder: number, previewUrl = '', fileUrl = ''
  ) {
    const rowId = id || `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    // 帶總價進來的列（OCR 展開 / 編輯模式回填）總價視為已確立，改現金 / 支票時不反推總價
    if (totalPrice > 0) this._pinnedTotals.add(rowId);
    return this.fb.group({
      id:          [rowId],
      fileName:    [fileName],
      invoiceNo:   [''],
      invoiceDate: [''],
      category:    [''],
      seqNo:       [seqNo],
      itemName:    [itemName],
      unitPrice:   [unitPrice, [Validators.min(0)]],
      quantity:    [quantity],
      totalPrice:  [totalPrice],
      cashAmount:  [cashAmount],
      checkAmount: [checkAmount],
      note:        [note],
      previewUrl:  [previewUrl],
      fileUrl:     [fileUrl],
      sortOrder:   [sortOrder],
    });
  }
}
