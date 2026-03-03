import {Component, inject, signal, computed} from '@angular/core';
import {DecimalPipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {PayrollService} from '../../services/payroll.service';
import {MonthlyPayroll, EmployeePayroll} from '../../models/payroll.model';

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

/** CIS 色彩設計語言（對應 tailwind.css :root Design Tokens） */
const CIS = {
  forest:      [45, 64, 46]    as const,  // #2D402E
  forestMid:   [74, 107, 58]   as const,  // #4A6B3A
  accent:      [140, 115, 85]  as const,  // #8C7355
  textPrimary: [45, 58, 46]    as const,  // #2D3A2E
  textMuted:   [163, 150, 133] as const,  // #A39685
  bgBase:      [245, 242, 237] as const,  // #F5F2ED
  bgSurface:   [253, 250, 245] as const,  // #FDFAF5
  border:      [221, 214, 200] as const,  // #DDD6C8
  softRed:     [160, 64, 64]   as const,  // #A04040
};

@Component({
  selector: 'app-payroll-list',
  templateUrl: './payroll-list.html',
  imports: [DecimalPipe, FormsModule],
})
export class PayrollList {
  private service = inject(PayrollService);

  private now = new Date();
  selectedYear  = signal(this.now.getFullYear());
  selectedMonth = signal(this.now.getMonth() + 1);

  yearMonth = computed(() => {
    const y = this.selectedYear();
    const m = String(this.selectedMonth()).padStart(2, '0');
    return `${y}-${m}`;
  });

  loading = signal(false);
  payroll = signal<MonthlyPayroll | null>(null);
  errorMsg = signal('');

  employees = computed(() => this.payroll()?.employees ?? []);

  /** 資源快取：字型 + Logo 只下載一次 */
  private assetCache: Promise<{regular: string; bold: string; logo: string}> | null = null;

  onMonthChange(value: string) {
    const [y, m] = value.split('-').map(Number);
    if (y && m) {
      this.selectedYear.set(y);
      this.selectedMonth.set(m);
    }
  }

  search() {
    this.loading.set(true);
    this.errorMsg.set('');
    this.service.getMonthly(this.selectedYear(), this.selectedMonth()).subscribe({
      next: data => {
        this.payroll.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.payroll.set(null);
        this.errorMsg.set('載入薪資資料失敗，請稍後再試。');
      },
    });
  }

  private loadAssets(): Promise<{regular: string; bold: string; logo: string}> {
    if (!this.assetCache) {
      this.assetCache = Promise.all([
        fetch('/assets/fonts/NotoSansTC-Regular.ttf').then(r => r.arrayBuffer()),
        fetch('/assets/fonts/NotoSansTC-Bold.ttf').then(r => r.arrayBuffer()),
        fetch('/assets/img/logo.jpg').then(r => r.arrayBuffer()),
      ]).then(([regular, bold, logo]) => ({
        regular: arrayBufferToBase64(regular),
        bold: arrayBufferToBase64(bold),
        logo: arrayBufferToBase64(logo),
      }));
    }
    return this.assetCache;
  }

  exportPdf() {
    const p = this.payroll();
    if (!p || p.employees.length === 0) return;

    this.loading.set(true);
    this.errorMsg.set('');

    Promise.all([
      import('jspdf'),
      import('jspdf-autotable'),
      this.loadAssets(),
    ]).then(([{default: jsPDF}, {default: autoTable}, assets]) => {
      const doc = new jsPDF('portrait', 'mm', 'a4');
      const F = 'NotoSansTC';

      // 註冊中文字型
      doc.addFileToVFS('NotoSansTC-Regular.ttf', assets.regular);
      doc.addFileToVFS('NotoSansTC-Bold.ttf', assets.bold);
      doc.addFont('NotoSansTC-Regular.ttf', F, 'normal');
      doc.addFont('NotoSansTC-Bold.ttf', F, 'bold');

      const pw = doc.internal.pageSize.getWidth();
      const ph = doc.internal.pageSize.getHeight();
      const mx = 18;
      const cw = pw - mx * 2;
      const fmt = (n: number) => n.toLocaleString('zh-TW');
      const printDate = new Date().toLocaleDateString('zh-TW', {year: 'numeric', month: '2-digit', day: '2-digit'});

      p.employees.forEach((emp, idx) => {
        if (idx > 0) doc.addPage();
        this.renderPaySlip(doc, autoTable, emp, p, idx, {pw, ph, mx, cw, F, fmt, printDate, logo: assets.logo});
      });

      doc.save(`payroll-${p.year}-${String(p.month).padStart(2, '0')}.pdf`);
      this.loading.set(false);
    }).catch(() => {
      this.loading.set(false);
      this.errorMsg.set('匯出 PDF 失敗，請確認字型檔案是否存在。');
    });
  }

  /** 繪製單一員工薪資明細頁 */
  private renderPaySlip(
    doc: any, autoTable: any,
    emp: EmployeePayroll, p: MonthlyPayroll, idx: number,
    cfg: {pw: number; ph: number; mx: number; cw: number; F: string; fmt: (n: number) => string; printDate: string; logo: string},
  ) {
    const {pw, ph, mx, cw, F, fmt, printDate, logo} = cfg;
    let y = 20;

    // ── 頂部裝飾線（森林綠雙線） ──
    doc.setDrawColor(...CIS.forest);
    doc.setLineWidth(0.8);
    doc.line(mx, y, pw - mx, y);
    doc.setLineWidth(0.3);
    doc.line(mx, y + 1.5, pw - mx, y + 1.5);

    // ── Logo + 標題區 ──
    y += 6;
    const logoW = 48;
    const logoH = 12;
    doc.addImage(`data:image/jpeg;base64,${logo}`, 'JPEG', mx, y, logoW, logoH);

    doc.setFont(F, 'bold');
    doc.setFontSize(18);
    doc.setTextColor(...CIS.forest);
    doc.text('薪資明細表', pw - mx, y + 5, {align: 'right'});
    doc.setFont(F, 'normal');
    doc.setFontSize(10);
    doc.setTextColor(...CIS.textMuted);
    doc.text(`${p.year} 年 ${p.month} 月`, pw - mx, y + 11, {align: 'right'});
    y += logoH + 2;

    // ── 員工資訊卡（暖色底） ──
    y += 10;
    const infoY = y;
    doc.setFillColor(...CIS.bgBase);
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.3);
    doc.roundedRect(mx, infoY, cw, 24, 2, 2, 'FD');

    const col1 = mx + 6;
    const col2 = mx + 24;
    const col3 = mx + cw / 2 + 6;
    const col4 = mx + cw / 2 + 24;

    y = infoY + 9;
    doc.setFont(F, 'normal');
    doc.setFontSize(8.5);
    doc.setTextColor(...CIS.textMuted);
    doc.text('姓名', col1, y);
    doc.text('部門', col3, y);
    doc.setFont(F, 'bold');
    doc.setFontSize(10);
    doc.setTextColor(...CIS.textPrimary);
    doc.text(emp.employeeName, col2, y);
    doc.text(emp.departmentName || '---', col4, y);

    y += 10;
    doc.setFont(F, 'normal');
    doc.setFontSize(8.5);
    doc.setTextColor(...CIS.textMuted);
    doc.text('職稱', col1, y);
    doc.text('到職日', col3, y);
    doc.setFont(F, 'bold');
    doc.setFontSize(10);
    doc.setTextColor(...CIS.textPrimary);
    doc.text(emp.jobTitleName || '---', col2, y);
    const hireDateStr = emp.hireDate
      ? new Date(emp.hireDate).toLocaleDateString('zh-TW', {year: 'numeric', month: '2-digit', day: '2-digit'})
      : '---';
    doc.text(hireDateStr, col4, y);

    // ── 薪資項目表格（森林綠表頭） ──
    y = infoY + 34;

    autoTable(doc, {
      startY: y,
      margin: {left: mx, right: mx},
      theme: 'plain',
      styles: {font: F},
      headStyles: {
        font: F, fillColor: [...CIS.forest], textColor: 255,
        fontSize: 9, fontStyle: 'bold',
        cellPadding: {top: 4, bottom: 4, left: 6, right: 6},
      },
      bodyStyles: {
        font: F, fontSize: 9, textColor: [...CIS.textPrimary],
        cellPadding: {top: 3.5, bottom: 3.5, left: 6, right: 6},
      },
      alternateRowStyles: {fillColor: [...CIS.bgSurface]},
      columnStyles: {
        0: {cellWidth: cw * 0.5},
        1: {cellWidth: cw * 0.5, halign: 'right'},
      },
      head: [['薪資項目', '金額 (NT$)']],
      body: [
        ['底薪', fmt(emp.baseSalary)],
        ['日薪（底薪 ÷ 30）', fmt(emp.dailySalary)],
        ['假日出差天數', `${emp.holidayTravelDays} 天`],
        ['假日津貼（日薪 × 天數）', fmt(emp.holidayAllowance)],
      ],
    });

    // ── 應發合計列（淺綠底） ──
    const afterSalary = (doc as any).lastAutoTable.finalY;
    y = afterSalary;
    doc.setFillColor(232, 240, 228);
    doc.rect(mx, y, cw, 9, 'F');
    doc.setFont(F, 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...CIS.forestMid);
    doc.text('應發合計', mx + 6, y + 6);
    doc.text(fmt(emp.baseSalary + emp.holidayAllowance), pw - mx - 6, y + 6, {align: 'right'});
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.2);
    doc.line(mx, y + 9, pw - mx, y + 9);

    // ── 扣款項目表格（柔紅表頭） ──
    y += 16;
    autoTable(doc, {
      startY: y,
      margin: {left: mx, right: mx},
      theme: 'plain',
      styles: {font: F},
      headStyles: {
        font: F, fillColor: [...CIS.softRed], textColor: 255,
        fontSize: 9, fontStyle: 'bold',
        cellPadding: {top: 4, bottom: 4, left: 6, right: 6},
      },
      bodyStyles: {
        font: F, fontSize: 9, textColor: [...CIS.textPrimary],
        cellPadding: {top: 3.5, bottom: 3.5, left: 6, right: 6},
      },
      alternateRowStyles: {fillColor: [252, 245, 245]},
      columnStyles: {
        0: {cellWidth: cw * 0.5},
        1: {cellWidth: cw * 0.5, halign: 'right'},
      },
      head: [['扣款項目', '金額 (NT$)']],
      body: [
        ['勞保費（員工負擔）', fmt(emp.laborInsurance)],
        ['健保費（員工負擔）', fmt(emp.healthInsurance)],
      ],
    });

    // ── 扣款合計列（淺紅底） ──
    const afterDeduct = (doc as any).lastAutoTable.finalY;
    y = afterDeduct;
    doc.setFillColor(248, 230, 230);
    doc.rect(mx, y, cw, 9, 'F');
    doc.setFont(F, 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...CIS.softRed);
    doc.text('扣款合計', mx + 6, y + 6);
    doc.text(`-${fmt(emp.laborInsurance + emp.healthInsurance)}`, pw - mx - 6, y + 6, {align: 'right'});
    doc.setDrawColor(...CIS.border);
    doc.setLineWidth(0.2);
    doc.line(mx, y + 9, pw - mx, y + 9);

    // ── 實領薪資區塊（森林綠底） ──
    y += 20;
    doc.setFillColor(...CIS.forest);
    doc.roundedRect(mx, y, cw, 18, 2, 2, 'F');
    doc.setFont(F, 'bold');
    doc.setFontSize(11);
    doc.setTextColor(255, 255, 255);
    doc.text('實領薪資', mx + 8, y + 11.5);
    doc.setFontSize(16);
    doc.text(`NT$ ${fmt(emp.netSalary)}`, pw - mx - 8, y + 12, {align: 'right'});

    // ── 計算公式說明 ──
    y += 28;
    doc.setFont(F, 'normal');
    doc.setFontSize(7.5);
    doc.setTextColor(...CIS.textMuted);
    doc.text('計算方式：實領薪資 = 底薪 + 假日津貼 - 勞保費 - 健保費', mx, y);
    y += 4.5;
    doc.text(`= ${fmt(emp.baseSalary)} + ${fmt(emp.holidayAllowance)} - ${fmt(emp.laborInsurance)} - ${fmt(emp.healthInsurance)} = ${fmt(emp.netSalary)}`, mx, y);

    // ── 底部裝飾線與資訊 ──
    const footerY = ph - 22;
    doc.setDrawColor(...CIS.forest);
    doc.setLineWidth(0.3);
    doc.line(mx, footerY, pw - mx, footerY);
    doc.setLineWidth(0.8);
    doc.line(mx, footerY + 1.5, pw - mx, footerY + 1.5);

    doc.setFont(F, 'normal');
    doc.setFontSize(7.5);
    doc.setTextColor(...CIS.textMuted);
    doc.text('此為系統自動產生之薪資明細，僅供參考。', mx, footerY + 8);
    doc.text(`列印日期：${printDate}`, pw - mx, footerY + 8, {align: 'right'});
    doc.setFontSize(7);
    doc.text(`${idx + 1} / ${p.employees.length}`, pw / 2, footerY + 8, {align: 'center'});
  }
}
