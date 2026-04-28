namespace Jabez.Api.Models.Entities;

/// <summary>
/// 打卡提醒推播紀錄。每次 TimerTrigger 命中時點 / 手動觸發都會：
/// 1) 寫一筆 batchStart（UserId 為 null，標示排程有跑、即使 0 對象也可驗證）
/// 2) 對每位推播對象寫一筆 success/failure
/// 同一次 tick 共用相同 BatchId。
/// </summary>
public class AttendanceReminderLog
{
    public long      Id                 { get; set; }
    public Guid      BatchId            { get; set; }
    public DateTime  TickedAt           { get; set; }              // UTC
    public DateTime  TickedAtTaipei     { get; set; }              // 台北時間（避免 SQL 端 timezone 轉換）
    public string    TargetTimeTaipei   { get; set; } = "";        // "09:00"
    public string    ReminderType       { get; set; } = "";        // clockIn / clockOut / batchStart
    public string    TriggerSource      { get; set; } = "";        // auto / manual
    public Guid?     TriggeredByUserId  { get; set; }              // manual 才有
    public Guid?     UserId             { get; set; }              // batchStart 為 null
    public string?   LineUserIdSnapshot { get; set; }
    public string?   UserNameSnapshot   { get; set; }
    public string    Status             { get; set; } = "";        // success / failure / batchStart
    public string?   ErrorCategory      { get; set; }              // not_friend / token_invalid / rate_limited / network_error / unknown / system_error
    public string?   ErrorMessage       { get; set; }              // 截斷至 500 字
    public int?      HttpStatusCode     { get; set; }
    public int?      DurationMs         { get; set; }
    public DateTime  CreatedAt          { get; set; }              // UTC

    public User?     User               { get; set; }
    public User?     TriggeredByUser    { get; set; }
}
