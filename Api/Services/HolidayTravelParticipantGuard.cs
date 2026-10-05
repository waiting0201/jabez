using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 假日執行活動（TravelRequest.IsHolidayTravel）參與人員的資格與重複給付防線（static，不呼叫 SaveChanges）。
///
/// 2026-10 安全稽核補強：假日津貼＝日薪 × 個人假日天數，是少數「一天可以被多張單重複領」的給付。
///   · <see cref="EnsureParticipantsActiveAsync"/>：參與人員必須在職（離職者掛名不得領津貼）。
///   · <see cref="EnsureNoConflictsAsync"/>：同一人同一個**假日**不可同時出現在
///       ① 另一張非拒絕、非草稿的假日活動單（送簽時比 pending / approved / returned；核准時只比 approved，
///          避免既有兩張 pending 互相擋死、誰都核准不了）、
///       ② 已核准、補償方式為「加班費」的加班單（同日重複給付；補休型不衝突）、
///       ③ 涵蓋該日的已核准**有薪**假（請假日當天不可再領假日津貼）。
///
/// 只比對「假日」：津貼只計行事曆假日（見 TravelRequestHandler.BuildParticipantEntities），
/// 平日的重疊不會重複給付，擋下只會誤傷。
/// 時段：同日 am 與 pm 可分屬兩張單（半天各 0.5 天）；full 與任何時段衝突。
/// 請假日清單走 <see cref="LeaveDayExpander"/>（只含實際請假的日子，已扣假日 / 排班；並扣掉已核准銷假日）。
/// </summary>
public static class HolidayTravelParticipantGuard
{
    /// <summary>無薪 / 半薪假別（不與假日津貼構成「請假又領津貼」的重複給付），其餘假別視為有薪。</summary>
    private static readonly HashSet<string> NonPaidLeaveTypes =
        ["personal", "sick", "menstrual", "family_care", "parental_leave", "parental_leave_daily"];

    private const int SlotAm = 1;
    private const int SlotPm = 2;
    private const int SlotFull = SlotAm | SlotPm;

    private static int SlotMask(string? slot) => ParticipantDateSlots.Normalize(slot) switch
    {
        ParticipantDateSlots.Am => SlotAm,
        ParticipantDateSlots.Pm => SlotPm,
        _                       => SlotFull,
    };

