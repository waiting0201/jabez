using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;

namespace Jabez.Api.Common;

/// <summary>
/// 出缺勤報表「打卡紀錄 ∪ 當日請假日 ∪ 缺勤日」的合併單一真相（純讀取型 static helper，比照 <see cref="LeaveDayExpander"/>）。
///
/// 為什麼不能純 SQL：逐日請假時數與時段必須走 <see cref="LeaveDayExpander"/>（行事曆判定 + 半天 / 小時編碼），
/// 那是 C# 規則；SQL 端複製一份必然漂移。故改為「區間全量載入 → 記憶體合併 → 記憶體切頁」，
/// 呼叫端（AttendanceHandler）負責把查詢區間收斂在 <see cref="MaxRangeDays"/> 內。
///
/// 合併粒度＝(員工, 日期) 一列，<see cref="AttendanceRecordDto.RowKind"/> 標示種類：
///   有打卡 + 有請假 → 同一列補上請假欄位（clock）
///   只有打卡       → 原樣（clock）
///   只有請假       → Id = null 的請假虛擬列（leave）
///   工作日既無打卡也無請假 → Id = null 的缺勤虛擬列（absent）
///
/// 請假列與缺勤列同樣 Id = null，**前端不可用 Id 判斷是哪一種**，一律看 RowKind。
///
/// 同日多張假單（例：上午事假 + 下午特休）合併為一列：LeaveHours 加總、Leaves 保留逐張顆粒度（含逐日時段），
/// LeaveType / LeaveStartDate / LeaveEndDate 填當日最早的一張（相容舊前端）。
/// 每列另帶 ExpectedStart / ExpectedEnd（扣掉請假後的應出勤時段，見 <see cref="ExpectedWorkWindow"/>）。
/// </summary>
public static class AttendanceLeaveMerger
{
    /// <summary>查詢區間跨度上限（天）。超過即擋件，避免全量載入退化成全表掃描。</summary>
    public const int MaxRangeDays = 400;

    /// <summary>匯出模式（?export=true）的 pageSize 上限。一般列表仍為 100。</summary>
    public const int ExportMaxPageSize = 5000;

    /// <summary>
    /// 缺勤列的展開上限（員工數 × 區間天數）。缺勤列是「員工 × 工作日」的笛卡兒積，
    /// 不設限的話 400 天 × 全公司會在記憶體端展開成數萬列。
    /// </summary>
    public const int AbsenceMaxCells = 60_000;

    public const string RowKindClock  = "clock";
    public const string RowKindLeave  = "leave";
    public const string RowKindAbsent = "absent";

