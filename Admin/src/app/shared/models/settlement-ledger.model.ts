/**
 * 預支 / 出差預支「金額歷程」（後端 AdvanceSettlementLedgerService 產出，全站數字單一真相）。
 * 前端只呈現、不重算；累計欄位與 outstanding 皆由後端依「只計已核准、前序依核准時間」算好。
 * 「尚未結清」為保守互抵規則（現金差額與總額差額同方向取較小者，後端 WriteOffRefundCalculator.Settle）；
 * 月結差額另列 checkDifference，現金與月結互抵的部分（cashDifference ≠ outstanding）不算應繳回 / 應補撥。
 */
export type SettlementEventKind =
  | 'advance_round'    // 各批次預支（第 1 次 + 追加）
  | 'advance_paid'     // 預支撥款（資訊列，不影響尚未結清）
  | 'write_off'        // 已核准沖銷
  | 'returned'         // 同仁繳回
  | 'overspend_paid'   // 公司補撥超支
  | 'closed';          // 結案

export interface SettlementEvent {
  date: string | null;
  kind: SettlementEventKind;
  label: string;
  requestNo?: string | null;
  writeOffId?: number | null;
  amount: number | null;
  runningAdvance: number;
  runningWrittenOff: number;
  runningOutstanding: number;
  note?: string | null;
}

export interface SettlementLedger {
  advanceTotal: number;
  approvedWriteOffTotal: number;
  returnedTotal: number;
  overspendPaidTotal: number;
  /** 尚未結清：>0 同仁尚應繳回、<0 公司尚應補撥（月結差額與現金 / 月結互抵不計，見 checkDifference / cashDifference） */
  outstanding: number;
  /** 簽核中 / 退回 / 草稿沖銷（不進累計） */
  pendingWriteOffTotal: number;
  isClosed: boolean;
  closedAt?: string | null;
  closedByName?: string | null;
  actualAmount?: number | null;
  /** advanceTotal − (actualAmount ?? approvedWriteOffTotal) */
  difference: number;
  events: SettlementEvent[];
  /** 月結（支票）預支，已核准批次；出差預支恆 0 */
  advanceCheckTotal?: number;
  /** 已核准沖銷的月結合計 */
  approvedWriteOffCheckTotal?: number;
  /** 月結預支 − 月結核銷：>0 實支較預支少、<0 較多。公司直接支付廠商，不計入 outstanding */
  checkDifference?: number;
  /** 現金側原始差額（現金預支 − 現金核銷 − 繳回 + 補撥）；≠ outstanding 代表有一部分與月結互抵 */
  cashDifference?: number;
}

/**
 * 月結差額說明（歷程卡 / PDF 共用文案）；無差額回 null。
 * 未結案且月結核銷尚少於預支時，差額只是「還沒核銷完」，不說成「實支較少」。
 */
export function checkDifferenceText(diff: number | null | undefined, isClosed = true): string | null {
  if (!diff) return null;
  if (!isClosed && diff > 0)
    return `月結尚有 ${diff.toLocaleString('en-US')} 元未核銷（公司直接支付廠商，無須繳回）`;
  return `月結實支較預支${diff > 0 ? '少' : '多'} ${Math.abs(diff).toLocaleString('en-US')} 元（公司直接支付廠商，無須繳回 ／ 補撥）`;
}

/**
 * 同仁與公司之間的結算差額（>0 同仁應繳回、<0 公司應補撥）——與後端 WriteOffRefundCalculator.Settle 同一規則：
 * 現金差額與總額差額同方向取絕對值較小者，異號或任一為 0 則為 0。
 * 只供「含簽核中 / 編輯中」的畫面試算；已核准的數字一律讀後端 ledger.outstanding。
 */
export function settleDifference(cashDiff: number, totalDiff: number): number {
  if (cashDiff > 0 && totalDiff > 0) return Math.min(cashDiff, totalDiff);
  if (cashDiff < 0 && totalDiff < 0) return Math.max(cashDiff, totalDiff);
  return 0;
}

/** 現金與月結互抵的說明（cashDiff ≠ settled 時）；無互抵回 null */
export function offsetText(cashDiff: number | null | undefined, settled: number | null | undefined): string | null {
  const off = (cashDiff ?? 0) - (settled ?? 0);
  if (!off) return null;
  return off > 0
    ? `現金剩餘 ${off.toLocaleString('en-US')} 元已與月結超支互抵，不另列同仁應繳回`
    : `現金超支 ${Math.abs(off).toLocaleString('en-US')} 元已與月結未用完的額度互抵，不另列應補撥同仁`;
}

/**
 * 應撥差額（refundDue）的拆分說明：補撥同仁 / 月結超支付廠商。toEmployee 未提供時回 null（沿用預設文案）。
 */
export function refundDueSplitText(refundDue: number, toEmployee: number | null | undefined): string | null {
  if (toEmployee == null || refundDue <= 0) return null;
  const vendor = refundDue - toEmployee;
  if (vendor <= 0) return '沖銷超出預支，公司須補撥給同仁';
  const f = (n: number) => n.toLocaleString('en-US');
  return toEmployee > 0
    ? `其中補撥同仁 ${f(toEmployee)} 元、月結超支（支付廠商）${f(vendor)} 元`
    : `月結超支，款項支付廠商（非補撥同仁）`;
}

export const SETTLEMENT_KIND_LABELS: Record<SettlementEventKind, string> = {
  advance_round:  '預支',
  advance_paid:   '預支撥款',
  write_off:      '沖銷',
  returned:       '同仁繳回',
  overspend_paid: '公司補撥',
  closed:         '結案',
};

export const SETTLEMENT_KIND_CLASSES: Record<SettlementEventKind, string> = {
  advance_round:  'bg-primary',
  advance_paid:   'bg-secondary',
  write_off:      'bg-info',
  returned:       'bg-success',
  overspend_paid: 'bg-warning text-dark',
  closed:         'bg-dark',
};
