namespace Jabez.Api.Common;

/// <summary>
/// 領域例外，附帶 HTTP status code。
/// 由 ExceptionMiddleware 攔截並轉為統一 JSON 回應。
/// </summary>
public sealed class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400)
        : base(message) => StatusCode = statusCode;

    // ── 便利工廠方法 ─────────────────────────────────────────────────
    public static AppException NotFound(string resource) =>
        new($"{resource} not found.", 404);

    public static AppException Unauthorized(string? detail = null) =>
        new(detail ?? "Unauthorized.", 401);

    public static AppException Forbidden(string? detail = null) =>
        new(detail ?? "Forbidden.", 403);

    public static AppException BadRequest(string detail) =>
        new(detail, 400);

    public static AppException Conflict(string detail) =>
        new(detail, 409);
}
