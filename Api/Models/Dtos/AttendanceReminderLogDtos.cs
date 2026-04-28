namespace Jabez.Api.Models.Dtos;

/// <summary>打卡提醒紀錄列表項目（前端列表頁、詳情頁通用）。</summary>
public sealed record AttendanceReminderLogDto(
    long      Id,
    Guid      BatchId,
    DateTime  TickedAt,
    DateTime  TickedAtTaipei,
    string    TargetTimeTaipei,
    string    ReminderType,
    string    TriggerSource,
    Guid?     TriggeredByUserId,
    string?   TriggeredByName,
    Guid?     UserId,
    string?   UserName,
    string?   LineUserIdSnapshot,
    string?   UserNameSnapshot,
    string    Status,
    string?   ErrorCategory,
    string?   ErrorMessage,
    int?      HttpStatusCode,
    int?      DurationMs,
    DateTime  CreatedAt);

/// <summary>列表頁頂部統計卡資料。</summary>
public sealed record AttendanceReminderLogStatsDto(
    int TodayPushed,
    int TodayFailed,
    int TodayBatchTicks,
    IReadOnlyList<AttendanceReminderLogDailyDto> Last7Days);

public sealed record AttendanceReminderLogDailyDto(
    DateTime Day,
    int Pushed,
    int Failed);
