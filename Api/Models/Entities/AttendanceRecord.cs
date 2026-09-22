namespace Jabez.Api.Models.Entities;

public class AttendanceRecord
{
    public int       Id                     { get; set; }
    public Guid      UserId                 { get; set; }
    public DateTime  RecordDate             { get; set; }
    public DateTime? ClockInTime            { get; set; }
    public double?   ClockInLatitude        { get; set; }
    public double?   ClockInLongitude       { get; set; }
    /// <summary>
    /// 上班時間為系統自動補卡（登入時，該日已有下班卡或加班卡卻沒有上班卡），非本人打卡。
    /// 補的時間避開當日已核准請假時段（見 AttendanceAutoClockService）。
    /// </summary>
    public bool      IsClockInAuto          { get; set; }
    public DateTime? ClockOutTime           { get; set; }
    public double?   ClockOutLatitude       { get; set; }
    public double?   ClockOutLongitude      { get; set; }
    /// <summary>下班時間為系統自動補卡（登入時補打漏打的下班卡），非本人打卡</summary>
    public bool      IsClockOutAuto         { get; set; }
    /// <summary>該日為出差（打卡時由本人勾選，四個打卡動作皆會覆寫），出缺勤清單以 badge 標示</summary>
    public bool      IsBusinessTrip         { get; set; }
    /// <summary>管理者於出缺勤編輯表單填寫的備註（僅編輯表單可見可填）</summary>
    public string?   Remark                 { get; set; }

    // ── 四週彈性工時（切換日之後才寫入；舊制一律維持 false / null）──────────
    /// <summary>遲到（上班打卡超過 09:30）。**出差當日不判定**。</summary>
    public bool      IsLate                 { get; set; }
    /// <summary>早退（下班打卡早於「上班打卡 ＋ 9 小時」）。**出差當日不判定**。</summary>
    public bool      IsEarlyLeave           { get; set; }
    /// <summary>
    /// 下班打卡的早退／逾時原因（上限 500 字）。
    /// ⚠ 與 <see cref="Remark"/> **語意不同、不可互相挪用**：Remark 是管理者事後在出缺勤編輯表單
    /// 補註的（走 reports-attendance:write），本欄是員工打卡當下自己填的。
    /// 出差當日欄位**仍顯示但改為非必填** —— 在外仍可能有值得記錄的異常，
    /// 讓同仁想填就能填比直接拿掉欄位有用；改的是必填性，不是可見性。
    /// </summary>
    public string?   ClockOutReason         { get; set; }
    public DateTime? OvertimeStartTime      { get; set; }
    public double?   OvertimeStartLatitude  { get; set; }
    public double?   OvertimeStartLongitude { get; set; }
    public DateTime? OvertimeEndTime        { get; set; }
    public double?   OvertimeEndLatitude    { get; set; }
    public double?   OvertimeEndLongitude   { get; set; }
    public int?      OvertimeRequestId      { get; set; }
    public DateTime  CreatedAt              { get; set; }

    // Navigation
    public User?            User            { get; set; }
    public OvertimeRequest? OvertimeRequest { get; set; }
}
