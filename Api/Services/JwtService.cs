using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Jabez.Api.Services;

public sealed class JwtService : IJwtService
{
    private readonly string             _secret;
    private readonly string             _issuer;
    private readonly string             _audience;
    private readonly int                _expiryMinutes;
    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _handler = new()
    {
        // 停用預設 claim type 映射，保持 JWT 原始 claim name（sub, name, email 等）
        // 否則 sub → http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
        MapInboundClaims = false,
    };

    public JwtService(IConfiguration config)
    {
        _secret        = config["Jwt:Secret"]   ?? throw new InvalidOperationException("Jwt:Secret is required.");
        _issuer        = config["Jwt:Issuer"]   ?? "jabez-api";
        _audience      = config["Jwt:Audience"] ?? "jabez-admin";
        _expiryMinutes = int.TryParse(config["Jwt:ExpiryMinutes"], out var em) ? em : 60;
        _key           = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
    }

    public string GenerateAccessToken(
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
        decimal?            avatarScale = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new(JwtRegisteredClaimNames.Name,  name),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        // 超管旗標 — Angular JWT decode reads payload.is_superadmin
        if (isSuperAdmin)
            claims.Add(new Claim("is_superadmin", "true"));

        // 部門名稱 — Angular JWT decode reads payload.department_name
        if (!string.IsNullOrEmpty(departmentName))
            claims.Add(new Claim("department_name", departmentName));

        // 職稱名稱 — Angular JWT decode reads payload.job_title_name
        if (!string.IsNullOrEmpty(jobTitleName))
            claims.Add(new Claim("job_title_name", jobTitleName));

        // 部門代碼 — Angular JWT decode reads payload.department_code
        if (!string.IsNullOrEmpty(departmentCode))
            claims.Add(new Claim("department_code", departmentCode));

        // 部門 Id — ProjectAccessResolver 依此查 CanViewSiblings 與同層兄弟部門
        if (departmentId.HasValue)
            claims.Add(new Claim("department_id", departmentId.Value.ToString()));

        // 職級 — Angular JWT decode reads payload.job_title_level（高階主管假權限判斷）
        if (jobTitleLevel.HasValue)
            claims.Add(new Claim("job_title_level", jobTitleLevel.Value.ToString()));

        // 頭像 — Angular JWT decode reads payload.avatar（topbar profile dropdown 顯示）
        if (!string.IsNullOrEmpty(avatar))
            claims.Add(new Claim("avatar", avatar));

        // 頭像位置 / 縮放 — 僅在非預設值時加入 claim 以節省 token byte
        if (avatarPositionX.HasValue && avatarPositionX.Value != 50m)
            claims.Add(new Claim("avatar_x", avatarPositionX.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (avatarPositionY.HasValue && avatarPositionY.Value != 50m)
            claims.Add(new Claim("avatar_y", avatarPositionY.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (avatarScale.HasValue && avatarScale.Value != 1m)
            claims.Add(new Claim("avatar_scale", avatarScale.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        // Roles — Angular JWT decode reads payload.roles[]
        foreach (var role in roleIds)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));          // literal "roles" for Angular decode
        }

        // Permissions — Angular JWT decode reads payload.permissions[]
        foreach (var perm in permissionCodes)
            claims.Add(new Claim("permissions", perm));

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = _issuer,
                ValidAudience            = _audience,
                IssuerSigningKey         = _key,
                ClockSkew                = TimeSpan.FromSeconds(30),
            };
            return _handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public Task<ClaimsPrincipal?> ValidateRequestAsync(HttpRequest req)
    {
        var authHeader = req.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<ClaimsPrincipal?>(null);

        var token = authHeader["Bearer ".Length..].Trim();
        return Task.FromResult(ValidateToken(token));
    }
}
