using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性廠商資料匯入工具（讀 vendor-import.json → Vendor）。
/// 來源：reference/壯圍沙丘廠商匯款資料0812.xlsx（31 筆）。
///
/// 觸發：local.settings.json 設 RUN_VENDOR_IMPORT=true（跑完切回 false）。
/// 演練：VENDOR_IMPORT_DRY_RUN=true 只印計畫不寫 DB（預設 false）。
///
/// 去重/覆蓋：以 Vendor.Name 命中既有廠商 → 覆蓋（update in place）；否則新增。
/// （Vendor 無 Code 欄位，且這批來源的統編／身分證字號全缺，故只能以名稱為鍵。）
/// 每筆包在 ExecutionStrategy + Transaction（DbContext 啟用 EnableRetryOnFailure）。
///
/// 此工具直寫 entity，刻意繞過 VendorHandler 的「統編／身分證字號二擇一必填」與
/// 「存摺封面必填」驗證 —— 來源沒有這兩項，但匯款資料本身有保存價值。
/// 匯入的廠商在後台按「編輯 → 儲存」時仍會被上述驗證擋下，須先補件；
/// 為此每筆的 Note 都寫入待補標記，方便清單上辨識。
/// </summary>
public static class VendorImporter
{
    public static async Task RunAsync(AppDbContext db, IConfiguration cfg)
    {
        bool dryRun = string.Equals(cfg["VENDOR_IMPORT_DRY_RUN"], "true", StringComparison.OrdinalIgnoreCase);

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "vendor-import.json");
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"[VendorImporter] 找不到資料檔：{jsonPath}");
            return;
        }

        var records = JsonSerializer.Deserialize<List<VendorImportRecord>>(
            await File.ReadAllTextAsync(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        Console.WriteLine($"[VendorImporter] 開始匯入 {records.Count} 筆（dryRun={dryRun}）");

        int created = 0, updated = 0, skipped = 0;

        foreach (var r in records)
        {
            // ── 必填驗證 ──────────────────────────────────────────
            var name = r.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.Error.WriteLine($"[VendorImporter] 跳過（缺廠商名稱）：戶名={r.BankAccountName}");
                skipped++; continue;
            }

            if (dryRun)
            {
                Console.WriteLine($"[VendorImporter] [dry] {name} / 聯絡人={r.ContactPerson ?? "—"} / 電話={r.Phone ?? "—"} / "
                                + $"戶名={r.BankAccountName ?? "—"} / {r.BankName ?? "—"} / 代號={r.BankCode ?? "—"} / 帳號={r.BankAccount ?? "—"}");
                continue;
            }

            var strategy = db.Database.CreateExecutionStrategy();
            bool wasUpdate = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                // ── 去重：Name 命中 → 覆蓋 ──────────────────────────
                var existing = await db.Vendors.FirstOrDefaultAsync(v => v.Name == name);
                bool isUpdate = existing is not null;
                var vendor = existing ?? new Vendor { Name = name, CreatedAt = Clock.Now };

                vendor.ContactPerson   = Normalize(r.ContactPerson);
                vendor.Phone           = Normalize(r.Phone);
                vendor.BankAccountName = Normalize(r.BankAccountName);
                vendor.BankName        = Normalize(r.BankName);
                vendor.BankCode        = Normalize(r.BankCode);
                vendor.BankAccount     = Normalize(r.BankAccount);
                vendor.Note            = Normalize(r.Note);
                vendor.IsActive        = true;
                // TaxId / IdNumber / Address / 存摺封面 / 身分證影本：來源無資料，維持 NULL（待補）

                if (!isUpdate) db.Vendors.Add(vendor);

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return isUpdate;
            });

            if (wasUpdate) updated++; else created++;
            Console.WriteLine($"[VendorImporter] {(wasUpdate ? "覆蓋" : "新增")}：{name}");
        }

        Console.WriteLine($"[VendorImporter] 完成：新增 {created}、覆蓋 {updated}、跳過 {skipped}（共 {records.Count} 筆）");
    }

    /// <summary>空白字串一律轉 NULL（比照 VendorHandler 的欄位處理）。</summary>
    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
