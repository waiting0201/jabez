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
    /// 出勤起算時間：落在「開放～準時界線」內一律視為當日上班時刻（早到不算加班），
    /// 超過則取實際打卡時刻。公司預設為 08:30–09:30 → 09:00；自訂時段者為 S−30 分～S+2 分 → S。
    /// </summary>
    public static DateTime AttendanceStart(DateTime actualClockIn, WorkdaySchedule schedule, ClockProfile? profile = null)
    {
        profile ??= ClockProfile.Company;
        var t = TimeOnly.FromDateTime(actualClockIn);
        return t >= profile.OpenFrom && t <= profile.OnTimeUntil
            ? actualClockIn.Date.Add((profile.FixedStart ?? schedule.Start).ToTimeSpan())
            : actualClockIn;
    }

    /// <summary>是否遲到（超過準時界線打上班卡）。**出差當日不判定**，由呼叫端決定是否套用。</summary>
    public static bool IsLate(DateTime actualClockIn, ClockProfile? profile = null) =>
        TimeOnly.FromDateTime(actualClockIn) > (profile ?? ClockProfile.Company).OnTimeUntil;

    /// <summary>
    /// 當日的遲到界線（超過即為遲到）。一般為個人準時界線（公司 09:30、自訂時段 S+2 分）；
    /// <paramref name="afternoonOnly"/>（請上午半天假、下午才上班）時改為 13:00 ＋ 交接容許帶 5 分 ——
    /// 否則 13:00 準時進來的人會因「超過 09:30」被記遲到（2026-10-06 修正）。
    /// 打卡寫旗標、必填遲到原因、首頁提示三處共用。
    /// </summary>
    public static TimeOnly LateThreshold(WorkdaySchedule schedule, bool afternoonOnly, ClockProfile? profile = null) =>
        afternoonOnly
            ? schedule.HalfDayPmStart.AddMinutes(LeaveHandoverToleranceMinutes)
            : (profile ?? ClockProfile.Company).OnTimeUntil;

    /// <summary>依當日情境判斷是否遲到（見 <see cref="LateThreshold"/>）。</summary>
    public static bool IsLate(DateTime actualClockIn, WorkdaySchedule schedule, bool afternoonOnly, ClockProfile? profile = null) =>
        TimeOnly.FromDateTime(actualClockIn) > LateThreshold(schedule, afternoonOnly, profile);

    /// <summary>
    /// 應下班時間。公司預設 ＝ 實際上班打卡時刻 ＋ 9 小時（含午休），**因人而異**；
    /// <paramref name="afternoonOnly"/>（當日請了上午半天假、下午才上班）時改為 ＋4 小時 ——
    /// 午休已過，不再扣那 1 小時。與 §5.2 的下班提醒時點同一套算法。
    ///
    /// 自訂時段者（賣店）一律為**當日固定下班時刻 E**，不隨打卡時刻浮動、上午請假亦同。
    /// </summary>
    public static DateTime ExpectedClockOut(
        DateTime actualClockIn, WorkdaySchedule schedule, bool afternoonOnly, ClockProfile? profile = null) =>
        profile?.FixedEnd is { } end
            ? actualClockIn.Date.Add(end.ToTimeSpan())
            : actualClockIn.AddHours((double)(afternoonOnly ? schedule.FullDayHours / 2m : schedule.ClockDayHours));

    /// <summary>
    /// 下班打卡屬於哪一種情形。三者互斥且涵蓋全部：
    /// `&lt; T` 早退／`[T, T+容許帶]` 正常／`&gt; T+容許帶` 逾時（公司預設 30 分、自訂時段 5 分）。
    /// </summary>
    public static ClockOutKind ResolveClockOutKind(
        DateTime actualClockIn, DateTime actualClockOut, WorkdaySchedule schedule, bool afternoonOnly,
        ClockProfile? profile = null)
    {
        profile ??= ClockProfile.Company;
        var expected = ExpectedClockOut(actualClockIn, schedule, afternoonOnly, profile);

        if (actualClockOut < expected) return ClockOutKind.Early;
        return actualClockOut <= expected.AddMinutes(profile.GraceMinutes)
            ? ClockOutKind.Normal
            : ClockOutKind.Overtime;
    }
}

/// <summary>
/// 個人打卡參數（開放時刻 / 準時界線 / 應下班 / 正常下班容許帶）的**單一真相**。
///
/// 絕大多數同仁走 <see cref="Company"/>（08:30 開放、09:30 準時、上班＋9h、容許 30 分）。
/// 賣店等在員工資料勾選「自訂上下班時段」者（<c>User.CustomWorkStartTime / CustomWorkEndTime</c>），
/// 由上班 S / 下班 E 兩個時刻推導全部參數，對應客戶給的兩組實例：
/// <code>
///   09:00–17:00 → 開放 08:30、準時 ≤ 09:02、提醒 08:58 / 16:58、正常下班 17:00–17:05
///   08:30–17:00 → 開放 08:00、準時 ≤ 08:32、提醒 08:28 / 16:58、正常下班 17:00–17:05
/// </code>
/// ⚠ 僅新制切換後生效（呼叫端只在 Flexible 分支使用）。
/// </summary>
public sealed record ClockProfile(
    TimeOnly  OpenFrom,
    TimeOnly  OnTimeUntil,
    TimeOnly? FixedStart,
    TimeOnly? FixedEnd,
    int       GraceMinutes)
{
    /// <summary>自訂時段：上班前幾分鐘開放打卡。</summary>
    public const int CustomOpenBeforeMinutes = 30;
    /// <summary>自訂時段：上班後幾分鐘內仍算準時（含）。</summary>
    public const int CustomOnTimeGraceMinutes = 2;
    /// <summary>自訂時段：正常下班容許帶（分）。</summary>
    public const int CustomClockOutGraceMinutes = 5;

    /// <summary>
    /// 自訂時段的合法範圍。上下班提醒只在 <c>AttendanceReminderCron</c> 的 7–9 / 16–18 時段內執行，
    /// 超出範圍的時段會永遠收不到提醒，故於員工資料存檔時擋下。
    /// </summary>
    public static readonly TimeOnly CustomStartEarliest = new(7, 30);
    public static readonly TimeOnly CustomStartLatest   = new(9, 30);
    public static readonly TimeOnly CustomEndEarliest   = new(16, 30);
    public static readonly TimeOnly CustomEndLatest     = new(18, 30);

    /// <summary>公司預設（現行新制規則）。</summary>
    public static readonly ClockProfile Company = new(
        ClockRules.ClockInOpenFrom, ClockRules.OnTimeUntil, null, null, ClockRules.NormalClockOutGraceMinutes);

    /// <summary>是否為自訂時段。</summary>
    public bool IsCustom => FixedEnd is not null;

    /// <summary>
    /// 由員工資料的兩個 "HH:mm" 字串建立；任一缺漏或無法解析即退回 <see cref="Company"/>
    /// （安全側：寧可照公司時段提醒，也不要讓人完全收不到）。
    /// </summary>
    public static ClockProfile For(string? start, string? end)
    {
        if (!TryParseHHmm(start, out var s) || !TryParseHHmm(end, out var e) || e <= s)
            return Company;

        return new ClockProfile(
            s.AddMinutes(-CustomOpenBeforeMinutes),
            s.AddMinutes(CustomOnTimeGraceMinutes),
            s, e, CustomClockOutGraceMinutes);
    }

    /// <summary>嚴格解析 "HH:mm"。</summary>
    public static bool TryParseHHmm(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value?.Trim(), "HH:mm", out time);
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
