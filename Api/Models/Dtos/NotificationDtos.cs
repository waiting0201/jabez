namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 鈴噹通知件數聚合：兩段式 dropdown 用。
/// key 為申請類型字串（payment_request / leave / travel / holiday_travel / overtime / advance / write_off / travel_write_off / travel_payment），
/// value 為當前使用者的件數。
/// </summary>
public sealed record NotificationCountsDto(
    IReadOnlyDictionary<string, int> Approvals,   // 待我簽核（依申請類型分組）
    IReadOnlyDictionary<string, int> MyRequests); // 我送出的進行中申請（pending / returned）
