using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class PreReviewRequestReadService(IDbConnection db) : IPreReviewRequestReadService
{
    // ── 通用 JOIN SQL ─────────────────────────────────────────────────────────
    private const string BaseSql = """
        SELECT prv.Id, prv.RequestNo, prv.Type, prv.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               prv.TotalAmount, prv.TaxAmount, prv.ApprovalStatus,
               sub.Name AS SubmittedBy, prv.CreatedAt,
               prv.ReviewedAt, prv.ReviewNote, prv.Reason,
               prv.VendorId, ven.Name AS VendorName, ven.TaxId AS VendorTaxId,
               pri.Id AS ItemId, pri.FileName, pri.ItemCategory, pri.Amount AS ItemAmount, pri.ItemName, pri.Description AS ItemDescription, pri.Note AS ItemNote, pri.FileUrl AS ItemFileUrl, pri.ItemDate
        FROM PreReviewRequests prv
        LEFT JOIN Projects proj         ON prv.ProjectId    = proj.Id
        LEFT JOIN Users   sub           ON prv.SubmittedById = sub.Id
        LEFT JOIN Vendors ven           ON prv.VendorId      = ven.Id
        LEFT JOIN PreReviewItems pri    ON pri.PreReviewRequestId = prv.Id
        """;

    // ── PreReviewRequest ──────────────────────────────────────────────────────

    public async Task<IEnumerable<PreReviewRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY prv.CreatedAt DESC, pri.Id";
        var rows = await db.QueryAsync<dynamic>(sql);
        return GroupToPreReviewRequests(rows);
    }

    public async Task<PagedResult<PreReviewRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null)
    {
        var userFilter = userId.HasValue ? "WHERE SubmittedById = @UserId" : "";
        var countSql = $"SELECT COUNT(*) FROM PreReviewRequests {userFilter}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM PreReviewRequests
                {userFilter}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE prv.Id IN (SELECT Id FROM PagedIds) ORDER BY prv.CreatedAt DESC, pri.Id
            """;
        int total = await db.ExecuteScalarAsync<int>(countSql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        var rows = await db.QueryAsync<dynamic>(sql, new { UserId = userId, Skip = (page - 1) * pageSize, Take = pageSize });
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        var dtos = GroupToPreReviewRequests(rows).ToList();
        return new PagedResult<PreReviewRequestDto>(dtos, total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<PreReviewRequestDto?> GetByIdAsync(int id)
    {
        const string sql = BaseSql + " WHERE prv.Id = @Id ORDER BY pri.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var dto = GroupToPreReviewRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'pre_review' AND rdr.RequestId = @RequestId
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
            FROM PreReviewRequestAttachments
            WHERE PreReviewRequestId = @Id
            ORDER BY SortOrder
            """;
        var attRows = await db.QueryAsync<dynamic>(attSql, new { Id = id });
        var attachments = attRows.Select(r => new AttachmentDto((int)r.Id, (string)r.FileName, (string?)r.FileUrl)).ToArray();

        return dto with
        {
            DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null,
            Attachments         = attachments.Length > 0 ? attachments : null,
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static IEnumerable<PreReviewRequestDto> GroupToPreReviewRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic prv, List<PreReviewItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);

            if (row.ItemId is not null)
                dict[id].items.Add(new PreReviewItemDto(
                    (int)row.ItemId,
                    (string)row.FileName,
                    (string?)row.ItemCategory,
                    (decimal)row.ItemAmount,
                    (string?)row.ItemName,
                    (string?)row.ItemDescription,
                    (string?)row.ItemNote,
                    (string?)row.ItemFileUrl,
                    (DateTime?)row.ItemDate));
        }

        return dict.Values.Select(x => new PreReviewRequestDto(
            (int)x.prv.Id,
            (string)x.prv.RequestNo,
            (string)x.prv.Type,
            (int)x.prv.ProjectId,
            (string)x.prv.ProjectCode,
            (string)x.prv.ProjectName,
            [.. x.items],
            (decimal)x.prv.TotalAmount,
            (decimal)x.prv.TaxAmount,
            (string)x.prv.ApprovalStatus,
            (string?)x.prv.SubmittedBy,
            (DateTime)x.prv.CreatedAt,
            (DateTime?)x.prv.ReviewedAt,
            (string?)x.prv.ReviewNote,
            (string?)x.prv.Reason,
            null,                          // DesignatedReviewers 以 null 回傳
            (int?)x.prv.VendorId,
            (string?)x.prv.VendorName,
            (string?)x.prv.VendorTaxId));
    }
}
