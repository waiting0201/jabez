import {InstallmentPaymentMethod} from '../../features/admin/approval-tasks/models/approval-task.model';

/**
 * 預支單撥款分期「款別」對照（2026-11-24）。
 *
 * 預支明細分現金與月結（支票），財務常把兩者拆成不同期撥付；每期可標 cash / check / 未分類。
 * 本對照**只提示、不擋存** —— 後端驗證仍只看「各期合計 = 預支總額」，
 * 已撥款列金額鎖定，且實務上有「月結改匯同仁代墊」等例外，強制逐款別相等會讓財務存不了。
 * 編輯器（installments-editor）與唯讀表（installments-table）共用這一支。
 */
export interface PaymentMethodSummary {
  cashTotal: number;
  checkTotal: number;
  /** 已排（各期金額，含未撥）*/
  cashPlanned: number;
  checkPlanned: number;
  unclassified: number;
  /** 已撥（有實際撥款日者）*/
  cashPaid: number;
  checkPaid: number;
  /** 有未分類的期數，或已排金額與該款別合計不符 */
  mismatch: boolean;
}

export function summarizeByPaymentMethod(
  rows: {amount: number; paymentMethod?: InstallmentPaymentMethod | '' | null; paid: boolean}[],
  grandTotal: number,
  checkTotal: number,
): PaymentMethodSummary {
  const cashTotal = grandTotal - checkTotal;   // 與後端 WriteOffRefundCalculator.CashOf 同一推導
  let cashPlanned = 0, checkPlanned = 0, unclassified = 0, cashPaid = 0, checkPaid = 0;
  for (const r of rows) {
    const amt = Number(r.amount) || 0;
    if (r.paymentMethod === 'cash') {
      cashPlanned += amt;
      if (r.paid) cashPaid += amt;
    } else if (r.paymentMethod === 'check') {
      checkPlanned += amt;
      if (r.paid) checkPaid += amt;
    } else {
      unclassified += amt;
    }
  }
  const mismatch = unclassified > 0.01
    || Math.abs(cashPlanned - cashTotal) > 0.01
    || Math.abs(checkPlanned - checkTotal) > 0.01;
  return {cashTotal, checkTotal, cashPlanned, checkPlanned, unclassified, cashPaid, checkPaid, mismatch};
}

/**
 * 新增一期時的預設款別：只有單一款別的預支單直接帶入；兩者都有則留白讓財務選。
 */
export function defaultPaymentMethod(grandTotal: number, checkTotal: number): InstallmentPaymentMethod | '' {
  if (checkTotal <= 0.01) return 'cash';
  if (grandTotal - checkTotal <= 0.01) return 'check';
  return '';
}
