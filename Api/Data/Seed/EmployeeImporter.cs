using System.Text.Json;
using Jabez.Api.Common;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jabez.Api.Data.Seed;

/// <summary>
/// 一次性員工人事資料匯入工具（讀 employee-import.json → User + EmployeeProfile + 子表 + 附件）。
///
/// 觸發：local.settings.json 設 RUN_EMPLOYEE_IMPORT=true（跑完切回 false）。
/// 附件：IMPORT_UPLOAD_FILES=true 才實際上傳 blob；預設 false 僅定位/驗證/log。
/// 來源夾：EMPLOYEE_IMPORT_SOURCE_DIR（預設為 repo 下 reference/hr/人事資料表單）。
///
/// 去重/覆蓋：以 Email（不分大小寫）或 EmployeeProfile.EmployeeNumber 命中既有 User → 覆蓋（update in place）；
/// 否則新增。每筆包在 ExecutionStrategy + Transaction（DbContext 啟用 EnableRetryOnFailure）。
/// 此工具直寫 entity，刻意繞過 Handler 的必填部門 / 限圖 / 1MB 等限制。
/// </summary>
public static class EmployeeImporter
{
    private const string AvatarContainer          = "avatars";
    private const string SignatureContainer       = "signatures";
    private const string IdCardContainer          = "id-cards";
    private const string EducationProofContainer  = "education-proofs";
    private const string IndigenousProofContainer = "indigenous-proofs";
    private const string DisabledProofContainer   = "disabled-proofs";

