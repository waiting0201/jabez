using Dapper;
using Jabez.Api.Common;
using Jabez.Api.Models.Dtos;
using System.Data;

namespace Jabez.Api.Services.Dapper;

public sealed class TravelRequestReadService(IDbConnection db, IInstallmentReadService installments) : ITravelRequestReadService
{
    private const string BaseSql = """
        SELECT tr.Id, tr.RequestNo, u.Name AS EmployeeName,
               tr.Destination, tr.StartDate, tr.EndDate,
               tr.GrandTotal, tr.Purpose,
               tr.ProjectId, proj.Code AS ProjectCode, proj.Name AS ProjectName,
               tr.IsHolidayTravel, tr.HolidayDays,
               tr.ApprovalStatus, tr.CreatedAt, tr.ReviewedAt, tr.ReviewNote,
               tr.ApprovalItemId, tr.CurrentStepOrder, tr.ReviewedById,
               tr.IsClosed, tr.ClosedAt, tr.RefundAmount, tr.RefundedAmount,
               tr.EstimatedPaymentDate, tr.PaidAt, tr.EstimatedRefundDate, tr.RefundedAt,
               ti.Id AS ItemId, ti.Category, ti.SeqNo, ti.ItemName,
               ti.UnitPrice, ti.Quantity, ti.TotalPrice,
               ti.Note AS ItemNote, ti.SortOrder,
               ti.InvoiceNo, ti.FileName AS ItemFileName, ti.FileUrl AS ItemFileUrl, ti.InvoiceDate AS ItemInvoiceDate
        FROM TravelRequests tr
        LEFT JOIN Users u             ON tr.EmployeeId = u.Id
        LEFT JOIN Projects proj       ON tr.ProjectId  = proj.Id
        LEFT JOIN TravelRequestItems ti ON ti.TravelRequestId = tr.Id
        """;

    public async Task<IEnumerable<TravelRequestDto>> GetAllAsync()
    {
        const string sql = BaseSql + " ORDER BY tr.CreatedAt DESC, ti.SortOrder, ti.Id";
        var rows = await db.QueryAsync<dynamic>(sql);
        return GroupToTravelRequests(rows);
    }

