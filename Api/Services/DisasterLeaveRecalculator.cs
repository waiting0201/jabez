using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 給薪颱風假對請假單的時數重算（冪等）。
///
/// 規則：
///   · 只動工作日型假別（<see cref="LeaveDayExpander.WorkingDayLeaveTypes"/>）的 pending / approved 假單；
///     產假之外的連續日曆天型（parental_leave、歲時祭儀假）不在其中，原封不動。
///   · 只有**給薪**的颱風假才退回時數；不給薪者假單完全不動（同仁照常用特休／事假）。
///   · 基準 ＝ <c>Hours + DisasterAdjustedHours</c>（先還原再重扣），故颱風假建立 / 改時段 / 改給薪 / 取消
///     都走同一支函式，不會越改越少。
///   · Hours 變 0 的單維持原狀態（不自動取消），前端加註「颱風假抵銷」。
///   · 補休假接著呼叫 <see cref="CompensatoryLotService.SyncUsageAsync"/>，lot 扣抵隨時數增減回補 / 重扣。
///   · 已核准銷假的日期不計入（與 <see cref="LeaveRevocationService.ApplyAsync"/> 同口徑）。
/// </summary>
public interface IDisasterLeaveRecalculator
{
    /// <summary>
    /// 重算「某縣市工作地的員工」在某日 pending / approved 的假單，並 SaveChanges。
    /// 供颱風假建立 / 修改 / 取消 / 切換給薪時呼叫；回傳被異動的假單數。
    /// </summary>
    Task<int> RecalculateForAsync(DateTime date, string county);

    /// <summary>
    /// 重算單張假單（不 SaveChanges，交易邊界交給呼叫端）。供新假單送簽時套用既有的給薪颱風假。
    /// </summary>
    Task<bool> RecalculateLeaveAsync(LeaveRequest leave);
}

public sealed class DisasterLeaveRecalculator(
    AppDbContext db,
    IEmployeeWorkdaysFactory workdaysFactory,
    IDisasterDayOffReadService disasterReader,
    IWorkCountyResolver countyResolver,
    IWorkdayScheduleProvider scheduleProvider) : IDisasterLeaveRecalculator
{
    public async Task<int> RecalculateForAsync(DateTime date, string county)
    {
        var userIds = await countyResolver.UserIdsInCountyAsync(county);
        if (userIds.Count == 0) return 0;

        var day = date.Date;
        var dayEnd = day.AddDays(1);
        var leaves = new List<LeaveRequest>();
        // 分批避開 IN 參數上限
        foreach (var chunk in userIds.Chunk(1000))
        {
            var ids = chunk.Select(i => (Guid?)i).ToList();
            leaves.AddRange(await db.LeaveRequests
                .Where(l => l.EmployeeId != null && ids.Contains(l.EmployeeId)
                         && (l.ApprovalStatus == "pending" || l.ApprovalStatus == "approved")
                         && l.StartDate < dayEnd && l.EndDate >= day
                         && LeaveDayExpander.WorkingDayLeaveTypes.Contains(l.LeaveType))
                .ToListAsync());
        }

        int changed = 0;
        foreach (var leave in leaves)
            if (await RecalculateLeaveAsync(leave)) changed++;

        if (changed > 0) await db.SaveChangesAsync();
        return changed;
    }

    public async Task<bool> RecalculateLeaveAsync(LeaveRequest leave)
    {
        if (leave.EmployeeId is not { } empId) return false;
        if (leave.ApprovalStatus is not ("pending" or "approved")) return false;
        if (!LeaveDayExpander.WorkingDayLeaveTypes.Contains(leave.LeaveType)) return false;

        var workdays = await workdaysFactory.ForAsync(empId);
        var all      = await LeaveDayExpander.ExpandAsync(workdays, leave);
        var revoked  = await LeaveRevocationService.GetApprovedRevokedDatesAsync(db, leave.Id);
        var remaining = all.Where(d => !revoked.Contains(d.Date.Date)).ToList();

        var reduction = await ReductionAsync(disasterReader, workdays, empId, remaining);

        var previousAdj = leave.DisasterAdjustedHours ?? 0m;
        var baseline    = leave.Hours + previousAdj;
        reduction       = Math.Min(reduction, baseline);
        var newHours    = baseline - reduction;

        if (newHours == leave.Hours && previousAdj == reduction) return false;

        leave.Hours                 = newHours;
        leave.DisasterAdjustedHours = reduction > 0 ? reduction : null;

        // 補休：扣抵紀錄隨時數增減（內部全額歸還再重新 FIFO，冪等）
        if (leave.LeaveType == "compensatory")
            await CompensatoryLotService.SyncUsageAsync(db, scheduleProvider, leave);

        return true;
    }

    /// <summary>
    /// 一組（已排除銷假日的）逐日請假在**給薪颱風假**下的總退回時數。
    /// 銷假核准重算 Hours 時（<see cref="LeaveRevocationService.ApplyAsync"/>）也走這支，兩條路徑口徑一致。
    /// </summary>
    public static async Task<decimal> ReductionAsync(
        IDisasterDayOffReadService reader, EmployeeWorkdays workdays, Guid employeeId, IReadOnlyList<LeaveDay> days)
    {
        if (days.Count == 0) return 0m;

        var from = days.Min(d => d.Date).Date;
        var to   = days.Max(d => d.Date).Date;
        var map  = await reader.GetSegmentsAsync([employeeId], from, to);

        var paid = new Dictionary<DateTime, string>();
        foreach (var (key, entry) in map)
            if (entry.IsPaid) paid[key.Date] = entry.Segment;
        if (paid.Count == 0) return 0m;

        return DisasterLeaveAdjuster.TotalReduction(days, paid, workdays.ScheduleFor);
    }
}
