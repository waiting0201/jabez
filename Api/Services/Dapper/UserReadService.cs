using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;

namespace Jabez.Api.Services.Dapper;

/// <summary>
/// 使用 Dapper 進行讀取查詢（含 JOIN），效能優於 EF Core Include。
/// </summary>
public sealed class UserReadService(IDbConnection db) : IUserReadService
{
    /// <summary>
    /// 員工清單篩選片段（姓名模糊比對 + 部門 + 在職狀態 + 勞退自提），供 GetAllAsync 與 GetPagedAsync 共用。
    /// 兩者的 WHERE 皆以 u.IsSuperAdmin = 0 起頭，故此處一律以 AND 串接。
    /// ⚠ 新增需要 Dapper 參數的條件時，兩個呼叫端的匿名參數物件都要同步補上，漏一個會丟 must declare the scalar variable。
    /// </summary>
    private static (string Filter, string? SearchParam) BuildFilter(
        string? search, int? departmentId, string? status, bool? hasLaborPension)
    {
        var filter = string.Empty;
        string? searchParam = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter += " AND u.Name LIKE @Search";
            searchParam = $"%{search.Trim()}%";
        }

        if (departmentId.HasValue)
            filter += " AND u.DepartmentId = @DepartmentId";

        if (!string.IsNullOrWhiteSpace(status))
            filter += " AND u.Status = @Status";

        // 勞退自提率 null 與 0 同義（皆為未自提），故以 ISNULL 正規化後比較；
        // 布林條件直接寫進 SQL 文字，不需額外 Dapper 參數
        if (hasLaborPension.HasValue)
            filter += hasLaborPension.Value
                ? " AND ISNULL(u.LaborPensionSelfContributionRate, 0) > 0"
                : " AND ISNULL(u.LaborPensionSelfContributionRate, 0) = 0";

