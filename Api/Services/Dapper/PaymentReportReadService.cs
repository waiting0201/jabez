using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Services;
using System.Data;
using System.Text;

namespace Jabez.Api.Services.Dapper;

public sealed class PaymentReportReadService(IDbConnection db) : IPaymentReportReadService
{
    // ========================================================================
    // 類別常數
    // ========================================================================
    public const string CategoryPayment        = "payment";
    public const string CategoryAdvance        = "advance";
    public const string CategoryWriteOff       = "writeoff";
    public const string CategoryTravelPayment  = "travel-payment";
    public const string CategoryTravel         = "travel";
    public const string CategoryTravelWriteOff = "travel-writeoff";

    public static readonly string[] AllCategories =
    [
        CategoryPayment, CategoryAdvance, CategoryWriteOff,
        CategoryTravelPayment, CategoryTravel, CategoryTravelWriteOff
    ];

    // ========================================================================
    // 共用：dept scope filter
    // ========================================================================
    /// <summary>
    /// 依 scope 產生「申請人部門」過濾片段（前綴 " AND "）。
    /// userAlias 由各 query 指定（例：u / submitter）。
    /// </summary>
    private static string BuildDeptScopeFilter(ProjectAccessScope scope, DynamicParameters parameters, string userAlias = "u")
    {
        if (scope.SeeAll) return "";
        if (scope.AllowedDepartmentIds.Count == 0) return " AND 1=0";
        parameters.Add("AllowedDeptIds", scope.AllowedDepartmentIds);
        return $" AND {userAlias}.DepartmentId IN @AllowedDeptIds";
    }

    // ========================================================================
    // Public dispatcher
    // ========================================================================
    public Task<PagedResult<PaymentReportDto>> GetPagedAsync(
        ProjectAccessScope scope,
        string category,
        int page, int pageSize,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null)
        => category switch
        {
            CategoryPayment        => GetPaymentPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            CategoryAdvance        => GetAdvancePagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            CategoryWriteOff       => GetWriteOffPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            CategoryTravelPayment  => GetTravelPaymentPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            CategoryTravel         => GetTravelPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            CategoryTravelWriteOff => GetTravelWriteOffPagedAsync(scope, page, pageSize, dateFrom, dateTo, paymentStatus),
            _ => throw new AppException("不支援的類別", 400),
        };

    public Task<List<PaymentExportRowDto>> GetExportRowsAsync(
        ProjectAccessScope scope,
        string category,
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string? paymentStatus = null)
        => category switch
        {
            CategoryPayment        => GetPaymentExportAsync(scope, dateFrom, dateTo, paymentStatus),
            CategoryAdvance        => GetAdvanceExportAsync(scope, dateFrom, dateTo, paymentStatus),
            CategoryWriteOff       => GetWriteOffExportAsync(scope, dateFrom, dateTo, paymentStatus),
            CategoryTravelPayment  => GetTravelPaymentExportAsync(scope, dateFrom, dateTo, paymentStatus),
            CategoryTravel         => GetTravelExportAsync(scope, dateFrom, dateTo, paymentStatus),
            CategoryTravelWriteOff => GetTravelWriteOffExportAsync(scope, dateFrom, dateTo, paymentStatus),
            _ => throw new AppException("不支援的類別", 400),
        };

    // ========================================================================
    // 共用：日期 / 付款狀態 where 片段
    // ========================================================================

