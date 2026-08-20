using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性廠商資料匯入工具（讀匯入 JSON → Vendor）。
///
/// 觸發：local.settings.json 設 RUN_VENDOR_IMPORT=true（跑完切回 false）。
/// 資料檔：VENDOR_IMPORT_FILE 指定 Data/Seed 下的檔名（預設 vendor-import.json）。
///        目前有 vendor-import.json（31 筆，壯圍沙丘匯款資料）與
///        vendor-import-1150820.json（109 筆，廠商及個人資料建置表）。
/// 演練：VENDOR_IMPORT_DRY_RUN=true 只印計畫不寫 DB（預設 false）。
///
/// 去重/覆蓋：優先以識別碼命中（TaxId → IdNumber），皆為空才退回 Vendor.Name；
/// 命中既有廠商 → 覆蓋（update in place），否則新增。
/// 識別碼優先是必要的 —— VendorConfiguration 對 TaxId / IdNumber 皆有 filtered unique index，
/// 只用 Name 比對會在「同一廠商換名字」時撞索引。
/// 若識別碼與名稱各自命中「不同」的既有廠商，則跳過該筆並印錯誤，交由人工判讀。
/// 每筆包在 ExecutionStrategy + Transaction（DbContext 啟用 EnableRetryOnFailure）。
///
/// 此工具直寫 entity，刻意繞過 VendorHandler 的「統編／身分證字號二擇一必填」、
/// 格式驗證與「存摺封面必填」—— 來源未必齊全，但匯款資料本身有保存價值。
/// 匯入的廠商在後台按「編輯 → 儲存」時仍會被上述驗證擋下，須先補件。
/// </summary>
public static class VendorImporter
{
    private const string DefaultFileName = "vendor-import.json";

    public static async Task RunAsync(AppDbContext db, IConfiguration cfg)
    {
        bool dryRun = string.Equals(cfg["VENDOR_IMPORT_DRY_RUN"], "true", StringComparison.OrdinalIgnoreCase);

        var fileName = cfg["VENDOR_IMPORT_FILE"] is { Length: > 0 } configured ? configured : DefaultFileName;
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", fileName);
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"[VendorImporter] 找不到資料檔：{jsonPath}");
            return;
        }

        var records = JsonSerializer.Deserialize<List<VendorImportRecord>>(
            await File.ReadAllTextAsync(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        Console.WriteLine($"[VendorImporter] 開始匯入 {records.Count} 筆（檔案={fileName}、dryRun={dryRun}）");

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

            var taxId    = Normalize(r.TaxId);
            var idNumber = Normalize(r.IdNumber);

            if (dryRun)
            {
                Console.WriteLine($"[VendorImporter] [dry] {name} / 統編={taxId ?? "—"} / 身分證={idNumber ?? "—"} / "
                                + $"聯絡人={r.ContactPerson ?? "—"} / 電話={r.Phone ?? "—"} / "
                                + $"戶名={r.BankAccountName ?? "—"} / {r.BankName ?? "—"} / 代號={r.BankCode ?? "—"} / "
                                + $"帳號={r.BankAccount ?? "—"} / 地址={r.Address ?? "—"}");
                continue;
            }

            var strategy = db.Database.CreateExecutionStrategy();
            bool? wasUpdate = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                // ── 去重：識別碼優先（TaxId → IdNumber），皆空才用 Name ──
                Vendor? byIdentifier = taxId is not null
                    ? await db.Vendors.FirstOrDefaultAsync(v => v.TaxId == taxId)
                    : idNumber is not null
                        ? await db.Vendors.FirstOrDefaultAsync(v => v.IdNumber == idNumber)
                        : null;

                var byName = await db.Vendors.FirstOrDefaultAsync(v => v.Name == name);

                // 識別碼與名稱各自命中不同的既有廠商 → 無法判定要覆蓋哪一筆，交人工處理
                if (byIdentifier is not null && byName is not null && byIdentifier.Id != byName.Id)
                {
                    Console.Error.WriteLine(
                        $"[VendorImporter] 跳過（識別碼與名稱命中不同廠商）：{name} / 識別碼命中 Id={byIdentifier.Id}「{byIdentifier.Name}」、名稱命中 Id={byName.Id}");
                    return (bool?)null;
                }

                var existing = byIdentifier ?? byName;
                bool isUpdate = existing is not null;
                var vendor = existing ?? new Vendor { Name = name, CreatedAt = Clock.Now };

                vendor.Name            = name;
                vendor.TaxId           = taxId;
                vendor.IdNumber        = idNumber;
                vendor.ContactPerson   = Normalize(r.ContactPerson);
                vendor.Phone           = Normalize(r.Phone);
                vendor.BankAccountName = Normalize(r.BankAccountName);
                vendor.BankName        = Normalize(r.BankName);
                vendor.BankCode        = Normalize(r.BankCode);
                vendor.BankAccount     = Normalize(r.BankAccount);
                vendor.Address         = Normalize(r.Address);
                vendor.Note            = Normalize(r.Note);
                vendor.IsActive        = true;
                // 存摺封面 / 身分證影本：來源無檔案，維持 NULL（待補）

                if (!isUpdate) db.Vendors.Add(vendor);

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                return isUpdate;
            });

            if (wasUpdate is null)      { skipped++; continue; }
            if (wasUpdate.Value) updated++; else created++;
            Console.WriteLine($"[VendorImporter] {(wasUpdate.Value ? "覆蓋" : "新增")}：{name}");
        }

        Console.WriteLine($"[VendorImporter] 完成：新增 {created}、覆蓋 {updated}、跳過 {skipped}（共 {records.Count} 筆）");
    }

    /// <summary>空白字串一律轉 NULL（比照 VendorHandler 的欄位處理）。</summary>
    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
