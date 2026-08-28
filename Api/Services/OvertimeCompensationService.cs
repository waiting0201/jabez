using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 加班補償方式（補休 / 加班費）共用邏輯（靜態，比照 LeaveRevocationService 慣例：
/// 不呼叫 SaveChanges，交易邊界交給呼叫端）。
///
/// 加班費金額採**快照**：送簽時算一次（讓審核者看得到金額，否則是盲簽）、
/// 最終核准時以核准當下的底薪與行事曆重算並落地；退回 / 拒絕 / 改單則清空。
/// 刻意不在薪資端即時重算 —— 薪資本身無月結快照表，一次調薪會回溯改動所有歷史月份的加班費。
/// </summary>
public static class OvertimeCompensationService
{
    /// <summary>補休：時數計入補休池（LeaveRequestHandler.ComputeCompensatoryAsync）。</summary>
    public const string Compensatory = "compensatory";

    /// <summary>加班費：依勞基法試算金額，隨加班日次月薪資發放。</summary>
    public const string Pay = "pay";

    public static bool IsValid(string? value) => value is Compensatory or Pay;

    /// <summary>未知 / null 一律正規化為補休（安全側：寧可少發現金，不可雙重給付）。</summary>
    public static string Normalize(string? value) => value == Pay ? Pay : Compensatory;

    /// <summary>
    /// 依補償方式計算並寫入加班費快照（冪等，可重複呼叫）。呼叫端負責 SaveChangesAsync。
    ///
    /// 補休型 / 無所有人 → 清空快照。查無底薪（null 或 ≤ 0）仍寫 Amount = 0 而非 null，
    /// 讓「算過但是 0」與「沒算過」可區分。
    ///
    /// 行事曆與排班制旗標**必須以加班單所有人（ot.EmployeeId）解析**，不可用核准者。
    /// </summary>
    public static async Task ApplyAsync(
        AppDbContext db,
        ICalendarDayReadService calendarReader,
        IWorkPatternReadService workPattern,
        OvertimeRequest ot)
    {
        ot.CompensationType = Normalize(ot.CompensationType);

        if (ot.CompensationType != Pay || ot.EmployeeId is null)
        {
            ClearSnapshot(ot);
            return;
        }

        var baseSalary = await db.Users.AsNoTracking()
            .Where(u => u.Id == ot.EmployeeId.Value)
            .Select(u => u.BaseSalary)
            .FirstOrDefaultAsync();

        var estimate = await OvertimePayCalculator.CalculateAsync(
            calendarReader, workPattern,
            baseSalary ?? 0m, ot.EmployeeId.Value, ot.OvertimeDate, ot.EstimatedHours);

        ot.OvertimePayAmount  = estimate.Amount;
        ot.HourlyRateSnapshot = estimate.HourlyRate;
        ot.PayableHours       = estimate.PayableHours;
        ot.IsHolidayOvertime  = estimate.IsHoliday;
    }

    /// <summary>
    /// 清空四個快照欄（退回 / 拒絕 / 草稿階段改時數或日期時呼叫）。
    /// 退回、拒絕也要清：薪資 SQL 雖然會濾 approved，但留著死金額會讓報表 / 簽核台
    /// 出現一張被拒絕卻標著金額的單，是純粹的認知陷阱。
    /// </summary>
    public static void ClearSnapshot(OvertimeRequest ot)
    {
        ot.OvertimePayAmount  = null;
        ot.HourlyRateSnapshot = null;
        ot.PayableHours       = null;
        ot.IsHolidayOvertime  = null;
    }

    /// <summary>
    /// 同日是否已有已核准的「假日執行活動」（該活動另計假日津貼＝日薪 × 天數）。
    /// 命中時假日加班費與假日津貼會就同一段工時雙重給付，故於試算 / 表單以警示呈現。
    /// 系統目前**不硬擋**（屬業務決策），僅讓申請人與審核者看得到。
    /// </summary>
    public static async Task<bool> HasHolidayTravelConflictAsync(AppDbContext db, Guid ownerId, DateTime date)
    {
        var d = date.Date;
        return await db.TravelRequests.AsNoTracking().AnyAsync(t =>
            t.IsHolidayTravel
            && t.ApprovalStatus == "approved"
            && t.StartDate <= d && d <= t.EndDate
            && (t.EmployeeId == ownerId || t.Participants.Any(p => p.UserId == ownerId)));
    }
}
