import { Injectable, inject, signal } from '@angular/core';
import { HolidayTravelRequest } from '../models/holiday-travel-request.model';
import { ApprovalRecord, ApprovalFlow } from '../../approval-tasks/models/approval-task.model';
import { PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, fmtDate, buildDynamicSignBlocks } from '../../../../shared/services/pdf-core.service';

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

      // ── 單號（右上角）──
      doc.setFont(F, 'normal');
      doc.setFontSize(9.5);
      doc.setTextColor(...CIS.textMuted);
      doc.text(`單號：${r.requestNo}`, pw - mx, y, { align: 'right' });
      doc.setTextColor(...CIS.textPrimary);

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

      // ── 參與執行人員（有勾選參與日期者附註日期；未列日期＝全程參與）──
      if (r.participants && r.participants.length > 0) {
        y += 6;
        const names = r.participants
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map(p => {
            const name = p.userName || p.userId;
            const dates = (p.dates ?? []).map(d => {
              const [, m, day] = String(d).slice(0, 10).split('-');
              return `${+m}/${+day}`;
            });
            return dates.length > 0 ? `${name}（${dates.join('、')}）` : name;
          })
          .join('、');
        const label = '參與執行人員：';
        doc.setFont(F, 'normal');
        const lw = doc.getTextWidth(label);
        const lines: string[] = doc.splitTextToSize(names, cw - lw);
        doc.text(label, mx, y);
        lines.forEach((line, idx) => doc.text(line, mx + lw, y + idx * 5));
        y += (lines.length - 1) * 5;

        const hasAnyDates = r.participants.some(p => p.dates?.length);
        if (hasAnyDates) {
          y += 5;
          doc.setFontSize(8);
          doc.setTextColor(...CIS.textMuted);
          doc.text('未列參與日期者為全程參與。', mx + lw, y);
          doc.setFontSize(9.5);
          doc.setTextColor(...CIS.textPrimary);
        }
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

      doc.save(`假日執行活動申請單-${r.requestNo}.pdf`);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 根據 flow steps 動態建立簽名欄資料 */
  private _buildSignBlocks(
    flow: ApprovalFlow | undefined,
    records: ApprovalRecord[],
    submittedBySignatureUrl: string | undefined,
    submitDate: string,
    applicantLabel: string,
  ): SignBlock[] {
    return buildDynamicSignBlocks({
      flow,
      records,
      submittedBySignatureUrl,
      submitDate,
      applicantLabel,
    });
  }
}