    public async Task<PagedResult<TravelRequestDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null, bool? isHolidayTravel = null)
    {
        // 動態組合 WHERE 條件
        var conditions = new List<string>();
        if (userId.HasValue)         conditions.Add("EmployeeId = @UserId");
        if (isHolidayTravel.HasValue) conditions.Add("IsHolidayTravel = @IsHolidayTravel");
        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var countSql = $"SELECT COUNT(*) FROM TravelRequests {whereClause}";
        var sql = $"""
            WITH PagedIds AS (
                SELECT Id FROM TravelRequests
                {whereClause}
                ORDER BY CreatedAt DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
            )
            {BaseSql} WHERE tr.Id IN (SELECT Id FROM PagedIds) ORDER BY tr.CreatedAt DESC, ti.SortOrder, ti.Id
            """;
        var parameters = new { UserId = userId, IsHolidayTravel = isHolidayTravel, Skip = (page - 1) * pageSize, Take = pageSize };
        int total = await db.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await db.QueryAsync<dynamic>(sql, parameters);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        var dtos = GroupToTravelRequests(rows).ToList();
        var idsForInst = dtos.Select(d => d.Id).ToList();
        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelRequest, idsForInst);
        var withStatus = dtos.Select(d => d with { PaymentStatus = installments.ComputeStatus(instDict.GetValueOrDefault(d.Id, [])) });
        return new PagedResult<TravelRequestDto>(
            withStatus,
            total, page, pageSize, Math.Max(1, totalPages));
    }

    public async Task<TravelRequestDto?> GetByIdAsync(int id)
    {
        var sql = BaseSql + " WHERE tr.Id = @Id ORDER BY ti.SortOrder, ti.Id";
        var rows = await db.QueryAsync<dynamic>(sql, new { Id = id });
        var dto = GroupToTravelRequests(rows).FirstOrDefault();
        if (dto is null) return null;

        // 額外查詢指定審核者（GetByIdAsync 才需要，列表查詢不包含）
        const string drSql = """
            SELECT rdr.Id, rdr.ReviewerId, u.Name AS ReviewerName,
                   rdr.StepOrder, rdr.Status, rdr.ReviewedAt, rdr.Comment
            FROM RequestDesignatedReviewers rdr
            JOIN Users u ON rdr.ReviewerId = u.Id
            WHERE rdr.RequestType = 'travel' AND rdr.RequestId = @RequestId
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

        // 額外查詢出差參與者（假日執行活動才需要，但統一回傳）
        const string participantSql = """
            SELECT trp.UserId, u.Name AS UserName, trp.SortOrder
            FROM TravelRequestParticipants trp
            JOIN Users u ON trp.UserId = u.Id
            WHERE trp.TravelRequestId = @TravelRequestId
            ORDER BY trp.SortOrder
            """;
        var participantRows = await db.QueryAsync<dynamic>(participantSql, new { TravelRequestId = id });
        var participants = participantRows.Select(r => new ParticipantDto(
            (Guid)r.UserId,
            (string)r.UserName,
            (int)r.SortOrder)).ToArray();

        var instDict = await installments.GetByParentIdsAsync(InstallmentParentTable.TravelRequest, new[] { id });
        var instList = instDict.GetValueOrDefault(id, []);

        return dto with
        {
            DesignatedReviewers = designatedReviewers.Length > 0 ? designatedReviewers : null,
            Participants        = participants.Length > 0 ? participants : null,
            Installments        = instList.Count > 0 ? instList.ToArray() : null,
            PaymentStatus       = installments.ComputeStatus(instList),
        };
    }

    // ── Grouping helpers ─────────────────────────────────────────────────────

    private static IEnumerable<TravelRequestDto> GroupToTravelRequests(IEnumerable<dynamic> rows)
    {
        var dict = new Dictionary<int, (dynamic tr, List<TravelRequestItemDto> items)>();
        foreach (var row in rows)
        {
            int id = (int)row.Id;
            if (!dict.ContainsKey(id))
                dict[id] = (row, []);
            if (row.ItemId is not null)
                dict[id].items.Add(new TravelRequestItemDto(
                    (int)row.ItemId,
                    (string)row.Category,
                    (int)row.SeqNo,
                    (string)row.ItemName,
                    (decimal)row.UnitPrice,
                    (string)row.Quantity,
                    (decimal)row.TotalPrice,
                    (string?)row.ItemNote,
                    (int)row.SortOrder,
                    (string?)row.InvoiceNo,
                    (string?)row.ItemFileName,
                    (string?)row.ItemFileUrl,
                    (DateTime?)row.ItemInvoiceDate));
        }

        return dict.Values.Select(x =>
        {
            var tr    = x.tr;
            TravelRequestItemDto[]? items = x.items.Count > 0 ? [.. x.items] : null;
            return new TravelRequestDto(
                (int)tr.Id,
                (string)tr.RequestNo,
                (string?)tr.EmployeeName ?? "—",
                (string)tr.Destination,
                (DateTime)tr.StartDate,
                (DateTime)tr.EndDate,
                (decimal)tr.GrandTotal,
                (string)tr.Purpose,
                (int?)tr.ProjectId,
                (string?)tr.ProjectCode,
                (string?)tr.ProjectName,
                (bool)tr.IsHolidayTravel,
                (string)tr.ApprovalStatus,
                (DateTime)tr.CreatedAt,
                (DateTime?)tr.ReviewedAt,
                (string?)tr.ReviewNote,
                ApprovalItemId:   (int?)tr.ApprovalItemId,
                CurrentStepOrder: (int?)tr.CurrentStepOrder,
                ReviewedById:     (Guid?)tr.ReviewedById,
                Items:            items,
                IsClosed:                (bool)tr.IsClosed,
                ClosedAt:                (DateTime?)tr.ClosedAt,
                RefundAmount:            (decimal?)tr.RefundAmount,
                RefundedAmount:          (decimal?)tr.RefundedAmount,
                EstimatedPaymentDate:    (DateTime?)tr.EstimatedPaymentDate,
                PaidAt:                  (DateTime?)tr.PaidAt,
                EstimatedRefundDate:     (DateTime?)tr.EstimatedRefundDate,
                RefundedAt:              (DateTime?)tr.RefundedAt,
                HolidayDays:             (int)tr.HolidayDays);
        });
    }
}
