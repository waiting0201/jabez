using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Jabez.Api.Services;

/// <summary>
/// 天然災害停班（颱風假）的寫入協調：手動建立 / 修改 / 取消，以及人事行政總處公告的自動同步。
/// 每次異動後都同步呼叫 <see cref="IDisasterLeaveRecalculator"/>（給薪者退回請假時數，冪等）。
///
/// 自動同步原則（<b>寧可漏套交人工，不可誤套</b>）：
///   · 新縣市公告 → 以 source=dgpa 建立並套用系統預設給薪，再通知 HR。
///   · HR 取消過的同日同縣市（列保留為 cancelled）→ 永不自動重建。
///   · 公告後來改為照常、或時段與現有有效紀錄不同 → <b>不自動撤銷 / 改動</b>，列為待人工項通知 HR。
///   · 只限部分鄉鎮、無法辨識的句型 → 不套用，列為待人工項通知 HR。
///   · 以頁面「更新時間」去重；待人工項與上一次相同者不重複通知。
/// </summary>
public interface IDisasterDayOffService
{
    Task<IReadOnlyList<DisasterDayOff>> CreateManualAsync(
        DateTime date, IReadOnlyCollection<string> counties, string segment, bool? isPaid, Guid? userId, string? userName);

    Task<DisasterDayOff> UpdateAsync(int id, string? segment, bool? isPaid);

    Task<DisasterDayOff> CancelAsync(int id, Guid? userId, string? userName);

    /// <summary>
    /// 抓取並套用公告。<paramref name="force"/> 為 true 時忽略「更新時間未變」的去重（手動觸發用）。
    /// 回傳本次寫入的紀錄；被去重略過時回 null。
    /// </summary>
    Task<DisasterDayOffFetchLog?> SyncAsync(string triggerSource, bool force, CancellationToken ct = default);
}

