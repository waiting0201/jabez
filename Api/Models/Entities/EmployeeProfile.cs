using Jabez.Api.Common;

namespace Jabez.Api.Models.Entities;

/// <summary>
/// 員工人事資料卡（1:1 User）。
/// PK = UserId，無獨立 Id 欄位。
/// 省略 User 已有的欄位（姓名 / 部門 / 職稱 / 到職日 / 離職日 / 出生日 / 底薪 / 伙食費 / 頭像）。
/// </summary>
public class EmployeeProfile
{
    public Guid    UserId          { get; set; }

    // 個人補充
    public string? EmployeeNumber  { get; set; }   // 員工代號
    public string? EnglishName     { get; set; }   // 英文姓名
    public string? IdNumber        { get; set; }   // 身分證號
    public string? Gender          { get; set; }   // M / F
    public string? MaritalStatus   { get; set; }   // single / married / divorced / widowed
    public string? BirthPlace      { get; set; }   // 出生地
    public string? MobilePhone     { get; set; }   // 行動電話

    // 聯絡資訊
    public string? ResidentialAddress { get; set; }   // 戶籍地址
    public string? ResidentialPhone   { get; set; }   // 戶籍電話
    public string? MailingAddress     { get; set; }   // 通訊地址
    public string? MailingPhone       { get; set; }   // 通訊電話

    // 緊急聯絡
    public string? EmergencyContactName  { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // 銀行帳號
    public string? BankCode    { get; set; }   // 銀行局號
    public string? BankAccount { get; set; }   // 銀行帳號

    // 保險與扶養
    public DateTime? InsuranceStartDate { get; set; }   // 投保起日
    public int?      DependentCount     { get; set; }   // 扶養人數（非健保眷屬概念）

    // 其他
    public string? Specialties       { get; set; }   // 專長興趣（nvarchar max）
    public string? ResignationReason { get; set; }   // 離職原因（nvarchar max）

    // 身分證影本（走授權 file proxy：GET /files/id-cards/{fileName}，需 users:read）
    public string? IdCardFrontUrl { get; set; }
    public string? IdCardBackUrl  { get; set; }

    // 最高學歷證明（走授權 file proxy：GET /files/education-proofs/{fileName}，需 users:read）
    public string? HighestEducationProofUrl { get; set; }

    public DateTime CreatedAt { get; set; } = Clock.Now;
    public DateTime UpdatedAt { get; set; } = Clock.Now;

    // Navigation
    public ICollection<EducationRecord>        EducationRecords       { get; set; } = [];
    public ICollection<EmploymentHistoryRecord> EmploymentHistoryRecords { get; set; } = [];
    public ICollection<FamilyMember>           FamilyMembers          { get; set; } = [];
    public ICollection<ProfessionalTraining>   ProfessionalTrainings  { get; set; } = [];
    public ICollection<LanguageAbility>        LanguageAbilities      { get; set; } = [];
    public ICollection<JobTransferRecord>      JobTransferRecords     { get; set; } = [];
    public ICollection<RewardPunishmentRecord> RewardPunishmentRecords { get; set; } = [];
    public ICollection<SalaryAdjustmentRecord> SalaryAdjustmentRecords { get; set; } = [];
    public ICollection<HealthInsuranceDependent> HealthInsuranceDependents { get; set; } = [];
}
