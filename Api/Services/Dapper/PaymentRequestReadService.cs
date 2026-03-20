using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PaymentRequestReadService(IDbConnection db) : IPaymentRequestReadService
{
    // ── 通用 JOIN SQL ─────────────────────────────────────────────────────────
    private const string BaseSql = """
        SELECT pr.Id, pr.Type, pr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               pr.TotalAmount, pr.ApprovalStatus, pr.EstimatedPaymentDate, pr.PaidAt,
               sub.Name AS SubmittedBy, pr.CreatedAt,
               pr.ReviewedAt, pr.ReviewNote,
               ii.Id AS InvId, ii.FileName, ii.InvoiceNo, ii.Amount AS InvAmount, ii.ItemName AS InvItemName, ii.Note AS InvNote, ii.FileUrl AS InvFileUrl
        FROM PaymentRequests pr
        LEFT JOIN Projects proj   ON pr.ProjectId    = proj.Id
        LEFT JOIN Users   sub     ON pr.SubmittedById = sub.Id
        LEFT JOIN InvoiceItems ii ON ii.PaymentRequestId = pr.Id
        """;

    // ── PaymentRequest ────────────────────────────────────────────────────────

    public async Task<IEnumerable<PaymentRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY pr.CreatedAt DESC, ii.Id";
        var rows = await db.QueryAsync<dynamic>(sql);
        return GroupToPaymentRequests(rows);
    }

    public async Task<PagedResult<PaymentRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM PaymentRequests {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM PaymentRequests
                {userFilter}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE pr.Id IN (SELECT Id FROM PagedIds) ORDER BY pr.CreatedAt DESC, ii.Id
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<PaymentRequestDto>(GroupToPaymentRequests(rows), total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<PaymentRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE pr.Id = @Id ORDER BY ii.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var dto = GroupToPaymentRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'payment_request' AND rdr.RequestId = @RequestId
            ORDER BY rdr.StepOrder
            """;
        var drRows = await db.QueryAsync<dynamic>(drSql, new { RequestId = id });
        var designatedReviewers = drRows.Select(r => new DesignatedReviewerDto(
            (int)r.Id,
            (Guid)r.ReviewerId,
            (string)r.ReviewerName,
            (int)r.StepOrder,
            (string)r.Status,
            (DateTime?)r.ReviewedAt,
            (string?)r.Comment)).ToArray();

        return dto with { DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null };
    }

    // ── ApprovalTask（彙總 PaymentRequest + LeaveRequest + TravelRequest + OvertimeRequest）──

    public async Task<IEnumerable<ApprovalTaskDto>> GetApprovalTasksAsync(
        int? reviewerJobTitleId = null, int? reviewerDepartmentId = null,
        string? status = null, Guid? reviewerUserId = null)
    {
        var (payments, leaves, travels, overtimes, advances, writeOffs, flows, records, designatedRows) =
            await FetchAllAsync(reviewerJobTitleId: reviewerJobTitleId, reviewerDepartmentId: reviewerDepartmentId,
                                statusFilter: status, reviewerUserId: reviewerUserId);
        return BuildApprovalTasks(payments, leaves, travels, overtimes, advances, writeOffs, flows, records, designatedRows);
    }

    public async Task<ApprovalTaskDto?> GetApprovalTaskByIdAsync(int id, string applicationType)
    {
        var (payments, leaves, travels, overtimes, advances, writeOffs, flows, records, designatedRows) = await FetchAllAsync(id, applicationType);
        return BuildApprovalTasks(payments, leaves, travels, overtimes, advances, writeOffs, flows, records, designatedRows)
            .FirstOrDefault(t => t.Id == id && t.ApplicationType == applicationType);
    }

    // Backward-compat overload (scans all types)
    public async Task<ApprovalTaskDto?> GetApprovalTaskByIdAsync(int id)
        => (await GetApprovalTasksAsync()).FirstOrDefault(t => t.Id == id);


    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(
        IEnumerable<dynamic> payments,
        IEnumerable<dynamic> leaves,
        IEnumerable<dynamic> travels,
        IEnumerable<dynamic> overtimes,
        IEnumerable<dynamic> advances,
        IEnumerable<dynamic> writeOffs,
        IEnumerable<dynamic> flows,
        IEnumerable<dynamic> records,
        IEnumerable<dynamic> designatedRows)> FetchAllAsync(
        int? filterId = null, string? filterType = null,
        int? reviewerJobTitleId = null, int? reviewerDepartmentId = null,
        string? statusFilter = null, Guid? reviewerUserId = null)
    {
        // ── WHERE clause for specific ID lookup ──────────────────────────────
        string paymentIdWhere  = (filterId.HasValue && filterType == "payment_request") ? "pr.Id = @Id" : "";
        string leaveIdWhere    = (filterId.HasValue && filterType == "leave")            ? "lr.Id = @Id" : "";
        string travelIdWhere   = (filterId.HasValue && filterType == "travel")           ? "tr.Id = @Id" : "";
        string overtimeIdWhere = (filterId.HasValue && filterType == "overtime")         ? "ot.Id = @Id" : "";
        string advanceIdWhere  = (filterId.HasValue && filterType == "advance")          ? "adv.Id = @Id" : "";
        string writeOffIdWhere = (filterId.HasValue && filterType == "write_off")        ? "wo.Id = @Id" : "";

        // ── Step-match filter for listing (reviewer's job title) ─────────────
        // Three modes:
        //   applyReviewerFilter   → normal user: match pending tasks by job title/dept
        //   superAdminStatusFilter → superadmin with explicit status param: filter by status only
        //   superAdminDefault      → superadmin without status param: exclude draft
        bool applyReviewerFilter    = !filterId.HasValue && reviewerJobTitleId.HasValue;
        bool superAdminStatusFilter = !filterId.HasValue && !reviewerJobTitleId.HasValue && !string.IsNullOrEmpty(statusFilter);
        bool superAdminDefault      = !filterId.HasValue && !reviewerJobTitleId.HasValue && string.IsNullOrEmpty(statusFilter);

        // userAlias: 各查詢中 JOIN 申請人 Users 表的 alias（payment 用 sub，其他用 u）
        // appType:   用於 "approved" 模式查詢 ApprovalRecords 時區分申請類型（字串常數，非 SQL 參數，已知安全值）
        string StepMatchClause(string alias, string userAlias, string appType)
        {
            // Superadmin without status param: show all except draft
            if (superAdminDefault) return $"{alias}.ApprovalStatus <> 'draft'";

            // Superadmin with status filter: simple status equality
            if (superAdminStatusFilter)
                return $"{alias}.ApprovalStatus = @StatusFilter";

            // Normal reviewer — "approved" tab: show tasks the user has already reviewed
            // 請款申請額外允許財務部成員查看所有已核准的請款（因需設定撥款日期）
            if (statusFilter == "approved")
                return $"""
                  {alias}.ApprovalStatus = 'approved'
                  AND (
                    EXISTS (
                      SELECT 1 FROM ApprovalRecords ar2
                      WHERE ar2.ApplicationType = '{appType}'
                        AND ar2.ApplicationId = {alias}.Id
                        AND ar2.ReviewedById = @ReviewerUserId
                    )
                    {(appType == "payment_request" ? $"""
                    OR EXISTS (
                      SELECT 1 FROM Departments d
                      WHERE d.Id = @ReviewerDepartmentId
                        AND d.Code = N'FIN'
                    )
                    """ : "")}
                  )
                  """;

            // Normal reviewer — "pending" tab (or no status param): match current step
            // Only show 'pending' status to avoid surfacing returned/rejected rows here.
            // Logic must stay in sync with AuthorizeStepAsync.
            return $"""
              {alias}.ApprovalStatus = 'pending'
              AND (
                {alias}.ApprovalItemId IS NULL
                OR EXISTS (
                  SELECT 1 FROM ApprovalItems ai2
                  JOIN ApprovalSteps s2 ON s2.ApprovalItemId = ai2.Id
                                       AND s2.StepOrder = {alias}.CurrentStepOrder
                  WHERE ai2.Id = {alias}.ApprovalItemId
                    AND s2.UseDirectSupervisor = 0
                    AND s2.UseApplicantDesignated = 0
                    AND (s2.JobTitleId IS NULL OR s2.JobTitleId = @ReviewerJobTitleId)
                    AND (
                      (s2.UseApplicantDepartment = 1
                        AND {userAlias}.DepartmentId IS NOT NULL
                        AND {userAlias}.DepartmentId = @ReviewerDepartmentId)
                      OR
                      (s2.UseApplicantDepartment = 0
                        AND (s2.DepartmentId IS NULL OR s2.DepartmentId = @ReviewerDepartmentId))
                    )
                )
                OR EXISTS (
                  SELECT 1 FROM ApprovalItems ai3
                  JOIN ApprovalSteps s3 ON s3.ApprovalItemId = ai3.Id
                                       AND s3.StepOrder = {alias}.CurrentStepOrder
                  WHERE ai3.Id = {alias}.ApprovalItemId
                    AND s3.UseDirectSupervisor = 1
                    AND {userAlias}.DepartmentId IS NOT NULL
                    AND {userAlias}.DepartmentId = @ReviewerDepartmentId
                    AND EXISTS (
                      SELECT 1 FROM JobTitles appJt
                      WHERE appJt.Id = {userAlias}.JobTitleId
                        AND (SELECT Level FROM JobTitles WHERE Id = @ReviewerJobTitleId) = (
                          SELECT Level FROM (
                            SELECT DISTINCT jt2.Level,
                                   ROW_NUMBER() OVER (ORDER BY jt2.Level DESC) AS rn
                            FROM Users u2
                            JOIN JobTitles jt2 ON jt2.Id = u2.JobTitleId
                            WHERE u2.DepartmentId = {userAlias}.DepartmentId
                              AND jt2.Level < appJt.Level
                              AND u2.IsSuperAdmin = 0
                          ) ranked
                          WHERE ranked.rn = (
                            SELECT COUNT(*) + 1 FROM ApprovalSteps prev
                            WHERE prev.ApprovalItemId = s3.ApprovalItemId
                              AND prev.UseDirectSupervisor = 1
                              AND prev.StepOrder < s3.StepOrder
                          )
                        )
                    )
                )
                OR EXISTS (
                  SELECT 1 FROM EscalationOverrides eo
                  WHERE eo.ApplicationType = '{appType}'
                    AND eo.ApplicationId = {alias}.Id
                    AND eo.StepOrder = {alias}.CurrentStepOrder
                    AND eo.ReviewerId = @ReviewerUserId
                )
                OR EXISTS (
                  SELECT 1 FROM ApprovalItems ai4
                  JOIN ApprovalSteps s4 ON s4.ApprovalItemId = ai4.Id
                                       AND s4.StepOrder = {alias}.CurrentStepOrder
                  WHERE ai4.Id = {alias}.ApprovalItemId
                    AND s4.UseApplicantDesignated = 1
                    AND EXISTS (
                      SELECT 1 FROM RequestDesignatedReviewers rdr
                      WHERE rdr.RequestType = '{appType}'
                        AND rdr.RequestId = {alias}.Id
                        AND rdr.ReviewerId = @ReviewerUserId
                        AND rdr.Status = 'pending'
                        AND rdr.StepOrder = (
                          SELECT MIN(rdr2.StepOrder)
                          FROM RequestDesignatedReviewers rdr2
                          WHERE rdr2.RequestType = '{appType}'
                            AND rdr2.RequestId = {alias}.Id
                            AND rdr2.Status = 'pending'
                        )
                    )
                )
              )
              """;
        }

        string BuildWhere(string idClause, string stepClause)
        {
            var parts = new[] { idClause, stepClause }.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            return parts.Length > 0 ? " WHERE " + string.Join(" AND ", parts) : "";
        }

        // 按 ID 查詢時不套用審核者過濾（StepMatchClause），只用 ID 條件
        string paymentWhere  = filterId.HasValue ? BuildWhere(paymentIdWhere,  "") : BuildWhere("", StepMatchClause("pr", "sub", "payment_request"));
        string leaveWhere    = filterId.HasValue ? BuildWhere(leaveIdWhere,    "") : BuildWhere("", StepMatchClause("lr", "u",   "leave"));
        string travelWhere   = filterId.HasValue ? BuildWhere(travelIdWhere,   "") : BuildWhere("", StepMatchClause("tr", "u",   "travel"));
        string overtimeWhere = filterId.HasValue ? BuildWhere(overtimeIdWhere, "") : BuildWhere("", StepMatchClause("ot", "u",   "overtime"));
        string advanceWhere  = filterId.HasValue ? BuildWhere(advanceIdWhere,  "") : BuildWhere("", StepMatchClause("adv", "asub", "advance"));
        string writeOffWhere = filterId.HasValue ? BuildWhere(writeOffIdWhere, "") : BuildWhere("", StepMatchClause("wo", "wsub", "write_off"));

        var paymentSql = $"""
            SELECT pr.Id, pr.Type AS PaymentType, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   pr.TotalAmount, pr.ApprovalStatus, pr.EstimatedPaymentDate, pr.PaidAt, pr.ApprovalItemId, pr.CurrentStepOrder,
                   sub.Name AS SubmittedBy, sub.SignatureUrl AS SubmittedBySignatureUrl, pr.CreatedAt, pr.ReviewedAt, pr.ReviewNote,
                   ii.Id AS InvId, ii.FileName, ii.InvoiceNo, ii.Amount AS InvAmount, ii.ItemName AS InvItemName, ii.Note AS InvNote, ii.FileUrl AS InvFileUrl
            FROM PaymentRequests pr
            LEFT JOIN Projects proj   ON pr.ProjectId    = proj.Id
            LEFT JOIN Users   sub     ON pr.SubmittedById = sub.Id
            LEFT JOIN InvoiceItems ii ON ii.PaymentRequestId = pr.Id
            {paymentWhere}
            ORDER BY pr.CreatedAt DESC, ii.Id
            """;

        var leaveSql = $"""
            SELECT lr.Id, lr.LeaveType, lr.StartDate, lr.EndDate, lr.Hours, lr.Reason,
                   lr.ApprovalStatus, lr.ApprovalItemId, lr.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, lr.CreatedAt, lr.ReviewedAt, lr.ReviewNote
            FROM LeaveRequests lr
            LEFT JOIN Users u ON lr.EmployeeId = u.Id
            {leaveWhere}
            ORDER BY lr.CreatedAt DESC
            """;

        var travelSql = $"""
            SELECT tr.Id, tr.Destination, tr.StartDate, tr.EndDate,
                   tr.EstimatedCost, tr.Purpose, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   tr.IsHolidayTravel,
                   tr.ApprovalStatus, tr.ApprovalItemId, tr.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote
            FROM TravelRequests tr
            LEFT JOIN Users u       ON tr.EmployeeId = u.Id
            LEFT JOIN Projects proj ON tr.ProjectId  = proj.Id
            {travelWhere}
            ORDER BY tr.CreatedAt DESC
            """;

        var overtimeSql = $"""
            SELECT ot.Id, ot.OvertimeDate, ot.EstimatedHours, ot.Reason,
                   ot.ProjectIds,
                   ot.ApprovalStatus, ot.ApprovalItemId, ot.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, ot.CreatedAt, ot.ReviewedAt, ot.ReviewNote
            FROM OvertimeRequests ot
            LEFT JOIN Users u ON ot.EmployeeId = u.Id
            {overtimeWhere}
            ORDER BY ot.CreatedAt DESC
            """;

        var advanceSql = $"""
            SELECT adv.Id, adv.RequestNo, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   adv.ActivityName, adv.GrandTotal,
                   adv.ApprovalStatus, adv.ApprovalItemId, adv.CurrentStepOrder,
                   adv.EstimatedPaymentDate, adv.PaidAt,
                   asub.Name AS SubmittedBy, asub.SignatureUrl AS SubmittedBySignatureUrl, adv.CreatedAt, adv.ReviewedAt, adv.ReviewNote
            FROM AdvanceRequests adv
            LEFT JOIN Projects proj ON adv.ProjectId    = proj.Id
            LEFT JOIN Users   asub  ON adv.SubmittedById = asub.Id
            {advanceWhere}
            ORDER BY adv.CreatedAt DESC
            """;

        var writeOffSql = $"""
            SELECT wo.Id, wo.RequestNo, wo.AdvanceRequestId, arx.RequestNo AS AdvanceRequestNo,
                   proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   wo.GrandTotal,
                   wo.ApprovalStatus, wo.ApprovalItemId, wo.CurrentStepOrder,
                   wsub.Name AS SubmittedBy, wsub.SignatureUrl AS SubmittedBySignatureUrl, wo.CreatedAt, wo.ReviewedAt, wo.ReviewNote,
                   wo.SubmittedById
            FROM WriteOffRecords wo
            JOIN AdvanceRequests arx ON wo.AdvanceRequestId = arx.Id
            LEFT JOIN Projects proj  ON arx.ProjectId       = proj.Id
            LEFT JOIN Users   wsub   ON wo.SubmittedById    = wsub.Id
            {writeOffWhere}
            ORDER BY wo.CreatedAt DESC
            """;

        const string flowSql = """
            SELECT ai.Id AS FlowId, ai.Name AS FlowName, ai.ApplicationType,
                   s.StepOrder, d.Name AS DepartmentName, d.Code AS DepartmentCode, j.Name AS JobTitleName,
                   s.UseDirectSupervisor, s.UseApplicantDesignated, s.Note
            FROM ApprovalItems ai
            LEFT JOIN ApprovalSteps s ON s.ApprovalItemId = ai.Id
            LEFT JOIN Departments d   ON s.DepartmentId = d.Id
            LEFT JOIN JobTitles j     ON s.JobTitleId   = j.Id
            ORDER BY ai.Id, s.StepOrder
            """;

        const string recordSql = """
            SELECT ar.ApplicationType, ar.ApplicationId, ar.StepOrder, ar.Action,
                   u.Name AS ReviewedBy, ar.ReviewedAt, ar.ReviewNote,
                   obu.Name AS OnBehalfOf, ar.IsEscalated,
                   u.SignatureUrl AS ReviewerSignatureUrl
            FROM ApprovalRecords ar
            LEFT JOIN Users u   ON ar.ReviewedById     = u.Id
            LEFT JOIN Users obu ON ar.OnBehalfOfUserId  = obu.Id
            ORDER BY ar.ApplicationType, ar.ApplicationId, ar.StepOrder
            """;

        var param = new
        {
            Id = filterId ?? 0,
            ReviewerJobTitleId   = reviewerJobTitleId,
            ReviewerDepartmentId = reviewerDepartmentId,
            ReviewerUserId       = reviewerUserId,
            StatusFilter         = statusFilter,
        };

        const string drSql = """
            SELECT rdr.RequestType, rdr.RequestId, rdr.Id, rdr.ReviewerId,
                   u.Name AS ReviewerName, rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            ORDER BY rdr.RequestType, rdr.RequestId, rdr.StepOrder
            """;

        var payments       = await db.QueryAsync<dynamic>(paymentSql,  param);
        var leaves         = await db.QueryAsync<dynamic>(leaveSql,    param);
        var travels        = await db.QueryAsync<dynamic>(travelSql,   param);
        var overtimes      = await db.QueryAsync<dynamic>(overtimeSql, param);
        var advances       = await db.QueryAsync<dynamic>(advanceSql,  param);
        var writeOffs      = await db.QueryAsync<dynamic>(writeOffSql, param);
        var flows          = await db.QueryAsync<dynamic>(flowSql);
        var records        = await db.QueryAsync<dynamic>(recordSql);
        var designatedRows = await db.QueryAsync<dynamic>(drSql);

        return (payments, leaves, travels, overtimes, advances, writeOffs, flows, records, designatedRows);
    }

    private static IEnumerable<ApprovalTaskDto> BuildApprovalTasks(
        IEnumerable<dynamic> paymentRows,
        IEnumerable<dynamic> leaveRows,
        IEnumerable<dynamic> travelRows,
        IEnumerable<dynamic> overtimeRows,
        IEnumerable<dynamic> advanceRows,
        IEnumerable<dynamic> writeOffRows,
        IEnumerable<dynamic> flowRows,
        IEnumerable<dynamic> recordRows,
        IEnumerable<dynamic> designatedRows)
    {
        // Build designated reviewer lookup keyed by (RequestType, RequestId)
        var drDict = new Dictionary<(string, int), List<DesignatedReviewerDto>>();
        foreach (var row in designatedRows)
        {
            var key = ((string)row.RequestType, (int)row.RequestId);
            if (!drDict.ContainsKey(key))
                drDict[key] = [];
            drDict[key].Add(new DesignatedReviewerDto(
                (int)row.Id,
                (Guid)row.ReviewerId,
                (string)row.ReviewerName,
                (int)row.StepOrder,
                (string)row.Status,
                (DateTime?)row.ReviewedAt,
                (string?)row.Comment));
        }

        DesignatedReviewerDto[]? GetDesignatedReviewers(string appType, int id) =>
            drDict.TryGetValue((appType, id), out var drs) && drs.Count > 0 ? [.. drs] : null;

        // Build flow lookup keyed by ApplicationType
        var flowDict = new Dictionary<string, (int Id, string Name, List<ApprovalFlowStepDto> Steps)>();
        foreach (var row in flowRows)
        {
            string? appType = (string?)row.ApplicationType;
            if (string.IsNullOrEmpty(appType)) continue;
            if (!flowDict.ContainsKey(appType))
                flowDict[appType] = ((int)row.FlowId, (string)row.FlowName, []);
            if (row.StepOrder is not null)
                flowDict[appType].Steps.Add(new ApprovalFlowStepDto(
                    (int)row.StepOrder,
                    (string?)row.DepartmentName,
                    (string?)row.DepartmentCode,
                    (string?)row.JobTitleName,
                    (bool)(row.UseDirectSupervisor ?? false),
                    (bool)(row.UseApplicantDesignated ?? false),
                    (string?)row.Note));
        }

        ApprovalFlowDto? GetFlow(string appType) =>
            flowDict.TryGetValue(appType, out var f)
                ? new ApprovalFlowDto(f.Id, f.Name, [.. f.Steps])
                : null;

        // Build approval record lookup keyed by (ApplicationType, ApplicationId)
        var recordDict = new Dictionary<(string, int), List<ApprovalRecordDto>>();
        foreach (var row in recordRows)
        {
            var key = ((string)row.ApplicationType, (int)row.ApplicationId);
            if (!recordDict.ContainsKey(key))
                recordDict[key] = [];
            recordDict[key].Add(new ApprovalRecordDto(
                (int)row.StepOrder,
                (string)row.Action,
                (string?)row.ReviewedBy ?? "—",
                (DateTime)row.ReviewedAt,
                (string?)row.ReviewNote,
                (string?)row.OnBehalfOf,
                (bool)(row.IsEscalated ?? false),
                (string?)row.ReviewerSignatureUrl));
        }

        ApprovalRecordDto[] GetRecords(string appType, int id) =>
            recordDict.TryGetValue((appType, id), out var recs) ? [.. recs] : [];

        // Payment requests (one-to-many with InvoiceItems)
        var paymentGrouped = new Dictionary<int, (dynamic pr, List<InvoiceItemDto> invoices)>();
        foreach (var row in paymentRows)
        {
            int id = (int)row.Id;
            if (!paymentGrouped.ContainsKey(id))
                paymentGrouped[id] = (row, []);
            if (row.InvId is not null)
                paymentGrouped[id].invoices.Add(new InvoiceItemDto(
                    (int)row.InvId, (string)row.FileName,
                    (string)row.InvoiceNo, (decimal)row.InvAmount,
                    (string?)row.InvItemName, (string?)row.InvNote, (string?)row.InvFileUrl));
        }
        var paymentTasks = paymentGrouped.Values.Select(x => new ApprovalTaskDto(
            (int)x.pr.Id,
            "payment_request",
            $"請款申請 #{x.pr.Id}（{x.pr.ProjectCode}）",
            (string?)x.pr.SubmittedBy ?? "—",
            (DateTime)x.pr.CreatedAt,
            (string)x.pr.ApprovalStatus,
            (int)x.pr.CurrentStepOrder,
            (DateTime?)x.pr.ReviewedAt,
            (string?)x.pr.ReviewNote,
            GetFlow("payment_request"),
            new PaymentTaskDetailDto(
                (int)x.pr.Id,
                (string)x.pr.PaymentType,
                (string)x.pr.ProjectCode,
                (string)x.pr.ProjectName,
                [.. x.invoices],
                (decimal)x.pr.TotalAmount,
                (DateTime?)x.pr.EstimatedPaymentDate,
                (DateTime?)x.pr.PaidAt),
            null, null, null, null, null,
            GetRecords("payment_request", (int)x.pr.Id),
            GetDesignatedReviewers("payment_request", (int)x.pr.Id),
            (string?)x.pr.SubmittedBySignatureUrl));

        // Leave requests
        var leaveTasks = leaveRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "leave",
            $"請假申請 #{row.Id}（{row.LeaveType}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("leave"),
            null,
            new LeaveTaskDetailDto(
                (int)row.Id,
                (string)row.LeaveType,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (decimal)row.Hours,
                (string)row.Reason),
            null, null, null, null,
            GetRecords("leave", (int)row.Id),
            GetDesignatedReviewers("leave", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Travel requests
        var travelTasks = travelRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "travel",
            $"出差申請 #{row.Id}（{row.Destination}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("travel"),
            null,
            null,
            new TravelTaskDetailDto(
                (int)row.Id,
                (string)row.Destination,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (decimal)row.EstimatedCost,
                (string)row.Purpose,
                (string?)row.ProjectCode,
                (string?)row.ProjectName,
                (bool)row.IsHolidayTravel),
            null, null, null,
            GetRecords("travel", (int)row.Id),
            GetDesignatedReviewers("travel", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Overtime requests
        var overtimeTasks = overtimeRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "overtime",
            $"加班申請 #{row.Id}（{(decimal)row.EstimatedHours}h）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("overtime"),
            null, null, null,
            new OvertimeTaskDetailDto(
                (int)row.Id,
                (DateTime)row.OvertimeDate,
                (string?)row.ProjectIds,
                (decimal)row.EstimatedHours,
                (string)row.Reason),
            null, null,
            GetRecords("overtime", (int)row.Id),
            GetDesignatedReviewers("overtime", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Advance requests
        var advanceTasks = advanceRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "advance",
            $"預支申請 #{row.Id}（{row.ProjectCode}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("advance"),
            null, null, null, null,
            new AdvanceTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.ProjectCode,
                (string)row.ProjectName,
                (string)row.ActivityName,
                (decimal)row.GrandTotal,
                (DateTime?)row.EstimatedPaymentDate,
                (DateTime?)row.PaidAt),
            null,
            GetRecords("advance", (int)row.Id),
            GetDesignatedReviewers("advance", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Write-off requests
        var writeOffTasks = writeOffRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "write_off",
            $"沖銷申請 #{row.Id}（{row.ProjectCode}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("write_off"),
            null, null, null, null, null,
            new WriteOffTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.AdvanceRequestNo,
                (string?)row.ProjectCode ?? "",
                (string?)row.ProjectName ?? "",
                (decimal)row.GrandTotal),
            GetRecords("write_off", (int)row.Id),
            GetDesignatedReviewers("write_off", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        return paymentTasks
            .Concat(leaveTasks)
            .Concat(travelTasks)
            .Concat(overtimeTasks)
            .Concat(advanceTasks)
            .Concat(writeOffTasks)
            .OrderByDescending(t => t.SubmittedAt);
    }

    private static IEnumerable<PaymentRequestDto> GroupToPaymentRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic pr, List<InvoiceItemDto> invoices)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);

            if (row.InvId is not null)
                dict[id].invoices.Add(new InvoiceItemDto(
                    (int)row.InvId,
                    (string)row.FileName,
                    (string)row.InvoiceNo,
                    (decimal)row.InvAmount,
                    (string?)row.InvItemName,
                    (string?)row.InvNote,
                    (string?)row.InvFileUrl));
        }

        return dict.Values.Select(x => new PaymentRequestDto(
            (int)x.pr.Id,
            (string)x.pr.Type,
            (int)x.pr.ProjectId,
            (string)x.pr.ProjectCode,
            (string)x.pr.ProjectName,
            [.. x.invoices],
            (decimal)x.pr.TotalAmount,
            (string)x.pr.ApprovalStatus,
            (string?)x.pr.SubmittedBy,
            (DateTime)x.pr.CreatedAt,
            (DateTime?)x.pr.EstimatedPaymentDate,
            (DateTime?)x.pr.PaidAt,
            (DateTime?)x.pr.ReviewedAt,
            (string?)x.pr.ReviewNote,
            null)); // DesignatedReviewers 以 null 回傳
    }
}
