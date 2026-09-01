import { Injectable, inject } from '@angular/core';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { PdfCoreService, CIS, FONT_FAMILY, fmtDate } from '../../../../shared/services/pdf-core.service';
import { EmployeeProfileDetail, GENDERS, MARITAL_STATUSES, DEGREE_OPTIONS, LANGUAGE_LEVELS, REWARD_PUNISHMENT_TYPES } from '../models/employee-profile.model';
import { User } from '../models/user.model';

// 本地色彩常數（autoTable Color = [number,number,number]，不可用 readonly tuple as number[]）
const TEXT_PRIMARY:   [number, number, number] = [82, 83, 88];
const TEXT_SECONDARY: [number, number, number] = [110, 111, 115];
const TEXT_MUTED:     [number, number, number] = [163, 150, 133];
const FOREST:         [number, number, number] = [105, 159, 52];
const FOREST_MID:     [number, number, number] = [74, 107, 58];
const BORDER_COLOR:   [number, number, number] = [221, 214, 200];
const WHITE:          [number, number, number] = [255, 255, 255];

/** null/undefined → '—' */
function val(v: string | number | null | undefined): string {
  if (v === null || v === undefined || String(v).trim() === '') return '—';
  return String(v);
}

/** 日期字串 YYYY-MM-DD → YYYY/MM/DD（含 null guard） */
function fmtD(d: string | null | undefined): string {
  if (!d) return '—';
  return d.replace(/-/g, '/').slice(0, 10);
}

/** 薪資數字格式化（千分位） */
function fmtMoney(n: number | null | undefined): string {
  if (n === null || n === undefined) return '—';
  return n.toLocaleString('zh-TW');
}

@Injectable({ providedIn: 'root' })
export class HrProfilePdfService {
  private pdfCore = inject(PdfCoreService);

