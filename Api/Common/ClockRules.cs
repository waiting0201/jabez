namespace Jabez.Api.Common;

/// <summary>下班打卡的三種情形，互斥且涵蓋全部（以應下班時間 T 為界）。</summary>
public enum ClockOutKind
{
    /// <summary>早退：`&lt; T`，必填原因（出差當日改為非必填）。</summary>
    Early,
    /// <summary>正常：`[T, T + 30 分]`，只需確認、不必填原因。</summary>
    Normal,
    /// <summary>逾時：`&gt; T + 30 分`，必填原因（出差當日改為非必填）。</summary>
    Overtime,
}

/// <summary>
/// 四週彈性工時的打卡規則（純函式、無 I/O，比照 <see cref="OvertimePayCalculator"/>）。
///
/// ⚠ **全部只在切換日之後生效**。切換日之前呼叫端不會走到這裡，舊制行為完全不變。
///
/// 規格 §2 / §5.1 的核心是「**實際打卡時刻**」與「**出勤起算時間**」是兩個值，混用會讓
/// 提醒時點與早退判定整組算錯：
/// <list type="bullet">
///   <item><b>實際打卡時刻</b>＝使用者按下按鈕的當下。用於下班提醒時點、應下班時間、早退／逾時判定。</item>
///   <item><b>出勤起算時間</b>＝衍生值，落在 08:30–09:30 內一律視為 09:00（早到不算加班）。用於工時／薪資換算。</item>
/// </list>
/// 系統**只保存實際打卡時刻一個值**，另一個以 <see cref="AttendanceStart"/> 推導，不新增第二個欄位。
/// </summary>
public static class ClockRules
{
    /// <summary>上班打卡鍵開放時刻（早於此不開放）。</summary>
    public static readonly TimeOnly ClockInOpenFrom = new(8, 30);

    /// <summary>
    /// 上班打卡的準時界線。**超過此刻仍可打卡**，只是記為遲到 ——
    /// 擋住不讓打卡會讓遲到的人整天沒有上班時間，反而更糟。
    /// </summary>
    public static readonly TimeOnly OnTimeUntil = new(9, 30);

    /// <summary>正常下班的容許帶（應下班時間 ～ ＋30 分，出勤 9～9.5 小時）。</summary>
    public const int NormalClockOutGraceMinutes = 30;

    /// <summary>
    /// 半天假 13:00 交接的容許帶（前後各 5 分鐘）。
    /// 下午請假者於此區間打**下班**卡、上午請假者於此區間打**上班**卡，皆視為準時。
    /// 同一個容許帶也用於「請假開始前 5 分鐘提前解鎖下班打卡鍵」（§5.1.4）。
    /// </summary>
    public const int LeaveHandoverToleranceMinutes = 5;

    /// <summary>
    /// 出勤起算時間：落在 08:30–09:30 內一律視為當日上班時刻（早到不算加班），
    /// 超過則取實際打卡時刻。
    /// </summary>
    public static DateTime AttendanceStart(DateTime actualClockIn, WorkdaySchedule schedule)
    {
        var t = TimeOnly.FromDateTime(actualClockIn);
        return t >= ClockInOpenFrom && t <= OnTimeUntil
            ? actualClockIn.Date.Add(schedule.Start.ToTimeSpan())
            : actualClockIn;
    }

    /// <summary>是否遲到（超過 09:30 打上班卡）。**出差當日不判定**，由呼叫端決定是否套用。</summary>
    public static bool IsLate(DateTime actualClockIn) =>
        TimeOnly.FromDateTime(actualClockIn) > OnTimeUntil;

    /// <summary>
    /// 應下班時間 ＝ 實際上班打卡時刻 ＋ 9 小時（含午休），**因人而異**。
    ///
    /// <paramref name="afternoonOnly"/>（當日請了上午半天假、下午才上班）時改為 ＋4 小時 ——
    /// 午休已過，不再扣那 1 小時。與 §5.2 的下班提醒時點同一套算法。
    /// </summary>
    public static DateTime ExpectedClockOut(DateTime actualClockIn, WorkdaySchedule schedule, bool afternoonOnly) =>
        actualClockIn.AddHours((double)(afternoonOnly ? schedule.FullDayHours / 2m : schedule.ClockDayHours));

    /// <summary>
    /// 下班打卡屬於哪一種情形。三者互斥且涵蓋全部：
    /// `&lt; T` 早退／`[T, T+30分]` 正常／`&gt; T+30分` 逾時。
    /// </summary>
    public static ClockOutKind ResolveClockOutKind(
        DateTime actualClockIn, DateTime actualClockOut, WorkdaySchedule schedule, bool afternoonOnly)
    {
        var expected = ExpectedClockOut(actualClockIn, schedule, afternoonOnly);

        if (actualClockOut < expected) return ClockOutKind.Early;
        return actualClockOut <= expected.AddMinutes(NormalClockOutGraceMinutes)
            ? ClockOutKind.Normal
            : ClockOutKind.Overtime;
    }
}

/// <summary>
/// 「某人今天能不能打卡」的單一真相（§5.3）。
///
/// | 當日狀態 | 上下班打卡 | 加班打卡 | 加班申請 |
/// |---|---|---|---|
/// | 上班日 | 開放 | 須先打下班卡 | 可提 |
/// | 休假日 | **鎖定** | 須核准加班單 | 可提 |
/// | 例假日 | **全鎖** | **全鎖** | **全鎖** |
/// | 國定假日 ‧ 活動日預定人力 | **解鎖**（不需加班單） | 第 9 小時起須加班單 | 可提 |
/// | 國定假日 ‧ 未排活動日 | 鎖定 | 須核准加班單 | 可提 |
///
/// 例假日全鎖是為完全符合勞基法例假規範（違者罰鍰 2 萬～100 萬元）——
/// 這是唯一一種連「加班申請」都不給提的日別。
/// </summary>
public static class ClockDayPolicy
{
    /// <summary>今日可否打上下班卡。</summary>
    public static bool AllowsClockInOut(string dayType, bool isActivityAssignee) => dayType switch
    {
        WorkDayTypes.Work          => true,
        WorkDayTypes.PublicHoliday => isActivityAssignee,   // 被排為活動日預定人力才解鎖
        _                          => false,                // 休假日 / 例假日
    };

    /// <summary>今日可否打加班卡（仍須另有已核准的加班申請單，此處只管日別）。</summary>
    public static bool AllowsOvertime(string dayType) =>
        dayType != WorkDayTypes.StatutoryOff;

    /// <summary>今日可否提出加班申請。</summary>
    public static bool AllowsOvertimeRequest(string dayType) =>
        dayType != WorkDayTypes.StatutoryOff;

    /// <summary>不可打上下班卡時，給使用者看的原因（可打卡時回 null）。</summary>
    public static string? ClockInOutLockReason(string dayType, bool isActivityAssignee) => dayType switch
    {
        WorkDayTypes.Work          => null,
        WorkDayTypes.PublicHoliday => isActivityAssignee
            ? null
            : "本日為國定假日（免出勤）。如有出勤需要，請提出加班申請，核准後即可打加班卡。",
        WorkDayTypes.RestDay       => "本日為您排定的休假日。如因緊急公務需加班，請事前提出加班申請，核准後即可打加班卡。",
        WorkDayTypes.StatutoryOff  => "本日為您排定的例假日，依法嚴禁出勤。如遇公務需求，請於上班日再行處理或委由職務代理人協助。",
        _                          => null,
    };
}