    /// <summary>
    /// 用主表 alias + 子表 installments 名稱 + parentFk 組裝日期 / 付款狀態 where。
    /// </summary>
    /// <param name="parentAlias">主表 alias（如 "pr"、"adv"）</param>
    /// <param name="dateCol">主表日期欄位（多為 "CreatedAt"）</param>
    /// <param name="installmentsTable">分期撥款子表名（如 "PaymentRequestInstallments"），null 表示無 installments</param>
    /// <param name="installmentsFk">分期撥款子表 FK 欄位（如 "PaymentRequestId"），null 表示無 installments</param>
    private static string BuildDateAndPaymentStatus(
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus,
        DynamicParameters parameters,
        string parentAlias, string dateCol,
        string? installmentsTable, string? installmentsFk)
    {
        var where = new StringBuilder();

        if (dateFrom.HasValue)
        {
            where.Append($" AND {parentAlias}.{dateCol} >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        }
        if (dateTo.HasValue)
        {
            where.Append($" AND {parentAlias}.{dateCol} < DATEADD(day, 1, @DateTo)");
            parameters.Add("DateTo", dateTo.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (string.IsNullOrEmpty(paymentStatus)) return where.ToString();
        if (installmentsTable == null || installmentsFk == null) return where.ToString();  // 沖銷類無 installments，忽略

        if (paymentStatus == "paid")
        {
            where.Append($" AND EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id)");
            where.Append($" AND NOT EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id AND i.PaidAt IS NULL)");
        }
        else if (paymentStatus == "unpaid")
        {
            where.Append($" AND (NOT EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id)");
            where.Append($"      OR EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id AND i.PaidAt IS NULL))");
        }
        return where.ToString();
    }

    /// <summary>
    /// 撥款日推算 CASE 子句：全撥完 → MAX(PaidAt)，否則 NULL；無 installments 表 → 永遠 NULL。
    /// </summary>
    private static string PaidAtCase(string parentAlias, string? installmentsTable, string? installmentsFk)
    {
        if (installmentsTable == null || installmentsFk == null) return "NULL";
        return $"""
            CASE
              WHEN NOT EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id) THEN NULL
              WHEN EXISTS (SELECT 1 FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id AND i.PaidAt IS NULL) THEN NULL
              ELSE (SELECT MAX(i.PaidAt) FROM {installmentsTable} i WHERE i.{installmentsFk} = {parentAlias}.Id)
            END
            """;
    }

    // ========================================================================
    // 1) PaymentRequest 請款
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetPaymentPagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE pr.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "pr", "CreatedAt", "PaymentRequestInstallments", "PaymentRequestId"));

        var paidAt = PaidAtCase("pr", "PaymentRequestInstallments", "PaymentRequestId");

        var countSql = $"""
            SELECT COUNT(*) FROM PaymentRequests pr
            JOIN Users   u    ON pr.SubmittedById = u.Id
            JOIN Projects proj ON pr.ProjectId    = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT pr.Id,
                   pr.RequestNo,
                   u.Name AS EmployeeName,
                   pr.Type,
                   proj.Code AS ProjectCode,
                   proj.Name AS ProjectName,
                   (SELECT STRING_AGG(InvoiceNo, ',') FROM InvoiceItems WHERE PaymentRequestId = pr.Id) AS InvoiceNos,
                   pr.TotalAmount,
                   pr.ApprovalStatus,
                   {paidAt} AS PaidAt,
                   pr.CreatedAt
            FROM PaymentRequests pr
            JOIN Users   u    ON pr.SubmittedById = u.Id
            JOIN Projects proj ON pr.ProjectId    = proj.Id
            {where}
            ORDER BY pr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryPayment, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetPaymentExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE pr.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "pr", "CreatedAt", "PaymentRequestInstallments", "PaymentRequestId"));

        var paidAt = PaidAtCase("pr", "PaymentRequestInstallments", "PaymentRequestId");

        var sql = $"""
            SELECT pr.Id          AS ParentId,
                   pr.RequestNo,
                   u.Name         AS EmployeeName,
                   pr.Type,
                   proj.Code      AS ProjectCode,
                   proj.Name      AS ProjectName,
                   pr.ApprovalStatus,
                   pr.CreatedAt,
                   {paidAt}       AS PaidAt,
                   pr.TotalAmount AS PaymentTotalAmount,
                   ii.InvoiceNo   AS ItemCol1,
                   ii.ItemName    AS ItemName,
                   NULL           AS ItemCol3Text,
                   ii.InvoiceDate AS ItemCol3Date,
                   ii.Amount      AS ItemAmount
            FROM PaymentRequests pr
            JOIN Users     u    ON pr.SubmittedById = u.Id
            JOIN Projects  proj ON pr.ProjectId    = proj.Id
            LEFT JOIN InvoiceItems ii ON ii.PaymentRequestId = pr.Id
            {where}
            ORDER BY pr.CreatedAt DESC, pr.Id DESC, ii.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // 2) AdvanceRequest 預支
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetAdvancePagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE adv.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "adv", "CreatedAt", "AdvanceRequestInstallments", "AdvanceRequestId"));

