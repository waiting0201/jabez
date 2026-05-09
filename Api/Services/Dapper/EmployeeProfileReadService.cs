using Dapper;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 員工人事資料卡 Dapper 讀取服務。
/// 使用 QueryMultipleAsync 一次讀回主表 + 9 個子表，減少 round-trip。
/// </summary>
public sealed class EmployeeProfileReadService(IDbConnection db) : IEmployeeProfileReadService
{
    public async Task<EmployeeProfileDetailDto> GetByUserIdAsync(Guid userId)
    {
        // 10 個 SELECT 合併為單次 QueryMultiple，減少 DB round-trip
        const string sql = """
            SELECT EmployeeNumber, EnglishName, IdNumber, Gender, MaritalStatus, BirthPlace, MobilePhone,
                   ResidentialAddress, ResidentialPhone, MailingAddress, MailingPhone,
                   EmergencyContactName, EmergencyContactPhone,
                   BankCode, BankAccount, InsuranceStartDate, DependentCount,
                   Specialties, ResignationReason, IdCardFrontUrl, IdCardBackUrl,
                   HighestEducationProofUrl
            FROM EmployeeProfiles
            WHERE UserId = @UserId;

            SELECT Id, School, Department, Degree, StartDate, EndDate, [Order]
            FROM EducationRecords
            WHERE UserId = @UserId
            ORDER BY [Order];

            SELECT Id, Organization, JobTitle, StartDate, EndDate, [Order]
            FROM EmploymentHistoryRecords
            WHERE UserId = @UserId
            ORDER BY [Order];

            SELECT Id, Name, Relationship, Age, Occupation
            FROM FamilyMembers
            WHERE UserId = @UserId;

            SELECT Id, TrainingName, TrainingOrg, StartDate, EndDate, Hours
            FROM ProfessionalTrainings
            WHERE UserId = @UserId;

            SELECT Id, Language, Listening, Speaking, Reading, Writing
            FROM LanguageAbilities
            WHERE UserId = @UserId;

            SELECT Id, EffectiveDate, FromDepartment, ToDepartment, FromJobTitle, ToJobTitle
            FROM JobTransferRecords
            WHERE UserId = @UserId
            ORDER BY EffectiveDate DESC;

            SELECT Id, EffectiveDate, Type, Category, Count, Reason
            FROM RewardPunishmentRecords
            WHERE UserId = @UserId
            ORDER BY EffectiveDate DESC;

            SELECT Id, EffectiveDate, BaseSalary, PositionAllowance, DutyAllowance, OtherAllowance,
                   AdjustmentDifference, OverseasAllowance, MealAllowance, TotalAmount, Notes
            FROM SalaryAdjustmentRecords
            WHERE UserId = @UserId
            ORDER BY EffectiveDate DESC;

            SELECT Id, Name, Relationship, IdNumber, BirthDate
            FROM HealthInsuranceDependents
            WHERE UserId = @UserId;
            """;

        using var multi = await db.QueryMultipleAsync(sql, new { UserId = userId });

        // 主表（可能不存在）
        var profile = await multi.ReadFirstOrDefaultAsync<dynamic>();

        // 9 個子表
        var educationRows    = (await multi.ReadAsync<dynamic>()).ToList();
        var employmentRows   = (await multi.ReadAsync<dynamic>()).ToList();
        var familyRows       = (await multi.ReadAsync<dynamic>()).ToList();
        var trainingRows     = (await multi.ReadAsync<dynamic>()).ToList();
        var languageRows     = (await multi.ReadAsync<dynamic>()).ToList();
        var transferRows     = (await multi.ReadAsync<dynamic>()).ToList();
        var rewardRows       = (await multi.ReadAsync<dynamic>()).ToList();
        var salaryRows       = (await multi.ReadAsync<dynamic>()).ToList();
        var dependentRows    = (await multi.ReadAsync<dynamic>()).ToList();

        return new EmployeeProfileDetailDto(
            UserId: userId,

            EmployeeNumber: (string?)profile?.EmployeeNumber,
            EnglishName:    (string?)profile?.EnglishName,
            IdNumber:       (string?)profile?.IdNumber,
            Gender:         (string?)profile?.Gender,
            MaritalStatus:  (string?)profile?.MaritalStatus,
            BirthPlace:     (string?)profile?.BirthPlace,
            MobilePhone:    (string?)profile?.MobilePhone,

            ResidentialAddress: (string?)profile?.ResidentialAddress,
            ResidentialPhone:   (string?)profile?.ResidentialPhone,
            MailingAddress:     (string?)profile?.MailingAddress,
            MailingPhone:       (string?)profile?.MailingPhone,

            EmergencyContactName:  (string?)profile?.EmergencyContactName,
            EmergencyContactPhone: (string?)profile?.EmergencyContactPhone,

            BankCode:    (string?)profile?.BankCode,
            BankAccount: (string?)profile?.BankAccount,

            InsuranceStartDate: (DateTime?)profile?.InsuranceStartDate,
            DependentCount:     (int?)profile?.DependentCount,

            Specialties:       (string?)profile?.Specialties,
            ResignationReason: (string?)profile?.ResignationReason,

            IdCardFrontUrl: (string?)profile?.IdCardFrontUrl,
            IdCardBackUrl:  (string?)profile?.IdCardBackUrl,

            HighestEducationProofUrl: (string?)profile?.HighestEducationProofUrl,

            EducationRecords: educationRows.Select(r => new EducationRecordDto(
                (Guid?)r.Id, (string)r.School, (string?)r.Department,
                (string)r.Degree, (DateTime?)r.StartDate, (DateTime?)r.EndDate, (int)r.Order)).ToArray(),

            EmploymentHistoryRecords: employmentRows.Select(r => new EmploymentHistoryRecordDto(
                (Guid?)r.Id, (string)r.Organization, (string)r.JobTitle,
                (DateTime?)r.StartDate, (DateTime?)r.EndDate, (int)r.Order)).ToArray(),

            FamilyMembers: familyRows.Select(r => new FamilyMemberDto(
                (Guid?)r.Id, (string)r.Name, (string)r.Relationship,
                (int?)r.Age, (string?)r.Occupation)).ToArray(),

            ProfessionalTrainings: trainingRows.Select(r => new ProfessionalTrainingDto(
                (Guid?)r.Id, (string)r.TrainingName, (string?)r.TrainingOrg,
                (DateTime?)r.StartDate, (DateTime?)r.EndDate, (decimal?)r.Hours)).ToArray(),

            LanguageAbilities: languageRows.Select(r => new LanguageAbilityDto(
                (Guid?)r.Id, (string)r.Language,
                (string)r.Listening, (string)r.Speaking,
                (string)r.Reading, (string)r.Writing)).ToArray(),

            JobTransferRecords: transferRows.Select(r => new JobTransferRecordDto(
                (Guid?)r.Id, (DateTime)r.EffectiveDate,
                (string?)r.FromDepartment, (string?)r.ToDepartment,
                (string?)r.FromJobTitle, (string?)r.ToJobTitle)).ToArray(),

            RewardPunishmentRecords: rewardRows.Select(r => new RewardPunishmentRecordDto(
                (Guid?)r.Id, (DateTime)r.EffectiveDate, (string)r.Type,
                (string?)r.Category, (int)r.Count, (string?)r.Reason)).ToArray(),

            SalaryAdjustmentRecords: salaryRows.Select(r => new SalaryAdjustmentRecordDto(
                (Guid?)r.Id, (DateTime)r.EffectiveDate, (decimal)r.BaseSalary,
                (decimal?)r.PositionAllowance, (decimal?)r.DutyAllowance, (decimal?)r.OtherAllowance,
                (decimal?)r.AdjustmentDifference, (decimal?)r.OverseasAllowance, (decimal?)r.MealAllowance,
                (decimal)r.TotalAmount, (string?)r.Notes)).ToArray(),

            HealthInsuranceDependents: dependentRows.Select(r => new HealthInsuranceDependentDto(
                (Guid?)r.Id, (string)r.Name, (string)r.Relationship,
                (string?)r.IdNumber, (DateTime?)r.BirthDate)).ToArray());
    }
}
