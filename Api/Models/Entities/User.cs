using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class User
{
    public Guid     Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public string?  Avatar       { get; set; }
    public string?  SignatureUrl { get; set; }
    public string    Status       { get; set; } = "active"; // "active" | "inactive"
    public DateTime  CreatedAt    { get; set; } = Clock.Now;
    public DateTime  UpdatedAt    { get; set; } = Clock.Now;

    // Employee fields
    public int?      DepartmentId { get; set; }
    public int?      JobTitleId   { get; set; }
    public DateTime? HireDate     { get; set; }
    public DateTime? ResignDate   { get; set; }
    public decimal?  BaseSalary   { get; set; }
    public Guid?     AgentUserId  { get; set; }
    public DateTime? Birthday     { get; set; }

    // 超管旗標：不受角色/權限異動影響，永遠擁有全系統存取權
    public bool IsSuperAdmin { get; set; } = false;

    // 首次登入須修改密碼
    public bool MustChangePassword { get; set; } = false;

    // Navigation
    public ICollection<UserRole>     UserRoles     { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public Department?               Department    { get; set; }
    public JobTitle?                 JobTitle      { get; set; }
    public User?                     Agent         { get; set; }
}
