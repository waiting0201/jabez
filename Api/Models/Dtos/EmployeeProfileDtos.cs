namespace Jabez.Api.Models.Dtos;

// ── 子表 DTO ─────────────────────────────────────────────────────────────────

public sealed record EducationRecordDto(
    Guid?     Id,
    string    School,
    string?   Department,
    string    Degree,
    DateTime? StartDate,
    DateTime? EndDate,
    int       Order);

public sealed record EmploymentHistoryRecordDto(
    Guid?     Id,
    string    Organization,
    string    JobTitle,
    DateTime? StartDate,
    DateTime? EndDate,
    int       Order);

public sealed record FamilyMemberDto(
    Guid?   Id,
    string  Name,
    string  Relationship,
    int?    Age,
    string? Occupation);

public sealed record ProfessionalTrainingDto(
    Guid?     Id,
    string    TrainingName,
    string?   TrainingOrg,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal?  Hours);

public sealed record LanguageAbilityDto(
    Guid?  Id,
    string Language,
    string Listening,
    string Speaking,
    string Reading,
    string Writing);

public sealed record JobTransferRecordDto(
    Guid?     Id,
    DateTime  EffectiveDate,
    string?   FromDepartment,
    string?   ToDepartment,
    string?   FromJobTitle,
    string?   ToJobTitle);

public sealed record RewardPunishmentRecordDto(
    Guid?     Id,
    DateTime  EffectiveDate,
    string    Type,
    string?   Category,
    int       Count,
    string?   Reason);

public sealed record SalaryAdjustmentRecordDto(
    Guid?     Id,
    DateTime  EffectiveDate,
    decimal   BaseSalary,
    decimal?  PositionAllowance,
    decimal?  DutyAllowance,
    decimal?  OtherAllowance,
    decimal?  AdjustmentDifference,
    decimal?  OverseasAllowance,
    decimal?  MealAllowance,
    decimal   TotalAmount,
    string?   Notes);

public sealed record HealthInsuranceDependentDto(
    Guid?     Id,
    string    Name,
    string    Relationship,
    string?   IdNumber,
    DateTime? BirthDate);

// ── 主 DTO（完整人事資料卡 + 所有子表）──────────────────────────────────────

/// <summary>GET /users/{id}/profile 的完整回傳格式</summary>
public sealed record EmployeeProfileDetailDto(
    Guid    UserId,

    // 個人補充
    string? EmployeeNumber,
    string? EnglishName,
    string? IdNumber,
    string? Gender,
    string? MaritalStatus,
    string? BirthPlace,
    string? MobilePhone,

    // 聯絡資訊
    string? ResidentialAddress,
    string? ResidentialPhone,
    string? MailingAddress,
    string? MailingPhone,

    // 緊急聯絡
    string? EmergencyContactName,
    string? EmergencyContactPhone,

    // 銀行
    string? BankCode,
    string? BankAccount,

    // 保險與扶養
    DateTime? InsuranceStartDate,
    int?      DependentCount,

    // 其他
    string? Specialties,
    string? ResignationReason,

    // 身分證影本 URL（走 /files/id-cards/ 代理）
    string? IdCardFrontUrl,
    string? IdCardBackUrl,

    // 最高學歷證明 URL（走 /files/education-proofs/ 代理）
    string? HighestEducationProofUrl,

    // 存摺封面 URL（走 /files/passbooks/ 代理）
    string? BankBookImageUrl,

    // 9 個子表
    EducationRecordDto[]         EducationRecords,
    EmploymentHistoryRecordDto[] EmploymentHistoryRecords,
    FamilyMemberDto[]            FamilyMembers,
    ProfessionalTrainingDto[]    ProfessionalTrainings,
    LanguageAbilityDto[]         LanguageAbilities,
    JobTransferRecordDto[]       JobTransferRecords,
    RewardPunishmentRecordDto[]  RewardPunishmentRecords,
    SalaryAdjustmentRecordDto[]  SalaryAdjustmentRecords,
    HealthInsuranceDependentDto[] HealthInsuranceDependents);

// ── Upsert Request（PUT /users/{id}/profile 接收格式）──────────────────────

/// <summary>
/// PUT /users/{id}/profile 的 JSON payload（放在 multipart text part "payload"）。
/// 子表 Id 為 nullable：null = 新增，有值 = 伺服器忽略（整批替換模式，不做 patch）。
///
/// 例外：<see cref="SalaryAdjustmentRecords"/> 為 nullable —— null = 不變更（保留既有整批），
/// [] = 清空。無 payroll:read 的呼叫者前端不送此 key，若當成空陣列處理會把薪資歷史整批刪光。
/// </summary>
public sealed record EmployeeProfileUpsertRequest(
    string? EmployeeNumber,
    string? EnglishName,
    string? IdNumber,
    string? Gender,
    string? MaritalStatus,
    string? BirthPlace,
    string? MobilePhone,
    string? ResidentialAddress,
    string? ResidentialPhone,
    string? MailingAddress,
    string? MailingPhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BankCode,
    string? BankAccount,
    DateTime? InsuranceStartDate,
    int?    DependentCount,
    string? Specialties,
    string? ResignationReason,
    EducationRecordDto[]         EducationRecords,
    EmploymentHistoryRecordDto[] EmploymentHistoryRecords,
    FamilyMemberDto[]            FamilyMembers,
    ProfessionalTrainingDto[]    ProfessionalTrainings,
    LanguageAbilityDto[]         LanguageAbilities,
    JobTransferRecordDto[]       JobTransferRecords,
    RewardPunishmentRecordDto[]  RewardPunishmentRecords,
    SalaryAdjustmentRecordDto[]? SalaryAdjustmentRecords,
    HealthInsuranceDependentDto[] HealthInsuranceDependents);
