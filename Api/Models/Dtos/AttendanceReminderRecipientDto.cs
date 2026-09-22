namespace Jabez.Api.Models.Dtos;

/// <summary>打卡提醒推播對象。</summary>
public sealed record AttendanceReminderRecipientDto(
    Guid   UserId,
    string LineUserId,
    string UserName);

/// <summary>
/// 個人化下班提醒的候選人：今天打了上班卡、還沒打下班卡的人。
///
/// ⚠ 舊制的收件人 SQL 只用 <c>NOT EXISTS</c> 判「今天有沒有打卡」，**沒把 ClockInTime 的值撈出來**；
/// 新制的下班提醒時點是「實際上班打卡 ＋ 9 小時 − 2 分」，每人不同，沒有這個值就算不出來。
/// </summary>
/// <param name="AfternoonOnly">當日請了上午半天假（下午才上班）→ 應下班時間改為 ＋4 小時。</param>
public sealed record AttendanceReminderClockOutCandidateDto(
    Guid     UserId,
    string   LineUserId,
    string   UserName,
    DateTime ClockInTime,
    bool     AfternoonOnly);
