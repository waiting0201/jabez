import {Injectable, inject, signal} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {ApprovalTask} from '../../approval-tasks/models/approval-task.model';
import {PreReviewTaskDetail} from '../../approval-tasks/models/approval-task.model';
import {PdfCoreService, SignBlock, CIS, FONT_FAMILY, fmtDT, buildDynamicSignBlocks, resolveFileProxyUrl} from '../../../../shared/services/pdf-core.service';
import {environment} from '@/environments/environment';

@Injectable({providedIn: 'root'})
export class PreReviewPdfService {
  pdfLoading = signal(false);

  private pdfCore = inject(PdfCoreService);
  private http = inject(HttpClient);

  /** 列印預審單 PDF（含合併上傳檔案）*/
  async printPreReviewRequest(task: ApprovalTask) {
    if (!task.preReviewDetail || task.status !== 'approved') return;

    this.pdfLoading.set(true);
    try {
      const [{ default: jsPDF }, { default: autoTable }, fonts, { PDFDocument }] = await Promise.all([
        import('jspdf'),
        import('jspdf-autotable'),
        this.pdfCore.loadFonts(),
        import('pdf-lib'),
      ]);

      const doc = new jsPDF('portrait', 'mm', 'a4');
      const F = FONT_FAMILY;

      this.pdfCore.registerFonts(doc, fonts);

      const pw = doc.internal.pageSize.getWidth();   // 210
      const ph = doc.internal.pageSize.getHeight();   // 297
      const mx = 18;
      const cw = pw - mx * 2;
      const d = task.preReviewDetail!;
      const fmt = (n: number) => n.toLocaleString('zh-TW');

      let y = 22;

      // ── 頂部裝飾線（森林綠雙線）──
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.8);
      doc.line(mx, y, pw - mx, y);
      doc.setLineWidth(0.3);
      doc.line(mx, y + 1.5, pw - mx, y + 1.5);

      // ── 標題 ──
      y += 12;
      const pdfTitle = d.paymentType === 'designer' ? '設 計 師 預 審 單' : '協 力 廠 商 預 審 單';
      doc.setFont(F, 'bold');
      doc.setFontSize(20);
      doc.setTextColor(...CIS.forest);
      doc.text(pdfTitle, pw / 2, y, {align: 'center'});

      // ── 單號（右上角）──
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textMuted);
      doc.text(`單號：${d.requestNo}`, pw - mx, y, {align: 'right'});
      doc.setTextColor(...CIS.textPrimary);

      // ── 申請人 / 申請日期 ──
      y += 12;
      doc.setFont(F, 'normal');
      doc.setFontSize(10);
      doc.setTextColor(...CIS.textPrimary);

      const lv = (label: string, value: string, x: number, yy: number, bold = false) => {
        doc.setFont(F, 'normal');
        doc.text(label, x, yy);
        const lw = doc.getTextWidth(label);
        if (bold) doc.setFont(F, 'bold');
        doc.text(value, x + lw, yy);
        doc.setFont(F, 'normal');
      };

      const submitDate = fmtDT(task.submittedAt);
      lv('申請人：', task.submittedBy, mx, y, true);
      lv('申請日期：', submitDate, pw - mx - 55, y, true);

      // ── 預審說明 ──
      if (d.reason) {
        y += 8;
        doc.setFont(F, 'normal');
        doc.setFontSize(10);
        lv('預審說明：', d.reason, mx, y, false);
      }

