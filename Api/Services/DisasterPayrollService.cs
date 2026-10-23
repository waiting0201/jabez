using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 薪資用：某月每位員工的「不給薪颱風假扣薪天數」。
/// 事實蒐集（工作地套用的停班、打卡、核准請假、本人工作日）在此，判定規則在純函式
/// <see cref="DisasterPayDeduction"/>；金額（日薪 × 天數）由 <c>PayrollReadService</c> 以折減前日薪乘出。
///
/// 效能：該月沒有任何「有效且不給薪」的颱風假時直接回空（絕大多數月份），不做任何逐人展開。
/// </summary>
public interface IDisasterPayrollService
{
    /// <summary>員工 → 扣薪天數（只含 &gt; 0 者）。employeeId 非 null 時只算該員工。</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetDeductibleDaysAsync(int year, int month, Guid? employeeId);

    /// <summary>
    /// 某一筆颱風假對各員工的影響（管理頁「影響名單」用）：是否出勤、不給薪時的扣薪天數。
    /// </summary>
    Task<IReadOnlyList<DisasterEmployeeImpact>> GetImpactAsync(int disasterId);
}

public sealed record DisasterEmployeeImpact(Guid UserId, string Name, string? DepartmentName, bool Worked, decimal? DeductionDays);

public sealed class DisasterPayrollService(
    AppDbContext db,
    IDisasterDayOffReadService disasterReader,
    IWorkCountyResolver countyResolver,
    IEmployeeWorkdaysFactory workdaysFactory) : IDisasterPayrollService
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetDeductibleDaysAsync(int year, int month, Guid? employeeId)
    {
        var result = new Dictionary<Guid, decimal>();
        var first = new DateTime(year, month, 1);
        var last  = first.AddMonths(1).AddDays(-1);

        var anyUnpaid = await db.DisasterDayOffs.AsNoTracking()
            .AnyAsync(d => d.Status == DisasterDayStatus.Active && !d.IsPaid && d.Date >= first && d.Date <= last);
        if (!anyUnpaid) return result;

        var map = await disasterReader.GetSegmentsAsync(employeeId is { } e ? [e] : null, first, last);
        var unpaid = map.Where(kv => !kv.Value.IsPaid).ToList();
        if (unpaid.Count == 0) return result;

        foreach (var g in unpaid.GroupBy(kv => kv.Key.UserId))
        {
            var impacts = await BuildDaysAsync(g.Key, g.Select(kv => (kv.Key.Date, kv.Value.Segment, kv.Value.IsPaid)).ToList(), first, last);
            var days = DisasterPayDeduction.DeductibleDays(impacts);
            if (days > 0) result[g.Key] = days;
        }
        return result;
    }

    public async Task<IReadOnlyList<DisasterEmployeeImpact>> GetImpactAsync(int disasterId)
    {
        var rec = await db.DisasterDayOffs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == disasterId);
        if (rec is null) return [];

        var userIds = await countyResolver.UserIdsInCountyAsync(rec.County);
        if (userIds.Count == 0) return [];

        var users = new List<(Guid Id, string Name, string? Dept, DateTime? Hire, DateTime? Resign)>();
        foreach (var chunk in userIds.Chunk(1000))
            users.AddRange((await db.Users.AsNoTracking()
                .Where(u => chunk.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, Dept = u.Department != null ? u.Department.Name : null, u.HireDate, u.ResignDate })
                .ToListAsync()).Select(u => (u.Id, u.Name, u.Dept, u.HireDate, u.ResignDate)));

        var list = new List<DisasterEmployeeImpact>(users.Count);
        foreach (var u in users.OrderBy(u => u.Dept).ThenBy(u => u.Name))
        {
            var rows = await BuildDaysAsync(u.Id, [(rec.Date.Date, rec.Segment, rec.IsPaid)], rec.Date.Date, rec.Date.Date);
            var d = rows[0];
            decimal? deduct = d.IsPaid ? null : (DisasterPayDeduction.IsDeductible(d) ? DisasterSegments.Days(d.Segment) : 0m);
            list.Add(new DisasterEmployeeImpact(u.Id, u.Name, u.Dept, d.WorkedInSegment, deduct));
        }
        return list;
    }

    /// <summary>蒐集一位員工在指定颱風日的出勤事實。</summary>
    private async Task<List<DisasterPayDay>> BuildDaysAsync(
        Guid userId, IReadOnlyList<(DateTime Date, string Segment, bool IsPaid)> disasterDays, DateTime from, DateTime to)
    {
        var workdays = await workdaysFactory.ForAsync(userId);
        var (_, _, working) = await workdays.ComputeAsync(from, to, CalendarScope.Attendance);
        var workingSet = working.Select(d => d.Date).ToHashSet();

        var user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.HireDate, u.ResignDate }).FirstAsync();

        var punches = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.UserId == userId && a.RecordDate >= from && a.RecordDate < to.AddDays(1))
            .Select(a => new { a.RecordDate, a.ClockInTime, a.ClockOutTime })
            .ToListAsync();

        // 區間內與停班日相交、已核准（且未全數銷假）的假單，逐日展開比對時段
        var leaves = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.EmployeeId == userId && l.ApprovalStatus == "approved"
                     && l.StartDate < to.AddDays(1) && l.EndDate >= from)
            .ToListAsync();
        var leaveDays = new List<LeaveDay>();
        foreach (var l in leaves)
        {
            var revoked = await LeaveRevocationService.GetApprovedRevokedDatesAsync(db, l.Id);
            leaveDays.AddRange((await LeaveDayExpander.ExpandAsync(workdays, l))
                .Where(d => !revoked.Contains(d.Date.Date) && d.Hours > 0 && d.Date >= from && d.Date <= to));
        }

        var result = new List<DisasterPayDay>(disasterDays.Count);
        foreach (var (date, segment, isPaid) in disasterDays)
        {
            var d   = date.Date;
            var sch = workdays.ScheduleFor(d);
            var w   = DisasterSegments.ToLeaveDay(d, segment, sch);
            var wStart = d.Add(w.Start.ToTimeSpan());
            var wEnd   = d.Add(w.End.ToTimeSpan());

            bool isWorkingDay = workingSet.Contains(d)
                && (user.HireDate is null || user.HireDate.Value.Date <= d)
                && (user.ResignDate is null || user.ResignDate.Value.Date >= d);

            bool worked = punches.Any(p => p.RecordDate.Date == d
                && DisasterPayDeduction.ClockOverlapsSegment(p.ClockInTime, p.ClockOutTime, wStart, wEnd));

            bool onLeave = leaveDays.Any(ld => ld.Date.Date == d
                && d.Add(ld.Start.ToTimeSpan()) < wEnd && d.Add(ld.End.ToTimeSpan()) > wStart);

            result.Add(new DisasterPayDay(d, segment, isPaid, isWorkingDay, worked, onLeave));
        }
        return result;
    }
}
