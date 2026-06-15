namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性員工匯入中間 JSON（Api/Data/Seed/employee-import.json）對應的 record。
/// 日期欄位皆為原始字串，由 <see cref="RocDateParser"/> 解析。
/// 子表沿用人事資料卡語意；空陣列代表該員無該類資料。
/// </summary>
public sealed class EmployeeImportRecord
{
    public string  FolderName        { get; set; } = "";
    public string  EmployeeNumber    { get; set; } = "";

    // ── User ──────────────────────────────────────────────
    public string  Name              { get; set; } = "";
    public string  Email             { get; set; } = "";
    public bool    EmailIsPlaceholder { get; set; }
    public string? Birthday          { get; set; }
    public string? HireDate          { get; set; }
    public string? DepartmentText    { get; set; }
    public string? JobTitleText      { get; set; }
    public bool    IsIndigenous      { get; set; }
    public bool    IsLowIncome       { get; set; }
    public bool    IsDisabled        { get; set; }

    // ── EmployeeProfile ───────────────────────────────────
    public string? EnglishName           { get; set; }
    public string? IdNumber              { get; set; }
    public string? Gender                { get; set; }
    public string? MaritalStatus         { get; set; }
    public string? MobilePhone           { get; set; }
    public string? ResidentialAddress    { get; set; }
    public string? ResidentialPhone      { get; set; }
    public string? MailingAddress        { get; set; }
    public string? MailingPhone          { get; set; }
    public string? EmergencyContactName  { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BankCode              { get; set; }
    public string? BankAccount           { get; set; }

    // ── 子表 ──────────────────────────────────────────────
    public List<EducationImport>        EducationRecords          { get; set; } = [];
    public List<EmploymentImport>       EmploymentHistoryRecords  { get; set; } = [];
    public List<FamilyImport>           FamilyMembers             { get; set; } = [];
    public List<LanguageImport>         LanguageAbilities         { get; set; } = [];
    public List<DependentImport>        HealthInsuranceDependents { get; set; } = [];
}

public sealed class EducationImport
{
    public string  School     { get; set; } = "";
    public string? Department  { get; set; }
    public string  Degree     { get; set; } = "graduated";  // graduated | incomplete
    public string? StartDate  { get; set; }
    public string? EndDate    { get; set; }
    public int     Order      { get; set; } = 1;
}

public sealed class EmploymentImport
{
    public string  Organization { get; set; } = "";
    public string  JobTitle     { get; set; } = "";
    public string? StartDate    { get; set; }
    public string? EndDate      { get; set; }
    public int     Order        { get; set; } = 1;
}

public sealed class FamilyImport
{
    public string  Name         { get; set; } = "";
    public string  Relationship { get; set; } = "";
    public int?    Age          { get; set; }
    public string? Occupation   { get; set; }
}

public sealed class LanguageImport
{
    public string Language  { get; set; } = "";
    public string Listening { get; set; } = "good";  // good | fair
    public string Speaking  { get; set; } = "good";
    public string Reading   { get; set; } = "good";
    public string Writing   { get; set; } = "good";
}

public sealed class DependentImport
{
    public string  Name         { get; set; } = "";
    public string  Relationship { get; set; } = "";
    public string? IdNumber     { get; set; }
    public string? BirthDate    { get; set; }
}
