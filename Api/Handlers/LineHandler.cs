using System.IdentityModel.Tokens.Jwt;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Handlers;

/// <summary>LINE 帳號綁定/解綁 API。</summary>
public sealed class LineHandler(
    AppDbContext db,
    IJwtService jwt,
    ILineService lineService,
    IConfiguration cfg)
{
    /// <summary>GET /line/bind-url — 產生 LINE OAuth URL（含 state 防 CSRF）。</summary>
    public async Task<IActionResult> GetBindUrlAsync(HttpRequest req)
    {
        var channelId   = cfg["Line:LoginChannelId"] ?? "";
        var callbackUrl = cfg["Line:CallbackUrl"]    ?? "";
        var state = Guid.NewGuid().ToString("N");

        // bot_prompt=aggressive：授權完成後自動導向「加 OA 為好友」畫面，
        // 確保用戶能收到 Messaging API 推播（未加好友者一律收不到推播）。
        var url = $"https://access.line.me/oauth2/v2.1/authorize" +
                  $"?response_type=code" +
                  $"&client_id={channelId}" +
                  $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                  $"&state={state}" +
                  $"&scope=openid%20profile" +
                  $"&bot_prompt=aggressive";

        return await Task.FromResult<IActionResult>(
            new OkObjectResult(ApiResponse.Ok(new LineBindUrlDto(url, state))));
    }

    /// <summary>POST /line/bind — 用 OAuth code 換取 LINE userId 並綁定到當前用戶。</summary>
    public async Task<IActionResult> BindAsync(HttpRequest req)
    {
        var principal = await jwt.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var body = await req.ReadFromJsonAsync<LineBindRequest>();
        if (body is null || string.IsNullOrWhiteSpace(body.Code))
            return new BadRequestObjectResult(ApiResponse.Fail("缺少 LINE authorization code。"));

        // 用 code 換取 LINE userId
        var lineUserId = await lineService.ExchangeCodeForUserIdAsync(body.Code, body.RedirectUri);
        if (string.IsNullOrEmpty(lineUserId))
            return new BadRequestObjectResult(ApiResponse.Fail("LINE 驗證失敗，請重試。"));

        // 檢查此 LINE userId 是否已被其他帳號綁定
        var existing = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.LineUserId == lineUserId && u.Id != userId);
        if (existing is not null)
            return new ConflictObjectResult(ApiResponse.Fail("此 LINE 帳號已被其他使用者綁定。"));

        // 綁定
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return new NotFoundObjectResult(ApiResponse.Fail("使用者不存在。"));

        user.LineUserId  = lineUserId;
        user.LineLinkedAt = Clock.Now;
        user.UpdatedAt   = Clock.Now;
        await db.SaveChangesAsync();

        // 綁定成功後立刻查詢 OA 好友狀態（bot_prompt=aggressive 理論上應該是 true，
        // 但用戶可能在 LINE 的加好友畫面拒絕，故實際檢查）
        var isBotFriend = await lineService.IsBotFriendAsync(lineUserId);

        return new OkObjectResult(ApiResponse.Ok(
            new LineBindingStatusDto(true, user.LineLinkedAt, isBotFriend)));
    }

    /// <summary>POST /line/unbind — 解除 LINE 綁定。</summary>
    public async Task<IActionResult> UnbindAsync(HttpRequest req)
    {
        var principal = await jwt.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return new NotFoundObjectResult(ApiResponse.Fail("使用者不存在。"));

        user.LineUserId  = null;
        user.LineLinkedAt = null;
        user.UpdatedAt   = Clock.Now;
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok(
            new LineBindingStatusDto(false, null, false)));
    }

    /// <summary>GET /line/quota — 查詢 LINE Messaging API 月度推播用量（已用 / 上限）。
    /// 權限：line-quota:read（由 AppRouter.GetRequiredPermission 守門；Superadmin 自動通過）。
    /// LINE API 失敗時回傳 success=false，前端顯示「載入中…」即可，不影響其他功能。</summary>
    public async Task<IActionResult> GetQuotaAsync(HttpRequest req)
    {
        var quota = await lineService.GetMessageQuotaAsync();
        if (quota is null)
            return new ObjectResult(ApiResponse.Fail("無法取得 LINE 用量資訊（Token 無效或 LINE API 暫時不可用）。"))
            { StatusCode = StatusCodes.Status502BadGateway };

        return new OkObjectResult(ApiResponse.Ok(quota));
    }

    /// <summary>GET /line/binding-status — 查詢當前用戶的 LINE 綁定狀態。</summary>
    public async Task<IActionResult> GetStatusAsync(HttpRequest req)
    {
        var principal = await jwt.ValidateRequestAsync(req);
        var userIdStr = principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return new UnauthorizedObjectResult(ApiResponse.Fail("Unauthorized."));

        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.LineUserId, u.LineLinkedAt })
            .FirstOrDefaultAsync();

        if (user is null)
            return new NotFoundObjectResult(ApiResponse.Fail("使用者不存在。"));

        var isBound = !string.IsNullOrEmpty(user.LineUserId);
        // 已綁定時才查好友狀態（避免浪費 LINE API 呼叫）
        var isBotFriend = isBound && await lineService.IsBotFriendAsync(user.LineUserId!);

        return new OkObjectResult(ApiResponse.Ok(
            new LineBindingStatusDto(isBound, user.LineLinkedAt, isBotFriend)));
    }
}
