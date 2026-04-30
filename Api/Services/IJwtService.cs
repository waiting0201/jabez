using System.Security.Claims;

namespace Jabez.Api.Services;

public interface IJwtService
{
    /// <summary>產生 Access Token，claims 包含 sub / name / email / roles / permissions。isSuperAdmin=true 時額外加入 is_superadmin claim。</summary>
    string GenerateAccessToken(
        Guid                userId,
        string              name,
        string              email,
        IEnumerable<string> roleIds,
        IEnumerable<string> permissionCodes,
        bool                isSuperAdmin = false,
        string?             departmentName = null,
        string?             jobTitleName = null,
        string?             departmentCode = null,
        int?                departmentId = null,
        int?                jobTitleLevel = null,
        string?             avatar = null,
        decimal?            avatarPositionX = null,
        decimal?            avatarPositionY = null,
        decimal?            avatarScale = null);

    /// <summary>產生 Refresh Token（隨機不透明字串）。</summary>
    string GenerateRefreshToken();

    /// <summary>驗證 token，成功回傳 ClaimsPrincipal，失敗回傳 null。</summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>從 HttpRequest Authorization header 解析並驗證 JWT。</summary>
    Task<ClaimsPrincipal?> ValidateRequestAsync(Microsoft.AspNetCore.Http.HttpRequest req);
}
