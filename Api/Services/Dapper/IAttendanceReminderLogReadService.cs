using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReminderLogReadService
{
    /// <summary>列表（依篩選 + 分頁），日期為台北時區的日期。</summary>
    Task<PagedResult<AttendanceReminderLogDto>> GetPagedAsync(
        DateTime? fromTaipei,
        DateTime? toTaipei,
        string?   reminderType,
        string?   status,
        string?   errorCategory,
        Guid?     userId,
        string?   triggerSource,
        int       page,
        int       pageSize,
        CancellationToken ct);

    Task<AttendanceReminderLogDto?> GetByIdAsync(long id, CancellationToken ct);

    /// <summary>查同一批次（同一次 tick）的所有紀錄，含 batchStart。</summary>
    Task<IReadOnlyList<AttendanceReminderLogDto>> GetByBatchIdAsync(Guid batchId, CancellationToken ct);

    /// <summary>統計卡資料：今日推播數 + 失敗數 + 批次 tick 數，以及最近 7 天每日趨勢。</summary>
    Task<AttendanceReminderLogStatsDto> GetStatsAsync(DateTime todayTaipei, CancellationToken ct);
}
