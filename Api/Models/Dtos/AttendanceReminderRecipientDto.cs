namespace Jabez.Api.Models.Dtos;

/// <summary>打卡提醒推播對象。</summary>
public sealed record AttendanceReminderRecipientDto(
    Guid   UserId,
    string LineUserId,
    string UserName);
