using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services;

/// <summary>分期撥款共用驗證（4 種申請類型適用）</summary>
public static class InstallmentValidator
{
    /// <summary>
    /// 驗證 inputs：
    /// 1. 至少 1 筆
    /// 2. InstallmentNo 1-based 連續無重複
    /// 3. SUM(Amount) == totalAmount（容忍 0.01 浮點誤差）
    /// 4. 已撥（PaidAt 有值）的列：ExpectedDate / Amount / PaidAt 不可改、不可刪
    /// </summary>
    /// <param name="inputs">前端送來的分期清單</param>
    /// <param name="totalAmount">申請總金額（PaymentRequest.TotalAmount 或其他三類的 GrandTotal）</param>
    /// <param name="existing">既有的分期紀錄（用於檢查已撥款列保護）— 提供 (Id, InstallmentNo, ExpectedDate, PaidAt, Amount) 五欄</param>
    public static void Validate(
        List<InstallmentInput> inputs,
        decimal totalAmount,
        IReadOnlyList<(int Id, int InstallmentNo, DateTime ExpectedDate, DateTime? PaidAt, decimal Amount)> existing)
    {
        if (inputs is null || inputs.Count == 0)
            throw AppException.BadRequest("至少需要 1 筆撥款明細。");

        // 序號連續、1-based、無重複
        var nos = inputs.Select(i => i.InstallmentNo).OrderBy(n => n).ToList();
        for (var idx = 0; idx < nos.Count; idx++)
        {
            if (nos[idx] != idx + 1)
                throw AppException.BadRequest($"撥款序號必須 1-based 連續無斷號（目前：{string.Join(",", nos)}）。");
        }

        // 金額加總驗證
        var sum = inputs.Sum(i => i.Amount);
        if (Math.Abs(sum - totalAmount) > 0.01m)
            throw AppException.BadRequest($"各筆金額加總（{sum:N2}）需等於申請總額（{totalAmount:N2}）。");

        // 已撥款列保護：對於 existing 中 PaidAt.HasValue 的列，inputs 中不可改 ExpectedDate / Amount，且不可刪除
        var existingPaid = existing.Where(e => e.PaidAt.HasValue).ToDictionary(e => e.Id);
        foreach (var paidRow in existingPaid.Values)
        {
            var match = inputs.FirstOrDefault(i => i.Id == paidRow.Id);
            if (match is null)
                throw AppException.BadRequest($"第 {paidRow.InstallmentNo} 期已撥款，不可刪除。");
            if (match.ExpectedDate.Date != paidRow.ExpectedDate.Date)
                throw AppException.BadRequest($"第 {paidRow.InstallmentNo} 期已撥款，預計撥款日不可修改。");
            if (Math.Abs(match.Amount - paidRow.Amount) > 0.01m)
                throw AppException.BadRequest($"第 {paidRow.InstallmentNo} 期已撥款，金額不可修改。");
            if (!match.PaidAt.HasValue || match.PaidAt.Value.Date != paidRow.PaidAt!.Value.Date)
                throw AppException.BadRequest($"第 {paidRow.InstallmentNo} 期已撥款，撥款日不可修改。");
        }
    }

    /// <summary>計算父表撥款 cache 欄位（過渡用，兩階段策略）</summary>
    public static (DateTime? EstimatedPaymentDate, DateTime? PaidAt, PaymentInstallmentStatus Status) ComputeCache(
        IReadOnlyList<(DateTime ExpectedDate, DateTime? PaidAt)> installments)
    {
        if (installments.Count == 0)
            return (null, null, PaymentInstallmentStatus.Unpaid);

        // EstimatedPaymentDate = MAX(ExpectedDate)
        var estimated = installments.Max(i => i.ExpectedDate);

        // 全數撥畢時 PaidAt = MAX(PaidAt)，否則 null（沿用舊邏輯，配合既有「已撥款 = PaidAt IS NOT NULL」判斷）
        var paidCount = installments.Count(i => i.PaidAt.HasValue);
        DateTime? paidAt;
        PaymentInstallmentStatus status;
        if (paidCount == 0)
        {
            paidAt = null;
            status = PaymentInstallmentStatus.Unpaid;
        }
        else if (paidCount < installments.Count)
        {
            paidAt = null;
            status = PaymentInstallmentStatus.PartiallyPaid;
        }
        else
        {
            paidAt = installments.Max(i => i.PaidAt);
            status = PaymentInstallmentStatus.FullyPaid;
        }

        return (estimated, paidAt, status);
    }
}
