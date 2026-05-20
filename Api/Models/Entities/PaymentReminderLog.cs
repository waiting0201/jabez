namespace Jabez.Api.Models.Entities;

/// <summary>
/// 撥款提醒推播紀錄（TimerTrigger 每日跑時對財務人員推一則彙整通知）。
/// 用於：(1) 同日防止重複推播  (2) Superadmin 查詢推播歷史
/// </summary>
public class PaymentReminderLog
{
    public long      Id                 { get; set; }
    public Guid      BatchId            { get; set; }
    public DateTime  TickedAt           { get; set; }                // UTC
    public DateTime  TickedAtTaipei     { get; set; }                // 台北時間
    public DateOnly  ReminderDateTaipei { get; set; }                // 推播當日（台北日曆日）；用於同日去重
    public string    TriggerSource      { get; set; } = "";          // auto / manual
    public Guid?     TriggeredByUserId  { get; set; }                // manual 才有
    public Guid?     FinanceUserId      { get; set; }                // batchStart 為 null
    public string?   LineUserIdSnapshot { get; set; }
    public string?   UserNameSnapshot   { get; set; }
    public int       ItemCount          { get; set; }                // 該則通知含幾筆待撥 installments
    public string    Status             { get; set; } = "";          // success / failure / batchStart / skipped_already_sent
    public string?   ErrorCategory      { get; set; }
    public string?   ErrorMessage       { get; set; }                // 截斷至 500 字
    public int?      HttpStatusCode     { get; set; }
    public int?      DurationMs         { get; set; }
    public DateTime  CreatedAt          { get; set; }                // UTC

    public User?     FinanceUser        { get; set; }
    public User?     TriggeredByUser    { get; set; }
}
