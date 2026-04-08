namespace Jabez.Api.Models.Dtos;

/// <summary>LINE 綁定請求（前端傳入 OAuth code）。</summary>
public sealed record LineBindRequest(string Code, string RedirectUri);

/// <summary>LINE 綁定狀態回應。</summary>
public sealed record LineBindingStatusDto(bool IsBound, DateTime? LineLinkedAt);

/// <summary>LINE OAuth bind-url 回應。</summary>
public sealed record LineBindUrlDto(string Url, string State);