      // ── 品項明細表格 ──
      y += 8;
      const items = d.items || [];
      const taxAmount = d.taxAmount ?? 0;
      const grandTotal = d.totalAmount + taxAmount;
      const bodyRows: any[] = items.map(item => [
        d.projectCode,
        item.itemCategory || '—',
        item.itemName || '—',
        item.description || '',
        fmt(item.amount),
        item.note || '',
      ]);
      // 未稅小計列
      bodyRows.push([
        {content: '未稅小計', colSpan: 4, styles: {halign: 'right', fontStyle: 'bold'}} as any,
        {content: fmt(d.totalAmount), styles: {fontStyle: 'bold', halign: 'right'}} as any,
        '',
      ]);
      // 稅金列
      bodyRows.push([
        {content: '稅　　金', colSpan: 4, styles: {halign: 'right'}} as any,
        {content: fmt(taxAmount), styles: {halign: 'right'}} as any,
        '',
      ]);
      // 含稅總計列
      bodyRows.push([
        {content: '含稅總計', colSpan: 4, styles: {halign: 'right', fontStyle: 'bold'}} as any,
        {content: fmt(grandTotal), styles: {fontStyle: 'bold', halign: 'right'}} as any,
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
          0: {cellWidth: cw * 0.13, halign: 'center'},  // 案號
          1: {cellWidth: cw * 0.17, halign: 'center'},  // 品項類別
          2: {cellWidth: cw * 0.20},                     // 項目
          3: {cellWidth: cw * 0.20},                     // 說明
          4: {cellWidth: cw * 0.14, halign: 'right'},    // 金額
          5: {cellWidth: cw * 0.16},                     // 備註
        },
        head: [['案 號', '品項類別', '項　目', '說　明', '金　額', '備　註']],
        body: bodyRows,
      });

      // ── 廠商資訊（協力廠商 / 設計師 皆顯示）──
      if (d.paymentType === 'vendor' || d.paymentType === 'designer') {
        const dash = (v?: string | null) => (v?.trim() || '—');
        const vendorTableY = (doc as any).lastAutoTable.finalY + 6;
        autoTable(doc, {
          startY: vendorTableY,
          margin: {left: mx, right: mx, top: 20},
          theme: 'grid',
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
          columnStyles: {
            0: {cellWidth: cw * 0.14, fontStyle: 'bold', halign: 'right', fillColor: [248, 250, 247]},
            1: {cellWidth: cw * 0.36},
            2: {cellWidth: cw * 0.14, fontStyle: 'bold', halign: 'right', fillColor: [248, 250, 247]},
            3: {cellWidth: cw * 0.36},
          },
          head: [[{content: '廠商資訊', colSpan: 4, styles: {halign: 'center'}}]],
          body: [
            ['廠商名稱', dash(d.vendorName), '統　　編', dash(d.vendorTaxId)],
          ],
        });
      }

      // ── 簽名欄 ──
      const tableEndY = (doc as any).lastAutoTable.finalY;
      y = tableEndY + 12;

      const signBlocks = this._buildSignBlocks(task, submitDate);
      const sigImageMap = await this.pdfCore.loadSignatureImages(signBlocks);

      if (y + 40 > ph - 20) {
        doc.addPage();
        y = 30;
      }

      this.pdfCore.drawSignatureBlock(doc, mx, pw, cw, y, signBlocks, sigImageMap, {gap: 4, labelSize: 9, maxH: 12, padding: 4});

      // ── 底部裝飾線 ──
      const bottomY = y + 34;
      doc.setDrawColor(...CIS.forest);
      doc.setLineWidth(0.3);
      doc.line(mx, bottomY, pw - mx, bottomY);
      doc.setLineWidth(0.8);
      doc.line(mx, bottomY + 1.5, pw - mx, bottomY + 1.5);

      // ── 合併上傳檔案（報價單圖檔 + 附件）──
      const formPdfBytes = doc.output('arraybuffer');
      const mergedPdf = await PDFDocument.load(formPdfBytes);

      // 收集所有需要合併的檔案 URL（先品項圖檔，再附件）
      // 私有容器（quotes / request-attachments）的原始 blob URL 需轉為 JWT 代理路徑，
      // 否則 _fetchFileBytes 直接 fetch 會 403 / CORS 而被靜默略過。
      const fileEntries: {url: string; fileName: string}[] = [];
      for (const item of items) {
        if (item.fileUrl) {
          fileEntries.push({url: resolveFileProxyUrl(item.fileUrl), fileName: item.fileName});
        }
      }
      for (const att of d.attachments ?? []) {
        if (att.fileUrl) {
          fileEntries.push({url: resolveFileProxyUrl(att.fileUrl), fileName: att.fileName});
        }
      }

      for (const entry of fileEntries) {
        try {
          const bytes = await this._fetchFileBytes(entry.url);
          if (!bytes) continue;

          const isPdf = entry.fileName?.toLowerCase().endsWith('.pdf') ||
                        this._isPdfBytes(bytes);
          if (isPdf) {
            // PDF：copyPages 併頁
            const srcDoc = await PDFDocument.load(bytes, {ignoreEncryption: true});
            const pageIndices = srcDoc.getPageIndices();
            const copiedPages = await mergedPdf.copyPages(srcDoc, pageIndices);
            copiedPages.forEach(p => mergedPdf.addPage(p));
          } else {
            // 圖片：embed + draw on A4 頁（等比縮放）
            const isJpeg = this._isJpegBytes(bytes) || /\.(jpg|jpeg)$/i.test(entry.fileName ?? '');
            let embeddedImage;
            try {
              embeddedImage = isJpeg
                ? await mergedPdf.embedJpg(bytes)
                : await mergedPdf.embedPng(bytes);
            } catch {
              // 若 embedPng 失敗則嘗試 embedJpg
              try { embeddedImage = await mergedPdf.embedJpg(bytes); } catch { continue; }
            }
            const imgPage = mergedPdf.addPage([595.28, 841.89]); // A4 in points
            const {width: imgW, height: imgH} = embeddedImage;
            const pageW = imgPage.getWidth();
            const pageH = imgPage.getHeight();
            const margin = 40;
            const maxW = pageW - margin * 2;
            const maxH = pageH - margin * 2;
            const scale = Math.min(maxW / imgW, maxH / imgH, 1);
            const drawW = imgW * scale;
            const drawH = imgH * scale;
            const x = (pageW - drawW) / 2;
            const y2 = (pageH - drawH) / 2;
            imgPage.drawImage(embeddedImage, {x, y: y2, width: drawW, height: drawH});
          }
        } catch {
          // 個別檔案合併失敗不中斷整體流程
        }
      }

      const mergedBytes = await mergedPdf.save();
      const blob = new Blob([mergedBytes.buffer as ArrayBuffer], {type: 'application/pdf'});
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `預審單-${d.requestNo}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      this.pdfLoading.set(false);
    }
  }

  /** 從 URL 取得檔案位元組；優先透過後端 proxy（帶 JWT）避免 CORS 問題 */
  private async _fetchFileBytes(url: string): Promise<ArrayBuffer | null> {
    try {
      // 若 URL 含 apiUrl 前綴或為相對路徑，透過 HttpClient（帶 JWT）
      const apiBase = environment.apiUrl;
      if (url.startsWith(apiBase) || url.startsWith('/api/')) {
        const blob = await firstValueFrom(this.http.get(url, {responseType: 'blob'}));
        return blob.arrayBuffer();
      }
      // Azure Blob Storage 公開 URL 或 SAS URL：直接 fetch
      const resp = await fetch(url);
      if (!resp.ok) return null;
      return resp.arrayBuffer();
    } catch {
      return null;
    }
  }

  /** 判斷是否為 PDF bytes（magic bytes: %PDF）*/
  private _isPdfBytes(bytes: ArrayBuffer): boolean {
    const view = new Uint8Array(bytes, 0, 4);
    return view[0] === 0x25 && view[1] === 0x50 && view[2] === 0x44 && view[3] === 0x46; // %PDF
  }

  /** 判斷是否為 JPEG bytes（magic bytes: FF D8）*/
  private _isJpegBytes(bytes: ArrayBuffer): boolean {
    const view = new Uint8Array(bytes, 0, 2);
    return view[0] === 0xFF && view[1] === 0xD8;
  }

  /** 根據 flow steps 動態建立簽名欄資料 */
  private _buildSignBlocks(task: ApprovalTask, submitDate: string): SignBlock[] {
    return buildDynamicSignBlocks({
      flow: task.flow,
      records: task.approvalRecords || [],
      submittedBySignatureUrl: task.submittedBySignatureUrl,
      submitDate,
      applicantLabel: '申請人',
    });
  }
}
