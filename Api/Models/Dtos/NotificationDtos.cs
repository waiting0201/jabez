namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 鈴噹通知件數聚合：兩段式 dropdown 用。
/// key 為申請類型字串（payment_request / leave / travel / holiday_travel / overtime / advance / write_off / travel_write_off / travel_payment），
/// value 為當前使用者的件數。
/// </summary>
public sealed record NotificationCountsDto(
    IReadOnlyDictionary<string, int> Approvals,         // 待我簽核（依申請類型分組）
    IReadOnlyDictionary<string, int> MyRequests,        // 我送出的進行中申請（pending / returned）
    IReadOnlyList<RecentApprovalDto> RecentApprovals);  // 我送出且最近（時間窗內）被核准的單，供前端比對時間戳跳 toast

/// <summary>
/// 最近被核准的「我的單」：前端輪詢時用 ApprovedAt 與上次已提示時間比對，
/// 比對到新核准即跳 toast；後端維持無狀態（不需「已讀」資料表）。
/// </summary>
public sealed record RecentApprovalDto(
    string   Type,        // 申請類型字串（與 ApprovalTaskHandler.ValidAppTypes 一致）
    int      Id,          // 申請單 PK
    DateTime ApprovedAt); // 該單最後一次核准動作時間（台北時區）
