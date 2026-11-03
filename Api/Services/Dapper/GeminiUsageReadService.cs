using System.Data;
using Dapper;
using Jabez.Api.Models.Dtos;

namespace Jabez.Api.Services.Dapper;

public sealed class GeminiUsageReadService(IDbConnection db) : IGeminiUsageReadService
{
    public async Task<GeminiUsageDto> GetSummaryAsync(DateTime from, DateTime to)
    {
        const string sql = """
            SELECT l.Source,
                   COUNT(*)                         AS Calls,
                   ISNULL(SUM(CAST(l.InputTokens  AS bigint)), 0) AS InputTokens,
                   ISNULL(SUM(CAST(l.OutputTokens AS bigint)), 0) AS OutputTokens,
                   ISNULL(SUM(CAST(l.TotalTokens  AS bigint)), 0) AS TotalTokens
            FROM   GeminiUsageLogs l
            WHERE  l.CreatedAt >= @From AND l.CreatedAt < @To
            GROUP BY l.Source
            ORDER BY l.Source
            """;

        var rows = (await db.QueryAsync<GeminiUsageSourceDto>(sql, new { From = from, To = to })).ToList();
        return new GeminiUsageDto(
            from, to,
            rows.Sum(r => r.Calls),
            rows.Sum(r => r.InputTokens),
            rows.Sum(r => r.OutputTokens),
            rows.Sum(r => r.TotalTokens),
            rows);
    }
}