        var paidAt = PaidAtCase("adv", "AdvanceRequestInstallments", "AdvanceRequestId");

        var countSql = $"""
            SELECT COUNT(*) FROM AdvanceRequests adv
            JOIN Users    u    ON adv.SubmittedById = u.Id
            JOIN Projects proj ON adv.ProjectId    = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT adv.Id,
                   adv.RequestNo,
                   u.Name         AS EmployeeName,
                   'advance'      AS Type,
                   proj.Code      AS ProjectCode,
                   proj.Name      AS ProjectName,
                   NULL           AS InvoiceNos,
                   adv.GrandTotal AS TotalAmount,
                   adv.ApprovalStatus,
                   {paidAt}       AS PaidAt,
                   adv.CreatedAt
            FROM AdvanceRequests adv
            JOIN Users    u    ON adv.SubmittedById = u.Id
            JOIN Projects proj ON adv.ProjectId    = proj.Id
            {where}
            ORDER BY adv.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryAdvance, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetAdvanceExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE adv.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "adv", "CreatedAt", "AdvanceRequestInstallments", "AdvanceRequestId"));

        var paidAt = PaidAtCase("adv", "AdvanceRequestInstallments", "AdvanceRequestId");

        // 預支：item 4 欄 = 類別 / 品名 / 數量(string) / 金額
        var sql = $"""
            SELECT adv.Id         AS ParentId,
                   adv.RequestNo,
                   u.Name         AS EmployeeName,
                   'advance'      AS Type,
                   proj.Code      AS ProjectCode,
                   proj.Name      AS ProjectName,
                   adv.ApprovalStatus,
                   adv.CreatedAt,
                   {paidAt}       AS PaidAt,
                   adv.GrandTotal AS PaymentTotalAmount,
                   item.Category  AS ItemCol1,
                   item.ItemName  AS ItemName,
                   item.Quantity  AS ItemCol3Text,
                   NULL           AS ItemCol3Date,
                   item.TotalPrice AS ItemAmount
            FROM AdvanceRequests adv
            JOIN Users     u    ON adv.SubmittedById = u.Id
            JOIN Projects  proj ON adv.ProjectId    = proj.Id
            LEFT JOIN AdvanceRequestItems item ON item.AdvanceRequestId = adv.Id
            {where}
            ORDER BY adv.CreatedAt DESC, adv.Id DESC, item.SortOrder ASC, item.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // 3) WriteOffRecord 預支沖銷（無 installments；JOIN AdvanceRequest 取 Project + Submitter）
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetWriteOffPagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE wo.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "wo", "CreatedAt", installmentsTable: null, installmentsFk: null));

        var countSql = $"""
            SELECT COUNT(*) FROM WriteOffRecords wo
            JOIN Users           u   ON wo.SubmittedById = u.Id
            JOIN AdvanceRequests adv ON wo.AdvanceRequestId = adv.Id
            JOIN Projects        proj ON adv.ProjectId    = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT wo.Id,
                   wo.RequestNo,
                   u.Name         AS EmployeeName,
                   'writeoff'     AS Type,
                   proj.Code      AS ProjectCode,
                   proj.Name      AS ProjectName,
                   (SELECT STRING_AGG(InvoiceNo, ',') FROM WriteOffItems WHERE WriteOffRecordId = wo.Id AND InvoiceNo IS NOT NULL) AS InvoiceNos,
                   wo.GrandTotal  AS TotalAmount,
                   wo.ApprovalStatus,
                   adv.RefundedAt AS PaidAt,
                   wo.CreatedAt
            FROM WriteOffRecords wo
            JOIN Users           u    ON wo.SubmittedById = u.Id
            JOIN AdvanceRequests adv  ON wo.AdvanceRequestId = adv.Id
            JOIN Projects        proj ON adv.ProjectId    = proj.Id
            {where}
            ORDER BY wo.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryWriteOff, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetWriteOffExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE wo.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "wo", "CreatedAt", installmentsTable: null, installmentsFk: null));

        var sql = $"""
            SELECT wo.Id          AS ParentId,
                   wo.RequestNo,
                   u.Name         AS EmployeeName,
                   'writeoff'     AS Type,
                   proj.Code      AS ProjectCode,
                   proj.Name      AS ProjectName,
                   wo.ApprovalStatus,
                   wo.CreatedAt,
                   adv.RefundedAt AS PaidAt,
                   wo.GrandTotal  AS PaymentTotalAmount,
                   item.InvoiceNo AS ItemCol1,
                   item.ItemName  AS ItemName,
                   NULL           AS ItemCol3Text,
                   item.InvoiceDate AS ItemCol3Date,
                   item.TotalPrice  AS ItemAmount
            FROM WriteOffRecords wo
            JOIN Users           u    ON wo.SubmittedById = u.Id
            JOIN AdvanceRequests adv  ON wo.AdvanceRequestId = adv.Id
            JOIN Projects        proj ON adv.ProjectId    = proj.Id
            LEFT JOIN WriteOffItems item ON item.WriteOffRecordId = wo.Id
            {where}
            ORDER BY wo.CreatedAt DESC, wo.Id DESC, item.SortOrder ASC, item.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // 4) TravelPaymentRequest 出差請款
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetTravelPaymentPagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE tpr.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "tpr", "CreatedAt", "TravelPaymentRequestInstallments", "TravelPaymentRequestId"));

        var paidAt = PaidAtCase("tpr", "TravelPaymentRequestInstallments", "TravelPaymentRequestId");

        var countSql = $"""
            SELECT COUNT(*) FROM TravelPaymentRequests tpr
            JOIN Users        u    ON tpr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tpr.ProjectId = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT tpr.Id,
                   tpr.RequestNo,
                   u.Name         AS EmployeeName,
                   'travel-payment' AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   (SELECT STRING_AGG(InvoiceNo, ',') FROM TravelPaymentRequestItems WHERE TravelPaymentRequestId = tpr.Id AND InvoiceNo IS NOT NULL) AS InvoiceNos,
                   tpr.GrandTotal AS TotalAmount,
                   tpr.ApprovalStatus,
                   {paidAt}       AS PaidAt,
                   tpr.CreatedAt
            FROM TravelPaymentRequests tpr
            JOIN Users         u    ON tpr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tpr.ProjectId  = proj.Id
            {where}
            ORDER BY tpr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryTravelPayment, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetTravelPaymentExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE tpr.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "tpr", "CreatedAt", "TravelPaymentRequestInstallments", "TravelPaymentRequestId"));

        var paidAt = PaidAtCase("tpr", "TravelPaymentRequestInstallments", "TravelPaymentRequestId");

        var sql = $"""
            SELECT tpr.Id          AS ParentId,
                   tpr.RequestNo,
                   u.Name           AS EmployeeName,
                   'travel-payment' AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   tpr.ApprovalStatus,
                   tpr.CreatedAt,
                   {paidAt}         AS PaidAt,
                   tpr.GrandTotal   AS PaymentTotalAmount,
                   item.InvoiceNo   AS ItemCol1,
                   item.ItemName    AS ItemName,
                   NULL             AS ItemCol3Text,
                   item.InvoiceDate AS ItemCol3Date,
                   item.TotalPrice  AS ItemAmount
            FROM TravelPaymentRequests tpr
            JOIN Users         u    ON tpr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tpr.ProjectId  = proj.Id
            LEFT JOIN TravelPaymentRequestItems item ON item.TravelPaymentRequestId = tpr.Id
            {where}
            ORDER BY tpr.CreatedAt DESC, tpr.Id DESC, item.SortOrder ASC, item.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // 5) TravelRequest 出差預支（IsHolidayTravel=0；排除假日執行活動）
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetTravelPagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE tr.ApprovalStatus != 'draft' AND tr.IsHolidayTravel = 0");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "tr", "CreatedAt", "TravelRequestInstallments", "TravelRequestId"));

        var paidAt = PaidAtCase("tr", "TravelRequestInstallments", "TravelRequestId");

        var countSql = $"""
            SELECT COUNT(*) FROM TravelRequests tr
            JOIN Users         u    ON tr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tr.ProjectId  = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT tr.Id,
                   tr.RequestNo,
                   u.Name         AS EmployeeName,
                   'travel'       AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   (SELECT STRING_AGG(InvoiceNo, ',') FROM TravelRequestItems WHERE TravelRequestId = tr.Id AND InvoiceNo IS NOT NULL) AS InvoiceNos,
                   tr.GrandTotal  AS TotalAmount,
                   tr.ApprovalStatus,
                   {paidAt}       AS PaidAt,
                   tr.CreatedAt
            FROM TravelRequests tr
            JOIN Users         u    ON tr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tr.ProjectId  = proj.Id
            {where}
            ORDER BY tr.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryTravel, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetTravelExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE tr.ApprovalStatus != 'draft' AND tr.IsHolidayTravel = 0");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "tr", "CreatedAt", "TravelRequestInstallments", "TravelRequestId"));

        var paidAt = PaidAtCase("tr", "TravelRequestInstallments", "TravelRequestId");

        var sql = $"""
            SELECT tr.Id          AS ParentId,
                   tr.RequestNo,
                   u.Name           AS EmployeeName,
                   'travel'         AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   tr.ApprovalStatus,
                   tr.CreatedAt,
                   {paidAt}         AS PaidAt,
                   tr.GrandTotal    AS PaymentTotalAmount,
                   item.InvoiceNo   AS ItemCol1,
                   item.ItemName    AS ItemName,
                   NULL             AS ItemCol3Text,
                   item.InvoiceDate AS ItemCol3Date,
                   item.TotalPrice  AS ItemAmount
            FROM TravelRequests tr
            JOIN Users         u    ON tr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tr.ProjectId  = proj.Id
            LEFT JOIN TravelRequestItems item ON item.TravelRequestId = tr.Id
            {where}
            ORDER BY tr.CreatedAt DESC, tr.Id DESC, item.SortOrder ASC, item.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // 6) TravelWriteOffRecord 出差預支沖銷（無 installments；PaidAt 取自父 TravelRequest.RefundedAt）
    // ========================================================================
    private async Task<PagedResult<PaymentReportDto>> GetTravelWriteOffPagedAsync(
        ProjectAccessScope scope, int page, int pageSize,
        DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        var where = new StringBuilder(" WHERE two.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "two", "CreatedAt", installmentsTable: null, installmentsFk: null));

        var countSql = $"""
            SELECT COUNT(*) FROM TravelWriteOffRecords two
            JOIN Users          u    ON two.SubmittedById = u.Id
            JOIN TravelRequests tr   ON two.TravelRequestId = tr.Id
            LEFT JOIN Projects  proj ON tr.ProjectId  = proj.Id
            {where}
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);

        var sql = $"""
            SELECT two.Id,
                   two.RequestNo,
                   u.Name         AS EmployeeName,
                   'travel-writeoff' AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   (SELECT STRING_AGG(InvoiceNo, ',') FROM TravelWriteOffItems WHERE TravelWriteOffRecordId = two.Id AND InvoiceNo IS NOT NULL) AS InvoiceNos,
                   two.GrandTotal AS TotalAmount,
                   two.ApprovalStatus,
                   tr.RefundedAt  AS PaidAt,
                   two.CreatedAt
            FROM TravelWriteOffRecords two
            JOIN Users          u    ON two.SubmittedById = u.Id
            JOIN TravelRequests tr   ON two.TravelRequestId = tr.Id
            LEFT JOIN Projects  proj ON tr.ProjectId  = proj.Id
            {where}
            ORDER BY two.CreatedAt DESC OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            """;

        var rows = (await db.QueryAsync<dynamic>(sql, parameters)).ToList();
        var dtos = rows.Select(MapPaymentRow).ToList();
        return await AttachItemsAsync(CategoryTravelWriteOff, dtos, total, page, pageSize);
    }

    private async Task<List<PaymentExportRowDto>> GetTravelWriteOffExportAsync(
        ProjectAccessScope scope, DateOnly? dateFrom, DateOnly? dateTo, string? paymentStatus)
    {
        var parameters = new DynamicParameters();
        var where = new StringBuilder(" WHERE two.ApprovalStatus != 'draft'");
        where.Append(BuildDeptScopeFilter(scope, parameters));
        where.Append(BuildDateAndPaymentStatus(dateFrom, dateTo, paymentStatus, parameters,
            "two", "CreatedAt", installmentsTable: null, installmentsFk: null));

        var sql = $"""
            SELECT two.Id         AS ParentId,
                   two.RequestNo,
                   u.Name           AS EmployeeName,
                   'travel-writeoff' AS Type,
                   ISNULL(proj.Code, '') AS ProjectCode,
                   ISNULL(proj.Name, '') AS ProjectName,
                   two.ApprovalStatus,
                   two.CreatedAt,
                   tr.RefundedAt    AS PaidAt,
                   two.GrandTotal   AS PaymentTotalAmount,
                   item.InvoiceNo   AS ItemCol1,
                   item.ItemName    AS ItemName,
                   NULL             AS ItemCol3Text,
                   item.InvoiceDate AS ItemCol3Date,
                   item.TotalPrice  AS ItemAmount
            FROM TravelWriteOffRecords two
            JOIN Users          u    ON two.SubmittedById = u.Id
            JOIN TravelRequests tr   ON two.TravelRequestId = tr.Id
            LEFT JOIN Projects  proj ON tr.ProjectId  = proj.Id
            LEFT JOIN TravelWriteOffItems item ON item.TravelWriteOffRecordId = two.Id
            {where}
            ORDER BY two.CreatedAt DESC, two.Id DESC, item.SortOrder ASC, item.Id ASC
            """;

        return (await db.QueryAsync<PaymentExportRowDto>(sql, parameters)).AsList();
    }

    // ========================================================================
    // Row mapper：dynamic → PaymentReportDto（Items 預設空，後續再 attach）
    // ========================================================================
    private static PaymentReportDto MapPaymentRow(dynamic row)
    {
        string[] invoiceNos = string.IsNullOrEmpty((string?)row.InvoiceNos)
            ? []
            : ((string)row.InvoiceNos)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new PaymentReportDto(
            Id:             (int)row.Id,
            RequestNo:      (string)row.RequestNo,
            EmployeeName:   (string?)row.EmployeeName ?? "—",
            Type:           (string)row.Type,
            ProjectCode:    (string?)row.ProjectCode ?? "—",
            ProjectName:    (string?)row.ProjectName ?? "—",
            InvoiceNos:     invoiceNos,
            TotalAmount:    (decimal)row.TotalAmount,
            ApprovalStatus: (string)row.ApprovalStatus,
            PaidAt:         (DateTime?)row.PaidAt,
            CreatedAt:      (DateTime)row.CreatedAt,
            Items:          []);
    }

    // ========================================================================
    // 子表明細擷取：依 category 一次撈該頁 parentIds 的所有明細
    // ========================================================================
    private async Task<Dictionary<int, List<PaymentReportItemDto>>> FetchItemsByCategoryAsync(
        string category, IReadOnlyCollection<int> parentIds)
    {
        if (parentIds.Count == 0) return new();

        // 6 種類別 → 對應 子表 / FK 欄位 / 4 欄語意。
        var sql = category switch
        {
            CategoryPayment => """
                SELECT PaymentRequestId AS ParentId,
                       InvoiceNo        AS Col1,
                       ItemName,
                       CAST(NULL AS NVARCHAR(50)) AS Col3Text,
                       InvoiceDate      AS Col3Date,
                       Amount
                FROM InvoiceItems
                WHERE PaymentRequestId IN @Ids
                ORDER BY PaymentRequestId, Id
                """,
            CategoryAdvance => """
                SELECT AdvanceRequestId AS ParentId,
                       Category         AS Col1,
                       ItemName,
                       Quantity         AS Col3Text,
                       CAST(NULL AS DATETIME) AS Col3Date,
                       TotalPrice       AS Amount
                FROM AdvanceRequestItems
                WHERE AdvanceRequestId IN @Ids
                ORDER BY AdvanceRequestId, SortOrder, Id
                """,
            CategoryWriteOff => """
                SELECT WriteOffRecordId AS ParentId,
                       InvoiceNo        AS Col1,
                       ItemName,
                       CAST(NULL AS NVARCHAR(50)) AS Col3Text,
                       InvoiceDate      AS Col3Date,
                       TotalPrice       AS Amount
                FROM WriteOffItems
                WHERE WriteOffRecordId IN @Ids
                ORDER BY WriteOffRecordId, SortOrder, Id
                """,
            CategoryTravelPayment => """
                SELECT TravelPaymentRequestId AS ParentId,
                       InvoiceNo              AS Col1,
                       ItemName,
                       CAST(NULL AS NVARCHAR(50)) AS Col3Text,
                       InvoiceDate            AS Col3Date,
                       TotalPrice             AS Amount
                FROM TravelPaymentRequestItems
                WHERE TravelPaymentRequestId IN @Ids
                ORDER BY TravelPaymentRequestId, SortOrder, Id
                """,
            CategoryTravel => """
                SELECT TravelRequestId AS ParentId,
                       InvoiceNo       AS Col1,
                       ItemName,
                       CAST(NULL AS NVARCHAR(50)) AS Col3Text,
                       InvoiceDate     AS Col3Date,
                       TotalPrice      AS Amount
                FROM TravelRequestItems
                WHERE TravelRequestId IN @Ids
                ORDER BY TravelRequestId, SortOrder, Id
                """,
            CategoryTravelWriteOff => """
                SELECT TravelWriteOffRecordId AS ParentId,
                       InvoiceNo              AS Col1,
                       ItemName,
                       CAST(NULL AS NVARCHAR(50)) AS Col3Text,
                       InvoiceDate            AS Col3Date,
                       TotalPrice             AS Amount
                FROM TravelWriteOffItems
                WHERE TravelWriteOffRecordId IN @Ids
                ORDER BY TravelWriteOffRecordId, SortOrder, Id
                """,
            _ => throw new AppException("不支援的類別", 400),
        };

        var rows = await db.QueryAsync<dynamic>(sql, new { Ids = parentIds });
        return rows
            .GroupBy(r => (int)r.ParentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new PaymentReportItemDto(
                    Col1:     (string?)r.Col1,
                    ItemName: (string?)r.ItemName,
                    Col3Text: (string?)r.Col3Text,
                    Col3Date: (DateTime?)r.Col3Date,
                    Amount:   (decimal?)r.Amount)).ToList());
    }

    /// <summary>
    /// 將 items 依 parentId 掛回 dtos。
    /// </summary>
    private async Task<PagedResult<PaymentReportDto>> AttachItemsAsync(
        string category, List<PaymentReportDto> dtos, int total, int page, int pageSize)
    {
        var ids = dtos.Select(d => d.Id).ToList();
        var byParent = await FetchItemsByCategoryAsync(category, ids);
        var withItems = dtos
            .Select(d => d with { Items = byParent.GetValueOrDefault(d.Id, []) })
            .ToList();
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<PaymentReportDto>(withItems, total, page, pageSize, Math.Max(1, totalPages));
    }
}
