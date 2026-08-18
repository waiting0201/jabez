namespace Jabez.Api.Common;

/// <summary>
/// 年資與年假額度計算的單一真相。
///
/// 原本為 LeaveRequestHandler 的 private static 方法，2026-08 因「育嬰留職停薪期間不計入工作年資」
/// 需要在多處扣除留停天數而抽出成共用 helper。
///
/// 消費點：
///   LeaveRequestHandler.GetAnnualQuotaAsync   → 年假額度查詢端點
///   LeaveRequestHandler.ValidateLeaveQuotaAsync → 年假送出時的額度擋件
/// 兩處必須帶入相同的 excludedDays（育嬰留停累計天數），否則查得到的額度與實際能送的會不一致。
/// </summary>
public static class SeniorityHelper
{
    /// <summary>
    /// 計算年資（年, 月）。
    /// </summary>
    /// <param name="hireDate">到職日</param>
    /// <param name="now">計算基準時點</param>
    /// <param name="excludedDays">
    /// 不計入年資的天數（育嬰留職停薪累計天數）。作法是把到職日往後推相同天數，
    /// 得到「有效到職日」後再算日曆差，等同於年資在留停期間暫停累積。
    /// </param>
    public static (int Years, int Months) Calculate(DateTime hireDate, DateTime now, int excludedDays = 0)
    {
        var effectiveHireDate = excludedDays > 0 ? hireDate.AddDays(excludedDays) : hireDate;

        // 有效到職日已超過基準時點（留停天數大於在職天數）→ 年資歸零
        if (effectiveHireDate > now) return (0, 0);

        int years = now.Year - effectiveHireDate.Year;
        int months = now.Month - effectiveHireDate.Month;
        if (now.Day < effectiveHireDate.Day) months--;
        if (months < 0) { years--; months += 12; }
        return (years, months);
    }

    /// <summary>根據年資計算年假天數</summary>
    public static int CalculateAnnualLeaveDays(int years, int months)
    {
        int totalMonths = years * 12 + months;
        if (totalMonths < 6) return 0;          // 未滿 6 個月
        if (totalMonths < 12) return 3;         // 滿 6 個月 ~ 未滿 1 年
        if (years < 2) return 10;               // 滿 1 年 ~ 未滿 2 年
        if (years < 3) return 10;               // 滿 2 年 ~ 未滿 3 年
        if (years < 5) return 14;               // 滿 3 年 ~ 未滿 5 年
        if (years < 10) return 15;              // 滿 5 年 ~ 未滿 10 年
        return Math.Min(30, 15 + (years - 10)); // 10 年以上：每年加 1 天，上限 30 天
    }
}
