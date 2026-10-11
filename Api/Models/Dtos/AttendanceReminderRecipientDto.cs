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
    bool     AfternoonOnly,
    // 自訂上下班時段（"HH:mm"；null ＝ 公司預設），由呼叫端以 ClockProfile.For 轉成打卡參數
    string?  CustomWorkStartTime,
    string?  CustomWorkEndTime);

/// <summary>
/// 設有自訂上下班時段者（賣店等）的上班提醒收件人：時點每人不同（上班 − 2 分），
/// 故連同兩個時段一起回傳，由呼叫端判斷「現在該不該推」。僅四週彈性工時切換後使用。
/// </summary>
public sealed record AttendanceReminderCustomRecipientDto(
    Guid    UserId,
    string  LineUserId,
    string  UserName,
    string  CustomWorkStartTime,
    string  CustomWorkEndTime);
