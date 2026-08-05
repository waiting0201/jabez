namespace Jabez.Api.Models.Entities;

public class AttendanceRecord
{
    public int       Id                     { get; set; }
    public Guid      UserId                 { get; set; }
    public DateTime  RecordDate             { get; set; }
    public DateTime? ClockInTime            { get; set; }
    public double?   ClockInLatitude        { get; set; }
    public double?   ClockInLongitude       { get; set; }
    public DateTime? ClockOutTime           { get; set; }
    public double?   ClockOutLatitude       { get; set; }
    public double?   ClockOutLongitude      { get; set; }
    /// <summary>下班時間為系統自動補卡（登入時補打漏打的下班卡），非本人打卡</summary>
    public bool      IsClockOutAuto         { get; set; }
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