        return (filter, searchParam);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync(string? search = null, int? departmentId = null,
        string? status = null, bool? hasLaborPension = null)
    {
        var (filter, searchParam) = BuildFilter(search, departmentId, status, hasLaborPension);

        var sql = $"""
            SELECT
                u.Id, u.Name, u.Email, u.Avatar, u.SignatureUrl, u.Status, u.CreatedAt,
                u.DepartmentId, d.Name AS DepartmentName,
                u.JobTitleId,   jt.Name AS JobTitleName,
                u.HireDate, u.ResignDate, u.BaseSalary,
                u.MealAllowance, u.OvertimePay, u.SendPaySlip,
                u.AgentUserId,  ag.Name AS AgentName,
                u.Birthday,
                u.IsIndigenous,
                u.IndigenousProofUrl,
                u.LineUserId, u.LineLinkedAt,
                u.AvatarPositionX, u.AvatarPositionY, u.AvatarScale,
                u.IsLowIncome, u.LowIncomeProofUrl,
                u.IsDisabled, u.DisabledProofUrl,
                u.HealthInsuranceOverride, u.LaborInsuranceOverride,
                u.OtherAllowance, u.AdjustmentDifference,
                u.CompensatoryOpeningHours,
                u.IsShiftWorker,
                u.LaborPensionSelfContributionRate,
                r.Id AS RoleId
            FROM Users u
            LEFT JOIN UserRoles ur  ON u.Id = ur.UserId
            LEFT JOIN Roles r       ON ur.RoleId = r.Id
            LEFT JOIN Departments d ON u.DepartmentId = d.Id
            LEFT JOIN JobTitles jt  ON u.JobTitleId = jt.Id
            LEFT JOIN Users ag      ON u.AgentUserId = ag.Id
            WHERE u.IsSuperAdmin = 0{filter}
            ORDER BY u.CreatedAt
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { Search = searchParam, DepartmentId = departmentId, Status = status });

        var dict = new Dictionary<Guid, (
            string Name, string Email, string? Avatar, string? SignatureUrl, string Status,
            int? DepartmentId, string? DepartmentName,
            int? JobTitleId, string? JobTitleName,
            DateTime? HireDate, DateTime? ResignDate, decimal? BaseSalary,
            decimal? MealAllowance, decimal? OvertimePay, bool SendPaySlip,
            Guid? AgentUserId, string? AgentName,
            DateTime? Birthday,
            bool IsIndigenous,
            string? IndigenousProofUrl,
            string? LineUserId, DateTime? LineLinkedAt,
            decimal AvatarPositionX, decimal AvatarPositionY, decimal AvatarScale,
            DateTime CreatedAt,
            bool IsLowIncome, string? LowIncomeProofUrl,
            bool IsDisabled, string? DisabledProofUrl,
            decimal? HealthInsuranceOverride, decimal? LaborInsuranceOverride,
            decimal? OtherAllowance, decimal? AdjustmentDifference,
            decimal CompensatoryOpeningHours,
            bool IsShiftWorker,
            decimal? LaborPensionSelfContributionRate,
            List<string> RoleIds)>();

        foreach (var row in rows)
        {
            var id = (Guid)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (
                    (string)row.Name, (string)row.Email, (string?)row.Avatar, (string?)row.SignatureUrl, (string)row.Status,
                    (int?)row.DepartmentId, (string?)row.DepartmentName,
                    (int?)row.JobTitleId, (string?)row.JobTitleName,
                    (DateTime?)row.HireDate, (DateTime?)row.ResignDate, (decimal?)row.BaseSalary,
                    (decimal?)row.MealAllowance, (decimal?)row.OvertimePay, (bool)row.SendPaySlip,
                    row.AgentUserId is null ? null : (Guid?)row.AgentUserId,
                    (string?)row.AgentName,
                    (DateTime?)row.Birthday,
                    (bool)row.IsIndigenous,
                    (string?)row.IndigenousProofUrl,
                    (string?)row.LineUserId, (DateTime?)row.LineLinkedAt,
                    (decimal)row.AvatarPositionX, (decimal)row.AvatarPositionY, (decimal)row.AvatarScale,
                    (DateTime)row.CreatedAt,
                    (bool)row.IsLowIncome, (string?)row.LowIncomeProofUrl,
                    (bool)row.IsDisabled, (string?)row.DisabledProofUrl,
                    (decimal?)row.HealthInsuranceOverride, (decimal?)row.LaborInsuranceOverride,
                    (decimal?)row.OtherAllowance, (decimal?)row.AdjustmentDifference,
                    (decimal)row.CompensatoryOpeningHours,
                    (bool)row.IsShiftWorker,
                    (decimal?)row.LaborPensionSelfContributionRate,
                    []);

            if (row.RoleId is not null)
                dict[id].RoleIds.Add((string)row.RoleId);
        }

        return dict.Select(kv => new UserDto(
            kv.Key,
            kv.Value.Name, kv.Value.Email, kv.Value.Avatar, kv.Value.SignatureUrl,
            kv.Value.RoleIds.ToArray(), kv.Value.Status,
            kv.Value.DepartmentId, kv.Value.DepartmentName,
            kv.Value.JobTitleId,   kv.Value.JobTitleName,
            kv.Value.HireDate, kv.Value.ResignDate, kv.Value.BaseSalary,
            kv.Value.MealAllowance, kv.Value.OvertimePay, kv.Value.SendPaySlip,
            kv.Value.AgentUserId, kv.Value.AgentName,
            kv.Value.Birthday,
            kv.Value.CreatedAt,
            kv.Value.IsIndigenous,
            kv.Value.LineUserId, kv.Value.LineLinkedAt,
            kv.Value.IndigenousProofUrl,
            kv.Value.AvatarPositionX, kv.Value.AvatarPositionY, kv.Value.AvatarScale,
            kv.Value.IsLowIncome, kv.Value.LowIncomeProofUrl,
            kv.Value.IsDisabled, kv.Value.DisabledProofUrl,
            kv.Value.HealthInsuranceOverride, kv.Value.LaborInsuranceOverride,
            kv.Value.OtherAllowance, kv.Value.AdjustmentDifference,
            kv.Value.CompensatoryOpeningHours,
            kv.Value.IsShiftWorker,
            kv.Value.LaborPensionSelfContributionRate));
    }

    /// <summary>輕量級使用者清單（供指定審核者下拉選單，不需 users:read 權限）</summary>
    public async Task<IEnumerable<UserLookupDto>> GetLookupAsync()
    {
        const string sql = """
            SELECT u.Id, u.Name, u.JobTitleId, u.Status, u.DepartmentId, j.Level AS JobTitleLevel
            FROM Users u
            LEFT JOIN JobTitles j ON u.JobTitleId = j.Id
            WHERE u.IsSuperAdmin = 0
            ORDER BY u.Name
            """;

        return await db.QueryAsync<UserLookupDto>(sql);
    }

    /// <summary>
    /// 依部門 scope 過濾的輕量使用者清單（供報表員工下拉，避免顯示無法查到資料的員工）。
    /// 規則同 IProjectAccessResolver：SeeAll → 全部；AllowedIds 為空 → 空集合；否則 DepartmentId IN @AllowedDeptIds。
    /// </summary>
    public async Task<IEnumerable<UserLookupDto>> GetLookupAsync(ProjectAccessScope scope)
    {
        if (scope.SeeAll) return await GetLookupAsync();
        if (scope.AllowedDepartmentIds.Count == 0) return [];

        const string sql = """
            SELECT u.Id, u.Name, u.JobTitleId, u.Status, u.DepartmentId, j.Level AS JobTitleLevel
            FROM Users u
            LEFT JOIN JobTitles j ON u.JobTitleId = j.Id
            WHERE u.IsSuperAdmin = 0
              AND u.DepartmentId IN @AllowedDeptIds
            ORDER BY u.Name
            """;

        return await db.QueryAsync<UserLookupDto>(sql, new { AllowedDeptIds = scope.AllowedDepartmentIds });
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, string? search = null, int? departmentId = null,
        string? status = null, bool? hasLaborPension = null)
    {
        var (filter, searchParam) = BuildFilter(search, departmentId, status, hasLaborPension);

        var countSql = $"SELECT COUNT(*) FROM Users u WHERE u.IsSuperAdmin = 0{filter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT u.Id FROM Users u WHERE u.IsSuperAdmin = 0{filter}
                ORDER BY u.CreatedAt OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            SELECT
                u.Id, u.Name, u.Email, u.Avatar, u.SignatureUrl, u.Status, u.CreatedAt,
                u.DepartmentId, d.Name AS DepartmentName,
                u.JobTitleId,   jt.Name AS JobTitleName,
                u.HireDate, u.ResignDate, u.BaseSalary,
                u.MealAllowance, u.OvertimePay, u.SendPaySlip,
                u.AgentUserId,  ag.Name AS AgentName,
                u.Birthday,
                u.IsIndigenous,
                u.IndigenousProofUrl,
                u.LineUserId, u.LineLinkedAt,
                u.AvatarPositionX, u.AvatarPositionY, u.AvatarScale,
                u.IsLowIncome, u.LowIncomeProofUrl,
                u.IsDisabled, u.DisabledProofUrl,
                u.HealthInsuranceOverride, u.LaborInsuranceOverride,
                u.OtherAllowance, u.AdjustmentDifference,
                u.CompensatoryOpeningHours,
                u.IsShiftWorker,
                u.LaborPensionSelfContributionRate,
                r.Id AS RoleId
            FROM Users u
            INNER JOIN PagedIds pid ON u.Id = pid.Id
            LEFT JOIN UserRoles ur  ON u.Id = ur.UserId
            LEFT JOIN Roles r       ON ur.RoleId = r.Id
            LEFT JOIN Departments d ON u.DepartmentId = d.Id
            LEFT JOIN JobTitles jt  ON u.JobTitleId = jt.Id
            LEFT JOIN Users ag      ON u.AgentUserId = ag.Id
            ORDER BY u.CreatedAt
            """;

        var param = new { Search = searchParam, DepartmentId = departmentId, Status = status, Skip = (page - 1) * pageSize, Take = pageSize };

        int total = await db.ExecuteScalarAsync<int>(countSql, param);
        var rows = await db.QueryAsync<dynamic>(sql, param);

        var dict = new Dictionary<Guid, (
            string Name, string Email, string? Avatar, string? SignatureUrl, string Status,
            int? DepartmentId, string? DepartmentName,
            int? JobTitleId, string? JobTitleName,
            DateTime? HireDate, DateTime? ResignDate, decimal? BaseSalary,
            decimal? MealAllowance, decimal? OvertimePay, bool SendPaySlip,
            Guid? AgentUserId, string? AgentName,
            DateTime? Birthday,
            bool IsIndigenous,
            string? IndigenousProofUrl,
            string? LineUserId, DateTime? LineLinkedAt,
            decimal AvatarPositionX, decimal AvatarPositionY, decimal AvatarScale,
            DateTime CreatedAt,
            bool IsLowIncome, string? LowIncomeProofUrl,
            bool IsDisabled, string? DisabledProofUrl,
            decimal? HealthInsuranceOverride, decimal? LaborInsuranceOverride,
            decimal? OtherAllowance, decimal? AdjustmentDifference,
            decimal CompensatoryOpeningHours,
            bool IsShiftWorker,
            decimal? LaborPensionSelfContributionRate,
            List<string> RoleIds)>();

        foreach (var row in rows)
        {
            var id = (Guid)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (
                    (string)row.Name, (string)row.Email, (string?)row.Avatar, (string?)row.SignatureUrl, (string)row.Status,
                    (int?)row.DepartmentId, (string?)row.DepartmentName,
                    (int?)row.JobTitleId, (string?)row.JobTitleName,
                    (DateTime?)row.HireDate, (DateTime?)row.ResignDate, (decimal?)row.BaseSalary,
                    (decimal?)row.MealAllowance, (decimal?)row.OvertimePay, (bool)row.SendPaySlip,
                    row.AgentUserId is null ? null : (Guid?)row.AgentUserId,
                    (string?)row.AgentName,
                    (DateTime?)row.Birthday,
                    (bool)row.IsIndigenous,
                    (string?)row.IndigenousProofUrl,
                    (string?)row.LineUserId, (DateTime?)row.LineLinkedAt,
                    (decimal)row.AvatarPositionX, (decimal)row.AvatarPositionY, (decimal)row.AvatarScale,
                    (DateTime)row.CreatedAt,
                    (bool)row.IsLowIncome, (string?)row.LowIncomeProofUrl,
                    (bool)row.IsDisabled, (string?)row.DisabledProofUrl,
                    (decimal?)row.HealthInsuranceOverride, (decimal?)row.LaborInsuranceOverride,
                    (decimal?)row.OtherAllowance, (decimal?)row.AdjustmentDifference,
                    (decimal)row.CompensatoryOpeningHours,
                    (bool)row.IsShiftWorker,
                    (decimal?)row.LaborPensionSelfContributionRate,
                    []);

            if (row.RoleId is not null)
                dict[id].RoleIds.Add((string)row.RoleId);
        }

        var items = dict.Select(kv => new UserDto(
            kv.Key,
            kv.Value.Name, kv.Value.Email, kv.Value.Avatar, kv.Value.SignatureUrl,
            kv.Value.RoleIds.ToArray(), kv.Value.Status,
            kv.Value.DepartmentId, kv.Value.DepartmentName,
            kv.Value.JobTitleId,   kv.Value.JobTitleName,
            kv.Value.HireDate, kv.Value.ResignDate, kv.Value.BaseSalary,
            kv.Value.MealAllowance, kv.Value.OvertimePay, kv.Value.SendPaySlip,
            kv.Value.AgentUserId, kv.Value.AgentName,
            kv.Value.Birthday,
            kv.Value.CreatedAt,
            kv.Value.IsIndigenous,
            kv.Value.LineUserId, kv.Value.LineLinkedAt,
            kv.Value.IndigenousProofUrl,
            kv.Value.AvatarPositionX, kv.Value.AvatarPositionY, kv.Value.AvatarScale,
            kv.Value.IsLowIncome, kv.Value.LowIncomeProofUrl,
            kv.Value.IsDisabled, kv.Value.DisabledProofUrl,
            kv.Value.HealthInsuranceOverride, kv.Value.LaborInsuranceOverride,
            kv.Value.OtherAllowance, kv.Value.AdjustmentDifference,
            kv.Value.CompensatoryOpeningHours,
            kv.Value.IsShiftWorker,
            kv.Value.LaborPensionSelfContributionRate));

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<UserDto>(items, total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                u.Id, u.Name, u.Email, u.Avatar, u.SignatureUrl, u.Status, u.CreatedAt,
                u.DepartmentId, d.Name AS DepartmentName,
                u.JobTitleId,   jt.Name AS JobTitleName,
                u.HireDate, u.ResignDate, u.BaseSalary,
                u.MealAllowance, u.OvertimePay, u.SendPaySlip,
                u.AgentUserId,  ag.Name AS AgentName,
                u.Birthday,
                u.IsIndigenous,
                u.IndigenousProofUrl,
                u.LineUserId, u.LineLinkedAt,
                u.AvatarPositionX, u.AvatarPositionY, u.AvatarScale,
                u.IsLowIncome, u.LowIncomeProofUrl,
                u.IsDisabled, u.DisabledProofUrl,
                u.HealthInsuranceOverride, u.LaborInsuranceOverride,
                u.OtherAllowance, u.AdjustmentDifference,
                u.CompensatoryOpeningHours,
                u.IsShiftWorker,
                u.LaborPensionSelfContributionRate,
                r.Id AS RoleId
            FROM Users u
            LEFT JOIN UserRoles ur  ON u.Id = ur.UserId
            LEFT JOIN Roles r       ON ur.RoleId = r.Id
            LEFT JOIN Departments d ON u.DepartmentId = d.Id
            LEFT JOIN JobTitles jt  ON u.JobTitleId = jt.Id
            LEFT JOIN Users ag      ON u.AgentUserId = ag.Id
            WHERE u.Id = @Id
            """;

        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });

        UserDto? result = null;
        var roleIds = new List<string>();

        foreach (var row in rows)
        {
            result ??= new UserDto(
                (Guid)row.Id, (string)row.Name, (string)row.Email, (string?)row.Avatar, (string?)row.SignatureUrl,
                Array.Empty<string>(), (string)row.Status,
                (int?)row.DepartmentId, (string?)row.DepartmentName,
                (int?)row.JobTitleId,   (string?)row.JobTitleName,
                (DateTime?)row.HireDate, (DateTime?)row.ResignDate, (decimal?)row.BaseSalary,
                (decimal?)row.MealAllowance, (decimal?)row.OvertimePay, (bool)row.SendPaySlip,
                row.AgentUserId is null ? null : (Guid?)row.AgentUserId,
                (string?)row.AgentName,
                (DateTime?)row.Birthday,
                (DateTime)row.CreatedAt,
                (bool)row.IsIndigenous,
                (string?)row.LineUserId, (DateTime?)row.LineLinkedAt,
                (string?)row.IndigenousProofUrl,
                (decimal)row.AvatarPositionX, (decimal)row.AvatarPositionY, (decimal)row.AvatarScale,
                (bool)row.IsLowIncome, (string?)row.LowIncomeProofUrl,
                (bool)row.IsDisabled, (string?)row.DisabledProofUrl,
                (decimal?)row.HealthInsuranceOverride, (decimal?)row.LaborInsuranceOverride,
                (decimal?)row.OtherAllowance, (decimal?)row.AdjustmentDifference,
                (decimal)row.CompensatoryOpeningHours,
                (bool)row.IsShiftWorker,
                (decimal?)row.LaborPensionSelfContributionRate);

            if (row.RoleId is not null)
                roleIds.Add((string)row.RoleId);
        }

        return result is null ? null : result with { RoleIds = roleIds.ToArray() };
    }
}