public sealed class DisasterDayOffService(
    AppDbContext db,
    IDgpaTyphoonService dgpa,
    IDisasterLeaveRecalculator recalculator,
    IWorkCountyResolver countyResolver,
    IApprovalNotificationService notifier,
    ILogger<DisasterDayOffService> logger) : IDisasterDayOffService
{
    // ── 手動建立 / 修改 / 取消 ─────────────────────────────────────────

    public async Task<IReadOnlyList<DisasterDayOff>> CreateManualAsync(
        DateTime date, IReadOnlyCollection<string> counties, string segment, bool? isPaid, Guid? userId, string? userName)
    {
        if (!DisasterSegments.IsValid(segment))
            throw AppException.BadRequest("停班時段必須是 full / am / pm。");
        if (counties.Count == 0)
            throw AppException.BadRequest("請至少選擇一個縣市。");
        RequestDateGuard.Ensure(date, "停班日期");

        var codes = counties.Select(c => TaiwanCounties.NormalizeCodeOrThrow(c, "縣市")
                                         ?? throw AppException.BadRequest("縣市不可為空。")).Distinct().ToList();
        var day = date.Date;

        var dup = await db.DisasterDayOffs.AsNoTracking()
            .Where(d => d.Date == day && d.Status == DisasterDayStatus.Active && codes.Contains(d.County))
            .Select(d => d.County).ToListAsync();
        if (dup.Count > 0)
            throw AppException.BadRequest(
                $"{string.Join("、", dup.Select(TaiwanCounties.NameOf))} 在 {day:yyyy-MM-dd} 已有有效的颱風假紀錄，請直接修改。");

        var paid = isPaid ?? await DefaultPaidAsync();
        var created = new List<DisasterDayOff>();
        foreach (var code in codes)
        {
            // 取消過的同日同縣市列 → 手動重新建立等同「恢復」：保留歷史列，另開新列
            var rec = new DisasterDayOff
            {
                Date = day, County = code, Segment = segment, IsPaid = paid,
                Source = DisasterDayStatus.SourceManual, Status = DisasterDayStatus.Active,
                CreatedAt = Clock.Now, CreatedById = userId, CreatedByName = userName,
            };
            db.DisasterDayOffs.Add(rec);
            created.Add(rec);
        }
        await db.SaveChangesAsync();

        foreach (var rec in created)
            await recalculator.RecalculateForAsync(rec.Date, rec.County);
        return created;
    }

    public async Task<DisasterDayOff> UpdateAsync(int id, string? segment, bool? isPaid)
    {
        var rec = await db.DisasterDayOffs.FirstOrDefaultAsync(d => d.Id == id)
                  ?? throw AppException.NotFound("DisasterDayOff");
        if (rec.Status != DisasterDayStatus.Active)
            throw AppException.BadRequest("已取消的颱風假不可修改。");
        if (segment is not null && !DisasterSegments.IsValid(segment))
            throw AppException.BadRequest("停班時段必須是 full / am / pm。");

        if (segment is not null) rec.Segment = segment;
        if (isPaid is not null)  rec.IsPaid  = isPaid.Value;
        await db.SaveChangesAsync();

        await recalculator.RecalculateForAsync(rec.Date, rec.County);
        return rec;
    }

    public async Task<DisasterDayOff> CancelAsync(int id, Guid? userId, string? userName)
    {
        var rec = await db.DisasterDayOffs.FirstOrDefaultAsync(d => d.Id == id)
                  ?? throw AppException.NotFound("DisasterDayOff");
        if (rec.Status == DisasterDayStatus.Cancelled) return rec;

        rec.Status          = DisasterDayStatus.Cancelled;
        rec.CancelledAt     = Clock.Now;
        rec.CancelledById   = userId;
        rec.CancelledByName = userName;
        await db.SaveChangesAsync();

        // 取消後該日不再有有效紀錄 → 重算會把先前退回的時數還原
        await recalculator.RecalculateForAsync(rec.Date, rec.County);
        return rec;
    }

    // ── 公告自動同步 ──────────────────────────────────────────────────

    private sealed record ManualItem(string County, string CountyName, string Text);

    public async Task<DisasterDayOffFetchLog?> SyncAsync(string triggerSource, bool force, CancellationToken ct = default)
    {
        var page = await dgpa.FetchAsync(ct);
        var previous = await db.DisasterDayOffFetchLogs.AsNoTracking()
            .OrderByDescending(l => l.Id).FirstOrDefaultAsync(ct);

        if (page is null)
        {
            // 連續失敗只留第一筆，避免每 10 分鐘灌一筆（手動觸發一律留）
            if (!force && previous is { Success: false }) return null;
            return await SaveLogAsync(null, false, triggerSource, "無法取得或解析人事行政總處公告頁面。", null);
        }

        if (!force && page.SourceUpdatedAt is { } upd
            && await db.DisasterDayOffFetchLogs.AnyAsync(l => l.Success && l.SourceUpdatedAt == upd, ct))
            return null;

        var today        = Clock.Now.Date;
        var defaultPaid  = await DefaultPaidAsync();
        var existing     = await db.DisasterDayOffs.Where(d => d.Date >= today).ToListAsync(ct);
        var created      = new List<DisasterDayOff>();
        var manual       = new List<ManualItem>();

        foreach (var c in page.Counties)
        foreach (var dir in c.Directives)
        {
            var date = page.PageDate.AddDays(dir.DayOffset).Date;
            if (date < today) continue;
            var label = $"{date:MM/dd}";
            var rows  = existing.Concat(created).Where(d => d.Date == date && d.County == c.Code).ToList();
            var active = rows.FirstOrDefault(r => r.Status == DisasterDayStatus.Active);

            switch (dir.Kind)
            {
                case DgpaDirectiveKind.Full or DgpaDirectiveKind.Am or DgpaDirectiveKind.Pm:
                {
                    var seg = dir.Kind switch { DgpaDirectiveKind.Am => DisasterSegments.Am, DgpaDirectiveKind.Pm => DisasterSegments.Pm, _ => DisasterSegments.Full };
                    if (active is not null)
                    {
                        if (active.Segment != seg && active.Source == DisasterDayStatus.SourceDgpa)
                            manual.Add(new ManualItem(c.Code, c.CountyName,
                                $"{label} 公告時段已變更為「{DisasterSegments.Label(seg)}」（系統內為「{DisasterSegments.Label(active.Segment)}」），系統不自動修改，請確認：{dir.Text}"));
                        break;
                    }
                    // HR 取消過的同日同縣市 → 永不自動重建
                    if (rows.Any(r => r.Status == DisasterDayStatus.Cancelled)) break;

                    var rec = new DisasterDayOff
                    {
                        Date = date, County = c.Code, Segment = seg, IsPaid = defaultPaid,
                        Source = DisasterDayStatus.SourceDgpa, SourceText = Truncate($"{c.RawText}", 1000),
                        Status = DisasterDayStatus.Active, CreatedAt = Clock.Now,
                    };
                    db.DisasterDayOffs.Add(rec);
                    created.Add(rec);
                    break;
                }
                case DgpaDirectiveKind.None:
                    // 公告後來改為照常：不自動撤銷，通知 HR 自行決定
                    if (active is { Source: DisasterDayStatus.SourceDgpa })
                        manual.Add(new ManualItem(c.Code, c.CountyName,
                            $"{label} 公告已改為照常上班（系統內仍有颱風假），系統不會自動取消，請 HR 確認：{dir.Text}"));
                    break;
                case DgpaDirectiveKind.Partial:
                    manual.Add(new ManualItem(c.Code, c.CountyName, $"{label} 只限部分地區停班，未自動套用，請人工判斷：{dir.Text}"));
                    break;
                default:
                    manual.Add(new ManualItem(c.Code, c.CountyName, $"{label} 無法辨識的公告，未自動套用，請人工判斷：{dir.Text}"));
                    break;
            }
        }

        if (created.Count > 0) await db.SaveChangesAsync(ct);

        // 請假重算 + 影響人數（逐筆）
        var affected = new Dictionary<int, int>();
        foreach (var rec in created)
        {
            affected[rec.Id] = (await countyResolver.UserIdsInCountyAsync(rec.County)).Count;
            try { await recalculator.RecalculateForAsync(rec.Date, rec.County); }
            catch (Exception ex) { logger.LogError(ex, "颱風假請假重算失敗：{Date} {County}", rec.Date, rec.County); }
        }

        // 待人工項去重：與上一次成功紀錄相同者不重複通知
        var prevOk = await db.DisasterDayOffFetchLogs.AsNoTracking()
            .Where(l => l.Success).OrderByDescending(l => l.Id).FirstOrDefaultAsync(ct);
        var prevKeys = ParseManual(prevOk?.ManualItemsJson).Select(m => (m.County, m.Text)).ToHashSet();
        var newManual = manual.Where(m => !prevKeys.Contains((m.County, m.Text))).ToList();

        var summary = $"建立 {created.Count} 筆、待人工 {manual.Count} 筆（新增 {newManual.Count} 筆）";
        var log = await SaveLogAsync(page.SourceUpdatedAt, true, triggerSource, summary,
            manual.Count == 0 ? null : JsonSerializer.Serialize(manual.Select(m => new { county = m.County, countyName = m.CountyName, text = m.Text })));

        if (created.Count > 0 || newManual.Count > 0)
        {
            try { await NotifyHrAsync(created, affected, newManual); }
            catch (Exception ex) { logger.LogError(ex, "颱風假 HR 通知失敗"); }
        }
        return log;
    }

    private async Task NotifyHrAsync(
        List<DisasterDayOff> created, Dictionary<int, int> affected, List<ManualItem> manual)
    {
        var recipients = await db.Users.AsNoTracking()
            .Where(u => u.Status == "active"
                     && u.UserRoles.Any(ur => ur.Role.RolePermissions.Any(rp => rp.Permission.Code == PermissionCodes.DisasterDaysWrite)))
            .Select(u => u.Id).ToListAsync();
        if (recipients.Count == 0)
        {
            logger.LogWarning("颱風假通知：查無持有 {Perm} 的在職人員", PermissionCodes.DisasterDaysWrite);
            return;
        }

        var rows = new List<(string, string)>();
        foreach (var g in created.GroupBy(r => (r.Date, r.Segment, r.IsPaid)).OrderBy(g => g.Key.Date))
        {
            var names = string.Join("、", g.Select(r => TaiwanCounties.NameOf(r.County)));
            var cnt   = g.Sum(r => affected.GetValueOrDefault(r.Id));
            rows.Add(($"{g.Key.Date:MM/dd} {DisasterSegments.Label(g.Key.Segment)}",
                      $"{names}（{(g.Key.IsPaid ? "給薪" : "不給薪")}，影響約 {cnt} 人）"));
        }
        if (created.Count > 0)
        {
            var missing = await countyResolver.CountWithoutCountyAsync();
            if (missing > 0) rows.Add(("未設定工作地", $"{missing} 位在職同仁未設定工作地縣市，颱風假不會套用到他們，請補設定部門或個人工作地"));
        }
        foreach (var m in manual)
            rows.Add(($"待人工：{m.CountyName}", m.Text));

        var title = created.Count > 0 ? "已依公告自動套用颱風假" : "颱風假公告需人工判斷";
        foreach (var uid in recipients)
            await notifier.NotifyDisasterDayOffAsync(uid, title, rows);
    }

    private async Task<DisasterDayOffFetchLog> SaveLogAsync(
        DateTime? sourceUpdatedAt, bool success, string trigger, string summary, string? manualJson)
    {
        var log = new DisasterDayOffFetchLog
        {
            FetchedAt = Clock.Now, SourceUpdatedAt = sourceUpdatedAt, Success = success,
            TriggerSource = trigger, Summary = Truncate(summary, 1000)!, ManualItemsJson = manualJson,
        };
        db.DisasterDayOffFetchLogs.Add(log);
        await db.SaveChangesAsync();
        return log;
    }

    private async Task<bool> DefaultPaidAsync() =>
        await db.SystemSettings.AsNoTracking().OrderBy(s => s.Id)
            .Select(s => (bool?)s.DisasterDayOffPaidDefault).FirstOrDefaultAsync() ?? true;

    private static List<ManualItem> ParseManual(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray().Select(e => new ManualItem(
                e.GetProperty("county").GetString() ?? "", e.GetProperty("countyName").GetString() ?? "",
                e.GetProperty("text").GetString() ?? "")).ToList();
        }
        catch (Exception) { return []; }
    }

    private static string? Truncate(string? s, int max) => s is null || s.Length <= max ? s : s[..max];
}
