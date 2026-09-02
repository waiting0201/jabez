using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class AdvanceRequestReadService(IDbConnection db, IInstallmentReadService installments) : IAdvanceRequestReadService
{
    private const string BaseSql = """
        SELECT ar.Id, ar.RequestNo, ar.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               ar.ActivityName, ar.ActivityPeriod, ar.AdvanceDate, ar.AdvanceNeededDate,
               ar.CashTotal, ar.CheckTotal, ar.GrandTotal,
               ar.ApprovalStatus, ar.CurrentRoundNo,
               sub.Name AS SubmittedBy, ar.CreatedAt, ar.SubmittedAt,
               ar.ReviewedAt, ar.ReviewNote,
               ar.IsClosed, ar.ClosedAt, ar.RefundAmount, ar.RefundedAmount, ar.EstimatedRefundDate, ar.RefundedAt,
               ai.Id AS ItemId, ai.RoundNo AS ItemRoundNo, ai.Category, ai.SeqNo, ai.ItemName,
               ai.UnitPrice, ai.Quantity, ai.TotalPrice,
               ai.CashAmount AS ItemCash, ai.CheckAmount AS ItemCheck,
               ai.Note AS ItemNote, ai.SortOrder,
               ai.FileName AS ItemFileName, ai.FileUrl AS ItemFileUrl
        FROM AdvanceRequests ar
        LEFT JOIN Projects proj          ON ar.ProjectId    = proj.Id
        LEFT JOIN Users   sub            ON ar.SubmittedById = sub.Id
        LEFT JOIN AdvanceRequestItems ai ON ai.AdvanceRequestId = ar.Id
        """;

    public async Task<PagedResult<AdvanceRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM AdvanceRequests {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM AdvanceRequests
                {userFilter}
                ORDER BY COALESCE(SubmittedAt, CreatedAt) DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE ar.Id IN (SELECT Id FROM PagedIds) ORDER BY COALESCE(ar.SubmittedAt, ar.CreatedAt) DESC, ai.RoundNo, ai.SortOrder, ai.Id
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });

        // 取得沖銷摘要
        var ids = rows.Select(r => (int)r.Id).Distinct().ToArray();
        var writeOffSummaries = ids.Length > 0
            ? await db.QueryAsync<dynamic>(
                "SELECT Id, AdvanceRequestId, WriteOffNo, GrandTotal, CreatedAt FROM WriteOffRecords WHERE AdvanceRequestId IN @Ids ORDER BY WriteOffNo",
                new { Ids = ids })
            : Enumerable.Empty<dynamic>();

        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        var dtos = GroupToAdvanceRequests(rows, writeOffSummaries).ToList();
        var idsForInst = dtos.Select(d => d.Id).ToList();
        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.AdvanceRequest, idsForInst);
        var withStatus = dtos.Select(d => d with { PaymentStatus = installments.ComputeStatus(instDict.GetValueOrDefault(d.Id, [])) });
        return new PagedResult<AdvanceRequestDto>(
            withStatus,
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<AdvanceRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE ar.Id = @Id ORDER BY ai.RoundNo, ai.SortOrder, ai.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var writeOffSummaries = await db.QueryAsync<dynamic>(
            "SELECT Id, AdvanceRequestId, WriteOffNo, GrandTotal, CreatedAt FROM WriteOffRecords WHERE AdvanceRequestId = @Id ORDER BY WriteOffNo",
            new { Id = id });
        var dto = GroupToAdvanceRequests(rows, writeOffSummaries).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'advance' AND rdr.RequestId = @RequestId
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

        // 查詢沖銷紀錄含明細（PDF 列印用）
        const string woDetailSql = """
            SELECT wo.Id, wo.RequestNo, wo.WriteOffNo, wo.CashTotal, wo.CheckTotal,
                   wo.GrandTotal, wo.ApprovalStatus, wo.Note,
                   sub.Name AS SubmittedBy, wo.CreatedAt,
                   wi.Id AS ItemId, wi.Category, wi.SeqNo, wi.ItemName,
                   wi.UnitPrice, wi.Quantity, wi.TotalPrice,
                   wi.CashAmount AS ItemCash, wi.CheckAmount AS ItemCheck,
                   wi.Note AS ItemNote, wi.InvoiceNo, wi.FileName, wi.FileUrl,
                   wi.SortOrder, wi.InvoiceDate
            FROM WriteOffRecords wo
            LEFT JOIN Users sub ON wo.SubmittedById = sub.Id
            LEFT JOIN WriteOffItems wi ON wi.WriteOffRecordId = wo.Id
            WHERE wo.AdvanceRequestId = @Id
            ORDER BY wo.WriteOffNo, wi.SortOrder, wi.Id
            """;
        var woDetailRows = await db.QueryAsync<dynamic>(woDetailSql, new { Id = id });
        var writeOffRecords = GroupToWriteOffRecords(woDetailRows);

        // 載入分期撥款明細
        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.AdvanceRequest, new[] { id });
        var instList = instDict.GetValueOrDefault(id, []);

        // 追加預支批次（RoundNo ≥ 2；Round 1 = 父單本身）
        const string supSql = """
            SELECT s.RoundNo, s.AdvanceDate, s.AdvanceNeededDate, s.Reason
            FROM AdvanceRequestSupplements s
            WHERE s.AdvanceRequestId = @Id
            ORDER BY s.RoundNo
            """;
        var supRows = await db.QueryAsync<dynamic>(supSql, new { Id = id });

        return dto with
        {
            DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null,
            WriteOffRecords = writeOffRecords.Length > 0 ? writeOffRecords : null,
            Installments = instList.Count > 0 ? instList.ToArray() : null,
            PaymentStatus = installments.ComputeStatus(instList),
            Rounds = BuildRounds(dto.AdvanceDate, dto.AdvanceNeededDate, supRows, dto.Items),
        };
    }

    /// <summary>
    /// 組出各預支批次：Round 1 取父單 AdvanceDate / AdvanceNeededDate，Round ≥2 取 AdvanceRequestSupplements；
    /// 金額一律由該批次的明細加總推導（不讀任何金額快取欄位）。
    /// </summary>
    internal static AdvanceRoundDto[] BuildRounds(
        DateTime advanceDate, DateTime? advanceNeededDate,
        IEnumerable<dynamic> supplementRows, IEnumerable<AdvanceRequestItemDto> items)
    {
        var byRound = items.GroupBy(i => i.RoundNo).ToDictionary(g => g.Key, g => g.ToList());

        var rounds = new List<AdvanceRoundDto> { BuildRound(1, advanceDate, advanceNeededDate, null, byRound) };
        foreach (var row in supplementRows)
            rounds.Add(BuildRound((int)row.RoundNo, (DateTime)row.AdvanceDate,
                                  (DateTime?)row.AdvanceNeededDate, (string?)row.Reason, byRound));

        return [.. rounds.OrderBy(r => r.RoundNo)];
    }

    private static AdvanceRoundDto BuildRound(
        int roundNo, DateTime advanceDate, DateTime? advanceNeededDate, string? reason,
        Dictionary<int, List<AdvanceRequestItemDto>> byRound)
    {
        var list = byRound.GetValueOrDefault(roundNo, []);
        return new AdvanceRoundDto(
            roundNo, advanceDate, reason,
            list.Sum(i => i.CashAmount),
            list.Sum(i => i.CheckAmount),
            list.Sum(i => i.TotalPrice),
            list.Count,
            advanceNeededDate);
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static IEnumerable<AdvanceRequestDto> GroupToAdvanceRequests(
        IEnumerable<dynamic> rows, IEnumerable<dynamic> writeOffRows)
    {
        var dict = new Dictionary<int, (dynamic ar, List<AdvanceRequestItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new AdvanceRequestItemDto(
                    (int)row.ItemId, (string)row.Category, (int)row.SeqNo,
                    (string)row.ItemName, (decimal)row.UnitPrice, (string)row.Quantity,
                    (decimal)row.TotalPrice, (decimal)row.ItemCash, (decimal)row.ItemCheck,
                    (string?)row.ItemNote, (int)row.SortOrder,
                    (string?)row.ItemFileName, (string?)row.ItemFileUrl,
                    (int)row.ItemRoundNo));
        }

        // 沖銷摘要 grouped by AdvanceRequestId
        var woDict = new Dictionary<int, List<WriteOffSummaryDto>>();
        foreach (var row in writeOffRows)
        {
            int arId = (int)row.AdvanceRequestId;
            if (!woDict.ContainsKey(arId))
                woDict[arId] = [];
            woDict[arId].Add(new WriteOffSummaryDto(
                (int)row.Id, (int)row.WriteOffNo,
                (decimal)row.GrandTotal, (DateTime)row.CreatedAt));
        }

        return dict.Values.Select(x =>
        {
            int id = (int)x.ar.Id;
            var wos = woDict.TryGetValue(id, out var list) ? [.. list] : Array.Empty<WriteOffSummaryDto>();
            return new AdvanceRequestDto(
                id,
                (string?)x.ar.RequestNo,
                (int)x.ar.ProjectId,
                (string)x.ar.ProjectCode,
                (string)x.ar.ProjectName,
                (string)x.ar.ActivityName,
                (string)x.ar.ActivityPeriod,
                (DateTime)x.ar.AdvanceDate,
                (decimal)x.ar.CashTotal,
                (decimal)x.ar.CheckTotal,
                (decimal)x.ar.GrandTotal,
                (string)x.ar.ApprovalStatus,
                (string?)x.ar.SubmittedBy,
                (DateTime)x.ar.CreatedAt,
                (DateTime?)x.ar.SubmittedAt,
                (DateTime?)x.ar.ReviewedAt,
                (string?)x.ar.ReviewNote,
                [.. x.items],
                wos,
                null,                               // DesignatedReviewers 以 null 回傳
                (bool)x.ar.IsClosed,
                (DateTime?)x.ar.ClosedAt,
                (decimal?)x.ar.RefundAmount,
                (decimal?)x.ar.RefundedAmount,
                (DateTime?)x.ar.EstimatedRefundDate,
                (DateTime?)x.ar.RefundedAt,
                null,                               // WriteOffRecords（僅 GetByIdAsync 帶入）
                null,                               // Installments（稍後補上）
                null,                               // PaymentStatus（稍後補上）
                null,                               // Rounds（僅 GetByIdAsync 帶入）
                (int)x.ar.CurrentRoundNo,
                (DateTime?)x.ar.AdvanceNeededDate);
        });
    }

    /// <summary>將沖銷明細查詢結果 group 為 WriteOffRecordDto[]</summary>
    private static WriteOffRecordDto[] GroupToWriteOffRecords(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic wo, List<WriteOffItemDto> items)>();
        foreach (var row in rows)
        {
            int woId = (int)row.Id;
            if (!dict.ContainsKey(woId))
                dict[woId] = (row, []);
            if (row.ItemId is not null)
                dict[woId].items.Add(new WriteOffItemDto(
                    (int)row.ItemId, (string)row.Category, (int)row.SeqNo,
                    (string)row.ItemName, (decimal)row.UnitPrice, (string)row.Quantity,
                    (decimal)row.TotalPrice, (decimal)row.ItemCash, (decimal)row.ItemCheck,
                    (string?)row.ItemNote, (string?)row.InvoiceNo,
                    (string?)row.FileName, (string?)row.FileUrl,
                    (int)row.SortOrder, (DateTime?)row.InvoiceDate));
        }

        return dict.Values
            .OrderBy(x => (int)x.wo.WriteOffNo)
            .Select(x => new WriteOffRecordDto(
                (int)x.wo.Id,
                (string?)x.wo.RequestNo,
                (int)x.wo.WriteOffNo,
                (decimal)x.wo.CashTotal,
                (decimal)x.wo.CheckTotal,
                (decimal)x.wo.GrandTotal,
                (string)x.wo.ApprovalStatus,
                (string?)x.wo.Note,
                (string?)x.wo.SubmittedBy,
                (DateTime)x.wo.CreatedAt,
                [.. x.items]))
            .ToArray();
    }

}
