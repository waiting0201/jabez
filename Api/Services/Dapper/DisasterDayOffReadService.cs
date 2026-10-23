using Dapper;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>某員工某日適用的颱風假。</summary>
/// <param name="DisasterId">DisasterDayOff.Id</param>
/// <param name="County">縣市代碼</param>
/// <param name="Segment">full / am / pm</param>
/// <param name="IsPaid">是否給薪</param>
public sealed record DisasterDayEntry(int DisasterId, string County, string Segment, bool IsPaid);

/// <summary>
/// 颱風假的**單一真相**讀取：「(員工, 日期) → 當日適用的停班時段」。
/// 所有消費點（出缺勤報表、打卡提醒、自動補卡、請假重算、薪資扣款、打卡頁提示）一律走這裡，
/// 不得各自重新解讀 DisasterDayOffs。
///
/// 員工的工作地解析見 <see cref="WorkCountyResolver.EffectiveCountyCte"/>：
/// User.WorkCounty ?? 所屬部門 County ?? 沿 ParentId 往上最近一個有設定者；皆無 ＝ 不套用。
/// 只回傳 Status = 'active' 的紀錄（已取消者視同不存在）。
/// </summary>
public interface IDisasterDayOffReadService
{
    /// <summary>
    /// 區間內每位員工每一天適用的颱風假。<paramref name="userIds"/> 為 null ＝ 不限人員；
    /// 空集合 ＝ 沒有人（直接回空，**不是**不限人員）。
    /// </summary>
    Task<IReadOnlyDictionary<(Guid UserId, DateTime Date), DisasterDayEntry>> GetSegmentsAsync(
        IReadOnlyCollection<Guid>? userIds, DateTime from, DateTime to);

    /// <summary>單一員工單日（打卡頁提示 / 打卡判定用）。</summary>
    Task<DisasterDayEntry?> GetForUserOnAsync(Guid userId, DateTime date);
}

public sealed class DisasterDayOffReadService(IDbConnection db) : IDisasterDayOffReadService
{
    /// <summary>超過此人數就改為不帶 IN 清單、撈回後在記憶體過濾（避開 SQL Server 2100 參數上限）。</summary>
    private const int MaxInlineIds = 1000;

    public async Task<IReadOnlyDictionary<(Guid UserId, DateTime Date), DisasterDayEntry>> GetSegmentsAsync(
        IReadOnlyCollection<Guid>? userIds, DateTime from, DateTime to)
    {
        var result = new Dictionary<(Guid, DateTime), DisasterDayEntry>();
        if (userIds is { Count: 0 }) return result;

        bool inline = userIds is { Count: <= MaxInlineIds };
        var sql = $"""
            {WorkCountyResolver.EffectiveCountyCte}
            SELECT u.Id AS UserId, x.Date, x.Id AS DisasterId, x.County, x.Segment, x.IsPaid
            FROM Users u
            LEFT JOIN DeptCounty dc ON dc.RootId = u.DepartmentId
            JOIN DisasterDayOffs x
              ON x.County = COALESCE(u.WorkCounty, dc.County)
             AND x.Status = 'active'
             AND x.Date >= @From AND x.Date <= @To
            WHERE u.IsSuperAdmin = 0
              {(inline ? "AND u.Id IN @Ids" : "")}
            OPTION (MAXRECURSION 50)
            """;

        var rows = await db.QueryAsync<(Guid UserId, DateTime Date, int DisasterId, string County, string Segment, bool IsPaid)>(
            sql, new { From = from.Date, To = to.Date, Ids = userIds });

        HashSet<Guid>? filter = userIds is not null && !inline ? [.. userIds] : null;
        foreach (var r in rows)
        {
            if (filter is not null && !filter.Contains(r.UserId)) continue;
            result[(r.UserId, r.Date.Date)] = new DisasterDayEntry(r.DisasterId, r.County, r.Segment, r.IsPaid);
        }
        return result;
    }

    public async Task<DisasterDayEntry?> GetForUserOnAsync(Guid userId, DateTime date)
    {
        var map = await GetSegmentsAsync([userId], date, date);
        return map.TryGetValue((userId, date.Date), out var e) ? e : null;
    }
}
