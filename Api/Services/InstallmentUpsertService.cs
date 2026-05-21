using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 分期撥款共用 upsert 核心（4 種申請類型適用）。
/// 負責 validate + diff（delete / update / insert），<b>不呼叫 SaveChanges</b>，
/// 交易邊界由呼叫端決定 — 讓「核准動作 + 撥款明細」可在同一交易內原子寫入。
/// </summary>
public static class InstallmentUpsertService
{
    /// <summary>
    /// 套用 <paramref name="inputs"/> 到既有 <paramref name="existing"/>（已 Include 的導覽集合）。
    /// 回傳本次「新填 PaidAt」的清單供呼叫端發送已撥款通知。
    /// </summary>
    /// <param name="db">DbContext（用於 Add / Remove；不 SaveChanges）</param>
    /// <param name="existing">父實體已追蹤的 Installments 導覽集合</param>
    /// <param name="inputs">前端送來的分期清單</param>
    /// <param name="totalAmount">申請總金額（PaymentRequest.TotalAmount 或其他三類 GrandTotal）</param>
    /// <param name="userId">操作者（新填撥款時記為 PaidByUserId）</param>
    /// <param name="create">建立新列的工廠（須設好外鍵；其餘欄位由本方法填入）</param>
    public static List<NewlyPaidInstallment> Apply<TEntity>(
        DbContext              db,
        ICollection<TEntity>   existing,
        List<InstallmentInput> inputs,
        decimal                totalAmount,
        Guid                   userId,
        Func<TEntity>          create)
        where TEntity : class, IInstallmentEntity
    {
        // 共用驗證（序號連續 / SUM == 總額 / 已撥款列保護）
        var existingSnap = existing
            .Select(i => (i.Id, i.InstallmentNo, i.ExpectedDate, i.PaidAt, i.Amount))
            .ToList();
        InstallmentValidator.Validate(inputs, totalAmount, existingSnap);

        var nowUtc      = DateTime.UtcNow;
        var taipeiTz    = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
        var nowTaipei   = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, taipeiTz);
        var newlyPaid   = new List<NewlyPaidInstallment>();
        var inputIds    = inputs.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();

        // 1. delete：existing 中 inputs 沒帶 Id 的（已驗證已撥款不會落到此處）
        foreach (var r in existing.Where(e => !inputIds.Contains(e.Id)).ToList())
            db.Remove(r);

        // 2. update + insert
        foreach (var input in inputs)
        {
            if (input.Id.HasValue)
            {
                var ent = existing.FirstOrDefault(e => e.Id == input.Id.Value)
                    ?? throw AppException.BadRequest($"找不到要更新的撥款列 Id={input.Id.Value}。");

                var wasPaidNull = !ent.PaidAt.HasValue;
                ent.InstallmentNo = input.InstallmentNo;
                ent.ExpectedDate  = input.ExpectedDate.Date;
                ent.Amount        = input.Amount;
                ent.Note          = input.Note;
                if (input.PaidAt.HasValue)
                {
                    ent.PaidAt = input.PaidAt.Value.Date + nowTaipei.TimeOfDay;
                    if (wasPaidNull)
                    {
                        ent.PaidByUserId = userId;
                        newlyPaid.Add(new(ent.InstallmentNo, ent.PaidAt.Value, ent.Amount, inputs.Count));
                    }
                }
                ent.UpdatedAt = nowUtc;
            }
            else
            {
                var ent = create();
                ent.InstallmentNo = input.InstallmentNo;
                ent.ExpectedDate  = input.ExpectedDate.Date;
                ent.Amount        = input.Amount;
                ent.Note          = input.Note;
                ent.CreatedAt     = nowUtc;
                ent.UpdatedAt     = nowUtc;
                if (input.PaidAt.HasValue)
                {
                    ent.PaidAt = input.PaidAt.Value.Date + nowTaipei.TimeOfDay;
                    ent.PaidByUserId = userId;
                    newlyPaid.Add(new(ent.InstallmentNo, ent.PaidAt.Value, ent.Amount, inputs.Count));
                }
                db.Add(ent);
            }
        }

        return newlyPaid;
    }
}
