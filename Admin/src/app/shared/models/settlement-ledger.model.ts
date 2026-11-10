/**
 * 預支 / 出差預支「金額歷程」（後端 AdvanceSettlementLedgerService 產出，全站數字單一真相）。
 * 前端只呈現、不重算；累計欄位與 outstanding 皆由後端依「只計已核准、前序依核准時間」算好。
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
  /** >0 同仁尚應繳回、<0 公司尚應補撥 */
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
