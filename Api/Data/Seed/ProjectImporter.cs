using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性專案資料匯入工具（讀 project-import.json → Project + ProjectPaymentSchedule）。
/// 來源：reference/專案資料-115.07.29.xls。
///
/// 觸發：local.settings.json 設 RUN_PROJECT_IMPORT=true（跑完切回 false）。
/// 演練：PROJECT_IMPORT_DRY_RUN=true 只印計畫不寫 DB（預設 false）。
///
/// 去重/覆蓋：以 Project.Code 命中既有專案 → 覆蓋（update in place，期別明細全量重建）；否則新增。
/// 每筆包在 ExecutionStrategy + Transaction（DbContext 啟用 EnableRetryOnFailure）。
/// 此工具直寫 entity，刻意繞過 ProjectHandler 的「已結案不可修改」等限制，
/// 以便資料有誤時可修正 JSON 後重跑覆蓋。
/// </summary>
public static class ProjectImporter
{
    /// <summary>來源缺開始日期時的佔位日（民國 115.01.01）。Project.StartDate 為 NOT NULL，須有值。</summary>
    private static readonly DateTime PlaceholderStartDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public static async Task RunAsync(AppDbContext db, IConfiguration cfg)
    {
        bool dryRun = string.Equals(cfg["PROJECT_IMPORT_DRY_RUN"], "true", StringComparison.OrdinalIgnoreCase);

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "project-import.json");
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"[ProjectImporter] 找不到資料檔：{jsonPath}");
            return;
        }

        var records = JsonSerializer.Deserialize<List<ProjectImportRecord>>(
            await File.ReadAllTextAsync(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        Console.WriteLine($"[ProjectImporter] 開始匯入 {records.Count} 筆（dryRun={dryRun}）");

        // 部門文字 → ID 查表（讀 DB，不寫死）
        var deptByName = await db.Departments.AsNoTracking().ToDictionaryAsync(d => d.Name, d => d.Id);

        int created = 0, updated = 0, skipped = 0;

        foreach (var r in records)
        {
            // ── 必填驗證 ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(r.Code))
            {
                Console.Error.WriteLine($"[ProjectImporter] 跳過（缺專案編號）：{r.Name}");
                skipped++; continue;
            }
            if (string.IsNullOrWhiteSpace(r.Name))
            {
                Console.Error.WriteLine($"[ProjectImporter] 跳過（缺專案名稱）：{r.Code}");
                skipped++; continue;
            }
            if (ResolveDepartment(r.DepartmentText, deptByName, r.Code) is not { } deptId)
            {
                Console.Error.WriteLine($"[ProjectImporter] 跳過（部門對不到「{r.DepartmentText}」）：{r.Code}");
                skipped++; continue;
            }

            var startDate = RocDateParser.Parse(r.StartDate);
            if (startDate is null)
            {
                startDate = PlaceholderStartDate;
                Console.WriteLine($"[ProjectImporter] WARN 缺開始日期，套用佔位日 {PlaceholderStartDate:yyyy-MM-dd}：{r.Code} {r.Name}（請日後補正）");
            }

            var status = ResolveStatus(r.StatusText, r.Code);

            if (dryRun)
            {
                Console.WriteLine($"[ProjectImporter] [dry] {r.Code} {r.Name} / {r.DepartmentText}(#{deptId}) / {status} / "
                                + $"{startDate:yyyy-MM-dd}~{RocDateParser.Parse(r.EndDate):yyyy-MM-dd} / 契約={r.ContractAmount} / 期別×{r.PaymentSchedules.Count}");
                continue;
            }

            var strategy = db.Database.CreateExecutionStrategy();
            bool wasUpdate = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                // ── 去重：Code 命中 → 覆蓋 ──────────────────────────
                var existing = await db.Projects.FirstOrDefaultAsync(p => p.Code == r.Code);
                bool isUpdate = existing is not null;
                var project = existing ?? new Project { Code = r.Code, CreatedAt = Clock.Now };

                project.Name           = r.Name;
                project.Status         = status;
                project.StartDate      = startDate.Value;
                project.EndDate        = RocDateParser.Parse(r.EndDate);
                project.DepartmentId   = deptId;
                project.ContractAmount = r.ContractAmount;
                // BusinessAmount / RemainingAmount / GoogleDriveUrl：來源無有效值，維持 NULL（未設定）

                if (!isUpdate) db.Projects.Add(project);
                await db.SaveChangesAsync();

                // ── 期別明細：全量重建（比照 ProjectHandler.UpdateAsync）──
                var oldSchedules = await db.ProjectPaymentSchedules
                    .Where(s => s.ProjectId == project.Id)
                    .ToListAsync();
                if (oldSchedules.Count > 0) db.ProjectPaymentSchedules.RemoveRange(oldSchedules);

                foreach (var (s, idx) in r.PaymentSchedules.Select((s, i) => (s, i)))
                {
                    db.ProjectPaymentSchedules.Add(new ProjectPaymentSchedule
                    {
                        Id            = Guid.NewGuid(),
                        ProjectId     = project.Id,
                        PeriodNo      = ParsePeriodNo(s.PeriodText) ?? idx + 1,
                        BillingDate   = RocDateParser.Parse(s.BillingDate),
                        BillingAmount = s.BillingAmount,
                        InvoiceDate   = RocDateParser.Parse(s.InvoiceDate),
                        InvoiceAmount = s.InvoiceAmount,
                        DepositDate   = RocDateParser.Parse(s.DepositDate),
                        DepositAmount = s.DepositAmount,
                        DeductionNote = s.DeductionNote,
                    });
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return isUpdate;
            });

            if (wasUpdate) updated++; else created++;
            Console.WriteLine($"[ProjectImporter] {(wasUpdate ? "覆蓋" : "新增")}：{r.Code} {r.Name}");
        }

        Console.WriteLine($"[ProjectImporter] 完成：新增 {created}、覆蓋 {updated}、跳過 {skipped}（共 {records.Count} 筆）");
    }

    // ── 部門文字 → ID ───────────────────────────────────────────
    private static int? ResolveDepartment(string? text, Dictionary<string, int> byName, string who)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (byName.TryGetValue(text, out var id)) return id;
        Console.Error.WriteLine($"[ProjectImporter] WARN 部門對不到「{text}」：{who}");
        return null;
    }

    // ── 狀態文字 → active / closed ─────────────────────────────
    private static string ResolveStatus(string? text, string who) => text?.Trim() switch
    {
        "已結案" => "closed",
        "進行中" => "active",
        _        => LogAndDefault(text, who),
    };

    private static string LogAndDefault(string? text, string who)
    {
        Console.Error.WriteLine($"[ProjectImporter] WARN 狀態無法辨識「{text}」→ 預設 active：{who}");
        return "active";
    }

    // ── 期別文字（第一期 / 第二期 …）→ 數字 ──────────────────────
    private static readonly Dictionary<string, int> PeriodNumerals = new()
    {
        ["一"] = 1, ["二"] = 2, ["三"] = 3, ["四"] = 4, ["五"] = 5,
        ["六"] = 6, ["七"] = 7, ["八"] = 8, ["九"] = 9, ["十"] = 10,
    };

    private static int? ParsePeriodNo(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var s = text.Trim().Replace("第", "").Replace("期", "");
        if (int.TryParse(s, out var n)) return n;
        return PeriodNumerals.TryGetValue(s, out var v) ? v : null;
    }
}
