namespace Jabez.Api.Models.Dtos;

/// <summary>LINE 綁定請求（前端傳入 OAuth code）。</summary>
public sealed record LineBindRequest(string Code, string RedirectUri);

/// <summary>LINE 綁定狀態回應。IsBotFriend：是否為 OA 好友（影響推播能否送達）。</summary>
public sealed record LineBindingStatusDto(bool IsBound, DateTime? LineLinkedAt, bool IsBotFriend);

/// <summary>LINE OAuth bind-url 回應。</summary>
public sealed record LineBindUrlDto(string Url, string State);
