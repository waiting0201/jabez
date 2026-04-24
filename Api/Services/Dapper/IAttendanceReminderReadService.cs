using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public interface IAttendanceReminderReadService
{
    /// <summary>
    /// 查詢應被打卡提醒的員工清單。
    /// 排除條件：未綁定 LINE、Superadmin、已離職、今日已打該類型卡、今日在 approved 請假範圍內。
    /// </summary>
    /// <param name="today">今日日期（台北時區）</param>
    /// <param name="type">"clockIn" 或 "clockOut"</param>
    Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime today, string type, CancellationToken ct = default);
}
