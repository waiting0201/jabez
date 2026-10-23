using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jabez.Api.Handlers;

/// <summary>
/// 天然災害停班（颱風假）管理。業務規則與解析規則見 docs/business/disaster-day-off.md。
///
/// GET    /disaster-days?from=&amp;to=            disaster-days:read
/// POST   /disaster-days                        disaster-days:write（手動建立，可多縣市）
/// PATCH  /disaster-days/{id}                   disaster-days:write（改時段 / 給薪）
/// POST   /disaster-days/{id}/cancel            disaster-days:write
/// GET    /disaster-days/{id}/affected          disaster-days:read
/// GET    /disaster-days/fetch-logs?limit=      disaster-days:read
/// POST   /disaster-days/fetch                  Superadmin（手動觸發抓取，忽略更新時間去重）
/// GET    /me/disaster-day-off                  登入即可（輕量端點，打卡頁提示用）
///
/// 異動一律經 <see cref="IDisasterDayOffService"/>（內含請假時數重算），Handler 只負責 HTTP 與 DTO。
/// </summary>
public sealed class DisasterDayOffHandler(
    AppDbContext db,
    IDisasterDayOffService service,
    IDisasterPayrollService payrollService,
    IWorkCountyResolver countyResolver,
    IDisasterDayOffReadService disasterReader)
{
    // ── Read ─────────────────────────────────────────────────────────

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        var q = db.DisasterDayOffs.AsNoTracking().AsQueryable();
        if (DateTime.TryParse(req.Query["from"], out var from)) q = q.Where(d => d.Date >= from.Date);
        if (DateTime.TryParse(req.Query["to"],   out var to))   q = q.Where(d => d.Date <= to.Date);

        var rows = await q.OrderByDescending(d => d.Date).ThenBy(d => d.County).ToListAsync();
        var counts = new Dictionary<string, int>();
        foreach (var code in rows.Select(r => r.County).Distinct())
            counts[code] = (await countyResolver.UserIdsInCountyAsync(code)).Count;

        return new OkObjectResult(ApiResponse.Ok(rows.Select(r => ToDto(r, counts.GetValueOrDefault(r.County))).ToList()));
    }

    public async Task<IActionResult> GetAffectedAsync(HttpRequest req, string id)
    {
        var rec = await FindAsync(id);
        var impact = await payrollService.GetImpactAsync(rec.Id);

        var userIds = impact.Select(i => (Guid?)i.UserId).ToList();
        var day = rec.Date.Date;
        var dayEnd = day.AddDays(1);
        var leaves = new List<DisasterAdjustedLeaveDto>();
        foreach (var chunk in userIds.Chunk(1000))
        {
            var part = await db.LeaveRequests.AsNoTracking()
                .Where(l => chunk.Contains(l.EmployeeId)
                         && (l.ApprovalStatus == "pending" || l.ApprovalStatus == "approved")
                         && l.DisasterAdjustedHours > 0
                         && l.StartDate < dayEnd && l.EndDate >= day)
                .Select(l => new { l.Id, l.RequestNo, Name = l.Employee!.Name, l.LeaveType, l.Hours, Adj = l.DisasterAdjustedHours })
                .ToListAsync();
            leaves.AddRange(part.Select(l => new DisasterAdjustedLeaveDto(
                l.Id, l.RequestNo, l.Name, l.LeaveType, l.Hours + (l.Adj ?? 0m), l.Hours)));
        }

        var dto = new DisasterAffectedDto(
            impact.Select(i => new DisasterAffectedEmployeeDto(i.UserId, i.Name, i.DepartmentName, i.Worked, i.DeductionDays)).ToList(),
            leaves,
            await countyResolver.CountWithoutCountyAsync());
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    public async Task<IActionResult> GetFetchLogsAsync(HttpRequest req)
    {
        var limit = int.TryParse(req.Query["limit"], out var l) ? Math.Clamp(l, 1, 100) : 20;
        var logs = await db.DisasterDayOffFetchLogs.AsNoTracking()
            .OrderByDescending(x => x.Id).Take(limit).ToListAsync();
        return new OkObjectResult(ApiResponse.Ok(logs.Select(ToLogDto).ToList()));
    }

    /// <summary>GET /me/disaster-day-off：今天本人適用的颱風假；無則 data = null。</summary>
    public async Task<IActionResult> GetMineAsync(HttpRequest req)
    {
        var userId = GetUserId(req);
        var today  = Clock.Now.Date;
        var entry  = await disasterReader.GetForUserOnAsync(userId, today);
        MyDisasterDayOffDto? dto = entry is null ? null
            : new MyDisasterDayOffDto(today.ToString("yyyy-MM-dd"), entry.County,
                TaiwanCounties.NameOf(entry.County), entry.Segment, entry.IsPaid);
        return new OkObjectResult(ApiResponse.Ok(dto));
    }

    // ── Write ────────────────────────────────────────────────────────

    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var body = await req.ReadFromJsonAsync<CreateDisasterDayOffRequest>()
                   ?? throw AppException.BadRequest("請求 body 不可為空");
        if (body.Date == default) throw AppException.BadRequest("停班日期必填。");

        var (uid, name) = await GetActorAsync(req);
        var created = await service.CreateManualAsync(body.Date, body.Counties ?? [], body.Segment, body.IsPaid, uid, name);

        var counts = new Dictionary<string, int>();
        foreach (var c in created) counts[c.County] = (await countyResolver.UserIdsInCountyAsync(c.County)).Count;
        return new ObjectResult(ApiResponse.Ok(created.Select(r => ToDto(r, counts[r.County])).ToList(), "颱風假已建立。"))
        { StatusCode = 201 };
    }

    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        var rec  = await FindAsync(id);
        var body = await req.ReadFromJsonAsync<UpdateDisasterDayOffRequest>()
                   ?? throw AppException.BadRequest("請求 body 不可為空");
        var updated = await service.UpdateAsync(rec.Id, body.Segment, body.IsPaid);
        var n = (await countyResolver.UserIdsInCountyAsync(updated.County)).Count;
        return new OkObjectResult(ApiResponse.Ok(ToDto(updated, n), "颱風假已更新。"));
    }

    public async Task<IActionResult> CancelAsync(HttpRequest req, string id)
    {
        var rec = await FindAsync(id);
        var (uid, name) = await GetActorAsync(req);
        var cancelled = await service.CancelAsync(rec.Id, uid, name);
        var n = (await countyResolver.UserIdsInCountyAsync(cancelled.County)).Count;
        return new OkObjectResult(ApiResponse.Ok(ToDto(cancelled, n), "颱風假已取消。"));
    }

    /// <summary>POST /disaster-days/fetch（Superadmin）：手動抓取一次並套用，忽略更新時間去重。</summary>
    public async Task<IActionResult> FetchAsync(HttpRequest req)
    {
        var log = await service.SyncAsync("manual", force: true)
                  ?? throw AppException.Conflict("本次抓取未產生紀錄。");
        return new OkObjectResult(ApiResponse.Ok(ToLogDto(log), "已抓取人事行政總處公告。"));
    }

    // ── helpers ──────────────────────────────────────────────────────

    private async Task<DisasterDayOff> FindAsync(string id)
    {
        if (!int.TryParse(id, out var intId)) throw AppException.BadRequest("無效的 ID 格式");
        return await db.DisasterDayOffs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == intId)
               ?? throw AppException.NotFound("DisasterDayOff");
    }

    private static Guid GetUserId(HttpRequest req)
    {
        var sub = req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var g) ? g : throw AppException.Unauthorized("Invalid token claims.");
    }

    private async Task<(Guid? Id, string? Name)> GetActorAsync(HttpRequest req)
    {
        var id = GetUserId(req);
        var name = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Name).FirstOrDefaultAsync();
        return (id, name);
    }

    private static DisasterDayOffDto ToDto(DisasterDayOff r, int affected) => new(
        r.Id, r.Date.ToString("yyyy-MM-dd"), r.County, TaiwanCounties.NameOf(r.County), r.Segment, r.IsPaid,
        r.Source, r.SourceText, r.Status, r.CreatedAt, r.CreatedByName, r.CancelledAt, r.CancelledByName, affected);

    private static DisasterFetchLogDto ToLogDto(DisasterDayOffFetchLog l)
    {
        var items = new List<DisasterManualItemDto>();
        if (!string.IsNullOrWhiteSpace(l.ManualItemsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(l.ManualItemsJson);
                items = doc.RootElement.EnumerateArray().Select(e => new DisasterManualItemDto(
                    e.GetProperty("county").GetString() ?? "", e.GetProperty("countyName").GetString() ?? "",
                    e.GetProperty("text").GetString() ?? "")).ToList();
            }
            catch (JsonException) { /* 壞資料不擋列表 */ }
        }
        return new DisasterFetchLogDto(l.Id, l.FetchedAt, l.SourceUpdatedAt, l.Success, l.Summary, items);
    }
}
