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
    /// <param name="shiftWorkersOnly">
    /// true 時只回排班制員工（User.IsShiftWorker）。六日專用 —— 一般員工週末休假不提醒，
    /// 但賣店 / 營業所排班人員照常上班，仍需提醒。
    /// </param>
    Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetRecipientsAsync(
        DateTime targetTime, string type, bool shiftWorkersOnly = false, CancellationToken ct = default);

    /// <summary>
    /// 個人化下班提醒的候選人（四週彈性工時）：今天打了上班卡、尚未打下班卡、且未被請假覆蓋者，
    /// **連同 ClockInTime 一起回傳**（時點每人不同，算得出來才推得了）。
    /// 實際「現在該不該推」由呼叫端以 <c>ClockRules.ExpectedClockOut</c> 判斷，時點規則不下放到 SQL。
    /// </summary>
    Task<IReadOnlyList<AttendanceReminderClockOutCandidateDto>> GetClockOutCandidatesAsync(
        DateTime today, CancellationToken ct = default);

    /// <summary>
    /// 今天已經成功推播過該類型提醒的人。
    ///
    /// ⚠ 這是**每人每日每類型**的去重，取代整批層級的 batchStart 閘 ——
    /// 下班時點變成每人不同之後，第一個人推播寫下的 batchStart 會把其餘時點的人整批擋死。
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAlreadyPushedUserIdsAsync(
        DateTime today, string reminderType, CancellationToken ct = default);

    /// <summary>
    /// 當日請了半天假的人（供 12:55 交接提醒）。
    /// </summary>
    /// <param name="segment">"am" ＝ 上午請假（下午才上班，提醒打上班卡）／"pm" ＝ 下午請假（提醒先打下班卡）</param>
    Task<IReadOnlyList<AttendanceReminderRecipientDto>> GetHalfDayLeaveRecipientsAsync(
        DateTime today, string segment, DateTime boundary, CancellationToken ct = default);
}