    /// <summary>參與人員必須存在且在職（Status == active），否則 400。</summary>
    public static async Task EnsureParticipantsActiveAsync(AppDbContext db, IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var inactive = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.Status != "active")
            .Select(u => u.Name)
            .ToListAsync();
        if (inactive.Count > 0)
            throw AppException.BadRequest($"參與執行人員「{string.Join("、", inactive)}」已非在職狀態，請移除後再送出。");
    }

    /// <summary>
    /// 檢查指定假日活動單的參與人員是否與其他單據在同一假日重複。
    /// <paramref name="onlyAgainstApproved"/>：true＝只比對「已核准」的其他假日活動單（核准時用）；
    /// false＝比對 pending / approved / returned（送簽時用）。有衝突丟 400（訊息點名人員、日期與衝突單據）。
    /// </summary>
    public static async Task EnsureNoConflictsAsync(
        AppDbContext db, ICalendarDayReadService calendarReader, IEmployeeWorkdaysFactory workdaysFactory,
        int travelRequestId, bool onlyAgainstApproved)
    {
        var item = await db.TravelRequests.AsNoTracking()
            .Include(t => t.Participants).ThenInclude(p => p.Dates)
            .FirstOrDefaultAsync(t => t.Id == travelRequestId && t.IsHolidayTravel);
        if (item is null || item.Participants.Count == 0) return;

        var start = item.StartDate.Date;
        var end   = item.EndDate.Date;

        var holidaySet = (await calendarReader.GetHolidayDatesAsync(start, end))
            .Select(d => d.Date).ToHashSet();
        if (holidaySet.Count == 0) return;

        // 參與者 → (假日 → 時段遮罩)。未勾選日期＝全程參與（活動期間內每個假日皆 full）
        var mine = item.Participants.ToDictionary(
            p => p.UserId,
            p => BuildDaySlots(p, start, end, holidaySet));

        var userIds = mine.Keys.ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);
        string NameOf(Guid id) => names.TryGetValue(id, out var n) ? n : id.ToString();

        // ① 另一張假日活動單
        var otherStatuses = onlyAgainstApproved
            ? new[] { "approved" }
            : new[] { "pending", "approved", "returned" };
        var others = await db.TravelRequests.AsNoTracking()
            .Include(t => t.Participants).ThenInclude(p => p.Dates)
            .Where(t => t.IsHolidayTravel
                     && t.Id != item.Id
                     && otherStatuses.Contains(t.ApprovalStatus)
                     && t.StartDate < end.AddDays(1) && t.EndDate >= start
                     && t.Participants.Any(p => userIds.Contains(p.UserId)))
            .ToListAsync();
        foreach (var other in others)
        {
            foreach (var op in other.Participants.Where(p => mine.ContainsKey(p.UserId)))
            {
                var theirs = BuildDaySlots(op, other.StartDate.Date, other.EndDate.Date, holidaySet);
                foreach (var (day, mask) in theirs)
                {
                    if (mine[op.UserId].TryGetValue(day, out var myMask) && (myMask & mask) != 0)
                        throw AppException.BadRequest(
                            $"參與人員「{NameOf(op.UserId)}」於 {day:yyyy/MM/dd} 已列在另一張假日執行活動單"
                          + $"（{other.RequestNo ?? $"#{other.Id}"}），同一人同一天不可重複領取假日津貼。");
                }
            }
        }

        // ② 已核准的「加班費型」加班單（補休型不涉及現金給付，不衝突）
        var payOvertimes = await db.OvertimeRequests.AsNoTracking()
            .Where(o => o.ApprovalStatus == "approved"
                     && o.CompensationType == OvertimeCompensationService.Pay
                     && o.EmployeeId != null && userIds.Contains(o.EmployeeId.Value)
                     && o.OvertimeDate >= start && o.OvertimeDate < end.AddDays(1))
            .Select(o => new { o.EmployeeId, o.OvertimeDate, o.RequestNo, o.Id })
            .ToListAsync();
        foreach (var o in payOvertimes)
        {
            var day = o.OvertimeDate.Date;
            if (mine[o.EmployeeId!.Value].ContainsKey(day))
                throw AppException.BadRequest(
                    $"參與人員「{NameOf(o.EmployeeId.Value)}」於 {day:yyyy/MM/dd} 已有核准的加班費型加班申請"
                  + $"（{o.RequestNo ?? $"#{o.Id}"}），同一天不可重複領取假日津貼。");
        }

        // ③ 涵蓋該日的已核准有薪假
        var leaves = await db.LeaveRequests.AsNoTracking()
            .Where(l => l.ApprovalStatus == "approved"
                     && l.EmployeeId != null && userIds.Contains(l.EmployeeId.Value)
                     && !NonPaidLeaveTypes.Contains(l.LeaveType)
                     && l.StartDate < end.AddDays(1) && l.EndDate >= start)
            .ToListAsync();
        if (leaves.Count == 0) return;

        var leaveIds = leaves.Select(l => l.Id).ToList();
        var revoked = (await db.LeaveRevocationDates.AsNoTracking()
                .Where(d => leaveIds.Contains(d.LeaveRevocation!.LeaveRequestId)
                         && d.LeaveRevocation.ApprovalStatus == "approved")
                .Select(d => new { d.LeaveRevocation!.LeaveRequestId, d.Date })
                .ToListAsync())
            .Select(x => (x.LeaveRequestId, x.Date.Date))
            .ToHashSet();

        foreach (var group in leaves.GroupBy(l => l.EmployeeId!.Value))
        {
            var workdays = await workdaysFactory.ForAsync(group.Key);
            foreach (var leave in group)
            {
                foreach (var day in await LeaveDayExpander.ExpandAsync(workdays, leave))
                {
                    var d = day.Date.Date;
                    if (revoked.Contains((leave.Id, d))) continue;
                    if (!mine[group.Key].TryGetValue(d, out var myMask)) continue;

                    int leaveMask = day.Segment switch
                    {
                        LeaveDaySegments.Am => SlotAm,
                        LeaveDaySegments.Pm => SlotPm,
                        _                   => SlotFull,   // full / partial（小時假）一律視為整天，保守擋下
                    };
                    if ((myMask & leaveMask) != 0)
                        throw AppException.BadRequest(
                            $"參與人員「{NameOf(group.Key)}」於 {d:yyyy/MM/dd} 已有核准的有薪假"
                          + $"（{LeaveTypeNames.GetZh(leave.LeaveType)}），請假當天不可領取假日津貼。");
                }
            }
        }
    }

    /// <summary>一位參與者在一張單內的「假日 → 時段遮罩」。未勾選日期＝活動期間內每個假日皆全天。</summary>
    private static Dictionary<DateTime, int> BuildDaySlots(
        TravelRequestParticipant p, DateTime start, DateTime end, HashSet<DateTime> holidaySet)
    {
        var map = new Dictionary<DateTime, int>();
        if (p.Dates.Count == 0)
        {
            for (var d = start; d <= end; d = d.AddDays(1))
                if (holidaySet.Contains(d)) map[d] = SlotFull;
            return map;
        }

        foreach (var pd in p.Dates)
        {
            var d = pd.Date.Date;
            if (!holidaySet.Contains(d)) continue;
            map[d] = (map.TryGetValue(d, out var m) ? m : 0) | SlotMask(pd.Slot);
        }
        return map;
    }
}
