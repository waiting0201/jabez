import { CIS, FONT_FAMILY, fmt } from './pdf-core.service';

/** 逐張沖銷明細表所需資料（預支 PDF 的沖銷紀錄、沖銷 PDF 的前次沖銷共用） */
export interface PdfWriteOffTableData {
  writeOffNo: number;
  requestNo?: string | null;
  /** 小標題括號內的狀態文字 */
  statusLabel: string;
  items: {
    category: string; seqNo: number; itemName: string; unitPrice: number; quantity: string;
    totalPrice: number; cashAmount: number; checkAmount: number; invoiceNo?: string | null; note?: string | null;
  }[];
  cashTotal: number;
  checkTotal: number;
  grandTotal: number;
  returnedAmount?: number | null;
  returnedAt?: string | null;
}

/**
 * 畫「第 N 次沖銷 - 單號（狀態）」小標題 + 明細表（含現金小計 / 支票小計 / 總計 / 繳回）。
 * 自預支 PDF 抽出，預支與沖銷兩份 PDF 共用；回傳畫完後的 y。
 */
export function drawWriteOffTable(
  doc: any, autoTable: any, wo: PdfWriteOffTableData,
  layout: { mx: number; cw: number; ph: number; y: number },
): number {
  const { mx, cw, ph } = layout;
  let y = layout.y;
  const F = FONT_FAMILY;
  // 小標題：第 N 次沖銷
  if (y + 30 > ph - 15) { doc.addPage(); y = 20; }
  y += 6;
  doc.setFont(F, 'bold');
  doc.setFontSize(10);
  doc.setTextColor(...CIS.forestMid);
  doc.text(`第 ${wo.writeOffNo} 次沖銷 - ${wo.requestNo || '—'}（${wo.statusLabel}）`, mx, y);
  y += 6;

  // 沖銷明細表格
  const woBodyRows: any[][] = [];
  let woLastCat = '';
  for (const [idx, item] of wo.items.entries()) {
    const cat = item.category === woLastCat ? '' : item.category;
    woLastCat = item.category;
    woBodyRows.push([
      cat,
      String(item.seqNo || idx + 1),   // 舊沖銷單項次存 0，退回列序
      item.itemName,
      `${fmt(item.unitPrice)}元`,
      item.quantity,
      fmt(item.totalPrice),
      fmt(item.cashAmount),
      item.checkAmount > 0 ? fmt(item.checkAmount) : '',
      item.invoiceNo || '',
      item.note || '',
    ]);
  }

  // 純繳回（無費用明細）的沖銷仍印出一列說明，避免表格只剩合計列看不懂
  if (woBodyRows.length === 0) woBodyRows.push(['', '', '（無費用明細）', '', '', '', '', '', '', '']);

  // 合計列
  woBodyRows.push([
    { content: '現金小計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
    { content: fmt(wo.cashTotal), styles: { fontStyle: 'bold', halign: 'right' } },
    '', '', '',
  ]);
  woBodyRows.push([
    { content: '支票小計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
    '',
    { content: wo.checkTotal > 0 ? fmt(wo.checkTotal) : '', styles: { fontStyle: 'bold', halign: 'right' } },
    '', '',
  ]);
  woBodyRows.push([
    { content: '總計', colSpan: 6, styles: { halign: 'right', fontStyle: 'bold' } },
    { content: fmt(wo.grandTotal), colSpan: 2, styles: { fontStyle: 'bold', halign: 'right' } },
    '', '',
  ]);
  if ((wo.returnedAmount ?? 0) > 0) {
    const retDate = wo.returnedAt
      ? new Date(wo.returnedAt).toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', timeZone: 'Asia/Taipei' })
      : '日期未填';
    woBodyRows.push([
      { content: `現金/匯款繳回（${retDate}）`, colSpan: 6, styles: { halign: 'right' } },
      { content: fmt(wo.returnedAmount ?? 0), colSpan: 2, styles: { halign: 'right' } },
      '', '',
    ]);
  }

  autoTable(doc, {
    startY: y,
    margin: { left: mx, right: mx },
    theme: 'grid',
    styles: {
      font: F, fontSize: 8,
      textColor: [...CIS.textPrimary],
      lineColor: [...CIS.border],
      lineWidth: 0.3,
      cellPadding: { top: 2, bottom: 2, left: 2.5, right: 2.5 },
    },
    headStyles: {
      font: F, fillColor: [...CIS.forestMid], textColor: 255,
      fontSize: 8, fontStyle: 'bold', halign: 'center',
      cellPadding: { top: 2.5, bottom: 2.5, left: 2.5, right: 2.5 },
    },
    columnStyles: {
      0: { cellWidth: cw * 0.07, halign: 'center' },  // 分類
      1: { cellWidth: cw * 0.05, halign: 'center' },  // 項次
      2: { cellWidth: cw * 0.18 },                     // 項目(說明)
      3: { cellWidth: cw * 0.09, halign: 'right' },    // 單價
      4: { cellWidth: cw * 0.07, halign: 'center' },   // 數量/單位
      5: { cellWidth: cw * 0.09, halign: 'right' },    // 總價
      6: { cellWidth: cw * 0.11, halign: 'right' },    // 現金
      7: { cellWidth: cw * 0.11, halign: 'right' },    // 支票
      8: { cellWidth: cw * 0.11, halign: 'center' },   // 發票號碼
      9: { cellWidth: cw * 0.12 },                     // 備註
    },
    head: [['分類', '項次', '項目(說明)', '單價', '數量/\n單位', '總價', '現金', '支票', '發票號碼', '備註']],
    body: woBodyRows,
  });

  y = (doc as any).lastAutoTable.finalY + 4;
  return y;
}
