using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 出缺勤報表「打卡紀錄 ∪ 當日請假日」的合併單一真相（純讀取型 static helper，比照 <see cref="LeaveDayExpander"/>）。
///
/// 為什麼不能純 SQL：逐日請假時數必須走 <see cref="LeaveDayExpander"/>（行事曆判定 + 半天 / 小時編碼），
/// 那是 C# 規則；SQL 端複製一份必然漂移。故改為「區間全量載入 → 記憶體合併 → 記憶體切頁」，
/// 呼叫端（AttendanceHandler）負責把查詢區間收斂在 <see cref="MaxRangeDays"/> 內。
///
/// 合併粒度＝(員工, 日期) 一列：
///   有打卡 + 有請假 → 同一列補上請假欄位
///   只有請假       → Id = null 的虛擬列（前端不顯示編輯鈕）
///   只有打卡       → 原樣
///   沒打卡也沒請假 → 不產生列（缺勤不在本報表範圍）
///
/// 同日多張假單（例：上午事假 + 下午特休）合併為一列：LeaveHours 加總、Leaves 保留逐張顆粒度，
/// LeaveType / LeaveStartDate / LeaveEndDate 填第一張（相容舊前端）。
/// </summary>
public static class AttendanceLeaveMerger
{
    /// <summary>查詢區間跨度上限（天）。超過即擋件，避免全量載入退化成全表掃描。</summary>
    public const int MaxRangeDays = 400;

    /// <summary>匯出模式（?export=true）的 pageSize 上限。一般列表仍為 100。</summary>
    public const int ExportMaxPageSize = 5000;

    public static async Task<PagedResult<AttendanceRecordDto>> BuildPagedAsync(
        IAttendanceReadService reader,
        ICalendarDayReadService calendarReader,
        ProjectAccessScope scope,
        int page, int pageSize,
        Guid? employeeId, DateOnly dateFrom, DateOnly dateTo)
    {
        // 本次合併專用的行事曆快取：逐張假單展開會反覆查同一年度，收斂成每年最多 2 次查詢
        var cal = new CachedCalendarDayReadService(calendarReader);

        var clockRows = await reader.ListInRangeAsync(scope, employeeId, dateFrom, dateTo);
        var leaveRows = await reader.ListApprovedLeavesInRangeAsync(scope, employeeId, dateFrom, dateTo);

        var revoked = (await reader.ListApprovedRevokedDatesAsync([.. leaveRows.Select(l => l.Id)]))
            .ToLookup(x => x.LeaveRequestId, x => x.Date.Date);

        var from = dateFrom.ToDateTime(TimeOnly.MinValue);
        var to   = dateTo.ToDateTime(TimeOnly.MinValue);

        // (員工, 日期) → 該日所有假單
        var leavesByDay = new Dictionary<(Guid UserId, DateTime Date), List<AttendanceLeaveDto>>();
        var nameByUser  = new Dictionary<Guid, string>();

        foreach (var lr in leaveRows)
        {
            nameByUser[lr.UserId] = lr.UserName;
            var revokedSet = revoked[lr.Id].ToHashSet();
            var days = await LeaveDayExpander.ExpandAsync(cal, lr.LeaveType, lr.StartDate, lr.EndDate);

            foreach (var d in days)
            {
                var date = d.Date.Date;
                if (date < from || date > to) continue;         // 展開結果可能超出查詢區間（如產假 56 天）→ 裁切
                if (revokedSet.Contains(date))  continue;       // 該日已核准銷假 → 不算請假日
                if (d.Hours <= 0)               continue;

                var key = (lr.UserId, date);
                if (!leavesByDay.TryGetValue(key, out var list))
                    leavesByDay[key] = list = [];
                list.Add(new AttendanceLeaveDto(lr.Id, lr.LeaveType, d.Hours, lr.StartDate, lr.EndDate));
            }
        }

        var merged = new List<AttendanceRecordDto>(clockRows.Count + leavesByDay.Count);

        foreach (var row in clockRows)
        {
            // Remove：命中即從字典移除，剩下的自然就是「沒有打卡紀錄」的請假日
            merged.Add(leavesByDay.Remove((row.UserId, row.RecordDate.Date), out var leaves)
                ? WithLeaves(row, leaves)
                : row);
        }

        foreach (var (key, leaves) in leavesByDay)
            merged.Add(WithLeaves(CreateLeaveOnlyRow(key.UserId, nameByUser[key.UserId], key.Date), leaves));

        // 三段式 tiebreak 確保 total order：記憶體切頁若排序不穩定，翻頁會漏列 / 重複列
        var ordered = merged
            .OrderByDescending(r => r.RecordDate)
            .ThenBy(r => r.UserName, StringComparer.Ordinal)
            .ThenBy(r => r.Id ?? int.MaxValue)
            .ToList();

        var total      = ordered.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
        var items      = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<AttendanceRecordDto>(items, total, page, pageSize, totalPages);
    }

    /// <summary>把當日請假資訊掛到一列上（打卡列與虛擬列共用）。</summary>
    private static AttendanceRecordDto WithLeaves(AttendanceRecordDto row, List<AttendanceLeaveDto> leaves)
    {
        var sorted = leaves.OrderBy(l => l.StartDate).ThenBy(l => l.LeaveRequestId).ToList();
        return row with
        {
            LeaveType      = sorted[0].LeaveType,     // 相容欄位：第一張假單
            LeaveStartDate = sorted[0].StartDate,
            LeaveEndDate   = sorted[0].EndDate,
            LeaveHours     = sorted.Sum(l => l.Hours),
            Leaves         = sorted,
        };
    }

    /// <summary>當日只有請假、無任何打卡紀錄 → Id = null 的虛擬列。</summary>
    private static AttendanceRecordDto CreateLeaveOnlyRow(Guid userId, string userName, DateTime date) =>
        new(
            Id:                     null,
            UserId:                 userId,
            UserName:               userName,
            RecordDate:             date,
            ClockInTime:            null,
            ClockInLatitude:        null,
            ClockInLongitude:       null,
            ClockOutTime:           null,
            ClockOutLatitude:       null,
            ClockOutLongitude:      null,
            IsClockOutAuto:         false,
            OvertimeStartTime:      null,
            OvertimeStartLatitude:  null,
            OvertimeStartLongitude: null,
            OvertimeEndTime:        null,
            OvertimeEndLatitude:    null,
            OvertimeEndLongitude:   null,
            OvertimeRequestId:      null,
            CreatedAt:              null,
            LeaveType:              null,
            LeaveStartDate:         null,
            LeaveEndDate:           null);
}
