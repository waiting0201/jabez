using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReminderReadService
{
    /// <summary>
    /// 查詢應被打卡提醒的員工清單。
    /// 排除條件：未綁定 LINE、Superadmin、已離職、今日已打該類型卡、
    /// 請假覆蓋目標時刻（targetTime 落在 approved 請假的 [StartDate, EndDate] 區間內）。
    /// </summary>
    /// <param name="targetTime">目標時刻（台北時區，例如今日 09:00 為 clockIn 提醒目標）</param>
    /// <param name="type">"clockIn" 或 "clockOut"</param>
    Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime targetTime, string type, CancellationToken ct = default);
}
