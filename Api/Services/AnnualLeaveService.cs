using Jabez.Api.Common;
using Jabez.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 特休（年假 <c>annual</c>）額度與使用量的單一真相（2026-11-24 自 LeaveRequestHandler 抽出）。
///
/// 規則（見 docs/business/leave-rules.md §年假額度規則）：
///   · 本年度天數＝依<b>到職年資</b>（<see cref="SeniorityHelper"/>，扣除已過去的育嬰留停天數）
///   · 已使用＝該曆年（1/1～12/31，以假單起始日判定）送簽中 + 已核准的年假時數 ÷ 8，每年 1/1 重新計算
///
/// 消費點（四處必須同一套算法，否則各頁看到的數字不一致）：
///   GET /leave-requests/annual-quota、GET /me/annual-leave → 請假表單提示、個人資訊頁
///   GET /users/{id}/annual-leave                        → 員工管理（入職日旁）
///   GET /reports/compensatory                            → 加班補休時數總表的特休欄
/// 送出擋件（LeaveRequestHandler.ValidateLeaveQuotaAsync）亦用 <see cref="GetParentalExcludedDaysAsync"/>。
///
/// 靜態、唯讀（比照 <see cref="CompensatoryLotService"/>）。批次版一次撈回全部人，供報表使用。
/// </summary>
public static class AnnualLeaveService
{
    private static readonly string[] ParentalLeaveTypes = ["parental_leave", "parental_leave_daily"];

    public sealed record Balance(
        int     Year,
        bool    HasHireDate,
        int     TotalDays,
        decimal UsedDays,
        decimal AvailableDays,
        int     SeniorityYears,
        int     SeniorityMonths,
        int     ParentalLeaveExcludedDays);

    /// <summary>單人：本年度（Clock.Now 所屬曆年、年資算至今日）。</summary>
    public static async Task<Balance> GetAsync(AppDbContext db, Guid userId)
    {
        var now = Clock.Now;
        var map = await GetForUsersAsync(db, [userId], now.Year, now);
        return map.GetValueOrDefault(userId) ?? Empty(now.Year);
    }

    /// <summary>
    /// 批次：<paramref name="year"/> 年的已使用天數 ＋ 截至 <paramref name="asOf"/> 的年資所對應的天數。
    /// 查詢固定 3 句（使用者 / 育嬰留停 / 年假用量），不隨人數成長。
    /// </summary>
    public static async Task<Dictionary<Guid, Balance>> GetForUsersAsync(
        AppDbContext db, IReadOnlyCollection<Guid> userIds, int year, DateTime asOf)
    {
        var result = new Dictionary<Guid, Balance>();
        if (userIds.Count == 0) return result;

        var idList = userIds.ToList();
        var hireDates = await db.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .Select(u => new { u.Id, u.HireDate })
            .ToListAsync();

        var parental = await LoadParentalExcludedDaysAsync(db, userIds, asOf);

        // LeaveRequest.EmployeeId 為 Guid?，Contains 需同型別
        var nullableIds = idList.Select(x => (Guid?)x).ToList();

        var startOfYear = new DateTime(year, 1, 1);
        var endOfYear   = new DateTime(year, 12, 31, 23, 59, 59);
        var usedHours = await db.LeaveRequests.AsNoTracking()
            .Where(l => nullableIds.Contains(l.EmployeeId)
                     && l.LeaveType == "annual"
                     && (l.ApprovalStatus == "approved" || l.ApprovalStatus == "pending")
                     && l.StartDate >= startOfYear && l.StartDate <= endOfYear)
            .GroupBy(l => l.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Hours = g.Sum(l => l.Hours) })
            .ToDictionaryAsync(x => x.EmployeeId!.Value, x => x.Hours);

        foreach (var u in hireDates)
        {
            var usedDays = Math.Round(usedHours.GetValueOrDefault(u.Id) / 8m, 1);
            if (u.HireDate is null)
            {
                result[u.Id] = Empty(year) with { UsedDays = usedDays };
                continue;
            }

            var excluded = parental.GetValueOrDefault(u.Id);
            var (years, months) = SeniorityHelper.Calculate(u.HireDate.Value, asOf, excluded);
            var total = SeniorityHelper.CalculateAnnualLeaveDays(years, months);
            result[u.Id] = new Balance(
                year, true, total, usedDays, Math.Round(Math.Max(0, total - usedDays), 1),
                years, months, excluded);
        }
        return result;
    }

    /// <summary>
    /// 「已經過去」的育嬰留停累計天數（留停期間不計入工作年資）。
    /// 只計已核准者；進行中的留停只算到 asOf 為止；已結束者用 Hours ÷ 8（銷假後 Hours 已遞減）。
    /// </summary>
    public static async Task<int> GetParentalExcludedDaysAsync(AppDbContext db, Guid userId, DateTime asOf)
        => (await LoadParentalExcludedDaysAsync(db, [userId], asOf)).GetValueOrDefault(userId);

    private static async Task<Dictionary<Guid, int>> LoadParentalExcludedDaysAsync(
        AppDbContext db, IReadOnlyCollection<Guid> userIds, DateTime asOf)
    {
        var nullableIds = userIds.Select(x => (Guid?)x).ToList();
        var rows = await db.LeaveRequests.AsNoTracking()
            .Where(l => nullableIds.Contains(l.EmployeeId)
                     && ParentalLeaveTypes.Contains(l.LeaveType)
                     && l.ApprovalStatus == "approved"
                     && l.StartDate <= asOf)
            .Select(l => new { l.EmployeeId, l.StartDate, l.EndDate, l.Hours })
            .ToListAsync();

        var cutoff = asOf.Date;
        return rows.GroupBy(r => r.EmployeeId!.Value).ToDictionary(g => g.Key, g =>
        {
            decimal total = 0m;
            foreach (var r in g)
            {
                total += r.EndDate.Date <= cutoff
                    ? r.Hours / 8m                                  // 已結束
                    : (cutoff - r.StartDate.Date).Days + 1;         // 進行中，只算已過去的日曆天
            }
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        });
    }

    private static Balance Empty(int year) => new(year, false, 0, 0m, 0m, 0, 0, 0);
}
