import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {environment} from '../../../../../../environments/environment';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {AsyncPipe, DatePipe, DecimalPipe} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {DomSanitizer} from '@angular/platform-browser';
import {EMPTY, Observable, catchError, tap} from 'rxjs';
import {FilePreviewModal, PreviewFileData} from '../../../../../shared/components/file-preview-modal';
import {AuthService} from '../../../../../core/auth/services/auth.service';
import {PaymentRequestService} from '../../../payment-requests/services/payment-request.service';
import {AdvanceRequestService} from '../../../advance-requests/services/advance-request.service';
import {AdvancePdfService} from '../../../advance-requests/services/advance-pdf.service';
import {WriteOffRequestService} from '../../../write-off-requests/services/write-off-request.service';
import {WriteOffPdfService} from '../../../write-off-requests/services/write-off-pdf.service';
import {ApprovalTaskService} from '../../services/approval-task.service';
import {
  ApprovalTask, ApprovalRecord, TaskStatus,
  TASK_STATUS_LABELS, TASK_STATUS_CLASSES,
  APPLICATION_TYPE_LABELS, APPLICATION_TYPE_CLASSES,
  PAYMENT_TYPE_LABELS, LEAVE_TYPE_LABELS,
} from '../../models/approval-task.model';

/** ArrayBuffer → base64（分塊處理避免 stack overflow） */
function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  const chunk = 8192;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
  }
  return btoa(binary);
}

/**
 * 將簽名 URL 轉為可存取的端點：
 * - 相對路徑（如 files/signatures/xxx.png）→ 加上 apiUrl 前綴
 * - 完整 blob URL → 萃取檔名，轉為 API 代理路徑
 */
function resolveSignatureUrl(url: string): string {
  if (!url.startsWith('http')) {
    // 新格式：相對路徑，直接加上 API 根路徑
    return `${environment.apiUrl}/${url}`;
  }
  // 舊格式：完整 Azure Blob URL，萃取 signatures/ 之後的路徑，改走 API 代理
  const match = url.match(/\/signatures\/(.+)$/);
  if (match) {
    return `${environment.apiUrl}/files/signatures/${match[1]}`;
  }
  return url; // fallback：無法解析時原樣回傳
}

/** 格式化日期時間（保證日期與時間之間有空格） */
function fmtDT(val: string | Date): string {
  const d = new Date(val);
  const tz = 'Asia/Taipei';
  const date = d.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: tz });
  const time = d.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: tz });
  return `${date} ${time}`;
}

/** CIS 色彩設計語言 */
const CIS = {
  forest:      [105, 159, 52]  as const,
  forestMid:   [74, 107, 58]   as const,
  accent:      [140, 115, 85]  as const,
  textPrimary: [82, 83, 88]    as const,
  textMuted:   [163, 150, 133] as const,
  bgBase:      [245, 242, 237] as const,
  bgSurface:   [253, 250, 245] as const,
  border:      [221, 214, 200] as const,
};

@Component({
  selector: 'app-approval-task-review',
  templateUrl: './approval-task-review.html',
  imports: [RouterLink, ReactiveFormsModule, AsyncPipe, DatePipe, DecimalPipe, FilePreviewModal],
})
export class ApprovalTaskReview implements OnInit {
  private service           = inject(ApprovalTaskService);
  private paymentService    = inject(PaymentRequestService);
  private advanceService    = inject(AdvanceRequestService);
  protected advancePdfService = inject(AdvancePdfService);
  private writeOffService     = inject(WriteOffRequestService);
  protected writeOffPdfService = inject(WriteOffPdfService);
  private auth              = inject(AuthService);
  private route             = inject(ActivatedRoute);
  private router            = inject(Router);
  private fb                = inject(FormBuilder);
  private sanitizer         = inject(DomSanitizer);

  task$!: Observable<ApprovalTask | undefined>;
  taskId = 0;
  applicationType = '';
  taskStatus = signal<TaskStatus>('pending');
  errorMsg = signal('');
  showNoteError = false;

  /** 已核准後：財務部/Superadmin 可更新撥款日 */
  canUpdatePaymentDate = computed(() => this.auth.isSuperAdmin() || this.auth.isFinanceDept());
  paymentDateForm = {estimatedPaymentDate: '', paidAt: ''};
  paymentDateMsg   = signal('');
  paymentDateError = signal('');

