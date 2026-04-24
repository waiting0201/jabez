import { Injectable, inject, signal } from '@angular/core';
import { HolidayTravelRequest } from '../models/holiday-travel-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmtDate } from '../../../../shared/services/pdf-core.service';

@Injectable({ providedIn: 'root' })
export class HolidayTravelPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);

  /** 列印假日執行活動申請單 */
  async printHolidayTravelRequest(
    r: HolidayTravelRequest,
    submittedByName: string,
    approvalRecords: ApprovalRecord[] = [],
    flow?: ApprovalFlow,
    submittedBySignatureUrl?: string,
  ) {
    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, fonts] = await Promise.all([
        import('jspdf'),
        this.pdfCore.loadFonts(),
      ]);

      const doc = new jsPDF('landscape', 'mm', 'a4');
      const F = FONT_FAMILY;
      this.pdfCore.registerFonts(doc, fonts);

      const pw = doc.internal.pageSize.getWidth();   // 297
      const ph = doc.internal.pageSize.getHeight();   // 210
      const mx = 14;
      const cw = pw - mx * 2;

      let y = 16;

      // ── 頂部裝飾線 ──
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      // ── 公司名稱 + 標題 ──
      y += 10;
      doc.setFont(F, 'bold');
      doc.setFontSize(11);
      doc.setTextColor(...CIS.textPrimary);
      doc.text('雅比斯國際創意策略股份有限公司', pw / 2, y, { align: 'center' });
      y += 8;
      doc.setFontSize(16);
      doc.setTextColor(...CIS.forest);
      doc.text('假 日 執 行 活 動 申 請 單', pw / 2, y, { align: 'center' });

      // ── 表頭資訊 ──
      y += 10;
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
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

      const startDate = r.startDate ? fmtDate(r.startDate) : '';
      const endDate = r.endDate ? fmtDate(r.endDate) : '';

      lv('申請人：', submittedByName, mx, y, true);
      lv('執行活動地點：', r.destination, pw / 2, y, true);

      y += 6;
      lv('活動期間：', `${startDate} ～ ${endDate}`, mx, y);
      lv('假日天數：', r.holidayDays != null ? `${r.holidayDays} 天` : '—', pw - mx - 50, y);

      y += 6;
      if (r.projectCode || r.projectName) {
        lv('關聯專案：', `${r.projectCode ?? ''}${r.projectName ? ' - ' + r.projectName : ''}`, mx, y, true);
      }

      y += 6;
      lv('活動主旨及內容：', r.purpose || '', mx, y);

      // ── 參與執行人員 ──
      if (r.participants && r.participants.length > 0) {
        y += 6;
        const names = r.participants
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map(p => p.userName || p.userId)
          .join('、');
        lv('參與執行人員：', names, mx, y);
      }

      // ── 簽名欄 ──
      y += 16;

      if (y + 35 > ph - 15) { doc.addPage(); y = 20; }

      const submitDate = r.createdAt ? fmtDT(r.createdAt) : '';
      const signBlocks = this._buildSignBlocks(flow, approvalRecords, submittedBySignatureUrl, submitDate, '申請者');
      const sigMap = await this.pdfCore.loadSignatureImages(signBlocks);
      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigMap);

      // ── 底部裝飾線 ──
      y += 30;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.8);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      doc.save(`假日執行活動申請單-${r.id}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 根據簽核流程和記錄建立簽名欄資料 */
  private _buildSignBlocks(
    flow: ApprovalFlow | undefined,
    records: ApprovalRecord[],
    submittedBySignatureUrl: string | undefined,
    submitDate: string,
    applicantLabel: string,
  ): SignBlock[] {
    const blocks: SignBlock[] = [];

    // 從流程步驟建立 stepOrder → label 對照
    const stepLabels: Record<number, string> = {};
    if (flow) {
      for (const step of flow.steps) {
        if (step.jobTitleName?.includes('總監') || step.departmentName?.includes('總監')) {
          stepLabels[step.stepOrder] = '總監核准';
        } else if (step.departmentName?.includes('財務')) {
          stepLabels[step.stepOrder] = '財務部簽核';
        } else if (step.departmentName?.includes('會計')) {
          stepLabels[step.stepOrder] = '會計';
        } else if (step.stepOrder === 1) {
          stepLabels[step.stepOrder] = '部門主管';
        } else {
          stepLabels[step.stepOrder] = step.note || step.jobTitleName || `Step ${step.stepOrder}`;
        }
      }
    }

    // 建立 label → record 對照
    const labelRecordMap = new Map<string, ApprovalRecord>();
    for (const rec of records) {
      const label = stepLabels[rec.stepOrder];
      if (label) labelRecordMap.set(label, rec);
    }

    // 固定簽名欄標籤順序
    const fixedLabels = ['總監核准', '財務部簽核', '會計', '部門主管'];

    for (const label of fixedLabels) {
      const rec = labelRecordMap.get(label);
      blocks.push({
        label,
        signatureUrl: rec?.reviewerSignatureUrl,
        date: rec?.reviewedAt ? fmtDT(rec.reviewedAt) : '',
      });
    }

    // 申請者（最右邊）
    blocks.push({
      label: applicantLabel,
      signatureUrl: submittedBySignatureUrl,
      date: submitDate,
    });

    return blocks;
  }
}
