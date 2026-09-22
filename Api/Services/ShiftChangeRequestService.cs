using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Services;

/// <summary>
/// 改班申請共用（static，**不呼叫 SaveChanges** —— 交易邊界交給呼叫端，比照 LeaveRevocationService）。
/// </summary>
public static class ShiftChangeRequestService
{
    /// <summary>
    /// 簽核任務 / 簽核紀錄使用的 applicationType。
    ///
    /// ⚠ **與銷假申請的關鍵差異**：銷假是 <c>ResolveApprovalItemIdAsync("leave", …)</c> 借用請假的流程設定、
    /// 自己沒有 ApprovalItem；改班要的是 §3.5.2 的**逐部門六條簽核路線**，
    /// 管理員必須在〈簽核流程設定〉建得出 6 個 ApprovalItem，所以它必須有自己的 ApplicationType。
    /// </summary>
    public const string AppType = "shift_change";

    /// <summary>申請單號前綴（LV- / OT- / LVR- 已被佔用）。</summary>
    public const string RequestNoPrefix = "SC-";

    /// <summary>
    /// 核准後把異動寫進班表。**只有終局核准才呼叫** —— 未核准前原班表完全不動，
    /// 故退回 / 拒絕都不需要任何回滾。
    ///
    /// 寫入語意與 ShiftScheduleHandler 一致：上班日不落地（查無紀錄即上班日），
    /// 故 <c>ToDayType = work</c> 時是**刪除**該日的排班列。
    /// </summary>
    public static async Task ApplyAsync(AppDbContext db, ShiftChangeRequest request)
    {
        var dates = await db.ShiftChangeRequestDates.AsNoTracking()
            .Where(d => d.ShiftChangeRequestId == request.Id)
            .ToListAsync();

        if (dates.Count == 0 || request.EmployeeId is not { } userId) return;

        var targetDates = dates.Select(d => d.Date.Date).ToList();
        var existing = await db.ShiftScheduleDays
            .Where(d => d.UserId == userId && targetDates.Contains(d.Date))
            .ToListAsync();

        var now = Clock.Now;

        foreach (var d in dates)
        {
            var row = existing.FirstOrDefault(x => x.Date.Date == d.Date.Date);
            var to  = WorkDayTypes.Normalize(d.ToDayType);

            if (to == WorkDayTypes.Work)
            {
                // 改回上班日 ＝ 刪除該列（與「查無紀錄即上班日」的既定語意一致）
                if (row is not null) db.ShiftScheduleDays.Remove(row);
                continue;
            }

            if (row is null)
            {
                db.ShiftScheduleDays.Add(new ShiftScheduleDay
                {
                    UserId = userId, Date = d.Date.Date, DayType = to,
                    CreatedAt = now, UpdatedAt = now,
                });
            }
            else
            {
                row.DayType   = to;
                row.UpdatedAt = now;
            }
        }
    }
}
