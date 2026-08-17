using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class WriteOffRequestReadService(IDbConnection db, IInstallmentReadService installments) : IWriteOffRequestReadService
{
    // 預支沖銷申請主查詢：WriteOffRecords 關聯 AdvanceRequests、Projects、Users、WriteOffItems
    private const string BaseSql = """
        SELECT wo.Id, wo.RequestNo, wo.AdvanceRequestId, ar.RequestNo AS AdvanceRequestNo,
               wo.WriteOffNo, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               ar.ActivityName, ar.ActivityPeriod,
               wo.CashTotal, wo.CheckTotal, wo.GrandTotal,
               wo.Note, wo.ApprovalStatus,
               sub.Name AS SubmittedBy, wo.CreatedAt,
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
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE wo.Id IN (SELECT Id FROM PagedIds) ORDER BY wo.CreatedAt DESC, wi.SortOrder, wi.Id
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
        var advanceRounds   = await GetAdvanceRoundsAsync(dto.AdvanceRequestId);
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
    private async Task<AdvanceRoundDto[]> GetAdvanceRoundsAsync(int advanceRequestId)
    {
        const string headSql = "SELECT AdvanceDate FROM AdvanceRequests WHERE Id = @Id";
        var advanceDate = await db.ExecuteScalarAsync<DateTime?>(headSql, new { Id = advanceRequestId });
        if (advanceDate is null) return [];

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
            SELECT RoundNo, AdvanceDate, Reason
            FROM AdvanceRequestSupplements
            WHERE AdvanceRequestId = @Id
            ORDER BY RoundNo
            """;
        var supRows = await db.QueryAsync<dynamic>(supSql, new { Id = advanceRequestId });

        return AdvanceRequestReadService.BuildRounds(advanceDate.Value, supRows, items);
    }

    /// <summary>同一張預支單底下的各次沖銷（含本單；已拒絕的不列入）。</summary>
    private async Task<WriteOffRoundDto[]> GetWriteOffHistoryAsync(int advanceRequestId, int currentId)
    {
        const string sql = """
            SELECT Id, WriteOffNo, RequestNo, GrandTotal, ApprovalStatus, CreatedAt
            FROM WriteOffRecords
            WHERE AdvanceRequestId = @AdvanceRequestId AND ApprovalStatus <> 'rejected'
            ORDER BY WriteOffNo, Id
            """;
        var rows = await db.QueryAsync<dynamic>(sql, new { AdvanceRequestId = advanceRequestId });
        return [.. rows.Select(r => new WriteOffRoundDto(
            (int)r.Id,
            (int)r.WriteOffNo,
            (string)r.RequestNo,
            (decimal)r.GrandTotal,
            (string)r.ApprovalStatus,
            (DateTime)r.CreatedAt,
            (int)r.Id == currentId))];
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
            (string)x.wo.RequestNo,
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
            AdvanceClosedAt: (DateTime?)x.wo.AdvanceClosedAt));
    }
}