    public static async Task RunAsync(AppDbContext db, IBlobStorageService blob, IConfiguration cfg)
    {
        bool uploadFiles = string.Equals(cfg["IMPORT_UPLOAD_FILES"], "true", StringComparison.OrdinalIgnoreCase);

        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "employee-import.json");
        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"[EmployeeImporter] 找不到資料檔：{jsonPath}");
            return;
        }

        var sourceDir = cfg["EMPLOYEE_IMPORT_SOURCE_DIR"]
            ?? "/Users/tim/webapps/Jabez/reference/hr/人事資料表單";

        var records = JsonSerializer.Deserialize<List<EmployeeImportRecord>>(
            await File.ReadAllTextAsync(jsonPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        Console.WriteLine($"[EmployeeImporter] 開始匯入 {records.Count} 筆（uploadFiles={uploadFiles}, source={sourceDir}）");

        // 部門 / 職稱 文字 → ID 查表（讀 DB，不寫死）
        var deptByName = await db.Departments.AsNoTracking().ToDictionaryAsync(d => d.Name, d => d.Id);
        var titleByName = await db.JobTitles.AsNoTracking().ToDictionaryAsync(t => t.Name, t => t.Id);

        int created = 0, updated = 0, skipped = 0;

        foreach (var r in records)
        {
            // ── 必填驗證 ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(r.Name))
            {
                Console.Error.WriteLine($"[EmployeeImporter] 跳過（缺姓名）：{r.FolderName}");
                skipped++; continue;
            }
            var birthday = RocDateParser.Parse(r.Birthday);
            if (birthday is null)
            {
                Console.Error.WriteLine($"[EmployeeImporter] 跳過（生日無法解析「{r.Birthday}」，無法產生預設密碼）：{r.Name}");
                skipped++; continue;
            }
            if (string.IsNullOrWhiteSpace(r.Email))
            {
                Console.Error.WriteLine($"[EmployeeImporter] 跳過（缺 Email）：{r.Name}");
                skipped++; continue;
            }
            if (r.EmailIsPlaceholder)
                Console.WriteLine($"[EmployeeImporter] WARN 使用 placeholder email：{r.Name} → {r.Email}（請日後補真實 email）");

            int? deptId  = ResolveDepartment(r.DepartmentText, deptByName, r.Name);
            int? titleId = ResolveJobTitle(r.JobTitleText, titleByName, r.Name);

            var strategy = db.Database.CreateExecutionStrategy();
            bool wasUpdate = await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                // ── 去重：Email 或 EmployeeNumber 命中 → 覆蓋 ────────────
                var emailLower = r.Email.ToLower();
                var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
                existing ??= await (
                    from p in db.EmployeeProfiles
                    join u in db.Users on p.UserId equals u.Id
                    where p.EmployeeNumber == r.EmployeeNumber
                    select u).FirstOrDefaultAsync();

                bool isUpdate = existing is not null;
                var user = existing ?? new User { Id = Guid.NewGuid(), CreatedAt = Clock.Now };

                user.Name              = r.Name;
                user.Email             = r.Email;
                user.PasswordHash      = BCrypt.Net.BCrypt.HashPassword(birthday.Value.ToString("yyyyMMdd"));
                user.MustChangePassword = true;
                user.Status            = "active";
                user.DepartmentId      = deptId;
                user.JobTitleId        = titleId;
                user.HireDate          = RocDateParser.Parse(r.HireDate);
                user.Birthday          = birthday;
                user.IsIndigenous      = r.IsIndigenous;
                user.IsLowIncome       = r.IsLowIncome;
                user.IsDisabled        = r.IsDisabled;
                user.UpdatedAt         = Clock.Now;

                if (!isUpdate) db.Users.Add(user);
                await db.SaveChangesAsync();

                var userId = user.Id;
                var now = Clock.Now;

                // ── EmployeeProfile（upsert by UserId）────────────────
                var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                bool profileIsNew = profile is null;
                profile ??= new EmployeeProfile { UserId = userId, CreatedAt = now };

                profile.EmployeeNumber        = r.EmployeeNumber;
                profile.EnglishName           = r.EnglishName;
                profile.IdNumber              = r.IdNumber;
                profile.Gender                = r.Gender;
                profile.MaritalStatus         = r.MaritalStatus;
                profile.MobilePhone           = r.MobilePhone;
                profile.ResidentialAddress    = r.ResidentialAddress;
                profile.ResidentialPhone      = r.ResidentialPhone;
                profile.MailingAddress        = r.MailingAddress;
                profile.MailingPhone          = r.MailingPhone;
                profile.EmergencyContactName  = r.EmergencyContactName;
                profile.EmergencyContactPhone = r.EmergencyContactPhone;
                profile.BankCode              = r.BankCode;
                profile.BankAccount           = r.BankAccount;
                profile.UpdatedAt             = now;

                if (profileIsNew) db.EmployeeProfiles.Add(profile);

                // ── 子表整批替換（先刪後加，比照 EmployeeProfileHandler）──
                await db.EducationRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
                await db.EmploymentHistoryRecords.Where(e => e.UserId == userId).ExecuteDeleteAsync();
                await db.FamilyMembers.Where(f => f.UserId == userId).ExecuteDeleteAsync();
                await db.LanguageAbilities.Where(l => l.UserId == userId).ExecuteDeleteAsync();
                await db.HealthInsuranceDependents.Where(h => h.UserId == userId).ExecuteDeleteAsync();

                AddEducation(db, r, userId, now);
                AddEmployment(db, r, userId, now);
                AddFamily(db, r, userId, now);
                AddLanguages(db, r, userId, now);
                AddDependents(db, r, userId, now);

                await db.SaveChangesAsync();

                // ── 附件 ──────────────────────────────────────────────
                await HandleAttachmentsAsync(db, blob, r, profile, user, sourceDir, uploadFiles);
                await db.SaveChangesAsync();

                await tx.CommitAsync();
                return isUpdate;
            });

            if (wasUpdate) { updated++; Console.WriteLine($"[EmployeeImporter] 覆蓋：{r.EmployeeNumber} {r.Name}"); }
            else           { created++; Console.WriteLine($"[EmployeeImporter] 新增：{r.EmployeeNumber} {r.Name}"); }
        }

        Console.WriteLine($"[EmployeeImporter] 完成：新增 {created}、覆蓋 {updated}、跳過 {skipped}");
    }

    // ── 子表建構 ───────────────────────────────────────────────
    private static void AddEducation(AppDbContext db, EmployeeImportRecord r, Guid userId, DateTime now)
    {
        foreach (var e in r.EducationRecords)
        {
            if (string.IsNullOrWhiteSpace(e.School))
            { Console.Error.WriteLine($"[EmployeeImporter] WARN 略過學歷列（缺學校）：{r.Name}"); continue; }
            db.EducationRecords.Add(new EducationRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                School = e.School, Department = e.Department,
                Degree = e.Degree is "graduated" or "incomplete" ? e.Degree : "graduated",
                StartDate = RocDateParser.Parse(e.StartDate), EndDate = RocDateParser.Parse(e.EndDate),
                Order = e.Order, CreatedAt = now, UpdatedAt = now
            });
        }
    }

    private static void AddEmployment(AppDbContext db, EmployeeImportRecord r, Guid userId, DateTime now)
    {
        foreach (var e in r.EmploymentHistoryRecords)
        {
            if (string.IsNullOrWhiteSpace(e.Organization) || string.IsNullOrWhiteSpace(e.JobTitle))
            { Console.Error.WriteLine($"[EmployeeImporter] WARN 略過經歷列（缺單位/職稱）：{r.Name}"); continue; }
            db.EmploymentHistoryRecords.Add(new EmploymentHistoryRecord
            {
                Id = Guid.NewGuid(), UserId = userId,
                Organization = e.Organization, JobTitle = e.JobTitle,
                StartDate = RocDateParser.Parse(e.StartDate), EndDate = RocDateParser.Parse(e.EndDate),
                Order = e.Order, CreatedAt = now, UpdatedAt = now
            });
        }
    }

    private static void AddFamily(AppDbContext db, EmployeeImportRecord r, Guid userId, DateTime now)
    {
        foreach (var f in r.FamilyMembers)
        {
            if (string.IsNullOrWhiteSpace(f.Name) || string.IsNullOrWhiteSpace(f.Relationship))
            { Console.Error.WriteLine($"[EmployeeImporter] WARN 略過家庭成員列（缺姓名/關係）：{r.Name}"); continue; }
            db.FamilyMembers.Add(new FamilyMember
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name = f.Name, Relationship = f.Relationship,
                Age = f.Age, Occupation = f.Occupation,
                CreatedAt = now, UpdatedAt = now
            });
        }
    }

    private static void AddLanguages(AppDbContext db, EmployeeImportRecord r, Guid userId, DateTime now)
    {
        static string Norm(string v) => v is "good" or "fair" ? v : "fair";
        foreach (var l in r.LanguageAbilities)
        {
            if (string.IsNullOrWhiteSpace(l.Language))
            { Console.Error.WriteLine($"[EmployeeImporter] WARN 略過語言列（缺語言名）：{r.Name}"); continue; }
            db.LanguageAbilities.Add(new LanguageAbility
            {
                Id = Guid.NewGuid(), UserId = userId,
                Language = l.Language,
                Listening = Norm(l.Listening), Speaking = Norm(l.Speaking),
                Reading = Norm(l.Reading), Writing = Norm(l.Writing),
                CreatedAt = now, UpdatedAt = now
            });
        }
    }

    private static void AddDependents(AppDbContext db, EmployeeImportRecord r, Guid userId, DateTime now)
    {
        foreach (var d in r.HealthInsuranceDependents)
        {
            if (string.IsNullOrWhiteSpace(d.Name) || string.IsNullOrWhiteSpace(d.Relationship))
            { Console.Error.WriteLine($"[EmployeeImporter] WARN 略過健保眷屬列（缺姓名/關係）：{r.Name}"); continue; }
            db.HealthInsuranceDependents.Add(new HealthInsuranceDependent
            {
                Id = Guid.NewGuid(), UserId = userId,
                Name = d.Name, Relationship = d.Relationship,
                IdNumber = d.IdNumber, BirthDate = RocDateParser.Parse(d.BirthDate),
                CreatedAt = now, UpdatedAt = now
            });
        }
    }

    // ── 部門 / 職稱 文字 → ID ───────────────────────────────────
    private static int? ResolveDepartment(string? text, Dictionary<string, int> byName, string who)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (byName.TryGetValue(text, out var id)) return id;
        Console.Error.WriteLine($"[EmployeeImporter] WARN 部門對不到「{text}」→ 留空：{who}");
        return null;
    }

    private static int? ResolveJobTitle(string? text, Dictionary<string, int> byName, string who)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (byName.TryGetValue(text, out var id)) return id;

        // 別名表：人事卡用語 → seed JobTitle.Name
        var alias = text switch
        {
            "規劃師" or "店員"           => "專案規劃師/店員",
            "店長" or "副理"             => "專案副理/店長",
            _ => null
        };
        if (alias is not null && byName.TryGetValue(alias, out var aid)) return aid;

        Console.Error.WriteLine($"[EmployeeImporter] WARN 職稱對不到「{text}」→ 留空：{who}");
        return null;
    }

    private static readonly string[] ImageExts = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    // ── 附件處理（IMPORT_UPLOAD_FILES=false 時僅定位/驗證/log）───
    // 先依類別分組，每類挑一個最佳來源檔（頭像/簽名優先取圖檔）；
    // 頭像若只有 PDF 則以 pdftoppm 轉首頁為 JPG 再上傳（avatars 容器只接受圖片、前端以 <img> 顯示）。
    // 其餘證件（身分證/畢業證書/原住民/身障證明）以文件方式呈現，PDF 原樣上傳即可。
    private static async Task HandleAttachmentsAsync(
        AppDbContext db, IBlobStorageService blob, EmployeeImportRecord r,
        EmployeeProfile profile, User user, string sourceDir, bool uploadFiles)
    {
        var dir = Path.Combine(sourceDir, r.FolderName);
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[EmployeeImporter] WARN 找不到附件夾：{dir}");
            return;
        }

        // 1. 分組
        var byCat = new Dictionary<Cat, List<string>>();
        foreach (var path in Directory.GetFiles(dir))
        {
            var fileName = Path.GetFileName(path);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (fileName is "Thumbs.db" or ".DS_Store") continue;
            if (ext is ".doc" or ".docx" or ".pages") continue;
            if (fileName.Contains("人事資料卡") || fileName.Contains("員工基本資料表") || fileName.Contains("基本資料表")) continue;

            var cat = Classify(fileName);
            if (cat is Cat.Passbook) { Console.WriteLine($"[EmployeeImporter] 略過存摺封面（無對應欄位）：{r.Name}/{fileName}"); continue; }
            if (cat is Cat.Unknown)  { Console.Error.WriteLine($"[EmployeeImporter] WARN 無法分類附件：{r.Name}/{fileName}"); continue; }
            (byCat.TryGetValue(cat, out var list) ? list : byCat[cat] = []).Add(path);
        }

        // 2. 每類挑最佳來源檔後上傳
        foreach (var (cat, files) in byCat)
        {
            var path = PickBest(cat, files);
            var fileName = Path.GetFileName(path);
            var srcExt = Path.GetExtension(path).ToLowerInvariant();
            bool convert = cat is Cat.Avatar && srcExt == ".pdf";   // 頭像 PDF → JPG
            var ext = convert ? ".jpg" : srcExt;

            var (container, blobName, set) = Plan(cat, user.Id, ext, profile, user);
            if (set is null) continue;

            if (!uploadFiles)
            {
                Console.WriteLine($"[EmployeeImporter] [dry] 將上傳 {fileName} → {container}/{blobName}{(convert ? " (PDF→JPG)" : "")}");
                continue;
            }

            var uploadPath = path;
            var contentType = ContentTypeOf(ext);
            if (convert)
            {
                var jpg = await ConvertPdfFirstPageToJpegAsync(path);
                if (jpg is null)
                {
                    Console.Error.WriteLine($"[EmployeeImporter] WARN 頭像 PDF 轉檔失敗，跳過：{r.Name}/{fileName}");
                    continue;
                }
                uploadPath = jpg;
                contentType = "image/jpeg";
            }

            try
            {
                await using (var stream = File.OpenRead(uploadPath))
                    await blob.UploadAsync(container, blobName, stream, contentType);
                set($"files/{container}/{blobName}");
                Console.WriteLine($"[EmployeeImporter] 已上傳 {fileName} → {container}/{blobName}{(convert ? " (PDF→JPG)" : "")}");
            }
            finally
            {
                if (convert && File.Exists(uploadPath)) { try { File.Delete(uploadPath); } catch { /* tmp */ } }
            }
        }
    }

    /// <summary>頭像/簽名優先取圖檔；其餘取第一個。</summary>
    private static string PickBest(Cat cat, List<string> files)
    {
        if (cat is Cat.Avatar or Cat.Signature)
        {
            var img = files.FirstOrDefault(f => ImageExts.Contains(Path.GetExtension(f).ToLowerInvariant()));
            if (img is not null) return img;
        }
        return files[0];
    }

    /// <summary>用 pdftoppm 將 PDF 首頁轉為 JPG，回傳暫存檔路徑；失敗回 null。</summary>
    private static async Task<string?> ConvertPdfFirstPageToJpegAsync(string pdfPath)
    {
        var outPrefix = Path.Combine(Path.GetTempPath(), $"emp-avatar-{Guid.NewGuid():N}");
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("pdftoppm",
                $"-jpeg -singlefile -r 150 \"{pdfPath}\" \"{outPrefix}\"")
            { RedirectStandardError = true, UseShellExecute = false };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return null;
            await p.WaitForExitAsync();
            var outFile = outPrefix + ".jpg";
            return p.ExitCode == 0 && File.Exists(outFile) ? outFile : null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EmployeeImporter] WARN pdftoppm 例外：{ex.Message}");
            return null;
        }
    }

    private enum Cat { Avatar, Signature, IdCard, Education, Indigenous, Disabled, Passbook, Unknown }

    private static Cat Classify(string fileName)
    {
        // 存摺 / 薪轉 / 銀行帳戶（無欄位）優先排除
        if (Has(fileName, "存摺", "薪資轉帳", "帳戶", "富邦", "銀行")) return Cat.Passbook;
        // 原住民身分證明含「身分證」子字串，須在身分證之前判斷；配偶具原住民屬眷屬，無欄位
        if (fileName.Contains("原住民"))
            return fileName.Contains("配偶") ? Cat.Passbook : Cat.Indigenous;
        if (Has(fileName, "身心障礙", "障礙")) return Cat.Disabled;
        if (Has(fileName, "身分證", "身份證")) return Cat.IdCard;
        if (Has(fileName, "畢業證書", "最高學歷", "學歷")) return Cat.Education;
        if (Has(fileName, "簽名")) return Cat.Signature;
        if (Has(fileName, "大頭照", "證件照", "照片", "大頭")) return Cat.Avatar;
        return Cat.Unknown;
    }

    private static (string container, string blobName, Action<string>? set) Plan(
        Cat cat, Guid userId, string ext, EmployeeProfile profile, User user) => cat switch
    {
        Cat.Avatar     => (AvatarContainer,          $"{userId}{ext}",           url => user.Avatar = url),
        Cat.Signature  => (SignatureContainer,       $"{userId}{ext}",           url => user.SignatureUrl = url),
        Cat.IdCard     => (IdCardContainer,          $"{userId}_front{ext}",     url => profile.IdCardFrontUrl = url),
        Cat.Education  => (EducationProofContainer,  $"{userId}_education{ext}", url => profile.HighestEducationProofUrl = url),
        Cat.Indigenous => (IndigenousProofContainer, $"{userId}{ext}",           url => user.IndigenousProofUrl = url),
        Cat.Disabled   => (DisabledProofContainer,   $"{userId}{ext}",           url => user.DisabledProofUrl = url),
        _              => ("", "", null)
    };

    private static bool Has(string s, params string[] keys) => keys.Any(s.Contains);

    private static string ContentTypeOf(string ext) => ext switch
    {
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"  => "image/gif",
        ".webp" => "image/webp",
        ".pdf"  => "application/pdf",
        _       => "application/octet-stream"
    };
}