    public static async Task<PagedResult<AttendanceRecordDto>> BuildPagedAsync(
        IAttendanceReadService reader,
        ICalendarDayReadService calendarReader,
        ProjectAccessScope scope,
        int page, int pageSize,
        Guid? employeeId, DateOnly dateFrom, DateOnly dateTo)
    {
        // 本次合併專用的行事曆快取：逐張假單展開與缺勤列的工作日計算會反覆查同一年度，收斂成每年最多 2 次查詢
        var cal = new CachedCalendarDayReadService(calendarReader);

        var clockRows = await reader.ListInRangeAsync(scope, employeeId, dateFrom, dateTo);
        var leaveRows = await reader.ListApprovedLeavesInRangeAsync(scope, employeeId, dateFrom, dateTo);
        var employees = await reader.ListClockingEmployeesAsync(scope, employeeId, dateFrom, dateTo);

        var revoked = (await reader.ListApprovedRevokedDatesAsync([.. leaveRows.Select(l => l.Id)]))
            .ToLookup(x => x.LeaveRequestId, x => x.Date.Date);

        var from = dateFrom.ToDateTime(TimeOnly.MinValue);
        var to   = dateTo.ToDateTime(TimeOnly.MinValue);

        // (員工, 日期) → 該日所有假單。Dtos 供回傳、Days 供 ExpectedWorkWindow 計算應出勤時段
        var leavesByDay = new Dictionary<(Guid UserId, DateTime Date), (List<AttendanceLeaveDto> Dtos, List<LeaveDay> Days)>();
        var nameByUser  = new Dictionary<Guid, string>();

        foreach (var lr in leaveRows)
        {
            nameByUser[lr.UserId] = lr.UserName;
            var revokedSet = revoked[lr.Id].ToHashSet();
            // 排班制旗標隨資料列帶出（SQL 已 JOIN Users），避免逐張假單再查一次 DB
            var days = await LeaveDayExpander.ExpandAsync(cal, lr.IsShiftWorker, lr.LeaveType, lr.StartDate, lr.EndDate);

            foreach (var d in days)
            {
                var date = d.Date.Date;
                if (date < from || date > to) continue;         // 展開結果可能超出查詢區間（如產假 56 天）→ 裁切
                if (revokedSet.Contains(date))  continue;       // 該日已核准銷假 → 不算請假日
                if (d.Hours <= 0)               continue;

                var key = (lr.UserId, date);
                if (!leavesByDay.TryGetValue(key, out var entry))
                    leavesByDay[key] = entry = ([], []);

                entry.Dtos.Add(new AttendanceLeaveDto(
                    lr.Id, lr.LeaveType, d.Hours, lr.StartDate, lr.EndDate,
                    d.Segment, date.Add(d.Start.ToTimeSpan()), date.Add(d.End.ToTimeSpan())));
                entry.Days.Add(d);
            }
        }

        // ⚠️ 缺勤判定要用「合併前」的請假日快照：下面的 Remove 會把有打卡的請假日移走，
        //    直接沿用 leavesByDay.Keys 會讓「有打卡 + 有請假」的日子被誤判成缺勤。
        var leaveKeys   = new HashSet<(Guid, DateTime)>(leavesByDay.Keys);
        var clockedKeys = clockRows.Select(r => (r.UserId, r.RecordDate.Date)).ToHashSet();

        var merged = new List<AttendanceRecordDto>(clockRows.Count + leavesByDay.Count);

        foreach (var row in clockRows)
        {
            // Remove：命中即從字典移除，剩下的自然就是「沒有打卡紀錄」的請假日
            merged.Add(leavesByDay.Remove((row.UserId, row.RecordDate.Date), out var entry)
                ? WithLeaves(row, entry.Dtos, entry.Days)
                : row);
        }

        foreach (var (key, entry) in leavesByDay)
            merged.Add(WithLeaves(
                CreateVirtualRow(key.UserId, nameByUser[key.UserId], key.Date, RowKindLeave),
                entry.Dtos, entry.Days));

        // 工作日集合（依排班制旗標分兩組，行事曆查詢因此最多 2 次），應出勤時段與缺勤列共用
        var shiftByUser   = employees.ToDictionary(e => e.UserId, e => e.IsShiftWorker);
        var workingByFlag = new Dictionary<bool, HashSet<DateTime>>();

        async Task<HashSet<DateTime>> WorkingSetAsync(bool isShiftWorker)
        {
            if (workingByFlag.TryGetValue(isShiftWorker, out var cached)) return cached;
            var (_, _, working) = await WorkCalendarHelper.ComputeWorkingDatesAsync(cal, isShiftWorker, from, to);
            return workingByFlag[isShiftWorker] = [.. working];
        }

        await ApplyExpectedWindowAsync(merged, shiftByUser, WorkingSetAsync);
        await AppendAbsentRowsAsync(merged, employees, WorkingSetAsync, clockedKeys, leaveKeys, from, to);

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

    /// <summary>
    /// 為打卡列與請假列補上應出勤時段。
    /// <see cref="WithLeaves"/> 只在「當日有請假」時算得出來，沒請假的工作日同樣需要
    /// 08:00–17:00 的應出勤時段，否則前端無從判斷「這天該打卡卻沒打」；
    /// 休假日（含非工作日型假別展開到的假日）則一律清成 null＝免出勤。
    /// </summary>
    private static async Task ApplyExpectedWindowAsync(
        List<AttendanceRecordDto> merged,
        Dictionary<Guid, bool> shiftByUser,
        Func<bool, Task<HashSet<DateTime>>> workingSetAsync)
    {
        for (int i = 0; i < merged.Count; i++)
        {
            var row = merged[i];
            if (row.RowKind == RowKindAbsent) continue;   // 缺勤列建立時已填好

            // 不在員工母體內者（超管 / 未開通打卡權限卻有紀錄）退回以非排班制判定：
            // 此欄只服務顯示與 badge，不影響任何寫入
            var isShiftWorker = shiftByUser.TryGetValue(row.UserId, out var flag) && flag;
            var working = await workingSetAsync(isShiftWorker);
            var date = row.RecordDate.Date;

            if (!working.Contains(date))
                merged[i] = row with { ExpectedStart = null, ExpectedEnd = null };
            else if (row.Leaves is null)
                merged[i] = row with
                {
                    ExpectedStart = date.AddHours(WorkdayHours.StartHour),
                    ExpectedEnd   = date.AddHours(WorkdayHours.EndHour),
                };
        }
    }

    /// <summary>
    /// 補上「工作日、無打卡、無請假」的缺勤虛擬列。
    /// 今天與未來一律不算缺勤（今天還有機會打卡），故上界收在昨天。
    /// </summary>
    private static async Task AppendAbsentRowsAsync(
        List<AttendanceRecordDto> merged,
        IReadOnlyList<AttendanceEmployeeRow> employees,
        Func<bool, Task<HashSet<DateTime>>> workingSetAsync,
        HashSet<(Guid, DateTime)> clockedKeys,
        HashSet<(Guid, DateTime)> leaveKeys,
        DateTime from, DateTime to)
    {
        var lastDate = Clock.Now.Date.AddDays(-1);
        if (to < lastDate) lastDate = to;
        if (employees.Count == 0 || lastDate < from) return;

        long cells = (long)employees.Count * ((lastDate - from).Days + 1);
        if (cells > AbsenceMaxCells)
            throw AppException.BadRequest("查詢區間過大，缺勤日展開超出上限，請縮小日期區間或指定單一員工。");

        foreach (var emp in employees)
        {
            var working = await workingSetAsync(emp.IsShiftWorker);

            var floor = from;
            if (emp.HireDate is { } hire && hire.Date > floor) floor = hire.Date;

            var ceil = lastDate;
            if (emp.ResignDate is { } resign && resign.Date < ceil) ceil = resign.Date;

            for (var d = floor; d <= ceil; d = d.AddDays(1))
            {
                if (!working.Contains(d))                  continue;  // 非工作日（依該員工的排班制旗標）
                if (clockedKeys.Contains((emp.UserId, d))) continue;  // 有任何打卡紀錄（含只有加班時間的列）
                if (leaveKeys.Contains((emp.UserId, d)))   continue;  // 有請假 → 已有請假列，不重複產生

                merged.Add(CreateVirtualRow(emp.UserId, emp.UserName, d, RowKindAbsent));
            }
        }
    }

    /// <summary>把當日請假資訊與應出勤時段掛到一列上（打卡列與請假虛擬列共用）。</summary>
    private static AttendanceRecordDto WithLeaves(
        AttendanceRecordDto row, List<AttendanceLeaveDto> leaves, List<LeaveDay> days)
    {
        // 以「當日實際起時」排序，同日多張假才會照上午 → 下午的順序呈現
        var sorted = leaves.OrderBy(l => l.DayStart).ThenBy(l => l.LeaveRequestId).ToList();
        var window = ExpectedWorkWindow.Compute(row.RecordDate, days);

        return row with
        {
            LeaveType      = sorted[0].LeaveType,     // 相容欄位：當日最早的一張
            LeaveStartDate = sorted[0].StartDate,
            LeaveEndDate   = sorted[0].EndDate,
            LeaveHours     = sorted.Sum(l => l.Hours),
            Leaves         = sorted,
            ExpectedStart  = window.Start,
            ExpectedEnd    = window.End,
        };
    }

    /// <summary>
    /// 建立 Id = null 的虛擬列（請假列 / 缺勤列共用）。
    /// 請假列的 ExpectedStart / End 稍後由 <see cref="WithLeaves"/> 覆寫；
    /// 缺勤列當日無請假，應出勤時段即為標準工作日 08:00–17:00。
    /// </summary>
    private static AttendanceRecordDto CreateVirtualRow(Guid userId, string userName, DateTime date, string rowKind) =>
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
            LeaveEndDate:           null,
            RowKind:                rowKind,
            ExpectedStart:          date.AddHours(WorkdayHours.StartHour),
            ExpectedEnd:            date.AddHours(WorkdayHours.EndHour));
}