  /**
   * 產生人事資料卡 PDF（A4 直式）並開啟列印視窗。
   *
   * @param canSeeSalary 是否可看薪資（payroll:read）。false 時整個 PAGE 3（薪資調整歷史）
   *   連同 addPage() 一起跳過，輸出 2 頁；若只藏表格會留下一張只有頁首的空白頁。
   */
  async generate(profile: EmployeeProfileDetail, user: User, canSeeSalary = true): Promise<void> {
    const fonts = await this.pdfCore.loadFonts();

    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    this.pdfCore.registerFonts(doc, fonts);

    const F  = FONT_FAMILY;
    const mx = 12;   // 左右邊距
    const pw = 210;  // A4 寬度
    const cw = pw - mx * 2;

    // ════════════════════════════════════════════════
    // PAGE 1
    // ════════════════════════════════════════════════
    let y = 14;

    // 標題
    doc.setFont(F, 'bold');
    doc.setFontSize(16);
    doc.setTextColor(...CIS.forest);
    doc.text('人事資料卡', pw / 2, y, { align: 'center' });

    y += 7;
    doc.setFont(F, 'normal');
    doc.setFontSize(9);
    doc.setTextColor(...TEXT_SECONDARY);
    doc.text('雅比斯國際創意策略(股)公司', pw / 2, y, { align: 'center' });

    y += 5;
    doc.setDrawColor(...CIS.forest);
    doc.setLineWidth(0.5);
    doc.line(mx, y, pw - mx, y);
    y += 5;

    // ── 基本資料區（4 欄 grid）──────────────────────
    const genderLabel  = GENDERS.find(g => g.value === profile.gender)?.label ?? val(profile.gender);
    const maritalLabel = MARITAL_STATUSES.find(m => m.value === profile.maritalStatus)?.label ?? val(profile.maritalStatus);

    const basicFields: [string, string][] = [
      ['員工代號',  val(profile.employeeNumber)],
      ['中文姓名',  val(user.name)],
      ['英文姓名',  val(profile.englishName)],
      ['部門',      val(user.departmentName)],
      ['職稱',      val(user.jobTitleName)],
      ['到職日',    user.hireDate ? fmtDate(user.hireDate) : '—'],
      ['生日',      user.birthday ? fmtDate(user.birthday) : '—'],
      ['性別',      genderLabel],
      ['婚姻狀況',  maritalLabel],
      ['出生地',    val(profile.birthPlace)],
      ['身分證號',  val(profile.idNumber)],
      ['行動電話',  val(profile.mobilePhone)],
    ];

    y = this._drawGrid(doc, mx, pw, y, basicFields, 4, F);
    y += 4;

    // ── 戶籍 & 通訊 ─────────────────────────────────
    y = this._drawSection(doc, mx, y, '戶籍 / 通訊資訊', F);
    const addrFields: [string, string][] = [
      ['戶籍地址', val(profile.residentialAddress)],
      ['戶籍電話', val(profile.residentialPhone)],
      ['通訊地址', val(profile.mailingAddress)],
      ['通訊電話', val(profile.mailingPhone)],
    ];
    y = this._drawGrid(doc, mx, pw, y, addrFields, 2, F);
    y += 4;

    // ── 學歷表 ───────────────────────────────────────
    y = this._drawSection(doc, mx, y, '學歷紀錄', F);
    if (profile.educationRecords.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['學校', '科系', '學歷', '起始年月', '結束年月']],
        body: profile.educationRecords.map(r => [
          val(r.school),
          val(r.department),
          DEGREE_OPTIONS.find(d => d.value === r.degree)?.label ?? val(r.degree),
          fmtD(r.startDate),
          fmtD(r.endDate),
        ]),
      });
      y = (doc as any).lastAutoTable.finalY + 4;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 經歷表 ───────────────────────────────────────
    y = this._drawSection(doc, mx, y, '經歷紀錄', F);
    if (profile.employmentHistoryRecords.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['服務機關', '職稱', '起始日期', '結束日期']],
        body: profile.employmentHistoryRecords.map(r => [
          val(r.organization),
          val(r.jobTitle),
          fmtD(r.startDate),
          fmtD(r.endDate),
        ]),
      });
      y = (doc as any).lastAutoTable.finalY + 4;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 家庭狀況表 ───────────────────────────────────
    y = this._drawSection(doc, mx, y, '家庭狀況', F);
    if (profile.familyMembers.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['姓名', '關係', '年齡', '職業']],
        body: profile.familyMembers.map(r => [
          val(r.name),
          val(r.relationship),
          val(r.age),
          val(r.occupation),
        ]),
      });
      y = (doc as any).lastAutoTable.finalY + 4;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 專業訓練表 ───────────────────────────────────
    y = this._drawSection(doc, mx, y, '專業訓練', F);
    if (profile.professionalTrainings.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['訓練名稱', '訓練機構', '起始日期', '結束日期', '時數']],
        body: profile.professionalTrainings.map(r => [
          val(r.trainingName),
          val(r.trainingOrg),
          fmtD(r.startDate),
          fmtD(r.endDate),
          val(r.hours),
        ]),
      });
      y = (doc as any).lastAutoTable.finalY + 4;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 語言能力表 ───────────────────────────────────
    y = this._drawSection(doc, mx, y, '語言能力', F);
    if (profile.languageAbilities.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['語言', '聽', '說', '讀', '寫']],
        body: profile.languageAbilities.map(r => {
          const lvl = (v: string) => LANGUAGE_LEVELS.find(l => l.value === v)?.label ?? val(v);
          return [val(r.language), lvl(r.listening), lvl(r.speaking), lvl(r.reading), lvl(r.writing)];
        }),
      });
      y = (doc as any).lastAutoTable.finalY + 4;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 緊急聯絡 / 財務 / 其他 ─────────────────────
    y = this._drawSection(doc, mx, y, '緊急聯絡 / 財務 / 其他', F);
    const otherFields: [string, string][] = [
      ['緊急聯絡人',   val(profile.emergencyContactName)],
      ['緊急聯絡電話', val(profile.emergencyContactPhone)],
      ['銀行分行',     val(profile.bankCode)],
      ['銀行帳號',     val(profile.bankAccount)],
      ['銀行分行(二)', val(profile.bankCode2)],
      ['銀行帳號(二)', val(profile.bankAccount2)],
      ['投保起日',     fmtD(profile.insuranceStartDate)],
      ['扶養人數',     val(profile.dependentCount)],
    ];
    y = this._drawGrid(doc, mx, pw, y, otherFields, 3, F);

    if (profile.specialties) {
      y += 3;
      y = this._drawSection(doc, mx, y, '專長興趣', F);
      doc.setFont(F, 'normal');
      doc.setFontSize(8);
      doc.setTextColor(...CIS.textPrimary);
      const lines = doc.splitTextToSize(profile.specialties, cw);
      doc.text(lines, mx, y);
      y += (lines as string[]).length * 4 + 2;
    }

    if (profile.resignationReason) {
      y += 3;
      y = this._drawSection(doc, mx, y, '離職原因', F);
      doc.setFont(F, 'normal');
      doc.setFontSize(8);
      doc.setTextColor(...CIS.textPrimary);
      const lines = doc.splitTextToSize(profile.resignationReason, cw);
      doc.text(lines, mx, y);
    }

    // ════════════════════════════════════════════════
    // PAGE 2
    // ════════════════════════════════════════════════
    doc.addPage();
    y = 14;
    y = this._drawPageHeader(doc, mx, pw, y, profile, user, F);

    // ── 職務調整歷史 ─────────────────────────────────
    y = this._drawSection(doc, mx, y, '職務調整歷史', F);
    if (profile.jobTransferRecords.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['生效日期', '原部門', '新部門', '原職稱', '新職稱']],
        body: profile.jobTransferRecords.map(r => [
          fmtD(r.effectiveDate),
          val(r.fromDepartment),
          val(r.toDepartment),
          val(r.fromJobTitle),
          val(r.toJobTitle),
        ]),
      });
      y = (doc as any).lastAutoTable.finalY + 6;
    } else {
      y = this._drawEmpty(doc, mx, y, F);
    }

    // ── 獎懲歷史 ─────────────────────────────────────
    y = this._drawSection(doc, mx, y, '獎懲歷史', F);
    if (profile.rewardPunishmentRecords.length > 0) {
      autoTable(doc, {
        startY: y,
        margin: { left: mx, right: mx },
        styles:     { font: F, fontSize: 8, cellPadding: 2, textColor: TEXT_PRIMARY },
        headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold' },
        head: [['生效日期', '類型', '類別', '次數', '事由']],
        body: profile.rewardPunishmentRecords.map(r => [
          fmtD(r.effectiveDate),
          REWARD_PUNISHMENT_TYPES.find(t => t.value === r.type)?.label ?? val(r.type),
          val(r.category),
          val(r.count),
          val(r.reason),
        ]),
      });
    } else {
      this._drawEmpty(doc, mx, y, F);
    }

    // ════════════════════════════════════════════════
    // PAGE 3（薪資調整歷史；無 payroll:read 時整頁不產生）
    // ════════════════════════════════════════════════
    if (canSeeSalary) {
      doc.addPage();
      y = 14;
      y = this._drawPageHeader(doc, mx, pw, y, profile, user, F);

      // ── 薪資調整歷史 ─────────────────────────────────
      y = this._drawSection(doc, mx, y, '薪資調整歷史', F);
      if (profile.salaryAdjustmentRecords.length > 0) {
        autoTable(doc, {
          startY: y,
          margin: { left: mx, right: mx },
          styles:     { font: F, fontSize: 7, cellPadding: 1.5, textColor: TEXT_PRIMARY, halign: 'right' },
          headStyles: { fillColor: FOREST, textColor: WHITE, fontStyle: 'bold', halign: 'center', fontSize: 7 },
          columnStyles: { 0: { halign: 'center' }, 6: { halign: 'left' } },
          head: [['生效日', '底薪', '其他', '代扣代付', '伙食', '合計', '備註']],
          body: profile.salaryAdjustmentRecords.map(r => [
            fmtD(r.effectiveDate),
            fmtMoney(r.baseSalary),
            fmtMoney(r.otherAllowance),
            fmtMoney(r.adjustmentDifference),
            fmtMoney(r.mealAllowance),
            fmtMoney(r.totalAmount),
            val(r.notes),
          ]),
        });
      } else {
        this._drawEmpty(doc, mx, y, F);
      }
    }

    // 輸出 PDF（開啟新視窗）
    const blob = doc.output('blob');
    const url  = URL.createObjectURL(blob);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }

  // ── 私用 helpers ─────────────────────────────────────

  /** 頁面頂部：員工代號 / 姓名 / 到職日 */
  private _drawPageHeader(
    doc: jsPDF,
    mx: number,
    pw: number,
    y: number,
    profile: EmployeeProfileDetail,
    user: User,
    F: string,
  ): number {
    doc.setFont(F, 'bold');
    doc.setFontSize(14);
    doc.setTextColor(...CIS.forest);
    doc.text('人事資料卡', pw / 2, y, { align: 'center' });
    y += 6;
    doc.setFont(F, 'normal');
    doc.setFontSize(8);
    doc.setTextColor(...CIS.textPrimary);
    const hireDateStr = user.hireDate ? fmtDate(user.hireDate) : '—';
    const hdr = `員工代號：${val(profile.employeeNumber)}　　姓名：${val(user.name)}　　到職日：${hireDateStr}`;
    doc.text(hdr, pw / 2, y, { align: 'center' });
    y += 5;
    doc.setDrawColor(...BORDER_COLOR);
    doc.setLineWidth(0.3);
    doc.line(mx, y, pw - mx, y);
    y += 5;
    return y;
  }

  /** 區段標題列 */
  private _drawSection(doc: jsPDF, mx: number, y: number, title: string, F: string): number {
    doc.setFont(F, 'bold');
    doc.setFontSize(9);
    doc.setTextColor(...FOREST_MID);
    doc.text(title, mx, y);
    y += 1;
    doc.setDrawColor(...BORDER_COLOR);
    doc.setLineWidth(0.2);
    doc.line(mx, y + 1, 210 - mx, y + 1);
    return y + 4;
  }

  /** 空紀錄提示 */
  private _drawEmpty(doc: jsPDF, mx: number, y: number, F: string): number {
    doc.setFont(F, 'normal');
    doc.setFontSize(8);
    doc.setTextColor(...TEXT_MUTED);
    doc.text('（無紀錄）', mx, y);
    return y + 6;
  }

  /**
   * 繪製 label-value grid。
   * @param cols  每列欄數
   */
  private _drawGrid(
    doc: jsPDF,
    mx: number,
    pw: number,
    y: number,
    fields: [string, string][],
    cols: number,
    F: string,
  ): number {
    const cw   = pw - mx * 2;
    const colW = cw / cols;
    const rowH = 5.5;

    for (let i = 0; i < fields.length; i += cols) {
      const rowFields = fields.slice(i, i + cols);
      for (let j = 0; j < rowFields.length; j++) {
        const [label, value] = rowFields[j];
        const x = mx + j * colW;

        doc.setFont(F, 'bold');
        doc.setFontSize(7.5);
        doc.setTextColor(...TEXT_SECONDARY);
        const labelText = label + '：';
        doc.text(labelText, x, y);

        doc.setFont(F, 'normal');
        doc.setFontSize(8);
        doc.setTextColor(...CIS.textPrimary);
        const offsetX   = x + doc.getTextWidth(labelText);
        const maxW      = colW - doc.getTextWidth(labelText) - 1;
        const shortVal  = maxW > 5 ? doc.splitTextToSize(value, maxW)[0] ?? value : value;
        doc.text(shortVal, offsetX, y);
      }
      y += rowH;
    }
    return y;
  }
}
