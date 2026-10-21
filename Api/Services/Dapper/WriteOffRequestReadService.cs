using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class WriteOffRequestReadService(IDbConnection db, IInstallmentReadService installments) : IWriteOffRequestReadService
{
    /// <summary>
    /// 公司已補撥給同仁的超支差額：同預支單「已核准 ∪ 本單」沖銷的已撥款 WriteOffInstallments 合計與最後撥款日。
    /// 供「尚未結清金額」為負（沖銷大於預支）時判定是否已結清並顯示撥匯日；WriteOffRequestDto / WriteOffTaskDetailDto 共用。
    /// <paramref name="woAlias"/> 為外層 WriteOffRecords 別名。
    /// </summary>
    internal static string OverspendPaidSql(string woAlias) => $"""
        ISNULL((SELECT SUM(oi.Amount) FROM WriteOffInstallments oi
                JOIN WriteOffRecords ow ON ow.Id = oi.WriteOffRecordId
                WHERE ow.AdvanceRequestId = {woAlias}.AdvanceRequestId
                  AND (ow.ApprovalStatus = 'approved' OR ow.Id = {woAlias}.Id)
                  AND oi.PaidAt IS NOT NULL), 0) AS OverspendPaidTotal,
        (SELECT MAX(oi.PaidAt) FROM WriteOffInstallments oi
                JOIN WriteOffRecords ow ON ow.Id = oi.WriteOffRecordId
                WHERE ow.AdvanceRequestId = {woAlias}.AdvanceRequestId
                  AND (ow.ApprovalStatus = 'approved' OR ow.Id = {woAlias}.Id)
                  AND oi.PaidAt IS NOT NULL) AS OverspendLastPaidAt
        """;

    /// <summary>
    /// 「沖銷單是否已過財務管理部關卡」的單一真相（供沖銷狀態 badge：審核中 / 財務已審 / 已核准）。
    /// 判準：整單 approved，或存在「關卡部門 Code ∈ FinanceStep」且 Action='approved' 的簽核紀錄。
    /// 為何可信：送簽（draft / returned 皆然）一律清空舊 ApprovalRecords（WriteOffRequestHandler.SubmitAsync），
    /// 退回 / 拒絕後重送也從零開始，故存活的 approved 紀錄必屬本輪簽核；write_off 不走追加批次，RoundNo 恆為 1，不必過濾。
    /// 呼叫端須帶參數 <c>@FinanceStepCodes</c>（<see cref="FinanceStepCodesParam"/>），禁止硬編碼部門 Code。
    /// <paramref name="woAlias"/> 為外層 WriteOffRecords 別名；輸出欄位 FinancePassed（0 / 1）。
    /// </summary>
    internal static string FinancePassedSql(string woAlias) => $"""
        CASE WHEN {woAlias}.ApprovalStatus = 'approved' OR EXISTS (
                SELECT 1 FROM ApprovalRecords fpr
                JOIN ApprovalSteps fps ON fps.ApprovalItemId = {woAlias}.ApprovalItemId AND fps.StepOrder = fpr.StepOrder
                JOIN Departments fpd   ON fpd.Id = fps.DepartmentId
                WHERE fpr.ApplicationType = 'write_off'
                  AND fpr.ApplicationId   = {woAlias}.Id
                  AND fpr.Action          = 'approved'
                  AND fpd.Code IN @FinanceStepCodes)
             THEN 1 ELSE 0 END AS FinancePassed
        """;

    /// <summary><see cref="FinancePassedSql"/> 的 @FinanceStepCodes 參數值。</summary>
    internal static string[] FinanceStepCodesParam => [.. DepartmentCodes.FinanceStep];

    // 預支沖銷申請主查詢：WriteOffRecords 關聯 AdvanceRequests、Projects、Users、WriteOffItems
    private static readonly string BaseSql = $$"""
        SELECT wo.Id, wo.RequestNo, wo.AdvanceRequestId, ar.RequestNo AS AdvanceRequestNo,
               wo.WriteOffNo, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               ar.ActivityName, ar.ActivityPeriod,
               wo.CashTotal, wo.CheckTotal, wo.GrandTotal,
               wo.Note, wo.ApprovalStatus,
               sub.Name AS SubmittedBy, wo.CreatedAt, wo.SubmittedAt,
               wo.ReviewedAt, wo.ReviewNote,
               ar.GrandTotal AS AdvanceGrandTotal,
               -- 本單之前已沖銷金額：條件與 WriteOffRefundCalculator.PriorApprovedTotalAsync 一致
               -- （已核准且核准時間早於本單；本單未核准則全部已核准者；舊資料 ReviewedAt 為 null 視為更早）
               ISNULL((SELECT SUM(w2.GrandTotal) FROM WriteOffRecords w2
                       WHERE w2.AdvanceRequestId = wo.AdvanceRequestId
                         AND w2.ApprovalStatus = 'approved'
                         AND w2.Id <> wo.Id
                         AND (wo.ReviewedAt IS NULL
                           OR w2.ReviewedAt IS NULL
                           OR w2.ReviewedAt < wo.ReviewedAt
                           OR (w2.ReviewedAt = wo.ReviewedAt AND w2.Id < wo.Id))), 0) AS AdvanceWrittenOffTotal,
               -- 他單已核准的繳回合計（不含本單）；與 AdvanceWrittenOffTotal 不同，繳回不分前後序，全部他單皆計
               ISNULL((SELECT SUM(w3.ReturnedAmount) FROM WriteOffRecords w3
                       WHERE w3.AdvanceRequestId = wo.AdvanceRequestId
                         AND w3.ApprovalStatus = 'approved'
                         AND w3.Id <> wo.Id), 0) AS AdvanceReturnedTotal,
               wo.ReturnedAmount, wo.ReturnedAt, wo.ReturnedProofFileName, wo.ReturnedProofUrl,
               {{OverspendPaidSql("wo")}},
               CAST(ISNULL(ar.IsClosed, 0) AS BIT) AS AdvanceIsClosed,
               ar.ClosedAt AS AdvanceClosedAt,
               ar.EstimatedRefundDate, ar.RefundedAt,
               ar.RefundAmount AS AdvanceRefundAmount,
               ar.RefundedAmount AS AdvanceRefundedAmount,
               wi.Id AS ItemId, wi.Category, wi.SeqNo, wi.ItemName,
               wi.UnitPrice, wi.Quantity, wi.TotalPrice,
               wi.CashAmount AS ItemCash, wi.CheckAmount AS ItemCheck,
               wi.Note AS ItemNote, wi.InvoiceNo AS ItemInvoiceNo,
               wi.FileName AS ItemFileName, wi.FileUrl AS ItemFileUrl, wi.SortOrder,
               wi.InvoiceDate AS ItemInvoiceDate,
               wi.CheckPaid AS ItemCheckPaid, wi.CheckPaidAt AS ItemCheckPaidAt,
               cpb.Name AS ItemCheckPaidBy
        FROM WriteOffRecords wo
        LEFT JOIN AdvanceRequests ar ON wo.AdvanceRequestId = ar.Id
        LEFT JOIN Projects proj      ON ar.ProjectId        = proj.Id
        LEFT JOIN Users sub          ON wo.SubmittedById    = sub.Id
        LEFT JOIN WriteOffItems wi   ON wi.WriteOffRecordId = wo.Id
        LEFT JOIN Users cpb          ON wi.CheckPaidById    = cpb.Id
        """;

    public async Task<PagedResult<WriteOffRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM WriteOffRecords {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM WriteOffRecords
                {userFilter}
                ORDER BY COALESCE(SubmittedAt, CreatedAt) DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE wo.Id IN (SELECT Id FROM PagedIds) ORDER BY COALESCE(wo.SubmittedAt, wo.CreatedAt) DESC, wi.SortOrder, wi.Id
            """;

        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        return new PagedResult<WriteOffRequestDto>(
            GroupToWriteOffRequests(rows),
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<WriteOffRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE wo.Id = @Id ORDER BY wi.SortOrder, wi.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });

        var dto = GroupToWriteOffRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'write_off' AND rdr.RequestId = @RequestId
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

        // 整單批次附件（獨立查詢，避免與 items JOIN 笛卡兒相乘）
        const string attSql = """
            SELECT Id, FileName, FileUrl
            FROM WriteOffAttachments
            WHERE WriteOffRecordId = @RequestId
            ORDER BY SortOrder
            """;
        var attRows = await db.QueryAsync<dynamic>(attSql, new { RequestId = id });
        var attachments = attRows.Select(r => new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl)).ToArray();

        // 預支批次 + 各次沖銷 + 分期撥款（獨立查詢，避免與 items JOIN 造成笛卡兒相乘）
        var (advanceRounds, advanceItems) = await GetAdvanceRoundsAsync(dto.AdvanceRequestId);
        var writeOffHistory = await GetWriteOffHistoryAsync(dto.AdvanceRequestId, id);
        var refundDue       = WriteOffRefundCalculator.Calculate(
            dto.AdvanceGrandTotal, dto.AdvanceWrittenOffTotal, dto.GrandTotal);

        var ownInst = (await installments.GetByParentIdsAsync(InstallmentParentTable.WriteOffRecord, [id]))
                      .GetValueOrDefault(id, []);
        var advInst = (await installments.GetByParentIdsAsync(InstallmentParentTable.AdvanceRequest, [dto.AdvanceRequestId]))
                      .GetValueOrDefault(dto.AdvanceRequestId, []);

        return dto with
        {
            DesignatedReviewers  = designatedReviewers.Length > 0 ? designatedReviewers : null,
            Attachments          = attachments.Length > 0 ? attachments : null,
            AdvanceRounds        = advanceRounds,
            WriteOffHistory      = writeOffHistory,
            AdvanceItems         = advanceItems,
            RefundDue            = refundDue,
            Installments         = ownInst.Count > 0 ? [.. ownInst] : null,
            PaymentStatus        = installments.ComputeStatus(ownInst),
            AdvanceInstallments  = advInst.Count > 0 ? [.. advInst] : null,
            AdvancePaymentStatus = installments.ComputeStatus(advInst),
        };
    }

    /// <summary>
    /// 同一張預支單底下的全部沖銷單（含明細 / 指定審核者 / 附件 / 差額撥款分期），供依預支單彙總檢視使用。
    /// 子表一次撈回後於記憶體分派，不逐單查詢。預支批次與沖銷歷程由呼叫端從預支單本身取得，故不重複填入。
    /// </summary>
    public async Task<WriteOffRequestDto[]> GetByAdvanceIdAsync(int advanceRequestId)
    {
        var sql = BaseSql + " WHERE wo.AdvanceRequestId = @Id ORDER BY wo.WriteOffNo, wo.Id, wi.SortOrder, wi.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = advanceRequestId });

        var dtos = GroupToWriteOffRequests(rows).OrderBy(x => x.WriteOffNo).ThenBy(x => x.Id).ToList();
        if (dtos.Count == 0) return [];

        var ids = dtos.Select(d => d.Id).ToArray();

        const string drSql = """
            SELECT rdr.RequestId, rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'write_off' AND rdr.RequestId IN @Ids
            ORDER BY rdr.RequestId, rdr.StepOrder
            """;
        var drRows = await db.QueryAsync<dynamic>(drSql, new { Ids = ids });
        var drLookup = drRows.ToLookup(r => (int)r.RequestId, r => new DesignatedReviewerDto(
            (int)r.Id,
            (Guid)r.ReviewerId,
            (string)r.ReviewerName,
            (int)r.StepOrder,
            (string)r.Status,
            (DateTime?)r.ReviewedAt,
            (string?)r.Comment));

        const string attSql = """
            SELECT WriteOffRecordId, Id, FileName, FileUrl
            FROM WriteOffAttachments
            WHERE WriteOffRecordId IN @Ids
            ORDER BY WriteOffRecordId, SortOrder
            """;
        var attRows = await db.QueryAsync<dynamic>(attSql, new { Ids = ids });
        var attLookup = attRows.ToLookup(r => (int)r.WriteOffRecordId,
            r => new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl));

        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.WriteOffRecord, ids);

        return [.. dtos.Select(dto =>
        {
            var dr   = drLookup[dto.Id].ToArray();
            var att  = attLookup[dto.Id].ToArray();
            var inst = instDict.GetValueOrDefault(dto.Id, []);
            return dto with
            {
                DesignatedReviewers = dr.Length  > 0 ? dr  : null,
                Attachments         = att.Length > 0 ? att : null,
                RefundDue           = WriteOffRefundCalculator.Calculate(
                                          dto.AdvanceGrandTotal, dto.AdvanceWrittenOffTotal, dto.GrandTotal),
                Installments        = inst.Count > 0 ? [.. inst] : null,
                PaymentStatus       = installments.ComputeStatus(inst),
            };
        })];
    }

    // ── 批次金額檢視 helpers ─────────────────────────────────────────────────

    /// <summary>關聯預支單的各預支批次（含追加）；批次組裝規則與 GET /advance-requests/{id} 共用同一份實作。</summary>
    private async Task<(AdvanceRoundDto[] Rounds, AdvanceRequestItemDto[] Items)> GetAdvanceRoundsAsync(int advanceRequestId)
    {
        const string headSql = "SELECT AdvanceDate, AdvanceNeededDate FROM AdvanceRequests WHERE Id = @Id";
        var head = await db.QuerySingleOrDefaultAsync<dynamic>(headSql, new { Id = advanceRequestId });
        if (head is null) return ([], []);
        var advanceDate       = (DateTime)head.AdvanceDate;
        var advanceNeededDate = (DateTime?)head.AdvanceNeededDate;

        const string itemSql = """
            SELECT Id, Category, SeqNo, ItemName, UnitPrice, Quantity, TotalPrice,
                   CashAmount, CheckAmount, Note, SortOrder, FileName, FileUrl, RoundNo
            FROM AdvanceRequestItems
            WHERE AdvanceRequestId = @Id
            ORDER BY RoundNo, SortOrder, Id
            """;
        var itemRows = await db.QueryAsync<dynamic>(itemSql, new { Id = advanceRequestId });
        var items = itemRows.Select(r => new AdvanceRequestItemDto(
            (int)r.Id, (string)r.Category, (int)r.SeqNo, (string)r.ItemName,
            (decimal)r.UnitPrice, (string)r.Quantity, (decimal)r.TotalPrice,
            (decimal)r.CashAmount, (decimal)r.CheckAmount, (string?)r.Note, (int)r.SortOrder,
            (string?)r.FileName, (string?)r.FileUrl, (int)r.RoundNo)).ToList();

        const string supSql = """
            SELECT RoundNo, AdvanceDate, AdvanceNeededDate, Reason
            FROM AdvanceRequestSupplements
            WHERE AdvanceRequestId = @Id
            ORDER BY RoundNo
            """;
        var supRows = await db.QueryAsync<dynamic>(supSql, new { Id = advanceRequestId });

        return (AdvanceRequestReadService.BuildRounds(advanceDate, advanceNeededDate, supRows, items), [.. items]);
    }

    /// <summary>
    /// 同一張預支單底下的各次沖銷（含本單；已拒絕的不列入），附「是否過財務」；
    /// 非本單列另帶前次沖銷明細 Items（本單明細已在主 DTO，不重複）。
    /// </summary>
    private async Task<WriteOffRoundDto[]> GetWriteOffHistoryAsync(int advanceRequestId, int currentId)
    {
        var dict = await GetHistoryForAdvancesAsync([advanceRequestId], currentId);
        return dict.GetValueOrDefault(advanceRequestId, []);
    }

    /// <summary>
    /// 批次版沖銷歷程（依預支單 Id 分組；已拒絕不列入）。<paramref name="currentId"/> 為本單 Id（無則 null → 全部 IsCurrent=false），
    /// 本單列 Items 留 null、其餘列填明細（單次 WriteOffItems 查詢）。available-advances 與沖銷詳情共用。
    /// </summary>
    public async Task<Dictionary<int, WriteOffRoundDto[]>> GetHistoryForAdvancesAsync(
        IEnumerable<int> advanceRequestIds, int? currentId = null)
    {
        var advIds = advanceRequestIds.Distinct().ToArray();
        if (advIds.Length == 0) return [];

        var sql = $"""
            SELECT wo.Id, wo.AdvanceRequestId, wo.WriteOffNo, wo.RequestNo, wo.GrandTotal, wo.ApprovalStatus,
                   wo.CreatedAt, wo.ReturnedAmount, wo.ReturnedAt,
                   {FinancePassedSql("wo")}
            FROM WriteOffRecords wo
            WHERE wo.AdvanceRequestId IN @AdvIds AND wo.ApprovalStatus <> 'rejected'
            ORDER BY wo.AdvanceRequestId, wo.WriteOffNo, wo.Id
            """;
        var rows = (await db.QueryAsync<dynamic>(sql, new { AdvIds = advIds, FinanceStepCodes = FinanceStepCodesParam })).ToList();

        var itemLookup = await LoadItemsAsync(
            rows.Where(r => (int)r.Id != currentId).Select(r => (int)r.Id));

        return rows.GroupBy(r => (int)r.AdvanceRequestId).ToDictionary(
            g => g.Key,
            g => g.Select(r =>
            {
                int id = (int)r.Id;
                bool isCurrent = id == currentId;
                return new WriteOffRoundDto(
                    id, (int)r.WriteOffNo, (string?)r.RequestNo, (decimal)r.GrandTotal,
                    (string)r.ApprovalStatus, (DateTime)r.CreatedAt, isCurrent,
                    (decimal?)r.ReturnedAmount, (DateTime?)r.ReturnedAt,
                    (int)r.FinancePassed == 1,
                    isCurrent ? null : [.. itemLookup[id]]);
            }).ToArray());
    }

    /// <summary>以 WriteOffRecordId 批次載入沖銷明細（WriteOffItemDto 欄位對應與簽核頁相同）。</summary>
    private async Task<ILookup<int, WriteOffItemDto>> LoadItemsAsync(IEnumerable<int> woIds)
    {
        var ids = woIds.Distinct().ToArray();
        if (ids.Length == 0) return Enumerable.Empty<WriteOffItemDto>().ToLookup(_ => 0);
        const string sql = """
            SELECT wi.Id, wi.WriteOffRecordId, wi.Category, wi.SeqNo, wi.ItemName,
                   wi.UnitPrice, wi.Quantity, wi.TotalPrice, wi.CashAmount, wi.CheckAmount,
                   wi.Note, wi.InvoiceNo, wi.FileName, wi.FileUrl, wi.SortOrder, wi.InvoiceDate,
                   wi.CheckPaid, wi.CheckPaidAt, cpb.Name AS CheckPaidBy
            FROM WriteOffItems wi
            LEFT JOIN Users cpb ON wi.CheckPaidById = cpb.Id
            WHERE wi.WriteOffRecordId IN @Ids
            ORDER BY wi.WriteOffRecordId, wi.SortOrder, wi.Id
            """;
        var rows = await db.QueryAsync<dynamic>(sql, new { Ids = ids });
        return rows.ToLookup(r => (int)r.WriteOffRecordId, r => new WriteOffItemDto(
            (int)r.Id, (string)r.Category, (int)r.SeqNo, (string)r.ItemName,
            (decimal)r.UnitPrice, (string)r.Quantity, (decimal)r.TotalPrice,
            (decimal)r.CashAmount, (decimal)r.CheckAmount,
            (string?)r.Note, (string?)r.InvoiceNo,
            (string?)r.FileName, (string?)r.FileUrl, (int)r.SortOrder,
            (DateTime?)r.InvoiceDate,
            (bool)r.CheckPaid, (DateTime?)r.CheckPaidAt, (string?)r.CheckPaidBy));
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static IEnumerable<WriteOffRequestDto> GroupToWriteOffRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic wo, List<WriteOffItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new WriteOffItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (decimal)row.ItemCash,
                    (decimal)row.ItemCheck,
                    (string?)row.ItemNote,
                    (string?)row.ItemInvoiceNo,
                    (string?)row.ItemFileName,
                    (string?)row.ItemFileUrl,
                    (int)row.SortOrder,
                    (DateTime?)row.ItemInvoiceDate,
                    (bool)row.ItemCheckPaid,
                    (DateTime?)row.ItemCheckPaidAt,
                    (string?)row.ItemCheckPaidBy));
        }

        return dict.Values.Select(x => new WriteOffRequestDto(
            (int)x.wo.Id,
            (string?)x.wo.RequestNo,
            (int)x.wo.AdvanceRequestId,
            (string)x.wo.AdvanceRequestNo,
            (int)x.wo.WriteOffNo,
            (string)x.wo.ProjectCode,
            (string)x.wo.ProjectName,
            (string)x.wo.ActivityName,
            (string)x.wo.ActivityPeriod,
            (decimal)x.wo.CashTotal,
            (decimal)x.wo.CheckTotal,
            (decimal)x.wo.GrandTotal,
            (string?)x.wo.Note,
            (string)x.wo.ApprovalStatus,
            (string?)x.wo.SubmittedBy,
            (DateTime)x.wo.CreatedAt,
            (DateTime?)x.wo.SubmittedAt,
            (DateTime?)x.wo.ReviewedAt,
            (string?)x.wo.ReviewNote,
            [.. x.items],
            null, // DesignatedReviewers 以 null 回傳（GetByIdAsync 才填入）
            (decimal)x.wo.AdvanceGrandTotal,
            (decimal)x.wo.AdvanceWrittenOffTotal,
            (bool)x.wo.AdvanceIsClosed,
            (DateTime?)x.wo.EstimatedRefundDate,
            (DateTime?)x.wo.RefundedAt,
            (decimal?)x.wo.AdvanceRefundAmount,
            (decimal?)x.wo.AdvanceRefundedAmount,
            AdvanceClosedAt: (DateTime?)x.wo.AdvanceClosedAt,
            ReturnedAmount:        (decimal?)x.wo.ReturnedAmount,
            ReturnedAt:            (DateTime?)x.wo.ReturnedAt,
            ReturnedProofFileName: (string?)x.wo.ReturnedProofFileName,
            ReturnedProofUrl:      (string?)x.wo.ReturnedProofUrl,
            AdvanceReturnedTotal:  (decimal)x.wo.AdvanceReturnedTotal,
            OverspendPaidTotal:    (decimal)x.wo.OverspendPaidTotal,
            OverspendLastPaidAt:   (DateTime?)x.wo.OverspendLastPaidAt));
    }
}
