using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 加班結算（2026-10 防灌工時）：把「核准的申請時數」換算成「依實際加班打卡的給付時數」
/// （<see cref="OvertimeRequest.SettledHours"/>，規則與舊單界線見 <see cref="OvertimeSettlement"/>）。
///
/// 靜態、**不呼叫 SaveChanges**（比照 <see cref="OvertimeCompensationService"/> / <see cref="CompensatoryLotService"/>），
/// 交易邊界交給呼叫端。三個重算時機：
///   ① 終局核准 —— 走 <see cref="OvertimeCompensationService.ApplyAsync"/> 內建的 <see cref="ResolveFromDbAsync"/>，
///      ApprovalTaskHandler 的既有呼叫完全不用改；
///   ② 打加班結束卡（AttendanceHandler.OvertimeEndAsync）；
///   ③ 管理者修改出缺勤的加班起訖（AttendanceHandler.UpdateAsync）。
/// ②③ 的打卡時間還在 ChangeTracker 裡尚未落地，故由呼叫端直接傳入 <see cref="SyncFromPunchesAsync"/>，
/// 不可改成從 DB 重讀（會讀到修改前的值）。
///
/// 全部**冪等**：SettledHours 只由「核准時數 + 打卡起訖」決定，加班費快照與補休 lot 各自有 Apply / Revoke 模式，
/// 重複呼叫（例如管理者連改兩次）收斂到同一結果。
/// </summary>
public static class OvertimeSettlementService
{
    /// <summary>
    /// 跨日加班的「隔天補打結束卡」截止時刻（時）：加班過午夜時，結束卡落在隔天，
    /// 但打卡紀錄的 RecordDate 是加班開始那天。隔天 06:00 前仍可對前一天未結束的加班補打結束卡，
    /// 過了就留空（結算為 0），交由管理者於出缺勤補正。
    /// </summary>
    public const int CrossDayEndCutoffHour = 6;

    /// <summary>
    /// 從 DB 現有打卡紀錄推算結算時數（終局核准當下使用）。
    /// 只在核准前由管理者代為補登加班起訖時才會有值；一般情況下員工須核准後才能打加班卡，故為 0。
    /// 優先取「綁定本單」的紀錄，其次取同人同日、尚未綁定任何加班單的紀錄。
    /// </summary>
    public static async Task<decimal> ResolveFromDbAsync(AppDbContext db, OvertimeRequest ot)
    {
        if (ot.EmployeeId is null) return 0m;
        var day = ot.OvertimeDate.Date;

        var rec = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.UserId == ot.EmployeeId.Value
                     && (a.OvertimeRequestId == ot.Id
                         || (a.OvertimeRequestId == null && a.RecordDate == day)))
            .OrderByDescending(a => a.OvertimeRequestId == ot.Id)
            .Select(a => new { a.OvertimeStartTime, a.OvertimeEndTime })
            .FirstOrDefaultAsync();

        return rec is null
            ? 0m
            : OvertimeSettlement.ComputeSettled(ot.EstimatedHours, rec.OvertimeStartTime, rec.OvertimeEndTime);
    }

    /// <summary>
    /// 依指定的加班起訖重算結算時數，並同步加班費快照與補休 lot。
    /// 舊單（SettledHours 為 null）與非 approved 的單一律不處理 —— 回傳 false。
    /// </summary>
    public static async Task<bool> SyncFromPunchesAsync(
        AppDbContext db,
        IShiftScheduleReadService shiftSchedule,
        IWorkdayScheduleProvider scheduleProvider,
        OvertimeRequest ot,
        DateTime? overtimeStart,
        DateTime? overtimeEnd)
    {
        if (ot.SettledHours is null || ot.ApprovalStatus != "approved") return false;

        ot.SettledHours = OvertimeSettlement.ComputeSettled(ot.EstimatedHours, overtimeStart, overtimeEnd);

        // 快照與 lot 都以剛寫入的 SettledHours 為準（refreshSettlement:false 避免被 DB 舊值蓋回）
        await OvertimeCompensationService.ApplyAsync(db, shiftSchedule, scheduleProvider, ot, refreshSettlement: false);
        await CompensatoryLotService.ApplyAsync(db, shiftSchedule, scheduleProvider, ot);
        return true;
    }

    /// <summary>
    /// 找出一筆打卡紀錄對應的加班單（供管理者修改出缺勤時重算）。
    /// 優先用紀錄上的 OvertimeRequestId；沒有綁定時，取同人同日**唯一一張**已核准的新制單
    /// （多張時無從判定該算給誰，寧可不重算也不亂猜）。
    /// </summary>
    public static async Task<OvertimeRequest?> FindForRecordAsync(AppDbContext db, AttendanceRecord record)
    {
        if (record.OvertimeRequestId is { } id)
            return await db.OvertimeRequests.FirstOrDefaultAsync(o => o.Id == id);

        var day = record.RecordDate.Date;
        var nextDay = day.AddDays(1);
        var candidates = await db.OvertimeRequests
            .Where(o => o.EmployeeId == record.UserId
                     && o.OvertimeDate >= day && o.OvertimeDate < nextDay
                     && o.ApprovalStatus == "approved"
                     && o.SettledHours != null)
            .Take(2)
            .ToListAsync();

        return candidates.Count == 1 ? candidates[0] : null;
    }
}
