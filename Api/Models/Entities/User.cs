using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class User
{
    public Guid     Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public string?  Avatar              { get; set; }
    public string?  SignatureUrl        { get; set; }
    public string?  IndigenousProofUrl  { get; set; }   // 原住民身份證明文件（圖或 PDF）
    public string    Status       { get; set; } = "active"; // "active" | "inactive"
    public DateTime  CreatedAt    { get; set; } = Clock.Now;
    public DateTime  UpdatedAt    { get; set; } = Clock.Now;

    // Employee fields
    public int?      DepartmentId { get; set; }
    public int?      JobTitleId   { get; set; }
    public DateTime? HireDate     { get; set; }
    public DateTime? ResignDate   { get; set; }
    public decimal?  BaseSalary      { get; set; }
    public decimal?  MealAllowance   { get; set; }   // 伙食費
    public decimal?  OvertimePay     { get; set; }   // 加班費
    public bool      SendPaySlip     { get; set; }   // 是否寄送薪資表
    public Guid?     AgentUserId     { get; set; }
    public DateTime? Birthday        { get; set; }
    public bool      IsIndigenous    { get; set; }   // 是否為原住民（影響歲時祭儀假申請）

    // 超管旗標：不受角色/權限異動影響，永遠擁有全系統存取權
    public bool IsSuperAdmin { get; set; } = false;

    // 首次登入須修改密碼
    public bool MustChangePassword { get; set; } = false;

    // LINE 綁定
    public string?   LineUserId  { get; set; }   // LINE platform userId (U 開頭 33 字元)
    public DateTime? LineLinkedAt { get; set; }   // 綁定時間

    // Navigation
    public ICollection<UserRole>     UserRoles     { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public Department?               Department    { get; set; }
    public JobTitle?                 JobTitle      { get; set; }
    public User?                     Agent         { get; set; }
}