  previewFile: PreviewFileData | null = null;
  openPreview(name: string, url: string) {
    this.previewFile = {name, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url)};
  }
  closePreview() { this.previewFile = null; }

  readonly statusLabel    = TASK_STATUS_LABELS;
  readonly statusClass    = TASK_STATUS_CLASSES;
  readonly appTypeLabel   = APPLICATION_TYPE_LABELS;
  readonly appTypeClass   = APPLICATION_TYPE_CLASSES;
  readonly payTypeLabel   = PAYMENT_TYPE_LABELS;
  readonly leaveTypeLabel = LEAVE_TYPE_LABELS;

  form = this.fb.group({
    action:               ['approved', Validators.required],
    reviewNote:           [''],
    estimatedPaymentDate: [''],
    paidAt:               [''],
    closeAdvance:         [false],
  });

  ngOnInit() {
    this.applicationType = this.route.snapshot.paramMap.get('applicationType') ?? '';
    this.taskId = +this.route.snapshot.paramMap.get('id')!;
    this.task$  = this.service.getById(this.taskId, this.applicationType).pipe(
      tap(task => {
        if (!task) return;
        this.taskStatus.set(task.status);
        if (task.paymentDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.paymentDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.paymentDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
        if (task.advanceDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.advanceDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.advanceDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
        if (task.writeOffDetail) {
          this.paymentDateForm.estimatedPaymentDate = task.writeOffDetail.estimatedPaymentDate?.toString().slice(0, 10) ?? '';
          this.paymentDateForm.paidAt = task.writeOffDetail.paidAt?.toString().slice(0, 10) ?? '';
        }
      }),
      catchError((err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '載入簽核作業失敗。');
        return EMPTY;
      }),
    );
  }

  getRecord(records: ApprovalRecord[], stepOrder: number): ApprovalRecord | undefined {
    return records.find(r => r.stepOrder === stepOrder);
  }

  /** 判斷當前簽核步驟是否為財務部，或登入者為 Superadmin */
  canSetPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return step?.departmentCode === 'FIN';
  }

  /** 判斷是否顯示「預支結案」checkbox：預支沖銷申請 (write_off) 且當前步驟為財務部 */
  canCloseAdvance(task: ApprovalTask): boolean {
    if (task.applicationType !== 'write_off') return false;
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow) return false;
    const step = task.flow.steps.find(s => s.stepOrder === task.currentStepOrder);
    return step?.departmentCode === 'FIN';
  }

  /** 判斷已核准後是否可編輯撥款日：Superadmin、或曾審核過財務部步驟的使用者 */
  canEditPaymentDate(task: ApprovalTask): boolean {
    if (this.auth.isSuperAdmin()) return true;
    if (!task.flow || !task.approvalRecords?.length) return false;
    // 找出流程中所有財務部步驟（以部門代碼 'FIN' 判斷）的 stepOrder
    const financeStepOrders = task.flow.steps
      .filter(s => s.departmentCode === 'FIN')
      .map(s => s.stepOrder);
    if (!financeStepOrders.length) return false;
    // 檢查當前使用者是否審核過這些步驟
    const userName = this.auth.currentUser()?.name;
    return task.approvalRecords.some(
      r => financeStepOrders.includes(r.stepOrder) && r.reviewedBy === userName
    );
  }

  /** 更新已核准請款/預支的撥款日期 */
  updatePaymentDate(task: ApprovalTask) {
    const {estimatedPaymentDate, paidAt} = this.paymentDateForm;
    if (!estimatedPaymentDate && !paidAt) return;
    this.paymentDateMsg.set('');
    this.paymentDateError.set('');

    let update$: Observable<any>;
    if (task.writeOffDetail) {
      // 沖銷：更新關聯的預支申請撥款日
      update$ = this.advanceService.updatePaymentDate(
        task.writeOffDetail.advanceRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
    } else if (task.advanceDetail) {
      update$ = this.advanceService.updatePaymentDate(
        task.advanceDetail.advanceRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
    } else if (task.paymentDetail) {
      update$ = this.paymentService.updatePaymentDate(
        task.paymentDetail.paymentRequestId,
        estimatedPaymentDate || undefined,
        paidAt || undefined,
      );
    } else {
      return;
    }

    update$.subscribe({
      next: () => {
        this.paymentDateMsg.set('撥款日期已更新。');
        // 重新載入任務資料以反映更新
        this.task$ = this.service.getById(this.taskId, this.applicationType).pipe(
          tap(t => { if (t) this.taskStatus.set(t.status); }),
          catchError((err: HttpErrorResponse) => {
            this.errorMsg.set(err.error?.message || '載入簽核作業失敗。');
            return EMPTY;
          }),
        );
      },
      error: (err: HttpErrorResponse) => {
        this.paymentDateError.set(err.error?.message || '更新撥款日期失敗。');
      },
    });
  }

  /** 資源快取：字型只下載一次 */
  private assetCache: Promise<{regular: string; bold: string}> | null = null;
  pdfLoading = signal(false);

  private loadFonts(): Promise<{regular: string; bold: string}> {
    if (!this.assetCache) {
      this.assetCache = Promise.all([
        fetch('/assets/fonts/NotoSansTC-Regular.ttf').then(r => r.arrayBuffer()),
        fetch('/assets/fonts/NotoSansTC-Bold.ttf').then(r => r.arrayBuffer()),
      ]).then(([regular, bold]) => ({
        regular: arrayBufferToBase64(regular),
        bold: arrayBufferToBase64(bold),
      }));
    }
    return this.assetCache;
  }

  /** 列印請款單 PDF */
  printPaymentPdf(task: ApprovalTask) {
    if (!task.paymentDetail || task.status !== 'approved') return;

    this.pdfLoading.set(true);

    Promise.all([
      import('jspdf'),
      import('jspdf-autotable'),
      this.loadFonts(),
    ]).then(async ([{default: jsPDF}, {default: autoTable}, fonts]) => {
      const doc = new jsPDF('portrait', 'mm', 'a4');
      const F = 'NotoSansTC';

      doc.addFileToVFS('NotoSansTC-Regular.ttf', fonts.regular);
      doc.addFileToVFS('NotoSansTC-Bold.ttf', fonts.bold);
      doc.addFont('NotoSansTC-Regular.ttf', F, 'normal');
      doc.addFont('NotoSansTC-Bold.ttf', F, 'bold');

      const pw = doc.internal.pageSize.getWidth();   // 210
      const ph = doc.internal.pageSize.getHeight();   // 297
      const mx = 18;                                   // 左右邊距
      const cw = pw - mx * 2;                         // 內容寬度
      const d = task.paymentDetail!;
      const fmt = (n: number) => n.toLocaleString('zh-TW');

      let y = 22;

      // ── 頂部裝飾線（森林綠雙線）──
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      // ── 標題：依請款類型顯示 ──
      y += 12;
      const titleMap: Record<string, string> = {vendor: '廠 商 請 款 單', travel: '員 工 差 旅 請 款 單'};
      const pdfTitle = titleMap[d.paymentType] || '請 款 單';
      doc.setFont(F, 'bold');
      doc.setFontSize(20);
      doc.setTextColor(...CIS.forest);
      doc.text(pdfTitle, pw / 2, y, {align: 'center'});

      // ── 受款人 / 申請日期 ──
      y += 12;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);
      /** 畫標籤+值，值緊貼冒號後 */
      const lv = (label: string, value: string, x: number, yy: number, bold = false) => {
        doc.setFont(F, 'normal');
        doc.text(label, x, yy);
        const lw = doc.getTextWidth(label);
        if (bold) doc.setFont(F, 'bold');
        doc.text(value, x + lw, yy);
        doc.setFont(F, 'normal');
      };

      const submitDate = fmtDT(task.submittedAt);
      lv('受款人：', task.submittedBy, mx, y, true);
      lv('申請日期：', submitDate, pw - mx - 55, y, true);

      // ── 明細表格 ──
      y += 8;
      const invoices = d.invoices || [];
      const bodyRows = invoices.map(inv => [
        d.projectCode,
        inv.invoiceNo || '—',
        inv.itemName || '—',
        fmt(inv.amount),
        inv.note || '',
      ]);
      // 合計列
      bodyRows.push([
        {content: '合　計', colSpan: 3, styles: {halign: 'center', fontStyle: 'bold'}} as any,
        {content: fmt(d.totalAmount), styles: {fontStyle: 'bold'}} as any,
        '',
      ]);

      autoTable(doc, {
        startY: y,
        margin: {left: mx, right: mx, top: 20},
        theme: 'grid',
        showHead: 'everyPage',
        styles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary],
          lineColor: [...CIS.border],
          lineWidth: 0.3,
          cellPadding: {top: 3, bottom: 3, left: 4, right: 4},
        },
        headStyles: {
          font: F,
          fillColor: [...CIS.forest],
          textColor: 255,
          fontSize: 9.5,
          fontStyle: 'bold',
          halign: 'center',
          cellPadding: {top: 4, bottom: 4, left: 4, right: 4},
        },
        bodyStyles: {
          font: F,
          fontSize: 9,
          textColor: [...CIS.textPrimary],
        },
        columnStyles: {
          0: {cellWidth: cw * 0.14, halign: 'center'},  // 案號
          1: {cellWidth: cw * 0.16, halign: 'center'},  // 發票號碼
          2: {cellWidth: cw * 0.30},                     // 項目
          3: {cellWidth: cw * 0.16, halign: 'right'},    // 金額
          4: {cellWidth: cw * 0.24},                     // 備註
        },
        head: [['案 號', '發票號碼', '項　　目', '金　額', '備　註']],
        body: bodyRows,
      });

      // ── 簽名欄 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 16;

      // 簽名欄標題對應簽核步驟
      // Step 4: 總監, Step 3: 財務, Step 2: 會計, Step 1: 部門主管, 請款人
      const signBlocks: {label: string; signatureUrl?: string; date: string}[] = [];
      const records = task.approvalRecords || [];

      // 根據 flow steps 對應簽名欄
      const stepLabels: Record<number, string> = {};
      if (task.flow) {
        for (const step of task.flow.steps) {
          if (step.useApplicantDesignated) {
            stepLabels[step.stepOrder] = '指定審核';
          } else if (step.useDirectSupervisor) {
            stepLabels[step.stepOrder] = '上層級';
          } else if (step.jobTitleName?.includes('總監') || step.stepOrder === 4) {
            stepLabels[step.stepOrder] = '總監';
          } else if (step.departmentName?.includes('財務') || step.stepOrder === 3) {
            stepLabels[step.stepOrder] = '財務';
          } else if (step.departmentName?.includes('會計') || step.stepOrder === 2) {
            stepLabels[step.stepOrder] = '會計';
          } else if (step.stepOrder === 1) {
            stepLabels[step.stepOrder] = '部門主管';
          } else {
            stepLabels[step.stepOrder] = step.jobTitleName || `Step ${step.stepOrder}`;
          }
        }
      }

      // 按步驟順序倒排（總監在最左邊）
      const stepOrders = Object.keys(stepLabels).map(Number).sort((a, b) => b - a);
      for (const so of stepOrders) {
        const rec = records.find(r => r.stepOrder === so);
        signBlocks.push({
          label: stepLabels[so],
          signatureUrl: rec?.reviewerSignatureUrl,
          date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : '',
        });
      }

      // 出納（撥款者簽名 + 撥款日期）
      signBlocks.push({
        label: '出納',
        signatureUrl: task.paymentDetail?.paidBySignatureUrl,
        date: task.paymentDetail?.paidAt ? fmtDT(task.paymentDetail.paidAt) : '',
      });

      // 請款人（最右邊）
      signBlocks.push({
        label: '請款人',
        signatureUrl: task.submittedBySignatureUrl,
        date: submitDate,
      });

      // 預載所有簽名檔圖片（透過 resolveSignatureUrl 轉為 API 可存取路徑）
      const sigUrls = signBlocks.map(b => b.signatureUrl).filter((u): u is string => !!u);
      const sigImageMap = new Map<string, string>();
      await Promise.all(sigUrls.map(async url => {
        try {
          const fetchUrl = resolveSignatureUrl(url);
          const resp = await fetch(fetchUrl);
          const buf = await resp.arrayBuffer();
          const mime = resp.headers.get('content-type') || 'image/png';
          // Map key 仍使用原始 url，與 signBlocks 保持一致
          sigImageMap.set(url, `data:${mime};base64,${arrayBufferToBase64(buf)}`);
        } catch { /* 載入失敗則跳過，不顯示圖片 */ }
      }));

      // 如果簽名欄會超出頁面，換頁
      if (y + 40 > ph - 20) {
        doc.addPage();
        y = 30;
      }

      // 繪製簽名框
      const blockCount = signBlocks.length;
      const gap = 4;
      const blockW = (cw - gap * (blockCount - 1)) / blockCount;

      // 繪製上方分隔線
      doc.setDrawColor(...CIS.border);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);

      y += 6;
      for (let i = 0; i < blockCount; i++) {
        const bx = mx + i * (blockW + gap);
        const block = signBlocks[i];

        // 標籤
        doc.setFont(F, 'bold');
        doc.setFontSize(9);
        doc.setTextColor(...CIS.textPrimary);
        doc.text(block.label, bx + blockW / 2, y, {align: 'center'});

        // 簽名線
        const lineY = y + 16;
        doc.setDrawColor(...CIS.border);
        doc.setLineWidth(0.2);
        doc.line(bx + 2, lineY, bx + blockW - 2, lineY);

        // 簽名檔圖片（簽名線上方，等比例縮放）
        if (block.signatureUrl && sigImageMap.has(block.signatureUrl)) {
          const sigData = sigImageMap.get(block.signatureUrl)!;
          const maxW = blockW - 8;  // 左右各留 4mm
          const maxH = 12;          // 最大高度 12mm，不超出簽名欄
          try {
            const imgProps = doc.getImageProperties(sigData);
            const ratio = Math.min(maxW / imgProps.width, maxH / imgProps.height);
            const imgW = imgProps.width * ratio;
            const imgH = imgProps.height * ratio;
            const imgX = bx + (blockW - imgW) / 2;
            const imgY = lineY - imgH - 1;  // 簽名線上方 1mm
            doc.addImage(sigData, imgX, imgY, imgW, imgH);
          } catch { /* 圖片格式有誤則跳過 */ }
        }

        // 日期時間（簽名線下方）
        if (block.date) {
          doc.setFont(F, 'normal');
          doc.setFontSize(6.5);
          doc.setTextColor(...CIS.textMuted);
          doc.text(block.date, bx + blockW / 2, lineY + 5, {align: 'center'});
        }
      }

      // ── 底部裝飾線（簽名欄下方）──
      const bottomY = y + 26;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, bottomY, pw - mx, bottomY);
      doc.setLineWidth(0.8);
      doc.line(mx, bottomY + 1.5, pw - mx, bottomY + 1.5);

      doc.save(`請款單-${d.projectCode}-${task.id}.pdf`);
      this.pdfLoading.set(false);
    }).catch(() => {
      this.pdfLoading.set(false);
      this.errorMsg.set('匯出 PDF 失敗，請確認字型檔案是否存在。');
    });
  }

  /** 列印預支申請表 PDF */
  printAdvancePdf(task: ApprovalTask) {
    if (!task.advanceDetail || task.status !== 'approved') return;
    this.advanceService.getById(task.advanceDetail.advanceRequestId).subscribe({
      next: r => {
        this.advancePdfService.printAdvanceRequest(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
          task.advanceDetail?.paidBySignatureUrl,
          task.advanceDetail?.paidAt,
        );
      },
      error: () => {
        this.errorMsg.set('載入預支申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  /** 列印預支沖銷申請表 PDF */
  printWriteOffPdf(task: ApprovalTask) {
    if (!task.writeOffDetail || task.status !== 'approved') return;
    this.writeOffService.getById(task.writeOffDetail.writeOffRequestId).subscribe({
      next: r => {
        this.writeOffPdfService.printWriteOff(
          r,
          task.submittedBy,
          task.approvalRecords ?? [],
          task.flow,
          task.submittedBySignatureUrl,
          task.writeOffDetail?.paidBySignatureUrl,
          task.writeOffDetail?.paidAt,
        );
      },
      error: () => {
        this.errorMsg.set('載入預支沖銷申請資料失敗，無法匯出 PDF。');
      },
    });
  }

  submit() {
    if (this.taskStatus() !== 'pending') return;
    const action = this.form.value.action as TaskStatus;
    const note   = this.form.value.reviewNote?.trim() ?? '';
    const estimatedPaymentDate = this.form.value.estimatedPaymentDate || undefined;
    const paidAt = this.form.value.paidAt || undefined;
    const closeAdvance = this.form.value.closeAdvance ?? false;
    if ((action === 'rejected' || action === 'returned') && !note) {
      this.showNoteError = true;
      return;
    }
    this.showNoteError = false;
    this.errorMsg.set('');
    this.service.review(this.taskId, this.applicationType, action, note, estimatedPaymentDate, paidAt, closeAdvance).subscribe({
      next: () => this.router.navigate(['/admin/approval-tasks']),
      error: (err: HttpErrorResponse) => {
        this.errorMsg.set(err.error?.message || '審核失敗，請稍後再試。');
      },
    });
  }
}
