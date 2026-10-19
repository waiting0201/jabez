using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 事後加班申請（<see cref="OvertimeRequest.RequestKind"/> = post_hoc，2026-10）終局核准時，
/// 自動把申請單上自填的實際起訖寫入當日出缺勤的加班欄並結算。
///
/// 靜態、**不呼叫 SaveChanges**（比照 <see cref="OvertimeSettlementService"/>），交易邊界在呼叫端。
/// 三個「終局核准」出口都要呼叫：ApprovalTaskHandler（單筆 / 批次共用 ExecuteReview）、
/// OvertimeRequestHandler.SubmitAsync 的 Superadmin 自動核准與全自審自動核准。
///
/// 設計決議（2026-10-19）：
///  - 上班卡 / 下班卡<b>一律不動</b>：下班卡是本人親手打的證據（含 GPS / IP）；下班卡落在加班時段內屬預期，
///    結算只扣「正常上班時段」重疊、薪資不讀下班卡，故不影響給付。
///  - 起訖與下班卡<b>不做任何比對</b>（自由填寫，真實性由審核者把關；異動留 AttendanceAuditLogs 可追查）。
///  - 結算走 <see cref="OvertimeSettlementService.SyncFromPunchesAsync"/> 並直接傳入起訖，
///    <b>不可</b>走 ApplyAsync(refreshSettlement:true)：其 ResolveFromDbAsync 為 AsNoTracking，讀不到尚未存檔的時間 → 結算 0。
/// </summary>
public static class PostHocOvertimeService
{
    private static bool ShouldApply(OvertimeRequest ot) =>
        ot.RequestKind == OvertimeRequestKinds.PostHoc
        && ot.ActualStartTime is not null && ot.ActualEndTime is not null   // 舊回填單無起訖 → 跳過，維持現狀
        && ot.EmployeeId is not null;

    /// <summary>
    /// 核准前的預檢（在任何狀態變更之前呼叫，避免批次核准中途丟例外留下髒的 tracked 變更）：
    /// 當日出缺勤已有（非本單的）加班打卡 → 400，請 HR 處理。
    /// </summary>
    public static async Task EnsureCanApplyAsync(AppDbContext db, OvertimeRequest ot)
    {
        if (!ShouldApply(ot)) return;
        var rec = await FindRecordAsync(db, ot, track: false);
        EnsureRecordUsable(rec, ot);
    }

    /// <summary>終局核准（ApprovalStatus 已為 approved）後呼叫；其他狀態或非事後申請一律不處理。</summary>
    public static async Task ApplyOnApprovalAsync(
        AppDbContext db,
        IShiftScheduleReadService shiftSchedule,
        IWorkdayScheduleProvider scheduleProvider,
        OvertimeRequest ot,
        Guid approverId)
    {
        if (ot.ApprovalStatus != "approved" || !ShouldApply(ot)) return;

        var rec = await FindRecordAsync(db, ot, track: true);
        EnsureRecordUsable(rec, ot);
        var record = rec!;

        var start = ot.ActualStartTime!.Value;
        var end   = ot.ActualEndTime!.Value;
        var now   = Clock.Now;

        // 已綁定本單且值相同（重跑）→ 不重複寫稽核，只重新結算
        bool alreadyApplied = record.OvertimeRequestId == ot.Id
                           && record.OvertimeStartTime == start && record.OvertimeEndTime == end;
        if (!alreadyApplied)
        {
            // 稽核必須在寫入新值之前（Before 欄取自 record 現值）
            AttendanceAuditWriter.Add(db, record, approverId, $"系統（事後加班核准 {ot.RequestNo}）", now,
                record.ClockInTime, record.ClockOutTime, start, end, record.Remark);

            record.OvertimeStartTime  = start;
            record.OvertimeEndTime    = end;
            record.OvertimeRequestId  = ot.Id;
            record.IsManuallyAdjusted = true;
            record.LastAdjustedById   = approverId;
            record.LastAdjustedAt     = now;
        }

        // 結算 + 同步加班費快照與補休 lot（內含 refreshSettlement:false 的 ApplyAsync 與 lot ApplyAsync，冪等）。
        // 呼叫端先前以 DB 舊值算出的 0 時數快照會在此被覆蓋。
        await OvertimeSettlementService.SyncFromPunchesAsync(db, shiftSchedule, scheduleProvider, ot, start, end);
    }

    private static Task<AttendanceRecord?> FindRecordAsync(AppDbContext db, OvertimeRequest ot, bool track)
    {
        var day = ot.OvertimeDate.Date;
        var q = db.AttendanceRecords.Where(a => a.UserId == ot.EmployeeId!.Value && a.RecordDate == day);
        return (track ? q : q.AsNoTracking()).FirstOrDefaultAsync();
    }

    private static void EnsureRecordUsable(AttendanceRecord? rec, OvertimeRequest ot)
    {
        if (rec is null)
            throw AppException.BadRequest(
                $"找不到 {ot.OvertimeDate:yyyy/MM/dd} 的出缺勤紀錄，無法寫入事後加班，請洽 HR 處理後再核准。");

        bool hasOtherPunch = (rec.OvertimeStartTime is not null || rec.OvertimeEndTime is not null)
                          && rec.OvertimeRequestId != ot.Id;
        if (hasOtherPunch)
            throw AppException.BadRequest(
                $"{ot.OvertimeDate:yyyy/MM/dd} 的出缺勤已有加班打卡紀錄（送件後被新增或修改），" +
                "無法自動寫入事後加班，請洽 HR 處理後再核准。");
    }
}
