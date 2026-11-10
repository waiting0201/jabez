import { CIS, FONT_FAMILY, fmt } from './pdf-core.service';
import type { SettlementLedger } from '../models/settlement-ledger.model';
import { SETTLEMENT_KIND_LABELS } from '../models/settlement-ledger.model';

/**
 * 畫「金額歷程」小標題 + 摘要（預支 / 實際金額 / 差額）+ 時間軸表格。
 * 預支 PDF 與出差預支 PDF 共用；ledger 為空或無事件時不畫，回傳原 y。
 */
export function drawSettlementLedger(
  doc: any, autoTable: any, ledger: SettlementLedger | null | undefined,
  layout: { mx: number; cw: number; ph: number; y: number },
): number {
  if (!ledger || ledger.events.length === 0) return layout.y;
  const { mx, cw, ph } = layout;
  let y = layout.y;
  const F = FONT_FAMILY;

  if (y + 40 > ph - 15) { doc.addPage(); y = 20; }
  y += 6;
  doc.setFont(F, 'bold');
  doc.setFontSize(10);
  doc.setTextColor(...CIS.forestMid);
  doc.text('金額歷程', mx, y);
  y += 6;

  const actual = ledger.isClosed && ledger.actualAmount != null ? ledger.actualAmount : ledger.approvedWriteOffTotal;
  const diffLabel = ledger.outstanding > 0 ? '同仁應繳回' : ledger.outstanding < 0 ? '公司應補撥' : '已結清';
  doc.setFont(F, 'normal');
  doc.setFontSize(9);
  doc.setTextColor(...CIS.textPrimary);
  const summary = `預支金額：${fmt(ledger.advanceTotal)} 元　　${ledger.isClosed ? '實際金額' : '實際金額（核銷中）'}：${fmt(actual)} 元　　差額：${fmt(Math.abs(ledger.outstanding))} 元（${diffLabel}）`;
  doc.text(summary, mx, y);
  y += 4;

  autoTable(doc, {
    startY: y,
    margin: { left: mx, right: mx, top: 20 },
    theme: 'grid',
    styles: { font: F, fontSize: 8.5, textColor: [...CIS.textPrimary], lineColor: [...CIS.border], lineWidth: 0.3, cellPadding: { top: 2.5, bottom: 2.5, left: 3, right: 3 } },
    headStyles: { font: F, fillColor: [...CIS.forest], textColor: 255, fontSize: 9, fontStyle: 'bold', halign: 'center' },
    columnStyles: {
      0: { cellWidth: cw * 0.10, halign: 'center' },
      1: { cellWidth: cw * 0.28 },
      2: { cellWidth: cw * 0.16 },
      3: { cellWidth: cw * 0.11, halign: 'right' },
      4: { cellWidth: cw * 0.11, halign: 'right' },
      5: { cellWidth: cw * 0.11, halign: 'right' },
      6: { cellWidth: cw * 0.13, halign: 'right' },
    },
    head: [['日期', '事項', '單號', '金額', '累計預支', '累計核銷', '尚未結清']],
    body: ledger.events.map(e => [
      e.date ? e.date.slice(0, 10) : '—',
      `${SETTLEMENT_KIND_LABELS[e.kind] ?? e.kind}　${e.label}${e.note ? `（${e.note}）` : ''}`,
      e.requestNo || '—',
      e.amount != null ? fmt(e.amount) : '—',
      fmt(e.runningAdvance),
      fmt(e.runningWrittenOff),
      fmt(e.runningOutstanding),
    ]),
  });
  return (doc as any).lastAutoTable.finalY + 6;
}
