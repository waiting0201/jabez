using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Common;

/// <summary>
/// 下班打卡「專案工時佔比」驗證單一真相（打卡 clock-out 與當日修改 PUT 共用）。
///
/// 規則：至少 1 筆；專案不得重複；每筆 &gt; 0 且 ≤ 100；佔比最多 2 位小數（與 decimal(5,2) 欄位一致，
/// 否則存入時被靜默四捨五入，合計就不再是 100）；合計須精確等於 100（decimal 比對）；
/// 專案須存在且為 active（<see cref="EnsureProjectsActiveAsync"/>）。
/// 違規一律回 400 + 中文訊息。<see cref="Validate"/> 為純函式無 I/O，專案存在性另走 DB。
/// </summary>
public static class ProjectAllocationValidator
{
    /// <summary>佔比合計必須等於的值（%）。</summary>
    public const decimal TotalPercentage = 100m;

    /// <summary>
    /// 本功能上線日（2026-10-16）。系統補下班卡的日子只有落在此日（含）之後才要求本人補填佔比；
    /// 之前的舊資料不追溯，工時總表維持列「未分配」。
    /// </summary>
    public static readonly DateTime StartDate = new(2026, 10, 16);

    /// <summary>純函式檢查（不查 DB）。</summary>
    public static void Validate(IReadOnlyList<ProjectAllocationInput>? inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw AppException.BadRequest("請至少選擇一個專案並填寫工時佔比。");

        var seen  = new HashSet<int>();
        decimal sum = 0m;
        foreach (var a in inputs)
        {
            if (a.ProjectId <= 0)
                throw AppException.BadRequest("請選擇專案。");
            if (!seen.Add(a.ProjectId))
                throw AppException.BadRequest("同一個專案不可重複填寫。");
            if (a.Percentage <= 0m || a.Percentage > 100m)
                throw AppException.BadRequest("每個專案的工時佔比須大於 0 且不超過 100。");
            if (Math.Round(a.Percentage, 2) != a.Percentage)
                throw AppException.BadRequest("工時佔比最多只能填到小數點後 2 位。");
            sum += a.Percentage;
        }

        if (sum != TotalPercentage)
            throw AppException.BadRequest($"工時佔比合計須為 100%，目前為 {sum:0.##}%。");
    }

    /// <summary>專案須存在且為進行中（active）；已結案或不存在回 400。</summary>
    public static async Task EnsureProjectsActiveAsync(AppDbContext db, IEnumerable<ProjectAllocationInput> inputs)
    {
        var ids = inputs.Select(i => i.ProjectId).Distinct().ToList();
        var activeIds = await db.Projects
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.Status == "active")
            .Select(p => p.Id)
            .ToListAsync();

        if (activeIds.Count != ids.Count)
            throw AppException.BadRequest("所選專案不存在或已結案，請重新選擇。");
    }

    /// <summary>轉成 entity（SortOrder 依輸入順序）；呼叫端負責設定 AttendanceRecordId 或掛導覽屬性。</summary>
    public static List<AttendanceProjectAllocation> ToEntities(int attendanceRecordId, IReadOnlyList<ProjectAllocationInput> inputs) =>
        [.. inputs.Select((a, i) => new AttendanceProjectAllocation
        {
            AttendanceRecordId = attendanceRecordId,
            ProjectId          = a.ProjectId,
            Percentage         = a.Percentage,
            SortOrder          = i,
        })];
}
