using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PaymentRequestReadService(IDbConnection db, IInstallmentReadService installments) : IPaymentRequestReadService
{
    /// <summary>
    /// 廠商匯款資料組字串（戶名 / 匯款銀行 / 銀行代號 / 銀行帳號 四欄併為一行）。
    /// 請款 PDF 的「帳戶資料」為單一欄位，故在 SQL 端組合，避免動到 detail DTO 的位置參數。
    /// 空欄自動略過；四欄皆空回 NULL（前端顯示破折號）。
    /// </summary>
    private const string VendorBankAccountExpr = """
        NULLIF(CONCAT_WS(N' / ',
            NULLIF(CONCAT(N'戶名：',     NULLIF(ven.BankAccountName, N'')), N'戶名：'),
            NULLIF(CONCAT(N'匯款銀行：', NULLIF(ven.BankName,        N'')), N'匯款銀行：'),
            NULLIF(CONCAT(N'銀行代號：', NULLIF(ven.BankCode,        N'')), N'銀行代號：'),
            NULLIF(ven.BankAccount, N'')), N'')
        """;

    // ── 通用 JOIN SQL ─────────────────────────────────────────────────────────
    private const string BaseSql = """
        SELECT pr.Id, pr.RequestNo, pr.Type, pr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               pr.TotalAmount, pr.ApprovalStatus,
               sub.Name AS SubmittedBy, pr.CreatedAt,
               pr.ReviewedAt, pr.ReviewNote, pr.Reason,
               pr.VendorId, ven.Name AS VendorName, ven.TaxId AS VendorTaxId,
               ii.Id AS InvId, ii.FileName, ii.InvoiceNo, ii.Amount AS InvAmount, ii.ItemName AS InvItemName, ii.Note AS InvNote, ii.FileUrl AS InvFileUrl, ii.InvoiceDate AS InvInvoiceDate
        FROM PaymentRequests pr
        LEFT JOIN Projects proj   ON pr.ProjectId    = proj.Id
        LEFT JOIN Users   sub     ON pr.SubmittedById = sub.Id
        LEFT JOIN Vendors ven     ON pr.VendorId      = ven.Id
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
        var dtos = GroupToPaymentRequests(rows).ToList();
        // 為列表頁注入 paymentStatus 三態 badge（不附完整 installments 陣列以節省 payload）
        var ids = dtos.Select(d => d.Id).ToList();
        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.PaymentRequest, ids);
        var withStatus = dtos.Select(d => d with { PaymentStatus = installments.ComputeStatus(instDict.GetValueOrDefault(d.Id, [])) });
        return new PagedResult<PaymentRequestDto>(withStatus, total, page, pageSize, Math.Max(1, totalPages));
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
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment,
                   rdr.ApprovalStepOrder, rdr.SelectedDepartmentId, d.Name AS SelectedDepartmentName
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            LEFT JOIN Departments d ON rdr.SelectedDepartmentId = d.Id
            WHERE rdr.RequestType = 'payment_request' AND rdr.RequestId = @RequestId
            ORDER BY rdr.ApprovalStepOrder, rdr.StepOrder
            """;
        var drRows = await db.QueryAsync<dynamic>(drSql, new { RequestId = id });
        var designatedReviewers = drRows.Select(r => new DesignatedReviewerDto(
            (int)r.Id,
            (Guid)r.ReviewerId,
            (string)r.ReviewerName,
            (int)r.StepOrder,
            (string)r.Status,
            (DateTime?)r.ReviewedAt,
            (string?)r.Comment,
            (int)r.ApprovalStepOrder,
            (int?)r.SelectedDepartmentId,
            (string?)r.SelectedDepartmentName)).ToArray();

        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.PaymentRequest, new[] { id });
        var instList = instDict.GetValueOrDefault(id, []);

        // 整單批次附件（獨立查詢，避免與 invoices JOIN 笛卡兒相乘）
        const string attSql = """
            SELECT Id, FileName, FileUrl
            FROM PaymentRequestAttachments
            WHERE PaymentRequestId = @Id
            ORDER BY SortOrder
            """;
        var attRows = await db.QueryAsync<dynamic>(attSql, new { Id = id });
        var attachments = attRows.Select(r => new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl)).ToArray();

        return dto with
        {
            DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null,
            Installments        = instList.Count > 0 ? instList.ToArray() : null,
            PaymentStatus       = installments.ComputeStatus(instList),
            Attachments         = attachments.Length > 0 ? attachments : null,
        };
    }

    // ── ApprovalTask（彙總 PaymentRequest + LeaveRequest + TravelRequest + OvertimeRequest）──

    public async Task<IEnumerable<ApprovalTaskDto>> GetApprovalTasksAsync(
        int? reviewerJobTitleId = null, int? reviewerDepartmentId = null,
        string? status = null, Guid? reviewerUserId = null, string? paymentStatus = null,
        string? applicationType = null, Guid? submittedByUserId = null,
        int? directorStepDeptId = null, bool directorScope = false)
    {
        var (payments, leaves, travels, holidayTravels, overtimes, advances, writeOffs, travelWriteOffs, travelPayments, preReviews, preReviewItems, flows, records, designatedRows, writeOffItems, advanceItems, advanceSupplements, travelItems, travelWriteOffItems, travelPaymentItems, holidayParticipants, overtimeProjects, leaveRevocations, leaveRevocationDates) =
            await FetchAllAsync(reviewerJobTitleId: reviewerJobTitleId, reviewerDepartmentId: reviewerDepartmentId,
                                statusFilter: status, reviewerUserId: reviewerUserId, paymentStatus: paymentStatus,
                                applicationType: applicationType, submittedByUserId: submittedByUserId,
                                directorStepDeptId: directorStepDeptId, directorScope: directorScope);
        var instDicts = await LoadInstallmentsAsync(payments, advances, travels, holidayTravels, travelPayments, writeOffs);
        var paymentAttachments   = await LoadPaymentAttachmentsAsync();
        var writeOffAttachments  = await LoadWriteOffAttachmentsAsync();
        var preReviewAttachments = await LoadPreReviewAttachmentsAsync();
        var writeOffHistory      = await LoadWriteOffHistoryAsync();
        return BuildApprovalTasks(payments, leaves, travels, holidayTravels, overtimes, advances, writeOffs, travelWriteOffs, travelPayments, preReviews, preReviewItems, flows, records, designatedRows, writeOffItems, advanceItems, advanceSupplements, travelItems, travelWriteOffItems, travelPaymentItems, holidayParticipants, overtimeProjects, leaveRevocations, leaveRevocationDates, instDicts, paymentAttachments, writeOffAttachments, preReviewAttachments, writeOffHistory);
    }

    public async Task<ApprovalTaskDto?> GetApprovalTaskByIdAsync(int id, string applicationType)
    {
        var (payments, leaves, travels, holidayTravels, overtimes, advances, writeOffs, travelWriteOffs, travelPayments, preReviews, preReviewItems, flows, records, designatedRows, writeOffItems, advanceItems, advanceSupplements, travelItems, travelWriteOffItems, travelPaymentItems, holidayParticipants, overtimeProjects, leaveRevocations, leaveRevocationDates) = await FetchAllAsync(id, applicationType);
        var instDicts = await LoadInstallmentsAsync(payments, advances, travels, holidayTravels, travelPayments, writeOffs);
        var paymentAttachments   = await LoadPaymentAttachmentsAsync();
        var writeOffAttachments  = await LoadWriteOffAttachmentsAsync();
        var preReviewAttachments = await LoadPreReviewAttachmentsAsync();
        var writeOffHistory      = await LoadWriteOffHistoryAsync();
        return BuildApprovalTasks(payments, leaves, travels, holidayTravels, overtimes, advances, writeOffs, travelWriteOffs, travelPayments, preReviews, preReviewItems, flows, records, designatedRows, writeOffItems, advanceItems, advanceSupplements, travelItems, travelWriteOffItems, travelPaymentItems, holidayParticipants, overtimeProjects, leaveRevocations, leaveRevocationDates, instDicts, paymentAttachments, writeOffAttachments, preReviewAttachments, writeOffHistory)
            .FirstOrDefault(t => t.Id == id && t.ApplicationType == applicationType);
    }

    // Backward-compat overload (scans all types)
    public async Task<ApprovalTaskDto?> GetApprovalTaskByIdAsync(int id)
        => (await GetApprovalTasksAsync()).FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// 簽核作業「申請人」下拉選項：10 種申請單中曾送出（非草稿）者的申請人去重清單，依姓名排序。
    /// 僅供財務體系部門篩選用（權限在 ApprovalTaskHandler.GetApplicantsAsync 檢查）。
    /// </summary>
    public async Task<IEnumerable<ApprovalTaskApplicantDto>> GetApprovalTaskApplicantsAsync()
    {
        const string sql = """
            SELECT u.Id, u.Name
            FROM Users u
            WHERE u.IsSuperAdmin = 0
              AND EXISTS (
                SELECT 1 FROM PaymentRequests          x WHERE x.SubmittedById = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM LeaveRequests            x WHERE x.EmployeeId    = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM TravelRequests           x WHERE x.EmployeeId    = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM OvertimeRequests         x WHERE x.EmployeeId    = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM AdvanceRequests          x WHERE x.SubmittedById = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM WriteOffRecords          x WHERE x.SubmittedById = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM TravelWriteOffRecords    x WHERE x.SubmittedById = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM TravelPaymentRequests    x WHERE x.EmployeeId    = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM PreReviewRequests        x WHERE x.SubmittedById = u.Id AND x.ApprovalStatus <> 'draft'
                UNION ALL
                SELECT 1 FROM LeaveRevocations         x WHERE x.EmployeeId    = u.Id AND x.ApprovalStatus <> 'draft'
              )
            ORDER BY u.Name
            """;
        var rows = await db.QueryAsync<dynamic>(sql);
        return rows.Select(r => new ApprovalTaskApplicantDto((Guid)r.Id, (string?)r.Name ?? "—")).ToArray();
    }


    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(
        IEnumerable<dynamic> payments,
        IEnumerable<dynamic> leaves,
        IEnumerable<dynamic> travels,
        IEnumerable<dynamic> holidayTravels,
        IEnumerable<dynamic> overtimes,
        IEnumerable<dynamic> advances,
        IEnumerable<dynamic> writeOffs,
        IEnumerable<dynamic> travelWriteOffs,
        IEnumerable<dynamic> travelPayments,
        IEnumerable<dynamic> preReviews,
        IEnumerable<dynamic> preReviewItems,
        IEnumerable<dynamic> flows,
        IEnumerable<dynamic> records,
        IEnumerable<dynamic> designatedRows,
        IEnumerable<dynamic> writeOffItems,
        IEnumerable<dynamic> advanceItems,
        IEnumerable<dynamic> advanceSupplements,
        IEnumerable<dynamic> travelItems,
        IEnumerable<dynamic> travelWriteOffItems,
        IEnumerable<dynamic> travelPaymentItems,
        IEnumerable<dynamic> holidayParticipants,
        IEnumerable<dynamic> overtimeProjects,
        IEnumerable<dynamic> leaveRevocations,
        IEnumerable<dynamic> leaveRevocationDates)> FetchAllAsync(
        int? filterId = null, string? filterType = null,
        int? reviewerJobTitleId = null, int? reviewerDepartmentId = null,
        string? statusFilter = null, Guid? reviewerUserId = null,
        string? paymentStatus = null, string? applicationType = null,
        Guid? submittedByUserId = null, int? directorStepDeptId = null, bool directorScope = false)
    {
        // ── WHERE clause for specific ID lookup ──────────────────────────────
        string paymentIdWhere        = (filterId.HasValue && filterType == "payment_request")  ? "pr.Id = @Id"  : "";
        string leaveIdWhere          = (filterId.HasValue && filterType == "leave")             ? "lr.Id = @Id"  : "";
        string travelIdWhere         = (filterId.HasValue && filterType == "travel")            ? "tr.Id = @Id"  : "";
        string holidayTravelIdWhere  = (filterId.HasValue && filterType == "holiday_travel")    ? "tr.Id = @Id"  : "";
        string overtimeIdWhere       = (filterId.HasValue && filterType == "overtime")          ? "ot.Id = @Id"  : "";
        string advanceIdWhere        = (filterId.HasValue && filterType == "advance")           ? "adv.Id = @Id" : "";
        string writeOffIdWhere       = (filterId.HasValue && filterType == "write_off")         ? "wo.Id = @Id"  : "";
        string travelWriteOffIdWhere  = (filterId.HasValue && filterType == "travel_write_off")  ? "two.Id = @Id"  : "";
        string travelPaymentIdWhere   = (filterId.HasValue && filterType == "travel_payment")    ? "tpr.Id = @Id" : "";
        string preReviewIdWhere       = (filterId.HasValue && filterType == "pre_review")         ? "prv.Id = @Id" : "";
        string leaveRevocationIdWhere = (filterId.HasValue && filterType == "leave_revocation")   ? "rv.Id = @Id"  : "";

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
            // 「總監室簽核」（scope=director）：與總監關卡（JobTitle.Level=1）有關的單，依 statusFilter 分四態。
            //   pending  → 維持原「總監待簽核」語意：已輪到總監關卡（綁 CurrentStepOrder）且尚未簽核
            //   其餘三態 → 只要流程中含總監關卡即可（讓檢視者看得到同一批單的最終結果）
            // 一律不受審核者職稱/部門限制（檢視用清單，可見權限已在 ApprovalTaskHandler.GetAllAsync 擋過，
            // 此處僅過濾資料）。
            // directorStepDeptId 有值時（會計室等非財務管理部的檢視者）再收斂為
            // 「該單流程中含綁定自己部門的關卡」＝這張單需要自己部門簽核（總監關卡多為最後一關，
            // 會計關卡在其之前，故刻意不比 StepOrder；沒有會計關卡的請假 / 銷假 / 加班因此被排除）。
            // 相容 DepartmentId 未綁定、只綁職稱的會計關卡（部分流程如此設定）。四態一律套用此收斂。
            if (directorScope)
            {
                string dirStatus = statusFilter switch
                {
                    "approved" => "approved",
                    "returned" => "returned",
                    "rejected" => "rejected",
                    _          => "pending",
                };
                // 待簽核才綁「目前卡在總監關卡」；已核准 / 退回 / 已拒絕的單 CurrentStepOrder 已無意義
                string dirStepOrderClause = dirStatus == "pending"
                    ? $"AND sDir.StepOrder = {alias}.CurrentStepOrder"
                    : "";

                return $"""
                  {alias}.ApprovalStatus = '{dirStatus}'
                  AND EXISTS (
                    SELECT 1 FROM ApprovalSteps sDir
                    JOIN JobTitles jtDir ON jtDir.Id = sDir.JobTitleId
                    WHERE sDir.ApprovalItemId = {alias}.ApprovalItemId
                      {dirStepOrderClause}
                      AND jtDir.Level = 1
                  )
                  """ + (directorStepDeptId.HasValue ? $"""

                  AND EXISTS (
                    SELECT 1 FROM ApprovalSteps sMine
                    WHERE sMine.ApprovalItemId = {alias}.ApprovalItemId
                      AND (
                        sMine.DepartmentId = @DirectorStepDeptId
                        OR (sMine.DepartmentId IS NULL AND sMine.JobTitleId = @ReviewerJobTitleId)
                      )
                  )
                  """ : "");
            }

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

            // Normal reviewer — "rejected" tab: show tasks the user has reviewed that were rejected
            if (statusFilter == "rejected")
                return $"""
                  {alias}.ApprovalStatus = 'rejected'
                  AND EXISTS (
                    SELECT 1 FROM ApprovalRecords ar2
                    WHERE ar2.ApplicationType = '{appType}'
                      AND ar2.ApplicationId = {alias}.Id
                      AND ar2.ReviewedById = @ReviewerUserId
                  )
                  """;

            // Normal reviewer — "returned" tab: 退回修改中（申請人手上待改的單）
            // 與 approved / rejected 只看「我親自審過」不同：退回常發生在還沒輪到我之前，
            // 只比對 ApprovalRecords 會漏掉流程後段的審核者，故再放行「流程中含我關卡」。
            //   1) 我留過簽核紀錄（含我親自退回的、退回前已核准的）
            //   2) 我是這張單的指定審核者（designee 快照；刻意不限 Status='pending'，
            //      退回時該筆已被設為 'returned'，限 pending 會漏掉自己被指定的那張）
            //   3) 我是這張單的升級審核者（自審時被指派的 EscalationOverride）
            //   4) 流程中存在綁定我職稱的固定關卡（部門相符或未綁部門）
            // 第 4 段刻意只涵蓋固定職稱/部門關卡，不重用 pending 分支的 UseDirectSupervisor 遞迴
            // ——那段靠 CurrentStepOrder 定位「第幾層主管」，脫離當前步驟即無從成立且成本極高；
            // 直屬主管的情形由第 1 段涵蓋（主管退回前必已留下紀錄）。
            // JobTitleId 亦刻意不放行 IS NULL，否則「不限職稱」關卡會讓全公司都看到這張單。
            if (statusFilter == "returned")
                return $"""
                  {alias}.ApprovalStatus = 'returned'
                  AND (
                    EXISTS (
                      SELECT 1 FROM ApprovalRecords ar2
                      WHERE ar2.ApplicationType = '{appType}'
                        AND ar2.ApplicationId = {alias}.Id
                        AND ar2.ReviewedById = @ReviewerUserId
                    )
                    OR EXISTS (
                      SELECT 1 FROM RequestDesignatedReviewers rdrRet
                      WHERE rdrRet.RequestType = '{appType}'
                        AND rdrRet.RequestId = {alias}.Id
                        AND rdrRet.ReviewerId = @ReviewerUserId
                    )
                    OR EXISTS (
                      SELECT 1 FROM EscalationOverrides eoRet
                      WHERE eoRet.ApplicationType = '{appType}'
                        AND eoRet.ApplicationId = {alias}.Id
                        AND eoRet.ReviewerId = @ReviewerUserId
                    )
                    OR EXISTS (
                      SELECT 1 FROM ApprovalSteps sRet
                      WHERE sRet.ApprovalItemId = {alias}.ApprovalItemId
                        AND sRet.UseApplicantDesignated = 0
                        AND sRet.UseDirectSupervisor = 0
                        AND sRet.JobTitleId = @ReviewerJobTitleId
                        AND (
                          (sRet.UseApplicantDepartment = 1
                            AND {userAlias}.DepartmentId IS NOT NULL
                            AND {userAlias}.DepartmentId = @ReviewerDepartmentId)
                          OR
                          (sRet.UseApplicantDepartment = 0
                            AND (sRet.DepartmentId IS NULL OR sRet.DepartmentId = @ReviewerDepartmentId))
                        )
                    )
                  )
                  """;

            // Normal reviewer — "pending" tab (or no status param): match current step
            // Only show 'pending' status to avoid surfacing returned/rejected rows here.
            // Logic must stay in sync with AuthorizeStepAsync.
            //
            // 例外指定審核（ApprovalStepException）：送單後的真相是 designee 快照 —— 只要該申請在
            // 「當前步驟」有 designee 綁定，此步驟即為指定審核步驟（不論步驟本身是否 UseApplicantDesignated）。
            // 因此 s2 / s3 一般分支必須排除這種情況，s4 指定分支則不再檢查 UseApplicantDesignated。
            // ★ NOT EXISTS 一定要綁 ApprovalStepOrder = CurrentStepOrder，否則「step1 原生指定 +
            //   step2~4 固定部門」的申請推進到 step2 後會從所有一般審核者的待審清單消失。
            string designatedBoundToCurrentStep = $"""
              EXISTS (
                SELECT 1 FROM RequestDesignatedReviewers rdrx
                WHERE rdrx.RequestType = '{appType}'
                  AND rdrx.RequestId   = {alias}.Id
                  AND rdrx.ApprovalStepOrder = {alias}.CurrentStepOrder
              )
              """;

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
                    AND NOT {designatedBoundToCurrentStep}
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
                    AND NOT {designatedBoundToCurrentStep}
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
                    AND EXISTS (
                      SELECT 1 FROM RequestDesignatedReviewers rdr
                      WHERE rdr.RequestType = '{appType}'
                        AND rdr.RequestId = {alias}.Id
                        AND rdr.ReviewerId = @ReviewerUserId
                        AND rdr.Status = 'pending'
                        AND rdr.ApprovalStepOrder = {alias}.CurrentStepOrder
                        AND rdr.StepOrder = (
                          SELECT MIN(rdr2.StepOrder)
                          FROM RequestDesignatedReviewers rdr2
                          WHERE rdr2.RequestType = '{appType}'
                            AND rdr2.RequestId = {alias}.Id
                            AND rdr2.Status = 'pending'
                            AND rdr2.ApprovalStepOrder = {alias}.CurrentStepOrder
                        )
                    )
                )
              )
              -- 跨步驟同人去重（限縮：總監 OR 相鄰 step）：僅排除「總監（JobTitle.Level=1）reviewer 已審」的殘留待審項目。
              -- 非總監 reviewer 的「相鄰 step 同人」由後端 SkipUnreviewableStepsAsync 在進入 step 前自動跳過 + 寫代簽，根本不會出現在 pending 清單。
              -- 非總監 reviewer 的「不相鄰 step 同人」不應被排除，需要該 reviewer 重新審核（這是新規則的設計核心）。
              AND NOT (
                EXISTS (
                  SELECT 1 FROM JobTitles jtDup
                  JOIN Users uDup ON uDup.JobTitleId = jtDup.Id
                  WHERE uDup.Id = @ReviewerUserId
                    AND jtDup.Level = 1
                )
                AND EXISTS (
                  SELECT 1 FROM ApprovalRecords arDup
                  WHERE arDup.ApplicationType = '{appType}'
                    AND arDup.ApplicationId = {alias}.Id
                    AND arDup.ReviewedById = @ReviewerUserId
                    AND arDup.Action = 'approved'
                    {(appType == "advance" ? $"AND arDup.RoundNo = {alias}.CurrentRoundNo" : "")}
                    AND arDup.ReviewedAt > ISNULL(
                      (SELECT MAX(arRet.ReviewedAt) FROM ApprovalRecords arRet
                       WHERE arRet.ApplicationType = '{appType}'
                         AND arRet.ApplicationId = {alias}.Id
                         AND arRet.Action = 'returned'
                         {(appType == "advance" ? $"AND arRet.RoundNo = {alias}.CurrentRoundNo" : "")}),
                      '0001-01-01'
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

        // ── 撥款/退款狀態篩選（僅在已核准頁籤且有 paymentStatus 時生效）───
        // 改從子表 installments 推算（Phase 2 已移除父表 cache），對應三態：
        //   paid    = 已全數撥款（有 installments 且所有 PaidAt 都非 null）
        //   partial = 部分撥款（至少一期 PaidAt 非 null，且至少一期 PaidAt 為 null）
        //   unpaid  = 尚未開始撥款（無 installments，或所有 PaidAt 都為 null）
        //   closed  = 已結案（父表 IsClosed = 1）；只有預支 / 出差預支有結案概念，其餘類型整批 short-circuit
        bool hasPaymentFilter = !string.IsNullOrEmpty(paymentStatus) && !filterId.HasValue;
        bool isClosedFilter   = hasPaymentFilter && paymentStatus == "closed";
        /// <param name="supportsClosed">父表是否有 IsClosed 欄位（僅 AdvanceRequests / TravelRequests）</param>
        string PaymentStatusClause(string parentAlias, string installmentTable, string fkCol, bool supportsClosed = false)
        {
            if (!hasPaymentFilter) return "";
            return paymentStatus switch
            {
                "paid"    => $" AND EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id)" +
                             $" AND NOT EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id AND ix.PaidAt IS NULL)",
                "partial" => $" AND EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id AND ix.PaidAt IS NOT NULL)" +
                             $" AND EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id AND ix.PaidAt IS NULL)",
                "closed"  => supportsClosed ? $" AND {parentAlias}.IsClosed = 1" : " AND 1=0",
                _         => $" AND (NOT EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id)" +
                             $"      OR NOT EXISTS (SELECT 1 FROM {installmentTable} ix WHERE ix.{fkCol} = {parentAlias}.Id AND ix.PaidAt IS NOT NULL))",
            };
        }

        /// <summary>
        /// 退款狀態篩選（沖銷類用）：仍以父表 RefundedAt 欄位判斷兩態。
        /// 沖銷類沒有分期也沒有結案欄位，遇 paymentStatus=partial / closed 時整批 short-circuit（不顯示任何沖銷案件）。
        /// </summary>
        string RefundStatusClause(string refundedAtColumn)
        {
            if (!hasPaymentFilter) return "";
            if (paymentStatus == "partial" || isClosedFilter) return " AND 1=0";
            return paymentStatus == "paid"
                ? $" AND {refundedAtColumn} IS NOT NULL"
                : $" AND {refundedAtColumn} IS NULL";
        }

        // ── 類型篩選（已核准頁籤可選擇單一申請類型；按 ID 查詢時略過）───
        // 非選定類型的 SQL 直接 short-circuit 為 WHERE 1=0，避免拉取後再丟棄
        bool hasTypeFilter = !string.IsNullOrEmpty(applicationType) && !filterId.HasValue;
        bool TypeAllowed(string thisType) => !hasTypeFilter || applicationType == thisType;

        // ── 申請人篩選（僅財務體系可用，權限在 Handler 檢查；按 ID 查詢時略過）───
        // 各申請單的申請人欄位不一致：請款 / 預支 / 沖銷 / 預審用 SubmittedById，請假 / 出差 / 加班 / 出差請款用 EmployeeId
        bool hasSubmitterFilter = submittedByUserId.HasValue && !filterId.HasValue;
        string SubmitterClause(string column) => hasSubmitterFilter ? $" AND {column} = @SubmittedByUserId" : "";

        // 按 ID 查詢時不套用審核者過濾（StepMatchClause），只用 ID 條件
        string paymentWhere       = !TypeAllowed("payment_request") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(paymentIdWhere,       "") : BuildWhere("", StepMatchClause("pr",  "sub",  "payment_request")) + PaymentStatusClause("pr",  "PaymentRequestInstallments",      "PaymentRequestId") + SubmitterClause("pr.SubmittedById"));
        string leaveWhere         = !TypeAllowed("leave") || hasPaymentFilter ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(leaveIdWhere,         "") : BuildWhere("", StepMatchClause("lr",  "u",    "leave")) + SubmitterClause("lr.EmployeeId"));
        string travelWhere        = !TypeAllowed("travel") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(travelIdWhere,        "") + " AND tr.IsHolidayTravel = 0" : BuildWhere("tr.IsHolidayTravel = 0", StepMatchClause("tr",  "u",    "travel")) + PaymentStatusClause("tr",  "TravelRequestInstallments",       "TravelRequestId", supportsClosed: true) + SubmitterClause("tr.EmployeeId"));
        // 假日執行活動津貼隨次月薪資發放、不走撥款流程，故套用「已撥款 / 未撥款」篩選時與 leave/overtime 一致直接排除
        string holidayTravelWhere = !TypeAllowed("holiday_travel") || hasPaymentFilter ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(holidayTravelIdWhere, "") + " AND tr.IsHolidayTravel = 1" : BuildWhere("tr.IsHolidayTravel = 1", StepMatchClause("tr",  "u",    "holiday_travel")) + SubmitterClause("tr.EmployeeId"));
        string overtimeWhere      = !TypeAllowed("overtime") || hasPaymentFilter ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(overtimeIdWhere,      "") : BuildWhere("", StepMatchClause("ot",  "u",    "overtime")) + SubmitterClause("ot.EmployeeId"));
        string advanceWhere       = !TypeAllowed("advance") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(advanceIdWhere,       "") : BuildWhere("", StepMatchClause("adv", "asub", "advance")) + PaymentStatusClause("adv", "AdvanceRequestInstallments",      "AdvanceRequestId", supportsClosed: true) + SubmitterClause("adv.SubmittedById"));
        string writeOffWhere      = !TypeAllowed("write_off") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(writeOffIdWhere,      "") : BuildWhere("", StepMatchClause("wo",  "wsub", "write_off")) + RefundStatusClause("arx.RefundedAt") + SubmitterClause("wo.SubmittedById"));
        string travelWriteOffWhere  = !TypeAllowed("travel_write_off") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(travelWriteOffIdWhere,  "") : BuildWhere("", StepMatchClause("two", "trsub", "travel_write_off")) + RefundStatusClause("trx.RefundedAt") + SubmitterClause("two.SubmittedById"));
        string travelPaymentWhere   = !TypeAllowed("travel_payment") ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(travelPaymentIdWhere,   "") : BuildWhere("", StepMatchClause("tpr", "tpru", "travel_payment")) + PaymentStatusClause("tpr", "TravelPaymentRequestInstallments", "TravelPaymentRequestId") + SubmitterClause("tpr.EmployeeId"));
        // 預審申請：無撥款流程，paymentStatus 篩選時排除
        string leaveRevocationWhere = !TypeAllowed("leave_revocation") || hasPaymentFilter ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(leaveRevocationIdWhere, "") : BuildWhere("", StepMatchClause("rv", "u", "leave_revocation")) + SubmitterClause("rv.EmployeeId"));
        string preReviewWhere       = !TypeAllowed("pre_review") || hasPaymentFilter ? " WHERE 1=0" : (filterId.HasValue ? BuildWhere(preReviewIdWhere, "") : BuildWhere("", StepMatchClause("prv", "sub_prv", "pre_review")) + SubmitterClause("prv.SubmittedById"));

        var paymentSql = $"""
            SELECT pr.Id, pr.RequestNo, pr.Type AS PaymentType, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   pr.TotalAmount, pr.ApprovalStatus, pr.ApprovalItemId, pr.CurrentStepOrder,
                   sub.Name AS SubmittedBy, sub.SignatureUrl AS SubmittedBySignatureUrl, pr.CreatedAt, pr.ReviewedAt, pr.ReviewNote,
                   pr.Reason,
                   pr.VendorId, ven.Name AS VendorName, ven.TaxId AS VendorTaxId,
                   ven.ContactPerson AS VendorContactPerson, ven.Phone AS VendorPhone,
                   {VendorBankAccountExpr} AS VendorBankAccount, ven.Address AS VendorAddress,
                   ii.Id AS InvId, ii.FileName, ii.InvoiceNo, ii.Amount AS InvAmount, ii.ItemName AS InvItemName, ii.Note AS InvNote, ii.FileUrl AS InvFileUrl, ii.InvoiceDate AS InvInvoiceDate
            FROM PaymentRequests pr
            LEFT JOIN Projects proj   ON pr.ProjectId    = proj.Id
            LEFT JOIN Users   sub     ON pr.SubmittedById = sub.Id
            LEFT JOIN Vendors ven     ON pr.VendorId     = ven.Id
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

        // 銷假申請：JOIN 原請假單帶出假別 / 原期間 / 原時數（OriginalHours 為 null 表尚未銷過，取 Hours）
        var leaveRevocationSql = $"""
            SELECT rv.Id, rv.LeaveRequestId, rv.Reason, rv.RevokedHours,
                   rv.ApprovalStatus, rv.ApprovalItemId, rv.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl,
                   rv.CreatedAt, rv.ReviewedAt, rv.ReviewNote,
                   lr.LeaveType, lr.StartDate AS LeaveStartDate, lr.EndDate AS LeaveEndDate,
                   ISNULL(lr.OriginalHours, lr.Hours) AS LeaveHours, lr.Reason AS LeaveReason
            FROM LeaveRevocations rv
            JOIN LeaveRequests lr ON rv.LeaveRequestId = lr.Id
            LEFT JOIN Users u ON rv.EmployeeId = u.Id
            {leaveRevocationWhere}
            ORDER BY rv.CreatedAt DESC
            """;

        const string leaveRevocationDateSql = """
            SELECT LeaveRevocationId, Date, Hours
            FROM LeaveRevocationDates
            ORDER BY LeaveRevocationId, Date
            """;

        var travelSql = $"""
            SELECT tr.Id, tr.RequestNo, tr.Destination, tr.StartDate, tr.EndDate, tr.AdvanceNeededDate,
                   tr.GrandTotal, tr.Purpose, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   tr.IsHolidayTravel,
                   tr.EstimatedRefundDate, tr.RefundedAt,
                   tr.IsClosed, tr.ClosedAt, tr.RefundAmount, tr.RefundedAmount,
                   tr.ApprovalStatus, tr.ApprovalItemId, tr.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote
            FROM TravelRequests tr
            LEFT JOIN Users u          ON tr.EmployeeId  = u.Id
            LEFT JOIN Projects proj    ON tr.ProjectId   = proj.Id
            {travelWhere}
            ORDER BY tr.CreatedAt DESC
            """;

        // 假日執行活動申請（IsHolidayTravel = 1），獨立 ApplicationType = "holiday_travel"
        // ApplicantId / ApplicantBaseSalary 用於計算申請人本人的假日津貼
        var holidayTravelSql = $"""
            SELECT tr.Id, tr.RequestNo, tr.Destination, tr.StartDate, tr.EndDate, tr.AdvanceNeededDate,
                   tr.GrandTotal, tr.Purpose, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   tr.IsHolidayTravel, tr.HolidayDays,
                   tr.EstimatedRefundDate, tr.RefundedAt,
                   tr.ApprovalStatus, tr.ApprovalItemId, tr.CurrentStepOrder,
                   tr.EmployeeId AS ApplicantId, u.BaseSalary AS ApplicantBaseSalary,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote
            FROM TravelRequests tr
            LEFT JOIN Users u          ON tr.EmployeeId  = u.Id
            LEFT JOIN Projects proj    ON tr.ProjectId   = proj.Id
            {holidayTravelWhere}
            ORDER BY tr.CreatedAt DESC
            """;

        var overtimeSql = $"""
            SELECT ot.Id, ot.OvertimeDate, ot.EstimatedHours, ot.Reason,
                   ot.CompensationType, ot.OvertimePayAmount, ot.PayableHours, ot.IsHolidayOvertime,
                   ot.ApprovalStatus, ot.ApprovalItemId, ot.CurrentStepOrder,
                   u.Name AS SubmittedBy, u.SignatureUrl AS SubmittedBySignatureUrl, ot.CreatedAt, ot.ReviewedAt, ot.ReviewNote
            FROM OvertimeRequests ot
            LEFT JOIN Users u ON ot.EmployeeId = u.Id
            {overtimeWhere}
            ORDER BY ot.CreatedAt DESC
            """;

        var advanceSql = $"""
            SELECT adv.Id, adv.RequestNo, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   adv.ActivityName, adv.GrandTotal, adv.AdvanceDate, adv.AdvanceNeededDate,
                   adv.ApprovalStatus, adv.ApprovalItemId, adv.CurrentStepOrder, adv.CurrentRoundNo,
                   adv.EstimatedRefundDate, adv.RefundedAt,
                   adv.IsClosed, adv.ClosedAt, adv.RefundAmount, adv.RefundedAmount,
                   asub.Name AS SubmittedBy, asub.SignatureUrl AS SubmittedBySignatureUrl, adv.CreatedAt, adv.ReviewedAt, adv.ReviewNote
            FROM AdvanceRequests adv
            LEFT JOIN Projects proj      ON adv.ProjectId    = proj.Id
            LEFT JOIN Users   asub       ON adv.SubmittedById = asub.Id
            {advanceWhere}
            ORDER BY adv.CreatedAt DESC
            """;

        var writeOffSql = $"""
            SELECT wo.Id, wo.RequestNo, wo.AdvanceRequestId, arx.RequestNo AS AdvanceRequestNo,
                   proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   wo.GrandTotal, wo.CashTotal, wo.CheckTotal, wo.Note,
                   wo.ApprovalStatus, wo.ApprovalItemId, wo.CurrentStepOrder,
                   wsub.Name AS SubmittedBy, wsub.SignatureUrl AS SubmittedBySignatureUrl, wo.CreatedAt, wo.ReviewedAt, wo.ReviewNote,
                   wo.SubmittedById,
                   arx.EstimatedRefundDate AS AdvanceEstimatedRefundDate,
                   arx.RefundedAt AS AdvanceRefundedAt,
                   arx.RefundAmount AS AdvanceRefundAmount,
                   arx.RefundedAmount AS AdvanceRefundedAmount,
                   arx.GrandTotal AS AdvanceGrandTotal,
                   ISNULL((SELECT SUM(w2.GrandTotal) FROM WriteOffRecords w2
                           WHERE w2.AdvanceRequestId = wo.AdvanceRequestId
                             AND w2.ApprovalStatus = 'approved'
                             AND w2.Id < wo.Id), 0) AS OtherWrittenOffTotal,
                   worefundby.SignatureUrl AS RefundedBySignatureUrl,
                   arx.IsClosed AS AdvanceIsClosed,
                   arx.ClosedAt AS AdvanceClosedAt,
                   wo.PendingClose,
                   arx.AdvanceDate AS AdvanceDate,
                   arx.AdvanceNeededDate AS AdvanceNeededDate,
                   wo.WriteOffNo
            FROM WriteOffRecords wo
            JOIN AdvanceRequests arx  ON wo.AdvanceRequestId = arx.Id
            LEFT JOIN Projects proj   ON arx.ProjectId       = proj.Id
            LEFT JOIN Users   wsub    ON wo.SubmittedById    = wsub.Id
            LEFT JOIN Users   worefundby ON arx.RefundedByUserId = worefundby.Id
            {writeOffWhere}
            ORDER BY wo.CreatedAt DESC
            """;

        var travelWriteOffSql = $"""
            SELECT two.Id, two.RequestNo, two.TravelRequestId,
                   CAST(trx.Id AS NVARCHAR(20)) AS TravelRequestNo,
                   trx.Destination, trx.StartDate, trx.EndDate, trx.Purpose,
                   proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   two.GrandTotal, two.Note,
                   two.ApprovalStatus, two.ApprovalItemId, two.CurrentStepOrder,
                   trsub.Name AS SubmittedBy, trsub.SignatureUrl AS SubmittedBySignatureUrl,
                   two.CreatedAt, two.ReviewedAt, two.ReviewNote,
                   trx.EstimatedRefundDate AS TravelEstimatedRefundDate,
                   trx.RefundedAt AS TravelRefundedAt,
                   trx.RefundAmount AS TravelRefundAmount,
                   trx.RefundedAmount AS TravelRefundedAmount,
                   trx.GrandTotal AS TravelGrandTotal,
                   ISNULL((SELECT SUM(tw2.GrandTotal) FROM TravelWriteOffRecords tw2
                           WHERE tw2.TravelRequestId = two.TravelRequestId
                             AND tw2.ApprovalStatus = 'approved'
                             AND tw2.Id < two.Id), 0) AS OtherWrittenOffTotal,
                   tworefundby.SignatureUrl AS RefundedBySignatureUrl,
                   trx.IsClosed AS TravelIsClosed,
                   trx.ClosedAt AS TravelClosedAt,
                   two.PendingClose
            FROM TravelWriteOffRecords two
            JOIN TravelRequests trx    ON two.TravelRequestId = trx.Id
            LEFT JOIN Projects proj    ON trx.ProjectId       = proj.Id
            LEFT JOIN Users   trsub    ON two.SubmittedById   = trsub.Id
            LEFT JOIN Users   tworefundby ON trx.RefundedByUserId = tworefundby.Id
            {travelWriteOffWhere}
            ORDER BY two.CreatedAt DESC
            """;

        var travelPaymentSql = $"""
            SELECT tpr.Id, tpr.RequestNo, tpr.Destination, tpr.StartDate, tpr.EndDate,
                   tpr.GrandTotal, tpr.Purpose, proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   tpr.ApprovalStatus, tpr.ApprovalItemId, tpr.CurrentStepOrder,
                   tpru.Name AS SubmittedBy, tpru.SignatureUrl AS SubmittedBySignatureUrl, tpr.CreatedAt, tpr.ReviewedAt, tpr.ReviewNote
            FROM TravelPaymentRequests tpr
            LEFT JOIN Users   tpru       ON tpr.EmployeeId   = tpru.Id
            LEFT JOIN Projects proj      ON tpr.ProjectId    = proj.Id
            {travelPaymentWhere}
            ORDER BY tpr.CreatedAt DESC
            """;

        // ── 預審申請（pre_review）──────────────────────────────────────────────
        var preReviewSql = $"""
            SELECT prv.Id, prv.RequestNo, prv.Type AS PreReviewType,
                   proj.Code AS ProjectCode, proj.Name AS ProjectName,
                   prv.TotalAmount, prv.TaxAmount, prv.Reason,
                   prv.ApprovalStatus, prv.ApprovalItemId, prv.CurrentStepOrder,
                   sub_prv.Name AS SubmittedBy, sub_prv.SignatureUrl AS SubmittedBySignatureUrl,
                   prv.CreatedAt, prv.ReviewedAt, prv.ReviewNote,
                   prv.VendorId, ven.Name AS VendorName, ven.TaxId AS VendorTaxId,
                   ven.ContactPerson AS VendorContactPerson, ven.Phone AS VendorPhone,
                   {VendorBankAccountExpr} AS VendorBankAccount, ven.Address AS VendorAddress
            FROM PreReviewRequests prv
            LEFT JOIN Projects proj    ON prv.ProjectId    = proj.Id
            LEFT JOIN Users   sub_prv  ON prv.SubmittedById = sub_prv.Id
            LEFT JOIN Vendors ven      ON prv.VendorId      = ven.Id
            {preReviewWhere}
            ORDER BY prv.CreatedAt DESC
            """;

        const string preReviewItemsSql = """
            SELECT pri.Id, pri.PreReviewRequestId, pri.FileName, pri.ItemCategory,
                   pri.Amount, pri.ItemName, pri.Description, pri.Note, pri.FileUrl, pri.ItemDate
            FROM PreReviewItems pri
            ORDER BY pri.PreReviewRequestId, pri.Id
            """;

        const string flowSql = """
            SELECT ai.Id AS FlowId, ai.Name AS FlowName, ai.ApplicationType, ai.DepartmentId AS FlowDepartmentId,
                   s.StepOrder, d.Name AS DepartmentName, d.Code AS DepartmentCode, j.Name AS JobTitleName, j.Level AS JobTitleLevel,
                   s.UseDirectSupervisor, s.UseApplicantDesignated, s.Note
            FROM ApprovalItems ai
            LEFT JOIN ApprovalSteps s ON s.ApprovalItemId = ai.Id
            LEFT JOIN Departments d   ON s.DepartmentId = d.Id
            LEFT JOIN JobTitles j     ON s.JobTitleId   = j.Id
            ORDER BY ai.Id, s.StepOrder
            """;

        const string recordSql = """
            SELECT ar.ApplicationType, ar.ApplicationId, ar.StepOrder, ar.RoundNo, ar.Action,
                   u.Name AS ReviewedBy, ar.ReviewedAt, ar.ReviewNote,
                   obu.Name AS OnBehalfOf, ar.IsEscalated,
                   u.SignatureUrl AS ReviewerSignatureUrl,
                   jt.Name AS ReviewerJobTitle, jt.Level AS ReviewerJobTitleLevel,
                   dep.Name AS ReviewerDepartmentName
            FROM ApprovalRecords ar
            LEFT JOIN Users u        ON ar.ReviewedById     = u.Id
            LEFT JOIN Users obu      ON ar.OnBehalfOfUserId = obu.Id
            LEFT JOIN JobTitles jt   ON u.JobTitleId        = jt.Id
            LEFT JOIN Departments dep ON u.DepartmentId     = dep.Id
            ORDER BY ar.ApplicationType, ar.ApplicationId, ar.StepOrder
            """;

        var param = new
        {
            Id = filterId ?? 0,
            ReviewerJobTitleId   = reviewerJobTitleId,
            ReviewerDepartmentId = reviewerDepartmentId,
            ReviewerUserId       = reviewerUserId,
            StatusFilter         = statusFilter,
            SubmittedByUserId    = submittedByUserId,
            DirectorStepDeptId = directorStepDeptId,
        };

        const string drSql = """
            SELECT rdr.RequestType, rdr.RequestId, rdr.Id, rdr.ReviewerId,
                   u.Name AS ReviewerName, rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment,
                   rdr.ApprovalStepOrder
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            ORDER BY rdr.RequestType, rdr.RequestId, rdr.ApprovalStepOrder, rdr.StepOrder
            """;

        var payments        = await db.QueryAsync<dynamic>(paymentSql,        param);
        var leaves          = await db.QueryAsync<dynamic>(leaveSql,          param);
        var travels         = await db.QueryAsync<dynamic>(travelSql,         param);
        var holidayTravels  = await db.QueryAsync<dynamic>(holidayTravelSql,  param);
        var overtimes       = await db.QueryAsync<dynamic>(overtimeSql,       param);
        var advances        = await db.QueryAsync<dynamic>(advanceSql,        param);
        var writeOffs       = await db.QueryAsync<dynamic>(writeOffSql,       param);
        var travelWriteOffs = await db.QueryAsync<dynamic>(travelWriteOffSql, param);
        var preReviews      = await db.QueryAsync<dynamic>(preReviewSql,      param);
        var leaveRevocations     = await db.QueryAsync<dynamic>(leaveRevocationSql, param);
        var leaveRevocationDates = await db.QueryAsync<dynamic>(leaveRevocationDateSql);
        var flows           = await db.QueryAsync<dynamic>(flowSql);
        var records         = await db.QueryAsync<dynamic>(recordSql);
        var designatedRows  = await db.QueryAsync<dynamic>(drSql);

        const string writeOffItemsSql = """
            SELECT wi.Id, wi.WriteOffRecordId, wi.Category, wi.SeqNo, wi.ItemName,
                   wi.UnitPrice, wi.Quantity, wi.TotalPrice, wi.CashAmount, wi.CheckAmount,
                   wi.Note, wi.InvoiceNo, wi.FileName, wi.FileUrl, wi.SortOrder, wi.InvoiceDate,
                   wi.CheckPaid, wi.CheckPaidAt, cpb.Name AS CheckPaidBy
            FROM WriteOffItems wi
            LEFT JOIN Users cpb ON wi.CheckPaidById = cpb.Id
            ORDER BY wi.WriteOffRecordId, wi.SortOrder
            """;
        var writeOffItemRows = await db.QueryAsync<dynamic>(writeOffItemsSql);

        const string advanceItemsSql = """
            SELECT ai.Id, ai.AdvanceRequestId, ai.RoundNo, ai.Category, ai.SeqNo, ai.ItemName,
                   ai.UnitPrice, ai.Quantity, ai.TotalPrice, ai.CashAmount, ai.CheckAmount,
                   ai.Note, ai.SortOrder, ai.FileName, ai.FileUrl
            FROM AdvanceRequestItems ai
            ORDER BY ai.AdvanceRequestId, ai.RoundNo, ai.SortOrder
            """;
        var advanceItemRows = await db.QueryAsync<dynamic>(advanceItemsSql);

        // 追加預支批次（RoundNo ≥ 2；Round 1 = 父單本身）
        const string advanceSupplementsSql = """
            SELECT s.AdvanceRequestId, s.RoundNo, s.AdvanceDate, s.AdvanceNeededDate, s.Reason
            FROM AdvanceRequestSupplements s
            ORDER BY s.AdvanceRequestId, s.RoundNo
            """;
        var advanceSupplementRows = await db.QueryAsync<dynamic>(advanceSupplementsSql);

        const string travelItemsSql = """
            SELECT ti.Id, ti.TravelRequestId, ti.Category, ti.SeqNo, ti.ItemName,
                   ti.UnitPrice, ti.Quantity, ti.TotalPrice, ti.Note, ti.SortOrder,
                   ti.InvoiceNo, ti.FileName, ti.FileUrl, ti.InvoiceDate
            FROM TravelRequestItems ti
            ORDER BY ti.TravelRequestId, ti.SortOrder
            """;
        var travelItemRows = await db.QueryAsync<dynamic>(travelItemsSql);

        const string travelWriteOffItemsSql = """
            SELECT twi.Id, twi.TravelWriteOffRecordId, twi.Category, twi.SeqNo, twi.ItemName,
                   twi.UnitPrice, twi.Quantity, twi.TotalPrice, twi.Note,
                   twi.InvoiceNo, twi.FileName, twi.FileUrl, twi.SortOrder, twi.InvoiceDate
            FROM TravelWriteOffItems twi
            ORDER BY twi.TravelWriteOffRecordId, twi.SortOrder
            """;
        var travelWriteOffItemRows = await db.QueryAsync<dynamic>(travelWriteOffItemsSql);

        var travelPayments      = await db.QueryAsync<dynamic>(travelPaymentSql,     param);
        var preReviewItemRows   = await db.QueryAsync<dynamic>(preReviewItemsSql);

        const string travelPaymentItemsSql = """
            SELECT tpi.Id, tpi.TravelPaymentRequestId, tpi.Category, tpi.SeqNo, tpi.ItemName,
                   tpi.UnitPrice, tpi.Quantity, tpi.TotalPrice, tpi.Note, tpi.SortOrder,
                   tpi.InvoiceNo, tpi.FileName, tpi.FileUrl, tpi.InvoiceDate
            FROM TravelPaymentRequestItems tpi
            ORDER BY tpi.TravelPaymentRequestId, tpi.SortOrder
            """;
        var travelPaymentItemRows = await db.QueryAsync<dynamic>(travelPaymentItemsSql);

        // 假日活動參與者（不含申請人本人；申請人在 holidayTravelSql 已帶 ApplicantBaseSalary）
        // 用於假日津貼預估顯示，金額計算公式與 PayrollReadService 一致
        // LEFT JOIN 個人參與日期後一列一 (參與者, 日期)，以 p.Id 分組還原（寫法比照 TravelRequestReadService）
        const string holidayParticipantsSql = """
            SELECT p.Id AS ParticipantId, p.TravelRequestId, p.UserId, u.Name AS UserName,
                   u.BaseSalary, p.SortOrder,
                   CAST(COALESCE(p.HolidayDays, tr.HolidayDays) AS decimal(5,1)) AS HolidayDays,
                   d.Date, d.Slot
            FROM TravelRequestParticipants p
            JOIN TravelRequests tr ON p.TravelRequestId = tr.Id
            LEFT JOIN Users u      ON p.UserId = u.Id
            LEFT JOIN TravelRequestParticipantDates d ON d.TravelRequestParticipantId = p.Id
            WHERE tr.IsHolidayTravel = 1
            ORDER BY p.TravelRequestId, p.SortOrder, d.Date
            """;
        var holidayParticipantRows = await db.QueryAsync<dynamic>(holidayParticipantsSql);

        // 加班申請的關聯專案明細（含各案預估時數）
        const string overtimeProjectsSql = """
            SELECT orp.OvertimeRequestId, orp.ProjectId,
                   p.Code AS ProjectCode, p.Name AS ProjectName, orp.EstimatedHours
            FROM OvertimeRequestProjects orp
            JOIN Projects p ON orp.ProjectId = p.Id
            ORDER BY orp.OvertimeRequestId, orp.SortOrder
            """;
        var overtimeProjectRows = await db.QueryAsync<dynamic>(overtimeProjectsSql);

        return (payments, leaves, travels, holidayTravels, overtimes, advances, writeOffs, travelWriteOffs, travelPayments, preReviews, preReviewItemRows, flows, records, designatedRows, writeOffItemRows, advanceItemRows, advanceSupplementRows, travelItemRows, travelWriteOffItemRows, travelPaymentItemRows, holidayParticipantRows, overtimeProjectRows, leaveRevocations, leaveRevocationDates);
    }

    /// <summary>給 BuildApprovalTasks 用的 installments 集合（依父表分組）</summary>
    private sealed record InstallmentDicts(
        Dictionary<int, List<InstallmentDto>> Payment,
        Dictionary<int, List<InstallmentDto>> Advance,
        Dictionary<int, List<InstallmentDto>> Travel,
        Dictionary<int, List<InstallmentDto>> TravelPayment,
        Dictionary<int, List<InstallmentDto>> WriteOff);

    private async Task<InstallmentDicts> LoadInstallmentsAsync(
        IEnumerable<dynamic> paymentRows,
        IEnumerable<dynamic> advanceRows,
        IEnumerable<dynamic> travelRows,
        IEnumerable<dynamic> holidayTravelRows,
        IEnumerable<dynamic> travelPaymentRows,
        IEnumerable<dynamic> writeOffRows)
    {
        var paymentIds       = paymentRows.Select(r => (int)r.Id).Distinct().ToList();
        // 沖銷任務要一併顯示「關聯預支單的撥款分期」，故預支 id 需併入沖銷單指向的預支單
        var advanceIds       = advanceRows.Select(r => (int)r.Id)
                                  .Concat(writeOffRows.Select(r => (int)r.AdvanceRequestId))
                                  .Distinct().ToList();
        var travelIds        = travelRows.Select(r => (int)r.Id).Concat(holidayTravelRows.Select(r => (int)r.Id)).Distinct().ToList();
        var travelPaymentIds = travelPaymentRows.Select(r => (int)r.Id).Distinct().ToList();
        var writeOffIds      = writeOffRows.Select(r => (int)r.Id).Distinct().ToList();

        var paymentInst       = await installments.GetByParentIdsAsync(InstallmentParentTable.PaymentRequest,       paymentIds);
        var advanceInst       = await installments.GetByParentIdsAsync(InstallmentParentTable.AdvanceRequest,       advanceIds);
        var travelInst        = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelRequest,        travelIds);
        var travelPaymentInst = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelPaymentRequest, travelPaymentIds);
        var writeOffInst      = await installments.GetByParentIdsAsync(InstallmentParentTable.WriteOffRecord,       writeOffIds);

        return new InstallmentDicts(paymentInst, advanceInst, travelInst, travelPaymentInst, writeOffInst);
    }

    /// <summary>整單批次附件（請款）依 PaymentRequestId 分組，供審核任務 mapper 取用</summary>
    private async Task<Dictionary<int, List<AttachmentDto>>> LoadPaymentAttachmentsAsync()
    {
        const string sql = "SELECT Id, PaymentRequestId, FileName, FileUrl FROM PaymentRequestAttachments ORDER BY PaymentRequestId, SortOrder";
        var rows = await db.QueryAsync<dynamic>(sql);
        var dict = new Dictionary<int, List<AttachmentDto>>();
        foreach (var r in rows)
        {
            int pid = (int)r.PaymentRequestId;
            if (!dict.ContainsKey(pid)) dict[pid] = [];
            dict[pid].Add(new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl));
        }
        return dict;
    }

    /// <summary>
    /// 同一預支單底下的各次沖銷（已拒絕的不列入），依 AdvanceRequestId 分組。
    /// 不套用任務查詢的篩選條件 —— 沖銷資訊卡要列出「全部」沖銷次數，不能只有本次查到的那幾筆。
    /// IsCurrent 於 mapper 內以 `with` 補上。
    /// </summary>
    private async Task<Dictionary<int, List<WriteOffRoundDto>>> LoadWriteOffHistoryAsync()
    {
        const string sql = """
            SELECT Id, AdvanceRequestId, WriteOffNo, RequestNo, GrandTotal, ApprovalStatus, CreatedAt
            FROM WriteOffRecords
            WHERE ApprovalStatus <> 'rejected'
            ORDER BY AdvanceRequestId, WriteOffNo, Id
            """;
        var rows = await db.QueryAsync<dynamic>(sql);
        var dict = new Dictionary<int, List<WriteOffRoundDto>>();
        foreach (var r in rows)
        {
            int advId = (int)r.AdvanceRequestId;
            if (!dict.ContainsKey(advId)) dict[advId] = [];
            dict[advId].Add(new WriteOffRoundDto(
                (int)r.Id, (int)r.WriteOffNo, (string)r.RequestNo, (decimal)r.GrandTotal,
                (string)r.ApprovalStatus, (DateTime)r.CreatedAt, false));
        }
        return dict;
    }

    /// <summary>整單批次附件（沖銷）依 WriteOffRecordId 分組，供審核任務 mapper 取用</summary>
    private async Task<Dictionary<int, List<AttachmentDto>>> LoadWriteOffAttachmentsAsync()
    {
        const string sql = "SELECT Id, WriteOffRecordId, FileName, FileUrl FROM WriteOffAttachments ORDER BY WriteOffRecordId, SortOrder";
        var rows = await db.QueryAsync<dynamic>(sql);
        var dict = new Dictionary<int, List<AttachmentDto>>();
        foreach (var r in rows)
        {
            int woId = (int)r.WriteOffRecordId;
            if (!dict.ContainsKey(woId)) dict[woId] = [];
            dict[woId].Add(new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl));
        }
        return dict;
    }

    /// <summary>整單批次附件（預審申請）依 PreReviewRequestId 分組，供審核任務 mapper 取用</summary>
    private async Task<Dictionary<int, List<AttachmentDto>>> LoadPreReviewAttachmentsAsync()
    {
        const string sql = "SELECT Id, PreReviewRequestId, FileName, FileUrl FROM PreReviewRequestAttachments ORDER BY PreReviewRequestId, SortOrder";
        var rows = await db.QueryAsync<dynamic>(sql);
        var dict = new Dictionary<int, List<AttachmentDto>>();
        foreach (var r in rows)
        {
            int pid = (int)r.PreReviewRequestId;
            if (!dict.ContainsKey(pid)) dict[pid] = [];
            dict[pid].Add(new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl));
        }
        return dict;
    }

    private IEnumerable<ApprovalTaskDto> BuildApprovalTasks(
        IEnumerable<dynamic> paymentRows,
        IEnumerable<dynamic> leaveRows,
        IEnumerable<dynamic> travelRows,
        IEnumerable<dynamic> holidayTravelRows,
        IEnumerable<dynamic> overtimeRows,
        IEnumerable<dynamic> advanceRows,
        IEnumerable<dynamic> writeOffRows,
        IEnumerable<dynamic> travelWriteOffRows,
        IEnumerable<dynamic> travelPaymentRows,
        IEnumerable<dynamic> preReviewRows,
        IEnumerable<dynamic> preReviewItemRows,
        IEnumerable<dynamic> flowRows,
        IEnumerable<dynamic> recordRows,
        IEnumerable<dynamic> designatedRows,
        IEnumerable<dynamic> writeOffItemRows,
        IEnumerable<dynamic> advanceItemRows,
        IEnumerable<dynamic> advanceSupplementRows,
        IEnumerable<dynamic> travelItemRows,
        IEnumerable<dynamic> travelWriteOffItemRows,
        IEnumerable<dynamic> travelPaymentItemRows,
        IEnumerable<dynamic> holidayParticipantRows,
        IEnumerable<dynamic> overtimeProjectRows,
        IEnumerable<dynamic> leaveRevocationRows,
        IEnumerable<dynamic> leaveRevocationDateRows,
        InstallmentDicts instDicts,
        Dictionary<int, List<AttachmentDto>> paymentAttachments,
        Dictionary<int, List<AttachmentDto>> writeOffAttachments,
        Dictionary<int, List<AttachmentDto>> preReviewAttachments,
        Dictionary<int, List<WriteOffRoundDto>> writeOffHistoryRows)
    {
        AttachmentDto[]? GetPaymentAttachments(int id) =>
            paymentAttachments.TryGetValue(id, out var a) && a.Count > 0 ? [.. a] : null;
        AttachmentDto[]? GetWriteOffAttachments(int id) =>
            writeOffAttachments.TryGetValue(id, out var a) && a.Count > 0 ? [.. a] : null;
        AttachmentDto[]? GetPreReviewAttachments(int id) =>
            preReviewAttachments.TryGetValue(id, out var a) && a.Count > 0 ? [.. a] : null;
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
                (string?)row.Comment,
                // 前端 PDF 簽名欄佈局需以此判斷哪些步驟為指定審核（含例外命中），不可省略
                (int)(row.ApprovalStepOrder ?? 0)));
        }

        DesignatedReviewerDto[]? GetDesignatedReviewers(string appType, int id) =>
            drDict.TryGetValue((appType, id), out var drs) && drs.Count > 0 ? [.. drs] : null;

        // Build flow lookup keyed by ApprovalItem.Id（= 申請列的 ApprovalItemId）。
        // 同一 ApplicationType 可能有多個流程（各部門專屬 + 通用預設），故不可用 ApplicationType 當 key
        // 否則會把多個 ApprovalItem 的 steps 合併到同一條流程，造成簽核流程重複顯示。
        var flowById = new Dictionary<int, (string Name, string? AppType, int? DeptId, List<ApprovalFlowStepDto> Steps)>();
        foreach (var row in flowRows)
        {
            int flowId = (int)row.FlowId;
            if (!flowById.ContainsKey(flowId))
                flowById[flowId] = ((string)row.FlowName, (string?)row.ApplicationType, (int?)row.FlowDepartmentId, []);
            if (row.StepOrder is not null)
                flowById[flowId].Steps.Add(new ApprovalFlowStepDto(
                    (int)row.StepOrder,
                    (string?)row.DepartmentName,
                    (string?)row.DepartmentCode,
                    (string?)row.JobTitleName,
                    (bool)(row.UseDirectSupervisor ?? false),
                    (bool)(row.UseApplicantDesignated ?? false),
                    (string?)row.Note,
                    (int?)row.JobTitleLevel));
        }

        // 每個 ApplicationType 的後備流程：申請列未帶 ApprovalItemId（理論上不應發生）時採用，
        // 優先取通用預設（DepartmentId 為 null），否則取該類型最小 Id 的流程，避免顯示空白。
        var fallbackFlowIdByType = new Dictionary<string, int>();
        foreach (var kv in flowById.OrderBy(k => k.Key))
        {
            string? appType = kv.Value.AppType;
            if (string.IsNullOrEmpty(appType)) continue;
            if (!fallbackFlowIdByType.ContainsKey(appType) || kv.Value.DeptId is null)
                fallbackFlowIdByType[appType] = kv.Key;
        }

        ApprovalFlowDto? GetFlow(string appType, int? approvalItemId)
        {
            if (approvalItemId is int id && flowById.TryGetValue(id, out var f))
                return new ApprovalFlowDto(id, f.Name, [.. f.Steps]);
            if (fallbackFlowIdByType.TryGetValue(appType, out var fid) && flowById.TryGetValue(fid, out var ff))
                return new ApprovalFlowDto(fid, ff.Name, [.. ff.Steps]);
            return null;
        }

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
                (string?)row.ReviewerSignatureUrl,
                (string?)row.ReviewerJobTitle,
                (string?)row.ReviewerDepartmentName,
                (int?)row.ReviewerJobTitleLevel,
                (int)(row.RoundNo ?? 1)));
        }

        ApprovalRecordDto[] GetRecords(string appType, int id) =>
            recordDict.TryGetValue((appType, id), out var recs) ? [.. recs] : [];

        // Advance request items lookup keyed by AdvanceRequestId
        var advItemDict = new Dictionary<int, List<AdvanceRequestItemDto>>();
        foreach (var ai in advanceItemRows)
        {
            int advId = (int)ai.AdvanceRequestId;
            if (!advItemDict.ContainsKey(advId))
                advItemDict[advId] = [];
            advItemDict[advId].Add(new AdvanceRequestItemDto(
                (int)ai.Id, (string)ai.Category, (int)ai.SeqNo, (string)ai.ItemName,
                (decimal)ai.UnitPrice, (string)ai.Quantity, (decimal)ai.TotalPrice,
                (decimal)ai.CashAmount, (decimal)ai.CheckAmount,
                (string?)ai.Note, (int)ai.SortOrder,
                (string?)ai.FileName, (string?)ai.FileUrl,
                (int)ai.RoundNo));
        }

        AdvanceRequestItemDto[] GetAdvanceItems(int id) =>
            advItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        // 追加預支批次 lookup keyed by AdvanceRequestId（Round 1 由父單 AdvanceDate 合成）
        var advSupplementDict = new Dictionary<int, List<dynamic>>();
        foreach (var s in advanceSupplementRows)
        {
            int advId = (int)s.AdvanceRequestId;
            if (!advSupplementDict.ContainsKey(advId))
                advSupplementDict[advId] = [];
            advSupplementDict[advId].Add(s);
        }

        AdvanceRoundDto[] GetAdvanceRounds(int id, DateTime advanceDate, DateTime? advanceNeededDate) =>
            AdvanceRequestReadService.BuildRounds(
                advanceDate, advanceNeededDate, advSupplementDict.GetValueOrDefault(id, []), GetAdvanceItems(id));

        // Travel request items lookup keyed by TravelRequestId
        var travelItemDict = new Dictionary<int, List<TravelRequestItemDto>>();
        foreach (var ti in travelItemRows)
        {
            int trId = (int)ti.TravelRequestId;
            if (!travelItemDict.ContainsKey(trId))
                travelItemDict[trId] = [];
            travelItemDict[trId].Add(new TravelRequestItemDto(
                (int)ti.Id, (string)ti.Category, (int)ti.SeqNo, (string)ti.ItemName,
                (decimal)ti.UnitPrice, (string)ti.Quantity, (decimal)ti.TotalPrice,
                (string?)ti.Note, (int)ti.SortOrder,
                (string?)ti.InvoiceNo, (string?)ti.FileName, (string?)ti.FileUrl,
                (DateTime?)ti.InvoiceDate));
        }

        TravelRequestItemDto[] GetTravelItems(int id) =>
            travelItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        // Overtime 關聯專案 lookup keyed by OvertimeRequestId
        var overtimeProjectDict = new Dictionary<int, List<OvertimeProjectDto>>();
        foreach (var op in overtimeProjectRows)
        {
            int otId = (int)op.OvertimeRequestId;
            if (!overtimeProjectDict.ContainsKey(otId))
                overtimeProjectDict[otId] = [];
            overtimeProjectDict[otId].Add(new OvertimeProjectDto(
                (int)op.ProjectId, (string)op.ProjectCode, (string)op.ProjectName, (decimal)op.EstimatedHours));
        }

        OvertimeProjectDto[]? GetOvertimeProjects(int id) =>
            overtimeProjectDict.TryGetValue(id, out var ps) && ps.Count > 0 ? [.. ps] : null;

        // Holiday travel participants lookup keyed by TravelRequestId
        // 每筆存 (UserId, UserName, BaseSalary, HolidayDays, Dates)；HolidayDays 已在 SQL 取 COALESCE(個人, 整單)，
        // 與 PayrollReadService 同一真相（有勾選參與日期者為個人天數，含半天 0.5）
        // 來源列因 LEFT JOIN 參與日期而一人多列，先以 ParticipantId 分組還原成一人一筆 + 逐日清單
        var holidayParticipantsDict = new Dictionary<int, List<(Guid UserId, string UserName, decimal? BaseSalary, decimal HolidayDays, ParticipantDateDto[]? Dates)>>();
        foreach (var g in holidayParticipantRows.GroupBy(r => (int)r.ParticipantId))
        {
            var hp = g.First();
            int trId = (int)hp.TravelRequestId;
            if (!holidayParticipantsDict.ContainsKey(trId))
                holidayParticipantsDict[trId] = [];
            var dates = g.Where(r => r.Date is not null)
                         .Select(r => new ParticipantDateDto(
                             (DateTime)r.Date,
                             ParticipantDateSlots.Normalize((string?)r.Slot)))
                         .ToArray();
            holidayParticipantsDict[trId].Add((
                (Guid)hp.UserId,
                (string?)hp.UserName ?? "—",
                (decimal?)hp.BaseSalary,
                (decimal?)hp.HolidayDays ?? 0m,
                dates.Length > 0 ? dates : null));
        }

        // 計算單筆假日活動的所有人員（申請人 + 參與者）參與明細與津貼合計
        // 公式：round(BaseSalary / 30) × 個人假日天數（半天 0.5），與 PayrollReadService.CalculateMonthlyPayrollAsync 一致
        // 申請人固定領整單 HolidayDays（不逐日勾選）；參與者領 COALESCE(個人, 整單)
        // 逐人金額只在此處相加後即丟棄，不進 DTO：個人津貼 ÷ 天數即為該員日薪，會反推出月薪
        (HolidayAllowanceDto[] List, int Total) BuildHolidayAllowances(int trId, Guid applicantId, string applicantName, decimal? applicantBaseSalary, decimal requestHolidayDays)
        {
            static int Allowance(decimal? baseSalary, decimal days)
                => baseSalary is { } bs && bs > 0 && days > 0
                    ? (int)Math.Round(Math.Round(bs / 30m, 0) * days, 0, MidpointRounding.AwayFromZero)
                    : 0;

            var list = new List<HolidayAllowanceDto>
            {
                new(applicantId, applicantName, requestHolidayDays, IsApplicant: true),
            };
            var total = Allowance(applicantBaseSalary, requestHolidayDays);
            if (holidayParticipantsDict.TryGetValue(trId, out var participants))
            {
                foreach (var (uid, name, bs, days, dates) in participants)
                {
                    list.Add(new HolidayAllowanceDto(uid, name, days, IsApplicant: false, dates));
                    total += Allowance(bs, days);
                }
            }
            return ([.. list], total);
        }

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
                    (string?)row.InvItemName, (string?)row.InvNote, (string?)row.InvFileUrl,
                    (DateTime?)row.InvInvoiceDate));
        }
        var paymentTasks = paymentGrouped.Values.Select(x => new ApprovalTaskDto(
            (int)x.pr.Id,
            "payment_request",
            $"請款申請 {x.pr.RequestNo}（{x.pr.ProjectCode}）",
            (string?)x.pr.SubmittedBy ?? "—",
            (DateTime)x.pr.CreatedAt,
            (string)x.pr.ApprovalStatus,
            (int)x.pr.CurrentStepOrder,
            (DateTime?)x.pr.ReviewedAt,
            (string?)x.pr.ReviewNote,
            GetFlow("payment_request", (int?)x.pr.ApprovalItemId),
            new PaymentTaskDetailDto(
                (int)x.pr.Id,
                (string)x.pr.RequestNo,
                (string)x.pr.PaymentType,
                (string)x.pr.ProjectCode,
                (string)x.pr.ProjectName,
                [.. x.invoices],
                (decimal)x.pr.TotalAmount,
                (string?)x.pr.Reason,
                (int?)x.pr.VendorId,
                (string?)x.pr.VendorName,
                (string?)x.pr.VendorTaxId,
                (string?)x.pr.VendorContactPerson,
                (string?)x.pr.VendorPhone,
                (string?)x.pr.VendorBankAccount,
                (string?)x.pr.VendorAddress,
                instDicts.Payment.TryGetValue((int)x.pr.Id, out var prInst) ? [.. prInst] : null,
                installments.ComputeStatus(instDicts.Payment.GetValueOrDefault((int)x.pr.Id, [])),
                GetPaymentAttachments((int)x.pr.Id)),
            null, null, null, null, null, null,
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
            GetFlow("leave", (int?)row.ApprovalItemId),
            null,
            new LeaveTaskDetailDto(
                (int)row.Id,
                (string)row.LeaveType,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (decimal)row.Hours,
                (string)row.Reason),
            null, null, null, null, null,
            GetRecords("leave", (int)row.Id),
            GetDesignatedReviewers("leave", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Leave revocations（銷假）
        var revocationDatesById = leaveRevocationDateRows
            .GroupBy(r => (int)r.LeaveRevocationId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new LeaveRevocationDateDto((DateTime)r.Date, (decimal)r.Hours)).ToArray());

        var leaveRevocationTasks = leaveRevocationRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "leave_revocation",
            $"銷假申請 #{row.Id}（{LeaveTypeNames.GetZh((string)row.LeaveType)}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("leave_revocation", (int?)row.ApprovalItemId),
            null, null, null, null, null, null, null,
            GetRecords("leave_revocation", (int)row.Id),
            GetDesignatedReviewers("leave_revocation", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl,
            null, null,
            new LeaveRevocationTaskDetailDto(
                (int)row.Id,
                (int)row.LeaveRequestId,
                (string)row.LeaveType,
                (DateTime)row.LeaveStartDate,
                (DateTime)row.LeaveEndDate,
                (decimal)row.LeaveHours,
                (string)row.LeaveReason,
                (decimal)row.RevokedHours,
                (string)row.Reason,
                revocationDatesById.GetValueOrDefault((int)row.Id, []))));

        // Travel requests (非假日執行活動，IsHolidayTravel = 0)
        var travelTasks = travelRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "travel",
            $"出差申請 {row.RequestNo}（{row.Destination}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("travel", (int?)row.ApprovalItemId),
            null,
            null,
            new TravelTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.Destination,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (decimal)row.GrandTotal,
                (string)row.Purpose,
                (string?)row.ProjectCode,
                (string?)row.ProjectName,
                (bool)row.IsHolidayTravel,
                (DateTime?)row.EstimatedRefundDate,
                (DateTime?)row.RefundedAt,
                null,
                GetTravelItems((int)row.Id),
                null,
                instDicts.Travel.TryGetValue((int)row.Id, out var trInst) ? [.. trInst] : null,
                installments.ComputeStatus(instDicts.Travel.GetValueOrDefault((int)row.Id, [])),
                (bool)row.IsClosed,
                (DateTime?)row.ClosedAt,
                (decimal?)row.RefundAmount,
                (decimal?)row.RefundedAmount,
                (DateTime?)row.AdvanceNeededDate),
            null, null, null, null,
            GetRecords("travel", (int)row.Id),
            GetDesignatedReviewers("travel", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Holiday travel requests (假日執行活動，IsHolidayTravel = 1)，使用獨立 ApplicationType = "holiday_travel"
        var holidayTravelTasks = holidayTravelRows.Select(row =>
        {
            var (htAllowances, htAllowanceTotal) = BuildHolidayAllowances(
                (int)row.Id,
                (Guid)row.ApplicantId,
                (string?)row.SubmittedBy ?? "—",
                (decimal?)row.ApplicantBaseSalary,
                (int?)row.HolidayDays ?? 0);
            return new ApprovalTaskDto(
                (int)row.Id,
                "holiday_travel",
                $"假日執行活動申請 {row.RequestNo}（{row.Destination}）",
                (string?)row.SubmittedBy ?? "—",
                (DateTime)row.CreatedAt,
                (string)row.ApprovalStatus,
                (int)row.CurrentStepOrder,
                (DateTime?)row.ReviewedAt,
                (string?)row.ReviewNote,
                GetFlow("holiday_travel", (int?)row.ApprovalItemId),
                null,
                null,
                new TravelTaskDetailDto(
                    (int)row.Id,
                    (string)row.RequestNo,
                    (string)row.Destination,
                    (DateTime)row.StartDate,
                    (DateTime)row.EndDate,
                    (decimal)row.GrandTotal,
                    (string)row.Purpose,
                    (string?)row.ProjectCode,
                    (string?)row.ProjectName,
                    (bool)row.IsHolidayTravel,
                    (DateTime?)row.EstimatedRefundDate,
                    (DateTime?)row.RefundedAt,
                    (int?)row.HolidayDays,
                    GetTravelItems((int)row.Id),
                    htAllowances,
                    instDicts.Travel.TryGetValue((int)row.Id, out var htInst) ? [.. htInst] : null,
                    installments.ComputeStatus(instDicts.Travel.GetValueOrDefault((int)row.Id, [])),
                    HolidayAllowanceTotal: htAllowanceTotal),
                null, null, null, null,
                GetRecords("holiday_travel", (int)row.Id),
                GetDesignatedReviewers("holiday_travel", (int)row.Id),
                (string?)row.SubmittedBySignatureUrl);
        });

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
            GetFlow("overtime", (int?)row.ApprovalItemId),
            null, null, null,
            new OvertimeTaskDetailDto(
                (int)row.Id,
                (DateTime)row.OvertimeDate,
                (decimal)row.EstimatedHours,
                (string)row.Reason,
                GetOvertimeProjects((int)row.Id),
                (string?)row.CompensationType ?? "compensatory",
                (decimal?)row.OvertimePayAmount,
                (decimal?)row.PayableHours,
                (bool?)row.IsHolidayOvertime),
            null, null, null,
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
            GetFlow("advance", (int?)row.ApprovalItemId),
            null, null, null, null,
            new AdvanceTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.ProjectCode,
                (string)row.ProjectName,
                (string)row.ActivityName,
                (decimal)row.GrandTotal,
                (DateTime?)row.EstimatedRefundDate,
                (DateTime?)row.RefundedAt,
                GetAdvanceItems((int)row.Id),
                (decimal?)row.RefundAmount,
                (decimal?)row.RefundedAmount,
                instDicts.Advance.TryGetValue((int)row.Id, out var advInst) ? [.. advInst] : null,
                installments.ComputeStatus(instDicts.Advance.GetValueOrDefault((int)row.Id, [])),
                GetAdvanceRounds((int)row.Id, (DateTime)row.AdvanceDate, (DateTime?)row.AdvanceNeededDate),
                (int)row.CurrentRoundNo,
                (bool)row.IsClosed,
                (DateTime?)row.ClosedAt,
                (DateTime?)row.AdvanceNeededDate),
            null, null,
            GetRecords("advance", (int)row.Id),
            GetDesignatedReviewers("advance", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Write-off items lookup keyed by WriteOffRecordId
        var woItemDict = new Dictionary<int, List<WriteOffItemDto>>();
        foreach (var wi in writeOffItemRows)
        {
            int woId = (int)wi.WriteOffRecordId;
            if (!woItemDict.ContainsKey(woId))
                woItemDict[woId] = [];
            woItemDict[woId].Add(new WriteOffItemDto(
                (int)wi.Id, (string)wi.Category, (int)wi.SeqNo, (string)wi.ItemName,
                (decimal)wi.UnitPrice, (string)wi.Quantity, (decimal)wi.TotalPrice,
                (decimal)wi.CashAmount, (decimal)wi.CheckAmount,
                (string?)wi.Note, (string?)wi.InvoiceNo,
                (string?)wi.FileName, (string?)wi.FileUrl, (int)wi.SortOrder,
                (DateTime?)wi.InvoiceDate,
                (bool)wi.CheckPaid, (DateTime?)wi.CheckPaidAt, (string?)wi.CheckPaidBy));
        }

        WriteOffItemDto[] GetWriteOffItems(int id) =>
            woItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        WriteOffRoundDto[] GetWriteOffHistory(int advanceRequestId, int currentId) =>
            [.. writeOffHistoryRows.GetValueOrDefault(advanceRequestId, []).Select(r => r with { IsCurrent = r.Id == currentId })];

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
            GetFlow("write_off", (int?)row.ApprovalItemId),
            null, null, null, null, null,
            new WriteOffTaskDetailDto(
                (int)row.Id,
                (int)row.AdvanceRequestId,
                (string)row.RequestNo,
                (string)row.AdvanceRequestNo,
                (string?)row.ProjectCode ?? "",
                (string?)row.ProjectName ?? "",
                (decimal)row.GrandTotal,
                (decimal)row.CashTotal,
                (decimal)row.CheckTotal,
                (string?)row.Note,
                GetWriteOffItems((int)row.Id),
                (DateTime?)row.AdvanceEstimatedRefundDate,
                (DateTime?)row.AdvanceRefundedAt,
                (decimal)row.AdvanceGrandTotal,
                (decimal)row.OtherWrittenOffTotal,
                (string?)row.RefundedBySignatureUrl,
                (bool)row.AdvanceIsClosed,
                (decimal?)row.AdvanceRefundAmount,
                (decimal?)row.AdvanceRefundedAmount,
                GetWriteOffAttachments((int)row.Id),
                GetAdvanceRounds((int)row.AdvanceRequestId, (DateTime)row.AdvanceDate, (DateTime?)row.AdvanceNeededDate),
                GetWriteOffHistory((int)row.AdvanceRequestId, (int)row.Id),
                WriteOffRefundCalculator.Calculate(
                    (decimal)row.AdvanceGrandTotal, (decimal)row.OtherWrittenOffTotal, (decimal)row.GrandTotal),
                instDicts.WriteOff.TryGetValue((int)row.Id, out var woInst) ? [.. woInst] : null,
                installments.ComputeStatus(instDicts.WriteOff.GetValueOrDefault((int)row.Id, [])),
                instDicts.Advance.TryGetValue((int)row.AdvanceRequestId, out var woAdvInst) ? [.. woAdvInst] : null,
                installments.ComputeStatus(instDicts.Advance.GetValueOrDefault((int)row.AdvanceRequestId, [])),
                (DateTime?)row.AdvanceClosedAt,
                (bool)row.PendingClose),
            null,
            GetRecords("write_off", (int)row.Id),
            GetDesignatedReviewers("write_off", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Travel write-off items lookup keyed by TravelWriteOffRecordId
        var twoItemDict = new Dictionary<int, List<TravelWriteOffItemDto>>();
        foreach (var twi in travelWriteOffItemRows)
        {
            int twoId = (int)twi.TravelWriteOffRecordId;
            if (!twoItemDict.ContainsKey(twoId))
                twoItemDict[twoId] = [];
            twoItemDict[twoId].Add(new TravelWriteOffItemDto(
                (int)twi.Id, (string)twi.Category, (int)twi.SeqNo, (string)twi.ItemName,
                (decimal)twi.UnitPrice, (string)twi.Quantity, (decimal)twi.TotalPrice,
                (string?)twi.Note, (string?)twi.InvoiceNo,
                (string?)twi.FileName, (string?)twi.FileUrl, (int)twi.SortOrder,
                (DateTime?)twi.InvoiceDate));
        }

        TravelWriteOffItemDto[] GetTravelWriteOffItems(int id) =>
            twoItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        // Travel payment request items lookup keyed by TravelPaymentRequestId
        var tpItemDict = new Dictionary<int, List<TravelPaymentRequestItemDto>>();
        foreach (var tpi in travelPaymentItemRows)
        {
            int tpId = (int)tpi.TravelPaymentRequestId;
            if (!tpItemDict.ContainsKey(tpId))
                tpItemDict[tpId] = [];
            tpItemDict[tpId].Add(new TravelPaymentRequestItemDto(
                (int)tpi.Id, (string)tpi.Category, (int)tpi.SeqNo, (string)tpi.ItemName,
                (decimal)tpi.UnitPrice, (string)tpi.Quantity, (decimal)tpi.TotalPrice,
                (string?)tpi.Note, (int)tpi.SortOrder,
                (string?)tpi.InvoiceNo, (string?)tpi.FileName, (string?)tpi.FileUrl,
                (DateTime?)tpi.InvoiceDate));
        }

        TravelPaymentRequestItemDto[] GetTravelPaymentItems(int id) =>
            tpItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        // Travel write-off requests
        var travelWriteOffTasks = travelWriteOffRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "travel_write_off",
            $"出差沖銷申請 #{row.Id}（{row.Destination}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("travel_write_off", (int?)row.ApprovalItemId),
            null, null, null, null, null, null,
            new TravelWriteOffTaskDetailDto(
                (int)row.Id,
                (int)row.TravelRequestId,
                (string)row.RequestNo,
                (string)row.TravelRequestNo,
                (string)row.Destination,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (string)row.Purpose,
                (string?)row.ProjectCode ?? "",
                (string?)row.ProjectName ?? "",
                (decimal)row.GrandTotal,
                (string?)row.Note,
                GetTravelWriteOffItems((int)row.Id),
                (DateTime?)row.TravelEstimatedRefundDate,
                (DateTime?)row.TravelRefundedAt,
                (decimal)row.TravelGrandTotal,
                (decimal)row.OtherWrittenOffTotal,
                (string?)row.RefundedBySignatureUrl,
                (bool)row.TravelIsClosed,
                (decimal?)row.TravelRefundAmount,
                (decimal?)row.TravelRefundedAmount,
                (DateTime?)row.TravelClosedAt,
                (bool)row.PendingClose),
            GetRecords("travel_write_off", (int)row.Id),
            GetDesignatedReviewers("travel_write_off", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl));

        // Travel payment requests
        var travelPaymentTasks = travelPaymentRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "travel_payment",
            $"出差請款申請 {row.RequestNo}（{row.Destination}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("travel_payment", (int?)row.ApprovalItemId),
            null, null, null, null, null, null, null,
            GetRecords("travel_payment", (int)row.Id),
            GetDesignatedReviewers("travel_payment", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl,
            new TravelPaymentTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.Destination,
                (DateTime)row.StartDate,
                (DateTime)row.EndDate,
                (decimal)row.GrandTotal,
                (string)row.Purpose,
                (string?)row.ProjectCode,
                (string?)row.ProjectName,
                GetTravelPaymentItems((int)row.Id),
                instDicts.TravelPayment.TryGetValue((int)row.Id, out var tpInst) ? [.. tpInst] : null,
                installments.ComputeStatus(instDicts.TravelPayment.GetValueOrDefault((int)row.Id, [])))));

        // Pre-review request items lookup keyed by PreReviewRequestId
        var prvItemDict = new Dictionary<int, List<PreReviewItemDto>>();
        foreach (var pri in preReviewItemRows)
        {
            int prvId = (int)pri.PreReviewRequestId;
            if (!prvItemDict.ContainsKey(prvId))
                prvItemDict[prvId] = [];
            prvItemDict[prvId].Add(new PreReviewItemDto(
                (int)pri.Id,
                (string)pri.FileName,
                (string?)pri.ItemCategory,
                (decimal)pri.Amount,
                (string?)pri.ItemName,
                (string?)pri.Description,
                (string?)pri.Note,
                (string?)pri.FileUrl,
                (DateTime?)pri.ItemDate));
        }

        PreReviewItemDto[] GetPreReviewItems(int id) =>
            prvItemDict.TryGetValue(id, out var items) ? [.. items] : [];

        // Pre-review requests
        var preReviewTasks = preReviewRows.Select(row => new ApprovalTaskDto(
            (int)row.Id,
            "pre_review",
            $"預審申請 {row.RequestNo}（{row.ProjectCode}）",
            (string?)row.SubmittedBy ?? "—",
            (DateTime)row.CreatedAt,
            (string)row.ApprovalStatus,
            (int)row.CurrentStepOrder,
            (DateTime?)row.ReviewedAt,
            (string?)row.ReviewNote,
            GetFlow("pre_review", (int?)row.ApprovalItemId),
            null, null, null, null, null, null, null,
            GetRecords("pre_review", (int)row.Id),
            GetDesignatedReviewers("pre_review", (int)row.Id),
            (string?)row.SubmittedBySignatureUrl,
            null,
            new PreReviewTaskDetailDto(
                (int)row.Id,
                (string)row.RequestNo,
                (string)row.PreReviewType,
                (string)row.ProjectCode,
                (string)row.ProjectName,
                GetPreReviewItems((int)row.Id),
                (decimal)row.TotalAmount,
                (decimal)row.TaxAmount,
                (string?)row.Reason,
                (int?)row.VendorId,
                (string?)row.VendorName,
                (string?)row.VendorTaxId,
                (string?)row.VendorContactPerson,
                (string?)row.VendorPhone,
                (string?)row.VendorBankAccount,
                (string?)row.VendorAddress,
                GetPreReviewAttachments((int)row.Id))));

        return paymentTasks
            .Concat(leaveTasks)
            .Concat(leaveRevocationTasks)
            .Concat(travelTasks)
            .Concat(holidayTravelTasks)
            .Concat(overtimeTasks)
            .Concat(advanceTasks)
            .Concat(writeOffTasks)
            .Concat(travelWriteOffTasks)
            .Concat(travelPaymentTasks)
            .Concat(preReviewTasks)
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
                    (string?)row.InvFileUrl,
                    (DateTime?)row.InvInvoiceDate));
        }

        return dict.Values.Select(x => new PaymentRequestDto(
            (int)x.pr.Id,
            (string)x.pr.RequestNo,
            (string)x.pr.Type,
            (int)x.pr.ProjectId,
            (string)x.pr.ProjectCode,
            (string)x.pr.ProjectName,
            [.. x.invoices],
            (decimal)x.pr.TotalAmount,
            (string)x.pr.ApprovalStatus,
            (string?)x.pr.SubmittedBy,
            (DateTime)x.pr.CreatedAt,
            (DateTime?)x.pr.ReviewedAt,
            (string?)x.pr.ReviewNote,
            (string?)x.pr.Reason,
            null,                                  // DesignatedReviewers 以 null 回傳
            (int?)x.pr.VendorId,
            (string?)x.pr.VendorName,
            (string?)x.pr.VendorTaxId));
    }
}
