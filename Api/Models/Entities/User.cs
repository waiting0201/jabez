using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

public class User
{
    public Guid     Id           { get; set; }
    public string   Name         { get; set; } = string.Empty;
    public string   Email        { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public string?  Avatar              { get; set; }
    public decimal  AvatarPositionX     { get; set; } = 50m;   // 頭像 X 位置（0-100%），預設 50 = 置中
    public decimal  AvatarPositionY     { get; set; } = 50m;   // 頭像 Y 位置（0-100%），預設 50 = 置中
    public decimal  AvatarScale         { get; set; } = 1m;    // 頭像縮放倍率（1.0-3.0），預設 1.0 = 無縮放
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

    // 加給（與 BaseSalary / MealAllowance 同：來自最新生效 SalaryAdjustmentRecord 自動同步，也可在基本資料手動編輯）
    // 2026-08 移除職務加給 / 主管加給 / 外派加給，僅保留以下兩項。
    // DB 欄位（Users.PositionAllowance / DutyAllowance / OverseasAllowance）刻意保留作歷史封存，未產 DROP migration。
    public decimal?  OtherAllowance       { get; set; }   // 其他加給
    public decimal?  AdjustmentDifference { get; set; }   // 調整差額
    public bool      SendPaySlip     { get; set; }   // 是否寄送薪資表

    // 期初補休時數：系統上線前（115/1~6/30）以紙本累計、匯入系統的補休時數；
    // 設定於 116/6/30 前未休完即歸零（到期日為固定常數，見 LeaveRequestHandler.CompensatoryOpeningExpiry）
    public decimal   CompensatoryOpeningHours { get; set; } = 0m;

    public Guid?     AgentUserId     { get; set; }
    public DateTime? Birthday        { get; set; }
    public bool      IsIndigenous    { get; set; }   // 是否為原住民（影響歲時祭儀假申請）

    // 低收入 / 殘障身份（與原住民相同的條件式上傳模式）
    public bool   IsLowIncome         { get; set; } = false;
    public string? LowIncomeProofUrl  { get; set; }   // 低收入戶證明文件（圖或 PDF）
    public bool   IsDisabled          { get; set; } = false;
    public string? DisabledProofUrl   { get; set; }   // 殘障手冊 / 身心障礙證明（圖或 PDF）

    // 健保 / 勞保金額手動覆寫（null = 走 lookup 級距表）
    public decimal? HealthInsuranceOverride { get; set; }
    public decimal? LaborInsuranceOverride  { get; set; }

    // 勞退自提率（%，員工自願提撥；null = 0%，非覆寫、無 lookup，直接乘底薪算扣款）
    public decimal? LaborPensionSelfContributionRate { get; set; }

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
