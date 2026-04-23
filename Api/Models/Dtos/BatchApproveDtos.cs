namespace Jabez.Api.Models.Dtos;

/// <summary>
/// 批次核准請求 — 由使用者在簽核作業清單勾選多筆待審後提交，僅支援 approved 動作。
/// 每筆仍會走 <c>AuthorizeStepAsync</c> 驗證，失敗者回報於 Failed 清單，不影響其他項目。
/// </summary>
public sealed record BatchApproveItem(
    string ApplicationType,
    int    Id);

public sealed record BatchApproveRequest(
    List<BatchApproveItem> Items);

public sealed record BatchApproveFailure(
    string ApplicationType,
    int    Id,
    string Reason);

/// <summary>
/// 核准後需補填撥款/退款日的提醒：
/// Kind = "payment"（撥款）適用 payment_request / advance；
/// Kind = "refund"（退款）適用 write_off / travel_write_off（僅在確實超額退款時提示）。
/// </summary>
public sealed record BatchApprovePending(
    string ApplicationType,
    int    Id,
    string RequestNo,
    string Kind);

public sealed record BatchApproveResult(
    int                        Succeeded,
    List<BatchApproveFailure>  Failed,
    List<BatchApprovePending>  PendingPayment);
